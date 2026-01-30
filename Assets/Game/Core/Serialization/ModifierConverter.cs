using System;
using System.Collections.Generic;

namespace OneMoreTurn.Core.Serialization
{
    /// <summary>
    /// Converts JSON DTOs to domain objects.
    /// </summary>
    public static class ModifierConverter
    {
        public static ModifierDefinition Convert(ModifierJson json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            var definition = new ModifierDefinition
            {
                Id = json.id,
                Name = json.name,
                Description = json.description,
                Rarity = ParseRarity(json.rarity),
                Tags = json.tags ?? new List<string>(),
                Priority = json.priority,
                IsStackable = json.isStackable,
                Duration = json.duration,
                Effects = new List<ModifierEffect>()
            };

            if (json.effects != null)
            {
                foreach (var effectJson in json.effects)
                {
                    definition.Effects.Add(ConvertEffect(effectJson));
                }
            }

            return definition;
        }

        public static List<ModifierDefinition> ConvertAll(ModifierFileJson fileJson)
        {
            var result = new List<ModifierDefinition>();
            if (fileJson?.modifiers == null) return result;

            foreach (var modJson in fileJson.modifiers)
            {
                result.Add(Convert(modJson));
            }
            return result;
        }

        private static ModifierEffect ConvertEffect(EffectJson json)
        {
            return new ModifierEffect
            {
                Hook = ParseHook(json.hook),
                Operation = ParseOperation(json.operation),
                Value = json.value,
                Priority = json.priority,
                Condition = json.condition != null ? ConvertCondition(json.condition) : null
            };
        }

        private static ModifierCondition ConvertCondition(ConditionJson json)
        {
            return new ModifierCondition
            {
                Type = ParseConditionType(json.type),
                Threshold = json.threshold,
                Flag = json.flag,
                Counter = json.counter,
                ModifierId = json.modifierId,
                TurnMultiple = json.turnMultiple
            };
        }

        private static ModifierRarity ParseRarity(string value)
        {
            if (string.IsNullOrEmpty(value)) return ModifierRarity.Common;

            return value.ToLowerInvariant() switch
            {
                "common" => ModifierRarity.Common,
                "uncommon" => ModifierRarity.Uncommon,
                "rare" => ModifierRarity.Rare,
                _ => throw new ArgumentException($"Unknown rarity: {value}")
            };
        }

        private static ModifierHook ParseHook(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Hook cannot be empty");

            return value switch
            {
                "OnPreTurn" => ModifierHook.OnPreTurn,
                "OnComputeGain" => ModifierHook.OnComputeGain,
                "OnComputeRiskDelta" => ModifierHook.OnComputeRiskDelta,
                "OnPostTurn" => ModifierHook.OnPostTurn,
                "OnBank" => ModifierHook.OnBank,
                "OnPush" => ModifierHook.OnPush,
                "OnSacrifice" => ModifierHook.OnSacrifice,
                "OnBust" => ModifierHook.OnBust,
                _ => throw new ArgumentException($"Unknown hook: {value}")
            };
        }

        private static ModifierOperation ParseOperation(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Operation cannot be empty");

            return value switch
            {
                "Add" => ModifierOperation.Add,
                "Multiply" => ModifierOperation.Multiply,
                "Set" => ModifierOperation.Set,
                "AddPercent" => ModifierOperation.AddPercent,
                _ => throw new ArgumentException($"Unknown operation: {value}")
            };
        }

        private static ConditionType ParseConditionType(string value)
        {
            if (string.IsNullOrEmpty(value)) return ConditionType.None;

            return value switch
            {
                "None" => ConditionType.None,
                "RiskAbove" => ConditionType.RiskAbove,
                "RiskBelow" => ConditionType.RiskBelow,
                "TurnAbove" => ConditionType.TurnAbove,
                "TurnBelow" => ConditionType.TurnBelow,
                "TurnMultiple" => ConditionType.TurnMultiple,
                "FlagSet" => ConditionType.FlagSet,
                "FlagNotSet" => ConditionType.FlagNotSet,
                "CounterAbove" => ConditionType.CounterAbove,
                "CounterBelow" => ConditionType.CounterBelow,
                "HasModifier" => ConditionType.HasModifier,
                "ScoreAbove" => ConditionType.ScoreAbove,
                "ScoreBelow" => ConditionType.ScoreBelow,
                _ => throw new ArgumentException($"Unknown condition type: {value}")
            };
        }
    }
}
