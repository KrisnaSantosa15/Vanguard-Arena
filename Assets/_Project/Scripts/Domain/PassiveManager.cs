using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Project.Domain
{
    /// <summary>
    /// Manages passive ability triggers and execution.
    /// Pure domain logic - no Unity dependencies except Mathf for rounding.
    /// </summary>
    public static class PassiveManager
    {
        /// <summary>
        /// Trigger passives at battle start.
        /// </summary>
        public static void TriggerOnBattleStart(UnitRuntimeState owner, List<UnitRuntimeState> allies, IDeterministicRandom rng)
        {
            if (owner == null || owner.Passive == null || owner.Passive.Type != PassiveType.OnBattleStart)
                return;

            ApplyPassiveEffect(owner.Passive, owner, allies, rng);
        }

        /// <summary>
        /// Trigger passives at turn start.
        /// </summary>
        public static void TriggerOnTurnStart(UnitRuntimeState owner, List<UnitRuntimeState> allies, IDeterministicRandom rng)
        {
            if (owner == null || owner.Passive == null || owner.Passive.Type != PassiveType.OnTurnStart)
                return;

            ApplyPassiveEffect(owner.Passive, owner, allies, rng);
        }

        /// <summary>
        /// Trigger passives after dealing damage.
        /// </summary>
        public static void TriggerOnDamageDealt(UnitRuntimeState owner, UnitRuntimeState target, int damageDealt, List<UnitRuntimeState> allies, IDeterministicRandom rng)
        {
            if (owner == null || owner.Passive == null || owner.Passive.Type != PassiveType.OnDamageDealt)
                return;

            ApplyPassiveEffect(owner.Passive, owner, allies, rng, null, damageDealt);
        }

        /// <summary>
        /// Trigger passives after taking damage.
        /// </summary>
        public static void TriggerOnDamageTaken(UnitRuntimeState owner, int damageTaken, List<UnitRuntimeState> allies, IDeterministicRandom rng)
        {
            if (owner == null || owner.Passive == null || owner.Passive.Type != PassiveType.OnDamageTaken)
                return;

            ApplyPassiveEffect(owner.Passive, owner, allies, rng);
        }

        /// <summary>
        /// Trigger passives after killing an enemy.
        /// </summary>
        public static void TriggerOnKill(UnitRuntimeState owner, UnitRuntimeState killed, List<UnitRuntimeState> allies, IDeterministicRandom rng)
        {
            if (owner == null || owner.Passive == null || owner.Passive.Type != PassiveType.OnKill)
                return;

            ApplyPassiveEffect(owner.Passive, owner, allies, rng);
        }

        /// <summary>
        /// Trigger passives when using ultimate.
        /// </summary>
        public static void TriggerOnUltimate(UnitRuntimeState owner, List<UnitRuntimeState> allies, IDeterministicRandom rng)
        {
            if (owner == null || owner.Passive == null || owner.Passive.Type != PassiveType.OnUltimate)
                return;

            ApplyPassiveEffect(owner.Passive, owner, allies, rng);
        }

        /// <summary>
        /// Trigger passives when HP crosses threshold.
        /// </summary>
        public static void TriggerOnHPThreshold(UnitRuntimeState owner, float hpPercent, List<UnitRuntimeState> allies, IDeterministicRandom rng)
        {
            if (owner == null || owner.Passive == null || owner.Passive.Type != PassiveType.OnHPThreshold)
                return;

            // Check if HP is below threshold
            if (hpPercent >= owner.Passive.TriggerThreshold)
                return;

            ApplyPassiveEffect(owner.Passive, owner, allies, rng);
        }

        /// <summary>
        /// Trigger passives when ally HP is low.
        /// </summary>
        public static void TriggerOnAllyLowHP(UnitRuntimeState owner, UnitRuntimeState lowHPAlly, List<UnitRuntimeState> allies, IDeterministicRandom rng)
        {
            if (owner == null || owner.Passive == null || owner.Passive.Type != PassiveType.OnAllyLowHP)
                return;

            // Don't trigger on self
            if (lowHPAlly == owner)
                return;

            ApplyPassiveEffect(owner.Passive, owner, allies, rng, lowHPAlly);
        }

        /// <summary>
        /// Apply passive effect to targets based on targeting rules.
        /// </summary>
        private static void ApplyPassiveEffect(PassiveAbility passive, UnitRuntimeState owner, List<UnitRuntimeState> allies, IDeterministicRandom rng, UnitRuntimeState specificTarget = null, int damageDealt = 0)
        {
            // Determine targets
            List<UnitRuntimeState> targets = new List<UnitRuntimeState>();

            if (specificTarget != null)
            {
                // Specific target (e.g., OnAllyLowHP)
                targets.Add(specificTarget);
            }
            else if (passive.TargetSelf)
            {
                // Target self
                targets.Add(owner);
            }
            else if (passive.TargetRandomAlly)
            {
                // Target 1 random ally (excluding self)
                var validAllies = allies.Where(a => a != null && a.IsAlive && a != owner).ToList();
                if (validAllies.Count > 0)
                {
                    int randomIndex = rng.Next(0, validAllies.Count);
                    targets.Add(validAllies[randomIndex]);
                }
            }
            else if (passive.TargetAllies)
            {
                // Target all allies (excluding self)
                targets.AddRange(allies.Where(a => a != null && a.IsAlive && a != owner));
            }

            // Apply effect to each target
            foreach (var target in targets)
            {
                ApplyEffectToTarget(passive, target, damageDealt);
            }
        }

        /// <summary>
        /// Apply status effect or direct heal/damage to a single target.
        /// </summary>
        private static void ApplyEffectToTarget(PassiveAbility passive, UnitRuntimeState target, int damageDealt = 0)
        {
            if (target == null || !target.IsAlive)
                return;

            // Direct effects (heal/damage)
            if (passive.EffectType == StatusEffectType.None)
            {
                // Lifesteal (modifier-based healing from damage dealt)
                if (passive.Modifier > 0 && damageDealt > 0)
                {
                    int healAmount = Mathf.RoundToInt(damageDealt * passive.Modifier);
                    target.CurrentHP = Mathf.Min(target.CurrentHP + healAmount, target.MaxHP);
                }
                // Direct heal (value-based)
                else if (passive.Value > 0)
                {
                    int healAmount = passive.Value;
                    target.CurrentHP = Mathf.Min(target.CurrentHP + healAmount, target.MaxHP);
                }
                // Direct damage (if value is negative, could be thorns damage)
                else if (passive.Value < 0)
                {
                    int damage = Mathf.Abs(passive.Value);
                    target.CurrentHP = Mathf.Max(0, target.CurrentHP - damage);
                }
            }
            // Status effect (buff/debuff)
            else
            {
                target.ApplyStatusEffect(passive.EffectType, passive.Value, passive.Modifier, passive.Duration);
            }
        }
    }
}
