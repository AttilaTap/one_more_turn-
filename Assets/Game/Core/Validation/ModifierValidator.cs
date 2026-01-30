using System.Collections.Generic;

namespace OneMoreTurn.Core.Validation
{
    /// <summary>
    /// Validates modifier definitions for correctness.
    /// </summary>
    public static class ModifierValidator
    {
        public static ValidationResult Validate(ModifierDefinition definition)
        {
            var errors = new List<string>();

            if (definition == null)
            {
                errors.Add("Modifier definition is null");
                return new ValidationResult(false, errors);
            }

            // Required fields
            if (string.IsNullOrWhiteSpace(definition.Id))
                errors.Add("Modifier id is required");

            if (string.IsNullOrWhiteSpace(definition.Name))
                errors.Add($"[{definition.Id}] Modifier name is required");

            if (string.IsNullOrWhiteSpace(definition.Description))
                errors.Add($"[{definition.Id}] Modifier description is required");

            // Effects validation
            if (definition.Effects == null || definition.Effects.Count == 0)
            {
                errors.Add($"[{definition.Id}] Modifier must have at least one effect");
            }
            else
            {
                for (int i = 0; i < definition.Effects.Count; i++)
                {
                    var effect = definition.Effects[i];
                    ValidateEffect(definition.Id, i, effect, errors);
                }
            }

            // Duration validation
            if (definition.Duration == 0)
                errors.Add($"[{definition.Id}] Duration cannot be 0 (use -1 for permanent)");

            return new ValidationResult(errors.Count == 0, errors);
        }

        public static ValidationResult ValidateAll(IEnumerable<ModifierDefinition> definitions)
        {
            var allErrors = new List<string>();
            var ids = new HashSet<string>();

            foreach (var def in definitions)
            {
                // Check for duplicate IDs
                if (!string.IsNullOrEmpty(def?.Id))
                {
                    if (ids.Contains(def.Id))
                    {
                        allErrors.Add($"Duplicate modifier id: {def.Id}");
                    }
                    else
                    {
                        ids.Add(def.Id);
                    }
                }

                var result = Validate(def);
                if (!result.IsValid)
                {
                    allErrors.AddRange(result.Errors);
                }
            }

            return new ValidationResult(allErrors.Count == 0, allErrors);
        }

        private static void ValidateEffect(string modId, int index, ModifierEffect effect, List<string> errors)
        {
            string prefix = $"[{modId}] Effect {index}:";

            if (effect == null)
            {
                errors.Add($"{prefix} Effect is null");
                return;
            }

            // Validate hook-operation combinations
            switch (effect.Hook)
            {
                case ModifierHook.OnComputeGain:
                case ModifierHook.OnComputeRiskDelta:
                    // All operations are valid
                    break;

                case ModifierHook.OnBust:
                    if (effect.Operation != ModifierOperation.Set)
                        errors.Add($"{prefix} OnBust hook should use Set operation");
                    if (effect.Value < 0 || effect.Value >= 1)
                        errors.Add($"{prefix} OnBust value should be between 0 and 1");
                    break;

                case ModifierHook.OnBank:
                    // Set to 0 means no banking allowed, or modify tax rate
                    break;
            }

            // Validate condition if present
            if (effect.Condition != null)
            {
                ValidateCondition(modId, index, effect.Condition, errors);
            }
        }

        private static void ValidateCondition(string modId, int effectIndex, ModifierCondition condition, List<string> errors)
        {
            string prefix = $"[{modId}] Effect {effectIndex} condition:";

            switch (condition.Type)
            {
                case ConditionType.RiskAbove:
                case ConditionType.RiskBelow:
                    if (condition.Threshold < 0 || condition.Threshold > 1)
                        errors.Add($"{prefix} Risk threshold should be between 0 and 1");
                    break;

                case ConditionType.FlagSet:
                case ConditionType.FlagNotSet:
                    if (string.IsNullOrWhiteSpace(condition.Flag))
                        errors.Add($"{prefix} Flag name is required");
                    break;

                case ConditionType.CounterAbove:
                case ConditionType.CounterBelow:
                    if (string.IsNullOrWhiteSpace(condition.Counter))
                        errors.Add($"{prefix} Counter name is required");
                    break;

                case ConditionType.HasModifier:
                    if (string.IsNullOrWhiteSpace(condition.ModifierId))
                        errors.Add($"{prefix} ModifierId is required");
                    break;

                case ConditionType.TurnMultiple:
                    if (condition.TurnMultiple <= 0)
                        errors.Add($"{prefix} TurnMultiple must be positive");
                    break;
            }
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }

        public ValidationResult(bool isValid, List<string> errors)
        {
            IsValid = isValid;
            Errors = errors;
        }
    }
}
