namespace Project.Domain
{
    /// <summary>
    /// Interface for deterministic random number generation.
    /// Allows tests to inject seeded RNG for reproducible results.
    /// </summary>
    public interface IDeterministicRandom
    {
        /// <summary>
        /// Returns a random integer between minInclusive and maxExclusive.
        /// </summary>
        int Next(int minInclusive, int maxExclusive);

        /// <summary>
        /// Returns a random float between min (inclusive) and max (inclusive).
        /// </summary>
        float Range(float min, float max);

        /// <summary>
        /// Returns a random float between 0.0 (inclusive) and 1.0 (exclusive).
        /// </summary>
        float Value();
    }
}
