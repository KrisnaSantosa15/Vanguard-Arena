using System;

namespace Project.Domain
{
    /// <summary>
    /// Types of status effects that can be applied to units
    /// </summary>
    public enum StatusEffectType
    {
        None,
        Shield,      // Absorbs damage
        Stun,        // Skip turn
        Burn,        // DoT per turn
        ATKUp,       // Increase ATK
        DEFDown,     // Reduce DEF
        DEFUp,       // Increase DEF
        ATKDown      // Reduce ATK
    }

    /// <summary>
    /// Represents an active status effect on a unit
    /// </summary>
    [Serializable]
    public sealed class StatusEffect
    {
        public StatusEffectType Type;
        public int Value;           // Shield amount, Burn damage, or stat modifier value
        public float Modifier;      // Percentage modifier for stat changes (e.g., 0.2 = +20%)
        public int DurationTurns;   // Remaining turns

        public StatusEffect(StatusEffectType type, int value, float modifier, int duration)
        {
            Type = type;
            Value = value;
            Modifier = modifier;
            DurationTurns = duration;
        }

        /// <summary>
        /// Tick down duration at turn start/end
        /// </summary>
        public void TickDuration()
        {
            if (DurationTurns > 0)
                DurationTurns--;
        }

        /// <summary>
        /// Check if effect has expired
        /// </summary>
        public bool IsExpired => DurationTurns <= 0;
    }
}
