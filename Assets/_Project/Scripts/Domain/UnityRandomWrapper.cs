using UnityEngine;

namespace Project.Domain
{
    /// <summary>
    /// Unity Random wrapper for production use.
    /// Delegates to UnityEngine.Random.
    /// </summary>
    public class UnityRandomWrapper : IDeterministicRandom
    {
        public int Next(int minInclusive, int maxExclusive)
        {
            return Random.Range(minInclusive, maxExclusive);
        }

        public float Range(float min, float max)
        {
            return Random.Range(min, max);
        }

        public float Value()
        {
            return Random.value;
        }
    }
}
