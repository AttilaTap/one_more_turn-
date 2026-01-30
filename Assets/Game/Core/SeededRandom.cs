using System;

namespace OneMoreTurn.Core
{
    /// <summary>
    /// Deterministic random number generator wrapper.
    /// All game randomness must flow through this to ensure reproducible runs.
    /// </summary>
    public class SeededRandom
    {
        private Random _random;
        private int _seed;
        private int _callCount;

        public int Seed => _seed;
        public int CallCount => _callCount;

        public SeededRandom(int seed)
        {
            _seed = seed;
            _random = new Random(seed);
            _callCount = 0;
        }

        /// <summary>
        /// Returns a random integer between 0 (inclusive) and maxValue (exclusive).
        /// </summary>
        public int Next(int maxValue)
        {
            _callCount++;
            return _random.Next(maxValue);
        }

        /// <summary>
        /// Returns a random integer between minValue (inclusive) and maxValue (exclusive).
        /// </summary>
        public int Next(int minValue, int maxValue)
        {
            _callCount++;
            return _random.Next(minValue, maxValue);
        }

        /// <summary>
        /// Returns a random float between 0.0 (inclusive) and 1.0 (exclusive).
        /// </summary>
        public float NextFloat()
        {
            _callCount++;
            return (float)_random.NextDouble();
        }

        /// <summary>
        /// Returns true with the given probability (0.0 to 1.0).
        /// </summary>
        public bool Chance(float probability)
        {
            return NextFloat() < probability;
        }

        /// <summary>
        /// Creates a snapshot for save/replay purposes.
        /// </summary>
        public SeededRandomSnapshot CreateSnapshot()
        {
            return new SeededRandomSnapshot
            {
                Seed = _seed,
                CallCount = _callCount
            };
        }

        /// <summary>
        /// Restores from a snapshot.
        /// </summary>
        public static SeededRandom FromSnapshot(SeededRandomSnapshot snapshot)
        {
            var rng = new SeededRandom(snapshot.Seed);
            // Fast-forward to the same state
            for (int i = 0; i < snapshot.CallCount; i++)
            {
                rng._random.Next();
            }
            rng._callCount = snapshot.CallCount;
            return rng;
        }
    }

    public struct SeededRandomSnapshot
    {
        public int Seed;
        public int CallCount;
    }
}
