using UnityEngine;

namespace Project.Domain
{
    /// <summary>
    /// Types of passive abilities that can be implemented
    /// </summary>
    public enum PassiveType
    {
        None,
        OnBattleStart,      // Trigger at battle start
        OnTurnStart,        // Trigger at unit's turn start
        OnDamageDealt,      // Trigger after dealing damage
        OnDamageTaken,      // Trigger after taking damage
        OnAllyLowHP,        // Trigger when ally HP < threshold
        OnKill,             // Trigger after killing an enemy
        OnUltimate,         // Trigger when using ultimate
        OnHPThreshold       // Trigger when own HP crosses threshold
    }

    /// <summary>
    /// Passive ability definition
    /// </summary>
    [System.Serializable]
    public class PassiveAbility
    {
        public PassiveType Type;
        public float TriggerThreshold;  // For HP thresholds (0.0-1.0)
        public int Value;                // Value for shields, damage bonus, etc.
        public float Modifier;           // Percentage modifiers
        public int Duration;             // Duration for applied effects
        
        // Status effect to apply
        public StatusEffectType EffectType;
        public bool TargetSelf;          // True = apply to self, False = apply to target
        public bool TargetAllies;        // True = apply to all allies
        public bool TargetRandomAlly;    // True = apply to 1 random ally (overrides TargetAllies)

        public PassiveAbility()
        {
            Type = PassiveType.None;
            TriggerThreshold = 0.5f;
            Value = 0;
            Modifier = 0.2f;
            Duration = 2;
            EffectType = StatusEffectType.None;
            TargetSelf = true;
            TargetAllies = false;
            TargetRandomAlly = false;
        }
    }
}
