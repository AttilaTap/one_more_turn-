using System.Collections.Generic;

namespace OneMoreTurn.Core
{
    /// <summary>
    /// Result of resolving a turn, used for UI display and history.
    /// </summary>
    public class TurnResult
    {
        public int TurnNumber { get; set; }

        // Gain breakdown
        public long BaseGain { get; set; }
        public float PushMultiplier { get; set; } = 1.0f;
        public long FinalGain { get; set; }
        public List<EffectContribution> GainContributions { get; set; } = new List<EffectContribution>();

        // Risk breakdown
        public float BaseRiskDelta { get; set; }
        public float FinalRiskDelta { get; set; }
        public List<EffectContribution> RiskContributions { get; set; } = new List<EffectContribution>();

        // State after turn
        public float RiskAfter { get; set; }
        public long AtRiskScoreAfter { get; set; }
        public long BankedScoreAfter { get; set; }

        // Outcome
        public bool DidBust { get; set; }
        public bool BustWasPrevented { get; set; } // e.g., Safety Net triggered
    }

    /// <summary>
    /// Tracks how a single modifier/effect contributed to a value.
    /// </summary>
    public class EffectContribution
    {
        public string SourceName { get; set; }
        public string SourceId { get; set; }
        public ModifierOperation Operation { get; set; }
        public float Value { get; set; }
        public float ValueBefore { get; set; }
        public float ValueAfter { get; set; }

        public string GetDisplayString()
        {
            return Operation switch
            {
                ModifierOperation.Add => $"+{Value:0.##}",
                ModifierOperation.Multiply => $"x{Value:0.##}",
                ModifierOperation.Set => $"={Value:0.##}",
                ModifierOperation.AddPercent => $"+{Value * 100:0.##}%",
                _ => Value.ToString("0.##")
            };
        }
    }
}
