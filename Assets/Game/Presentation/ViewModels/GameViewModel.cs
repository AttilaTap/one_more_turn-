using System.Collections.Generic;
using OneMoreTurn.Core;

namespace OneMoreTurn.Presentation.ViewModels
{
    /// <summary>
    /// Immutable view model for the main game UI.
    /// </summary>
    public class GameViewModel
    {
        public int Turn { get; }
        public float Risk { get; }
        public long AtRiskScore { get; }
        public long BankedScore { get; }
        public long TotalScore => AtRiskScore + BankedScore;

        public bool CanBank { get; }
        public bool CanPush { get; }
        public int PushStacksUsed { get; }
        public int PushStacksMax { get; }

        public bool IsGameOver { get; }
        public GameOverReason GameOverReason { get; }
        public long FinalScore { get; }

        public TurnBreakdownViewModel LastTurn { get; }
        public IReadOnlyList<ModifierViewModel> Modifiers { get; }

        public GameViewModel(RunState state, TurnResult lastTurn, ModifierRegistry registry)
        {
            Turn = state.Turn;
            Risk = state.Risk;
            AtRiskScore = state.AtRiskScore;
            BankedScore = state.BankedScore;

            CanBank = !state.HasBankedThisTurn && state.AtRiskScore > 0 && !state.IsGameOver;
            CanPush = state.PushStacksThisTurn < RunState.MaxPushStacks &&
                      state.Risk + RunState.PushRiskCost < 1.0f &&
                      !state.IsGameOver;
            PushStacksUsed = state.PushStacksThisTurn;
            PushStacksMax = RunState.MaxPushStacks;

            IsGameOver = state.IsGameOver;
            GameOverReason = state.EndReason;
            FinalScore = state.TotalScore;

            LastTurn = lastTurn != null ? new TurnBreakdownViewModel(lastTurn) : null;

            var modList = new List<ModifierViewModel>();
            foreach (var mod in state.ActiveModifiers)
            {
                var def = registry?.Get(mod.ModifierId);
                if (def != null)
                {
                    modList.Add(new ModifierViewModel(mod, def));
                }
            }
            Modifiers = modList;
        }
    }

    public class TurnBreakdownViewModel
    {
        public long BaseGain { get; }
        public float PushMultiplier { get; }
        public long FinalGain { get; }
        public float BaseRiskDelta { get; }
        public float FinalRiskDelta { get; }
        public bool DidBust { get; }
        public bool BustWasPrevented { get; }

        public TurnBreakdownViewModel(TurnResult result)
        {
            BaseGain = result.BaseGain;
            PushMultiplier = result.PushMultiplier;
            FinalGain = result.FinalGain;
            BaseRiskDelta = result.BaseRiskDelta;
            FinalRiskDelta = result.FinalRiskDelta;
            DidBust = result.DidBust;
            BustWasPrevented = result.BustWasPrevented;
        }
    }

    public class ModifierViewModel
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public ModifierRarity Rarity { get; }
        public int StackCount { get; }
        public int TurnsRemaining { get; }
        public float SacrificeRiskReduction { get; }
        public long SacrificeScoreGain { get; }

        public ModifierViewModel(ModifierInstance instance, ModifierDefinition definition)
        {
            Id = definition.Id;
            Name = definition.Name;
            Description = definition.Description;
            Rarity = definition.Rarity;
            StackCount = instance.StackCount;
            TurnsRemaining = instance.TurnsRemaining;
            SacrificeRiskReduction = definition.GetSacrificeRiskReduction();
            SacrificeScoreGain = definition.GetSacrificeScoreGain();
        }

        public ModifierViewModel(ModifierDefinition definition)
        {
            Id = definition.Id;
            Name = definition.Name;
            Description = definition.Description;
            Rarity = definition.Rarity;
            StackCount = 1;
            TurnsRemaining = definition.Duration;
            SacrificeRiskReduction = definition.GetSacrificeRiskReduction();
            SacrificeScoreGain = definition.GetSacrificeScoreGain();
        }
    }
}
