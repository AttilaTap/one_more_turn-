using System.Collections.Generic;

namespace OneMoreTurn.Core
{
    /// <summary>
    /// A runtime instance of a modifier attached to a run.
    /// </summary>
    public class ModifierInstance
    {
        public string ModifierId { get; set; }
        public int StackCount { get; set; } = 1;
        public int TurnsRemaining { get; set; } = -1; // -1 = permanent
        public Dictionary<string, int> LocalCounters { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Creates a new instance from a definition.
        /// </summary>
        public static ModifierInstance FromDefinition(ModifierDefinition definition)
        {
            return new ModifierInstance
            {
                ModifierId = definition.Id,
                StackCount = 1,
                TurnsRemaining = definition.Duration,
                LocalCounters = new Dictionary<string, int>()
            };
        }

        /// <summary>
        /// Creates a deep copy of this instance.
        /// </summary>
        public ModifierInstance Clone()
        {
            return new ModifierInstance
            {
                ModifierId = ModifierId,
                StackCount = StackCount,
                TurnsRemaining = TurnsRemaining,
                LocalCounters = new Dictionary<string, int>(LocalCounters)
            };
        }
    }
}
