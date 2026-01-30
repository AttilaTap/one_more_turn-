using NUnit.Framework;
using OneMoreTurn.Core;
using System.Collections.Generic;

namespace OneMoreTurn.Tests
{
    [TestFixture]
    public class TurnResolverTests
    {
        private ModifierRegistry _registry;
        private TurnResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _registry = new ModifierRegistry();
            _resolver = new TurnResolver(_registry);
        }

        #region Basic Turn Resolution

        [Test]
        public void NewRun_InitializesCorrectly()
        {
            var state = RunState.NewRun(12345);

            Assert.AreEqual(12345, state.Seed);
            Assert.AreEqual(1, state.Turn);
            Assert.AreEqual(0, state.Risk);
            Assert.AreEqual(0, state.AtRiskScore);
            Assert.AreEqual(0, state.BankedScore);
            Assert.IsFalse(state.IsGameOver);
            Assert.IsTrue(state.HasFlag("first_turn"));
        }

        [Test]
        public void ResolveTurn_IncreasesScoreAndRisk()
        {
            var state = RunState.NewRun(12345);
            var (newState, result) = _resolver.ResolveTurn(state);

            Assert.Greater(newState.AtRiskScore, 0);
            Assert.Greater(newState.Risk, 0);
            Assert.AreEqual(2, newState.Turn);
            Assert.IsFalse(newState.HasFlag("first_turn"));
        }

        [Test]
        public void ResolveTurn_IsDeterministic()
        {
            var state1 = RunState.NewRun(99999);
            var state2 = RunState.NewRun(99999);

            var (newState1, result1) = _resolver.ResolveTurn(state1);
            var (newState2, result2) = _resolver.ResolveTurn(state2);

            Assert.AreEqual(newState1.AtRiskScore, newState2.AtRiskScore);
            Assert.AreEqual(newState1.Risk, newState2.Risk);
            Assert.AreEqual(result1.FinalGain, result2.FinalGain);
            Assert.AreEqual(result1.FinalRiskDelta, result2.FinalRiskDelta);
        }

        [Test]
        public void ResolveTurn_BaseGainFormula()
        {
            var state = RunState.NewRun(12345);
            var (_, result) = _resolver.ResolveTurn(state);

            // Turn 1: baseGain = 10 * (1 + 1 * 0.15) = 10 * 1.15 = 11.5 -> 12 (rounded)
            Assert.AreEqual(12, result.BaseGain);
        }

        [Test]
        public void ResolveTurn_BaseRiskDeltaFormula()
        {
            var state = RunState.NewRun(12345);
            var (_, result) = _resolver.ResolveTurn(state);

            // Turn 1: baseRiskDelta = 0.03 + 1 * 0.002 = 0.032
            Assert.AreEqual(0.032f, result.BaseRiskDelta, 0.0001f);
        }

        #endregion

        #region Golden Seed Test

        [Test]
        public void GoldenSeed_FullRunWithoutModifiers()
        {
            // Run a full game to bust without modifiers, verify determinism
            var state = RunState.NewRun(42);
            var turnResults = new List<TurnResult>();

            while (!state.IsGameOver)
            {
                var (newState, result) = _resolver.ResolveTurn(state);
                turnResults.Add(result);
                state = newState;
            }

            // Should bust around turn 19-20 without modifiers
            Assert.IsTrue(state.IsGameOver);
            Assert.AreEqual(GameOverReason.Bust, state.EndReason);
            Assert.GreaterOrEqual(turnResults.Count, 15);
            Assert.LessOrEqual(turnResults.Count, 25);

            // Verify final state is deterministic
            Assert.AreEqual(0, state.AtRiskScore); // Busted = 0
        }

        #endregion

        #region Bank Action

        [Test]
        public void Bank_TransfersScoreWithTax()
        {
            var state = RunState.NewRun(12345);
            state.AtRiskScore = 1000;

            var result = _resolver.Bank(state, 0.5f);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(500, result.NewState.AtRiskScore); // 1000 - 500
            Assert.AreEqual(400, result.NewState.BankedScore); // 500 * 0.8 (20% tax)
            Assert.AreEqual(400, result.AmountBanked);
            Assert.IsTrue(result.NewState.HasBankedThisTurn);
        }

        [Test]
        public void Bank_CannotBankTwicePerTurn()
        {
            var state = RunState.NewRun(12345);
            state.AtRiskScore = 1000;

            var result1 = _resolver.Bank(state, 0.25f);
            var result2 = _resolver.Bank(result1.NewState, 0.25f);

            Assert.IsTrue(result1.Success);
            Assert.IsFalse(result2.Success);
            Assert.AreEqual("Already banked this turn", result2.FailureReason);
        }

        [Test]
        public void Bank_FailsWithNoScore()
        {
            var state = RunState.NewRun(12345);
            state.AtRiskScore = 0;

            var result = _resolver.Bank(state, 0.25f);

            Assert.IsFalse(result.Success);
        }

        #endregion

        #region Push Action

        [Test]
        public void Push_AddsRiskAndTracksStacks()
        {
            var state = RunState.NewRun(12345);

            var result = _resolver.Push(state);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(0.15f, result.NewState.Risk, 0.001f);
            Assert.AreEqual(1, result.NewState.PushStacksThisTurn);
            Assert.IsTrue(result.NewState.HasFlag("pushed_this_turn"));
        }

        [Test]
        public void Push_CanStackTwice()
        {
            var state = RunState.NewRun(12345);

            var result1 = _resolver.Push(state);
            var result2 = _resolver.Push(result1.NewState);

            Assert.IsTrue(result1.Success);
            Assert.IsTrue(result2.Success);
            Assert.AreEqual(0.30f, result2.NewState.Risk, 0.001f);
            Assert.AreEqual(2, result2.NewState.PushStacksThisTurn);
        }

        [Test]
        public void Push_CannotExceedMaxStacks()
        {
            var state = RunState.NewRun(12345);

            var r1 = _resolver.Push(state);
            var r2 = _resolver.Push(r1.NewState);
            var r3 = _resolver.Push(r2.NewState);

            Assert.IsTrue(r1.Success);
            Assert.IsTrue(r2.Success);
            Assert.IsFalse(r3.Success);
        }

        [Test]
        public void Push_FailsIfWouldCauseBust()
        {
            var state = RunState.NewRun(12345);
            state.Risk = 0.90f;

            var result = _resolver.Push(state);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Push would cause bust", result.FailureReason);
        }

        [Test]
        public void Push_IncreasesGainMultiplier()
        {
            var state = RunState.NewRun(12345);
            var pushResult = _resolver.Push(state);
            var (newState, turnResult) = _resolver.ResolveTurn(pushResult.NewState);

            // With 1 push stack, multiplier should be 2.0
            Assert.AreEqual(2.0f, turnResult.PushMultiplier, 0.001f);
            Assert.Greater(turnResult.FinalGain, turnResult.BaseGain);
        }

        #endregion

        #region Sacrifice Action

        [Test]
        public void Sacrifice_RemovesModifierAndReducesRisk()
        {
            // Register a test modifier
            _registry.Register(new ModifierDefinition
            {
                Id = "test_mod",
                Name = "Test Modifier",
                Rarity = ModifierRarity.Common
            });

            var state = RunState.NewRun(12345);
            state.Risk = 0.50f;
            state.ActiveModifiers.Add(new ModifierInstance { ModifierId = "test_mod" });

            var result = _resolver.Sacrifice(state, "test_mod", SacrificeChoice.ReduceRisk);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(0.40f, result.NewState.Risk, 0.001f); // -10% for common
            Assert.AreEqual(0, result.NewState.ActiveModifiers.Count);
        }

        [Test]
        public void Sacrifice_RemovesModifierAndAddsScore()
        {
            _registry.Register(new ModifierDefinition
            {
                Id = "rare_mod",
                Name = "Rare Modifier",
                Rarity = ModifierRarity.Rare
            });

            var state = RunState.NewRun(12345);
            state.AtRiskScore = 100;
            state.ActiveModifiers.Add(new ModifierInstance { ModifierId = "rare_mod" });

            var result = _resolver.Sacrifice(state, "rare_mod", SacrificeChoice.GainScore);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(500, result.NewState.AtRiskScore); // 100 + 400 for rare
            Assert.AreEqual(0, result.NewState.ActiveModifiers.Count);
        }

        #endregion

        #region Cash Out

        [Test]
        public void CashOut_EndsGameWithScore()
        {
            var state = RunState.NewRun(12345);
            state.AtRiskScore = 500;
            state.BankedScore = 200;

            var newState = _resolver.CashOut(state);

            Assert.IsTrue(newState.IsGameOver);
            Assert.AreEqual(GameOverReason.CashOut, newState.EndReason);
            Assert.AreEqual(700, newState.TotalScore);
        }

        #endregion

        #region Bust Behavior

        [Test]
        public void Bust_LosesAtRiskScoreButKeepsBanked()
        {
            var state = RunState.NewRun(12345);
            state.AtRiskScore = 500;
            state.BankedScore = 200;
            state.Risk = 0.99f; // Will bust on next turn

            var (newState, result) = _resolver.ResolveTurn(state);

            Assert.IsTrue(newState.IsGameOver);
            Assert.AreEqual(GameOverReason.Bust, newState.EndReason);
            Assert.AreEqual(0, newState.AtRiskScore);
            Assert.AreEqual(200, newState.BankedScore);
            Assert.IsTrue(result.DidBust);
        }

        #endregion

        #region Modifiers

        [Test]
        public void Modifier_AppliesGainMultiplier()
        {
            _registry.Register(new ModifierDefinition
            {
                Id = "greedy_gambler",
                Name = "Greedy Gambler",
                Rarity = ModifierRarity.Common,
                Effects = new List<ModifierEffect>
                {
                    new ModifierEffect
                    {
                        Hook = ModifierHook.OnComputeGain,
                        Operation = ModifierOperation.Multiply,
                        Value = 1.5f
                    }
                }
            });

            var state = RunState.NewRun(12345);
            state.ActiveModifiers.Add(new ModifierInstance { ModifierId = "greedy_gambler" });

            var (_, result) = _resolver.ResolveTurn(state);

            // Base gain of 12 * 1.5 = 18
            Assert.AreEqual(18, result.FinalGain);
        }

        [Test]
        public void Modifier_ConditionalEffect()
        {
            _registry.Register(new ModifierDefinition
            {
                Id = "risk_taker",
                Name = "Risk Taker",
                Rarity = ModifierRarity.Uncommon,
                Effects = new List<ModifierEffect>
                {
                    new ModifierEffect
                    {
                        Hook = ModifierHook.OnComputeGain,
                        Operation = ModifierOperation.Multiply,
                        Value = 2.0f,
                        Condition = new ModifierCondition
                        {
                            Type = ConditionType.RiskAbove,
                            Threshold = 0.5f
                        }
                    }
                }
            });

            // Test below threshold
            var stateLow = RunState.NewRun(12345);
            stateLow.Risk = 0.3f;
            stateLow.ActiveModifiers.Add(new ModifierInstance { ModifierId = "risk_taker" });

            var (_, resultLow) = _resolver.ResolveTurn(stateLow);
            Assert.AreEqual(12, resultLow.FinalGain); // No multiplier, base gain = 12

            // Test above threshold
            var stateHigh = RunState.NewRun(12345);
            stateHigh.Risk = 0.6f;
            stateHigh.ActiveModifiers.Add(new ModifierInstance { ModifierId = "risk_taker" });

            var (_, resultHigh) = _resolver.ResolveTurn(stateHigh);
            Assert.AreEqual(24, resultHigh.FinalGain); // 2x multiplier: 12 * 2 = 24
        }

        [Test]
        public void Modifier_SafetyNetPreventsBust()
        {
            _registry.Register(new ModifierDefinition
            {
                Id = "safety_net",
                Name = "Safety Net",
                Rarity = ModifierRarity.Rare,
                Effects = new List<ModifierEffect>
                {
                    new ModifierEffect
                    {
                        Hook = ModifierHook.OnBust,
                        Operation = ModifierOperation.Set,
                        Value = 0.99f
                    }
                }
            });

            var state = RunState.NewRun(12345);
            state.Risk = 0.99f;
            state.AtRiskScore = 500;
            state.ActiveModifiers.Add(new ModifierInstance { ModifierId = "safety_net" });

            var (newState, result) = _resolver.ResolveTurn(state);

            Assert.IsFalse(newState.IsGameOver);
            Assert.IsFalse(result.DidBust);
            Assert.IsTrue(result.BustWasPrevented);
            Assert.AreEqual(0.99f, newState.Risk, 0.001f);
            Assert.AreEqual(0, newState.ActiveModifiers.Count); // Safety net consumed
        }

        #endregion
    }
}
