using System.Collections.Generic;

namespace OneMoreTurn.Core
{
    /// <summary>
    /// Complete state of a single run. Immutable-ish design:
    /// TurnResolver returns new state rather than mutating.
    /// </summary>
    public class RunState
    {
        // Core
        public int Seed { get; set; }
        public int Turn { get; set; }
        public float Risk { get; set; }
        public SeededRandom RNG { get; set; }

        // Score (split)
        public long AtRiskScore { get; set; }
        public long BankedScore { get; set; }
        public long TotalScore => AtRiskScore + BankedScore;

        // Action tracking (reset each turn)
        public bool HasBankedThisTurn { get; set; }
        public int PushStacksThisTurn { get; set; }
        public const int MaxPushStacks = 2;
        public const float PushRiskCost = 0.15f;
        public const float PushGainBonus = 1.0f; // +100% per stack
        public const float BankTaxRate = 0.20f;

        // Modifiers
        public List<ModifierInstance> ActiveModifiers { get; set; } = new List<ModifierInstance>();

        // Extensible state
        public Dictionary<string, int> Counters { get; set; } = new Dictionary<string, int>();
        public HashSet<string> Flags { get; set; } = new HashSet<string>();

        // Run status
        public bool IsGameOver { get; set; }
        public GameOverReason EndReason { get; set; }

        /// <summary>
        /// Creates a new run with the given seed and starting modifiers.
        /// </summary>
        public static RunState NewRun(int seed, List<ModifierInstance> startingModifiers = null)
        {
            return new RunState
            {
                Seed = seed,
                Turn = 1,
                Risk = 0f,
                RNG = new SeededRandom(seed),
                AtRiskScore = 0,
                BankedScore = 0,
                HasBankedThisTurn = false,
                PushStacksThisTurn = 0,
                ActiveModifiers = startingModifiers ?? new List<ModifierInstance>(),
                Counters = new Dictionary<string, int>(),
                Flags = new HashSet<string> { "first_turn" },
                IsGameOver = false,
                EndReason = GameOverReason.None
            };
        }

        /// <summary>
        /// Creates a deep copy of the run state.
        /// </summary>
        public RunState Clone()
        {
            var clone = new RunState
            {
                Seed = Seed,
                Turn = Turn,
                Risk = Risk,
                RNG = SeededRandom.FromSnapshot(RNG.CreateSnapshot()),
                AtRiskScore = AtRiskScore,
                BankedScore = BankedScore,
                HasBankedThisTurn = HasBankedThisTurn,
                PushStacksThisTurn = PushStacksThisTurn,
                ActiveModifiers = new List<ModifierInstance>(),
                Counters = new Dictionary<string, int>(Counters),
                Flags = new HashSet<string>(Flags),
                IsGameOver = IsGameOver,
                EndReason = EndReason
            };

            foreach (var mod in ActiveModifiers)
            {
                clone.ActiveModifiers.Add(mod.Clone());
            }

            return clone;
        }

        // Helper methods for counter/flag management
        public int GetCounter(string name) => Counters.TryGetValue(name, out var val) ? val : 0;
        public void SetCounter(string name, int value) => Counters[name] = value;
        public void IncrementCounter(string name, int delta = 1)
        {
            Counters[name] = GetCounter(name) + delta;
        }

        public bool HasFlag(string flag) => Flags.Contains(flag);
        public void SetFlag(string flag) => Flags.Add(flag);
        public void ClearFlag(string flag) => Flags.Remove(flag);

        public bool HasModifier(string modifierId)
        {
            foreach (var mod in ActiveModifiers)
            {
                if (mod.ModifierId == modifierId) return true;
            }
            return false;
        }
    }

    public enum GameOverReason
    {
        None,
        Bust,
        CashOut
    }
}
