using System;
using System.Collections.Generic;

namespace OneMoreTurn.Core
{
    /// <summary>
    /// Static definition of a modifier, loaded from JSON.
    /// </summary>
    public class ModifierDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ModifierRarity Rarity { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<ModifierEffect> Effects { get; set; } = new List<ModifierEffect>();
        public int Priority { get; set; } = 100;
        public bool IsStackable { get; set; } = false;
        public int Duration { get; set; } = -1; // -1 = permanent

        /// <summary>
        /// Gets the sacrifice risk reduction based on rarity.
        /// </summary>
        public float GetSacrificeRiskReduction()
        {
            return Rarity switch
            {
                ModifierRarity.Common => 0.10f,
                ModifierRarity.Uncommon => 0.20f,
                ModifierRarity.Rare => 0.30f,
                _ => 0.10f
            };
        }

        /// <summary>
        /// Gets the sacrifice score gain based on rarity.
        /// </summary>
        public long GetSacrificeScoreGain()
        {
            return Rarity switch
            {
                ModifierRarity.Common => 50,
                ModifierRarity.Uncommon => 150,
                ModifierRarity.Rare => 400,
                _ => 50
            };
        }
    }

    public enum ModifierRarity
    {
        Common,
        Uncommon,
        Rare
    }

    public class ModifierEffect
    {
        public ModifierHook Hook { get; set; }
        public ModifierOperation Operation { get; set; }
        public float Value { get; set; }
        public ModifierCondition Condition { get; set; }
        public int Priority { get; set; } = 100;
    }

    public enum ModifierHook
    {
        OnPreTurn,
        OnComputeGain,
        OnComputeRiskDelta,
        OnPostTurn,
        OnBank,
        OnPush,
        OnSacrifice,
        OnBust
    }

    public enum ModifierOperation
    {
        Add,
        Multiply,
        Set,
        AddPercent
    }

    public class ModifierCondition
    {
        public ConditionType Type { get; set; }
        public float Threshold { get; set; }
        public string Flag { get; set; }
        public string Counter { get; set; }
        public string ModifierId { get; set; }
        public int TurnMultiple { get; set; }
    }

    public enum ConditionType
    {
        None,
        RiskAbove,
        RiskBelow,
        TurnAbove,
        TurnBelow,
        TurnMultiple,
        FlagSet,
        FlagNotSet,
        CounterAbove,
        CounterBelow,
        HasModifier,
        ScoreAbove,
        ScoreBelow
    }
}
