using System;
using System.Collections.Generic;

namespace OneMoreTurn.Core
{
    /// <summary>
    /// Main game logic. Resolves turns and player actions.
    /// All methods return new state (functional style) rather than mutating.
    /// </summary>
    public class TurnResolver
    {
        private readonly ModifierRegistry _registry;

        public TurnResolver(ModifierRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        #region Player Actions

        /// <summary>
        /// Bank a portion of at-risk score (25% or 50%).
        /// </summary>
        public ActionResult Bank(RunState state, float percentage)
        {
            if (state.IsGameOver)
                return ActionResult.Fail("Game is over");
            if (state.HasBankedThisTurn)
                return ActionResult.Fail("Already banked this turn");
            if (state.AtRiskScore <= 0)
                return ActionResult.Fail("No at-risk score to bank");
            if (percentage != 0.25f && percentage != 0.5f)
                return ActionResult.Fail("Must bank 25% or 50%");

            var newState = state.Clone();

            long amountToBank = (long)(state.AtRiskScore * percentage);
            float taxRate = RunState.BankTaxRate;

            // Apply OnBank hooks to modify tax rate
            foreach (var mod in newState.ActiveModifiers)
            {
                var def = _registry.Get(mod.ModifierId);
                if (def == null) continue;

                foreach (var effect in def.Effects)
                {
                    if (effect.Hook != ModifierHook.OnBank) continue;
                    if (!ConditionEvaluator.Evaluate(effect.Condition, newState)) continue;

                    // OnBank effects modify the tax rate
                    taxRate = ApplyOperation(taxRate, effect.Operation, effect.Value);
                }
            }

            taxRate = Math.Max(0, taxRate); // Tax can't go negative
            long taxedAmount = (long)(amountToBank * (1 - taxRate));

            newState.AtRiskScore -= amountToBank;
            newState.BankedScore += taxedAmount;
            newState.HasBankedThisTurn = true;
            newState.SetFlag("banked_this_turn");
            newState.IncrementCounter("total_banked", (int)taxedAmount);

            return new ActionResult
            {
                Success = true,
                NewState = newState,
                AmountBanked = taxedAmount
            };
        }

        /// <summary>
        /// Push: add risk now for bonus gain this turn.
        /// </summary>
        public ActionResult Push(RunState state)
        {
            if (state.IsGameOver)
                return ActionResult.Fail("Game is over");
            if (state.PushStacksThisTurn >= RunState.MaxPushStacks)
                return ActionResult.Fail($"Already at max push stacks ({RunState.MaxPushStacks})");

            var newState = state.Clone();

            float riskCost = RunState.PushRiskCost;

            // Apply OnPush hooks to modify risk cost
            foreach (var mod in newState.ActiveModifiers)
            {
                var def = _registry.Get(mod.ModifierId);
                if (def == null) continue;

                foreach (var effect in def.Effects)
                {
                    if (effect.Hook != ModifierHook.OnPush) continue;
                    if (!ConditionEvaluator.Evaluate(effect.Condition, newState)) continue;

                    riskCost = ApplyOperation(riskCost, effect.Operation, effect.Value);
                }
            }

            // Check if push would cause bust
            if (newState.Risk + riskCost >= 1.0f)
                return ActionResult.Fail("Push would cause bust");

            newState.Risk += riskCost;
            newState.PushStacksThisTurn++;
            newState.SetFlag("pushed_this_turn");
            newState.IncrementCounter("pushes_made");

            return new ActionResult
            {
                Success = true,
                NewState = newState,
                RiskAdded = riskCost
            };
        }

        /// <summary>
        /// Sacrifice a modifier for immediate benefit.
        /// </summary>
        public ActionResult Sacrifice(RunState state, string modifierId, SacrificeChoice choice)
        {
            if (state.IsGameOver)
                return ActionResult.Fail("Game is over");

            int modIndex = -1;
            for (int i = 0; i < state.ActiveModifiers.Count; i++)
            {
                if (state.ActiveModifiers[i].ModifierId == modifierId)
                {
                    modIndex = i;
                    break;
                }
            }

            if (modIndex < 0)
                return ActionResult.Fail($"Modifier '{modifierId}' not found");

            var def = _registry.Get(modifierId);
            if (def == null)
                return ActionResult.Fail($"Modifier definition '{modifierId}' not found");

            var newState = state.Clone();

            float riskReduction = def.GetSacrificeRiskReduction();
            long scoreGain = def.GetSacrificeScoreGain();

            // Apply OnSacrifice hooks to enhance effects
            foreach (var mod in newState.ActiveModifiers)
            {
                var modDef = _registry.Get(mod.ModifierId);
                if (modDef == null) continue;

                foreach (var effect in modDef.Effects)
                {
                    if (effect.Hook != ModifierHook.OnSacrifice) continue;
                    if (!ConditionEvaluator.Evaluate(effect.Condition, newState)) continue;

                    // OnSacrifice effects multiply the benefits
                    riskReduction = ApplyOperation(riskReduction, effect.Operation, effect.Value);
                    scoreGain = (long)ApplyOperation(scoreGain, effect.Operation, effect.Value);
                }
            }

            // Remove the modifier
            newState.ActiveModifiers.RemoveAt(modIndex);

            // Apply chosen benefit
            if (choice == SacrificeChoice.ReduceRisk)
            {
                newState.Risk = Math.Max(0, newState.Risk - riskReduction);
            }
            else
            {
                newState.AtRiskScore += scoreGain;
            }

            newState.SetFlag("sacrificed_this_turn");
            newState.IncrementCounter("sacrifices_made");

            return new ActionResult
            {
                Success = true,
                NewState = newState,
                SacrificedModifierId = modifierId
            };
        }

        /// <summary>
        /// Cash out: end the run with current score.
        /// </summary>
        public RunState CashOut(RunState state)
        {
            var newState = state.Clone();
            newState.IsGameOver = true;
            newState.EndReason = GameOverReason.CashOut;
            // Final score = AtRiskScore + BankedScore (both preserved)
            return newState;
        }

        #endregion

        #region Turn Resolution

        /// <summary>
        /// Resolve one turn (ONE MORE TURN action).
        /// </summary>
        public (RunState newState, TurnResult result) ResolveTurn(RunState state)
        {
            if (state.IsGameOver)
                throw new InvalidOperationException("Cannot resolve turn: game is over");

            var newState = state.Clone();
            var result = new TurnResult { TurnNumber = state.Turn };

            // Phase 1: Reset per-turn state (already done by Clone, but clear flags)
            newState.ClearFlag("banked_this_turn");
            newState.ClearFlag("pushed_this_turn");
            newState.ClearFlag("sacrificed_this_turn");

            // Phase 2: Pre-turn hooks
            newState = ApplyPreTurnHooks(newState);

            // Phase 3: Compute base values
            result.BaseGain = CalculateBaseGain(newState.Turn);
            result.BaseRiskDelta = CalculateBaseRiskDelta(newState.Turn);

            // Phase 4: Apply push bonus
            result.PushMultiplier = 1.0f + (newState.PushStacksThisTurn * RunState.PushGainBonus);
            float currentGain = result.BaseGain * result.PushMultiplier;

            result.GainContributions.Add(new EffectContribution
            {
                SourceName = "Base",
                Operation = ModifierOperation.Set,
                Value = result.BaseGain,
                ValueBefore = 0,
                ValueAfter = result.BaseGain
            });

            if (newState.PushStacksThisTurn > 0)
            {
                result.GainContributions.Add(new EffectContribution
                {
                    SourceName = $"Push x{newState.PushStacksThisTurn}",
                    Operation = ModifierOperation.Multiply,
                    Value = result.PushMultiplier,
                    ValueBefore = result.BaseGain,
                    ValueAfter = currentGain
                });
            }

            // Phase 5: Apply gain modifiers
            currentGain = ApplyGainModifiers(newState, currentGain, result.GainContributions);
            result.FinalGain = (long)Math.Max(0, currentGain);

            // Phase 6: Apply risk modifiers
            float currentRisk = result.BaseRiskDelta;
            result.RiskContributions.Add(new EffectContribution
            {
                SourceName = "Base",
                Operation = ModifierOperation.Set,
                Value = result.BaseRiskDelta,
                ValueBefore = 0,
                ValueAfter = result.BaseRiskDelta
            });

            currentRisk = ApplyRiskModifiers(newState, currentRisk, result.RiskContributions);
            result.FinalRiskDelta = currentRisk;

            // Phase 7: Update state
            newState.AtRiskScore += result.FinalGain;
            newState.Risk += result.FinalRiskDelta;
            newState.Risk = Math.Clamp(newState.Risk, 0f, 1f);

            result.AtRiskScoreAfter = newState.AtRiskScore;
            result.BankedScoreAfter = newState.BankedScore;
            result.RiskAfter = newState.Risk;

            // Phase 8: Bust check
            if (newState.Risk >= 1.0f)
            {
                bool bustPrevented = TryPreventBust(ref newState);
                result.BustWasPrevented = bustPrevented;

                if (!bustPrevented)
                {
                    result.DidBust = true;
                    newState.AtRiskScore = 0;
                    newState.IsGameOver = true;
                    newState.EndReason = GameOverReason.Bust;
                }
            }

            // Phase 9: Post-turn hooks
            if (!newState.IsGameOver)
            {
                newState = ApplyPostTurnHooks(newState);

                // Expire temporary modifiers
                ExpireModifiers(newState);

                // Phase 10: Increment turn
                newState.Turn++;
                newState.HasBankedThisTurn = false;
                newState.PushStacksThisTurn = 0;

                // Clear first_turn flag
                newState.ClearFlag("first_turn");
            }

            return (newState, result);
        }

        #endregion

        #region Private Helpers

        private long CalculateBaseGain(int turn)
        {
            // baseGain = 10 * (1 + turn * 0.15)
            return (long)(10 * (1 + turn * 0.15f));
        }

        private float CalculateBaseRiskDelta(int turn)
        {
            // baseRiskDelta = 0.03 + turn * 0.002
            return 0.03f + turn * 0.002f;
        }

        private float ApplyOperation(float current, ModifierOperation op, float value)
        {
            return op switch
            {
                ModifierOperation.Add => current + value,
                ModifierOperation.Multiply => current * value,
                ModifierOperation.Set => value,
                ModifierOperation.AddPercent => current + (current * value),
                _ => current
            };
        }

        private RunState ApplyPreTurnHooks(RunState state)
        {
            foreach (var mod in state.ActiveModifiers)
            {
                var def = _registry.Get(mod.ModifierId);
                if (def == null) continue;

                foreach (var effect in def.Effects)
                {
                    if (effect.Hook != ModifierHook.OnPreTurn) continue;
                    if (!ConditionEvaluator.Evaluate(effect.Condition, state)) continue;

                    // Pre-turn hooks can set flags, modify counters, etc.
                    // For MVP, we mainly use them for setup
                }
            }
            return state;
        }

        private float ApplyGainModifiers(RunState state, float currentGain, List<EffectContribution> contributions)
        {
            var effects = CollectEffects(state, ModifierHook.OnComputeGain);

            foreach (var (def, effect, instance) in effects)
            {
                if (!ConditionEvaluator.Evaluate(effect.Condition, state)) continue;

                float before = currentGain;
                currentGain = ApplyOperation(currentGain, effect.Operation, effect.Value * instance.StackCount);

                contributions.Add(new EffectContribution
                {
                    SourceName = def.Name,
                    SourceId = def.Id,
                    Operation = effect.Operation,
                    Value = effect.Value * instance.StackCount,
                    ValueBefore = before,
                    ValueAfter = currentGain
                });
            }

            return currentGain;
        }

        private float ApplyRiskModifiers(RunState state, float currentRisk, List<EffectContribution> contributions)
        {
            var effects = CollectEffects(state, ModifierHook.OnComputeRiskDelta);

            foreach (var (def, effect, instance) in effects)
            {
                if (!ConditionEvaluator.Evaluate(effect.Condition, state)) continue;

                float before = currentRisk;
                currentRisk = ApplyOperation(currentRisk, effect.Operation, effect.Value * instance.StackCount);

                contributions.Add(new EffectContribution
                {
                    SourceName = def.Name,
                    SourceId = def.Id,
                    Operation = effect.Operation,
                    Value = effect.Value * instance.StackCount,
                    ValueBefore = before,
                    ValueAfter = currentRisk
                });
            }

            return currentRisk;
        }

        private List<(ModifierDefinition def, ModifierEffect effect, ModifierInstance instance)> CollectEffects(
            RunState state, ModifierHook hook)
        {
            var list = new List<(ModifierDefinition, ModifierEffect, ModifierInstance)>();

            foreach (var instance in state.ActiveModifiers)
            {
                var def = _registry.Get(instance.ModifierId);
                if (def == null) continue;

                foreach (var effect in def.Effects)
                {
                    if (effect.Hook == hook)
                    {
                        list.Add((def, effect, instance));
                    }
                }
            }

            // Sort by priority (lower = earlier)
            list.Sort((a, b) => a.effect.Priority.CompareTo(b.effect.Priority));
            return list;
        }

        private bool TryPreventBust(ref RunState state)
        {
            foreach (var mod in state.ActiveModifiers)
            {
                var def = _registry.Get(mod.ModifierId);
                if (def == null) continue;

                foreach (var effect in def.Effects)
                {
                    if (effect.Hook != ModifierHook.OnBust) continue;
                    if (!ConditionEvaluator.Evaluate(effect.Condition, state)) continue;

                    // OnBust with Set operation sets risk to a specific value
                    if (effect.Operation == ModifierOperation.Set)
                    {
                        state.Risk = effect.Value;
                        // Remove the modifier (one-time use like Safety Net)
                        state.ActiveModifiers.Remove(mod);
                        return true;
                    }
                }
            }
            return false;
        }

        private RunState ApplyPostTurnHooks(RunState state)
        {
            foreach (var mod in state.ActiveModifiers)
            {
                var def = _registry.Get(mod.ModifierId);
                if (def == null) continue;

                foreach (var effect in def.Effects)
                {
                    if (effect.Hook != ModifierHook.OnPostTurn) continue;
                    if (!ConditionEvaluator.Evaluate(effect.Condition, state)) continue;

                    // Post-turn hooks for cleanup, counter updates
                }
            }
            return state;
        }

        private void ExpireModifiers(RunState state)
        {
            for (int i = state.ActiveModifiers.Count - 1; i >= 0; i--)
            {
                var mod = state.ActiveModifiers[i];
                if (mod.TurnsRemaining > 0)
                {
                    mod.TurnsRemaining--;
                    if (mod.TurnsRemaining == 0)
                    {
                        state.ActiveModifiers.RemoveAt(i);
                    }
                }
            }
        }

        #endregion
    }

    public enum SacrificeChoice
    {
        ReduceRisk,
        GainScore
    }
}
