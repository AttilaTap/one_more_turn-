using NUnit.Framework;
using OneMoreTurn.Core;
using OneMoreTurn.Core.Serialization;
using OneMoreTurn.Core.Validation;
using System.Collections.Generic;
using System.Linq;

namespace OneMoreTurn.Tests
{
    [TestFixture]
    public class ModifierLoaderTests
    {
        [Test]
        public void Converter_ParsesBasicModifier()
        {
            var json = new ModifierJson
            {
                id = "test_mod",
                name = "Test Modifier",
                description = "A test modifier",
                rarity = "Common",
                effects = new List<EffectJson>
                {
                    new EffectJson
                    {
                        hook = "OnComputeGain",
                        operation = "Multiply",
                        value = 1.5f
                    }
                }
            };

            var definition = ModifierConverter.Convert(json);

            Assert.AreEqual("test_mod", definition.Id);
            Assert.AreEqual("Test Modifier", definition.Name);
            Assert.AreEqual(ModifierRarity.Common, definition.Rarity);
            Assert.AreEqual(1, definition.Effects.Count);
            Assert.AreEqual(ModifierHook.OnComputeGain, definition.Effects[0].Hook);
            Assert.AreEqual(ModifierOperation.Multiply, definition.Effects[0].Operation);
            Assert.AreEqual(1.5f, definition.Effects[0].Value);
        }

        [Test]
        public void Converter_ParsesConditionalEffect()
        {
            var json = new ModifierJson
            {
                id = "conditional_mod",
                name = "Conditional",
                description = "Has condition",
                rarity = "Uncommon",
                effects = new List<EffectJson>
                {
                    new EffectJson
                    {
                        hook = "OnComputeGain",
                        operation = "Multiply",
                        value = 2.0f,
                        condition = new ConditionJson
                        {
                            type = "RiskAbove",
                            threshold = 0.5f
                        }
                    }
                }
            };

            var definition = ModifierConverter.Convert(json);

            Assert.IsNotNull(definition.Effects[0].Condition);
            Assert.AreEqual(ConditionType.RiskAbove, definition.Effects[0].Condition.Type);
            Assert.AreEqual(0.5f, definition.Effects[0].Condition.Threshold);
        }

        [Test]
        public void Converter_ParsesAllRarities()
        {
            Assert.AreEqual(ModifierRarity.Common,
                ModifierConverter.Convert(new ModifierJson { id = "a", name = "A", description = "A", rarity = "Common", effects = MinimalEffect() }).Rarity);
            Assert.AreEqual(ModifierRarity.Uncommon,
                ModifierConverter.Convert(new ModifierJson { id = "b", name = "B", description = "B", rarity = "Uncommon", effects = MinimalEffect() }).Rarity);
            Assert.AreEqual(ModifierRarity.Rare,
                ModifierConverter.Convert(new ModifierJson { id = "c", name = "C", description = "C", rarity = "Rare", effects = MinimalEffect() }).Rarity);
        }

        [Test]
        public void Converter_ParsesAllHooks()
        {
            var hooks = new[] { "OnPreTurn", "OnComputeGain", "OnComputeRiskDelta", "OnPostTurn", "OnBank", "OnPush", "OnSacrifice", "OnBust" };
            var expected = new[] { ModifierHook.OnPreTurn, ModifierHook.OnComputeGain, ModifierHook.OnComputeRiskDelta,
                                   ModifierHook.OnPostTurn, ModifierHook.OnBank, ModifierHook.OnPush,
                                   ModifierHook.OnSacrifice, ModifierHook.OnBust };

            for (int i = 0; i < hooks.Length; i++)
            {
                var json = new ModifierJson
                {
                    id = $"hook_test_{i}",
                    name = "Hook Test",
                    description = "Testing hook",
                    rarity = "Common",
                    effects = new List<EffectJson>
                    {
                        new EffectJson { hook = hooks[i], operation = "Add", value = 1 }
                    }
                };

                var def = ModifierConverter.Convert(json);
                Assert.AreEqual(expected[i], def.Effects[0].Hook, $"Failed for hook: {hooks[i]}");
            }
        }

        [Test]
        public void Converter_ParsesAllOperations()
        {
            var ops = new[] { "Add", "Multiply", "Set", "AddPercent" };
            var expected = new[] { ModifierOperation.Add, ModifierOperation.Multiply, ModifierOperation.Set, ModifierOperation.AddPercent };

            for (int i = 0; i < ops.Length; i++)
            {
                var json = new ModifierJson
                {
                    id = $"op_test_{i}",
                    name = "Op Test",
                    description = "Testing operation",
                    rarity = "Common",
                    effects = new List<EffectJson>
                    {
                        new EffectJson { hook = "OnComputeGain", operation = ops[i], value = 1 }
                    }
                };

                var def = ModifierConverter.Convert(json);
                Assert.AreEqual(expected[i], def.Effects[0].Operation, $"Failed for operation: {ops[i]}");
            }
        }

        [Test]
        public void Converter_ParsesDuration()
        {
            var json = new ModifierJson
            {
                id = "temp_mod",
                name = "Temporary",
                description = "Lasts 3 turns",
                rarity = "Common",
                duration = 3,
                effects = MinimalEffect()
            };

            var definition = ModifierConverter.Convert(json);
            Assert.AreEqual(3, definition.Duration);
        }

        [Test]
        public void Converter_DefaultDurationIsPermanent()
        {
            var json = new ModifierJson
            {
                id = "perm_mod",
                name = "Permanent",
                description = "Lasts forever",
                rarity = "Common",
                effects = MinimalEffect()
            };

            var definition = ModifierConverter.Convert(json);
            Assert.AreEqual(-1, definition.Duration);
        }

        #region Validation Tests

        [Test]
        public void Validator_RejectsNullDefinition()
        {
            var result = ModifierValidator.Validate(null);
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void Validator_RejectsMissingId()
        {
            var def = new ModifierDefinition
            {
                Name = "Test",
                Description = "Test",
                Effects = new List<ModifierEffect> { new ModifierEffect { Hook = ModifierHook.OnComputeGain, Operation = ModifierOperation.Add, Value = 1 } }
            };

            var result = ModifierValidator.Validate(def);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors[0].Contains("id"));
        }

        [Test]
        public void Validator_RejectsNoEffects()
        {
            var def = new ModifierDefinition
            {
                Id = "test",
                Name = "Test",
                Description = "Test",
                Effects = new List<ModifierEffect>()
            };

            var result = ModifierValidator.Validate(def);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors[0].Contains("effect"));
        }

        [Test]
        public void Validator_DetectsDuplicateIds()
        {
            var defs = new List<ModifierDefinition>
            {
                new ModifierDefinition { Id = "dupe", Name = "A", Description = "A", Effects = new List<ModifierEffect> { new ModifierEffect { Hook = ModifierHook.OnComputeGain, Operation = ModifierOperation.Add, Value = 1 } } },
                new ModifierDefinition { Id = "dupe", Name = "B", Description = "B", Effects = new List<ModifierEffect> { new ModifierEffect { Hook = ModifierHook.OnComputeGain, Operation = ModifierOperation.Add, Value = 1 } } }
            };

            var result = ModifierValidator.ValidateAll(defs);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Duplicate")));
        }

        [Test]
        public void Validator_AcceptsValidDefinition()
        {
            var def = new ModifierDefinition
            {
                Id = "valid",
                Name = "Valid Modifier",
                Description = "This is valid",
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
            };

            var result = ModifierValidator.Validate(def);
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        #endregion

        private List<EffectJson> MinimalEffect()
        {
            return new List<EffectJson>
            {
                new EffectJson { hook = "OnComputeGain", operation = "Add", value = 1 }
            };
        }
    }
}
