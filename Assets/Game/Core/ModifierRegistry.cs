using System.Collections.Generic;

namespace OneMoreTurn.Core
{
    /// <summary>
    /// Registry of all modifier definitions. Populated from JSON at startup.
    /// </summary>
    public class ModifierRegistry
    {
        private readonly Dictionary<string, ModifierDefinition> _definitions = new Dictionary<string, ModifierDefinition>();

        public void Register(ModifierDefinition definition)
        {
            _definitions[definition.Id] = definition;
        }

        public void RegisterAll(IEnumerable<ModifierDefinition> definitions)
        {
            foreach (var def in definitions)
            {
                Register(def);
            }
        }

        public ModifierDefinition Get(string id)
        {
            return _definitions.TryGetValue(id, out var def) ? def : null;
        }

        public bool TryGet(string id, out ModifierDefinition definition)
        {
            return _definitions.TryGetValue(id, out definition);
        }

        public IEnumerable<ModifierDefinition> GetAll()
        {
            return _definitions.Values;
        }

        public IEnumerable<ModifierDefinition> GetByRarity(ModifierRarity rarity)
        {
            foreach (var def in _definitions.Values)
            {
                if (def.Rarity == rarity) yield return def;
            }
        }

        public int Count => _definitions.Count;

        public void Clear()
        {
            _definitions.Clear();
        }
    }
}
