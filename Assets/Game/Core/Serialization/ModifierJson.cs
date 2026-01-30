using System.Collections.Generic;

namespace OneMoreTurn.Core.Serialization
{
    /// <summary>
    /// JSON DTO for modifier files. Maps directly to JSON structure.
    /// </summary>
    public class ModifierFileJson
    {
        public List<ModifierJson> modifiers { get; set; } = new List<ModifierJson>();
    }

    public class ModifierJson
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string rarity { get; set; }
        public List<string> tags { get; set; } = new List<string>();
        public List<EffectJson> effects { get; set; } = new List<EffectJson>();
        public int priority { get; set; } = 100;
        public bool isStackable { get; set; } = false;
        public int duration { get; set; } = -1;
    }

    public class EffectJson
    {
        public string hook { get; set; }
        public string operation { get; set; }
        public float value { get; set; }
        public int priority { get; set; } = 100;
        public ConditionJson condition { get; set; }
    }

    public class ConditionJson
    {
        public string type { get; set; }
        public float threshold { get; set; }
        public string flag { get; set; }
        public string counter { get; set; }
        public string modifierId { get; set; }
        public int turnMultiple { get; set; }
    }
}
