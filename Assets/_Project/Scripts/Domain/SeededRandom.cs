using System;

namespace Project.Domain
{
    /// <summary>
    /// Seeded random number generator for deterministic testing.
    /// Uses System.Random internally with a fixed seed.
    /// </summary>
    public class SeededRandom : IDeterministicRandom
    {
        private readonly Random _random;

        public SeededRandom(int seed)
        {
            _random = new Random(seed);
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            return _random.Next(minInclusive, maxExclusive);
        }

        public float Range(float min, float max)
        {
            // Convert System.Random's 0-1 to range
            float normalized = (float)_random.NextDouble();
            return min + (max - min) * normalized;
        }

        public float Value()
        {
            return (float)_random.NextDouble();
        }
    }
}
