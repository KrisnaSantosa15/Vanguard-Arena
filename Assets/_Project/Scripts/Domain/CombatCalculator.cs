using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Project.Domain
{
    /// <summary>
    /// Pure logic class responsible for damage and combat formula calculations.
    /// Stateless and easy to test. Can inject IDeterministicRandom for testing.
    /// </summary>
    public static class CombatCalculator
    {
        private static IDeterministicRandom _defaultRandom = new UnityRandomWrapper();

        public static (int damage, bool isCrit) ComputeBasicDamage(UnitRuntimeState actor, UnitRuntimeState target, IDeterministicRandom rng = null)
        {
            rng = rng ?? _defaultRandom;
            
            // Design doc formula: Base_DMG * Mitigation * Variance
            // Basic skill multiplier = 1.0
            float baseDmg = actor.CurrentATK * 1.0f;
            float mitigation = 1000f / (1000f + target.CurrentDEF);
            float variance = rng.Range(0.95f, 1.05f);

            int preCritDmg = Mathf.RoundToInt(baseDmg * mitigation * variance);

            // CRIT roll
            bool isCrit = rng.Range(0f, 100f) < actor.CritRate;
            float critMultiplier = isCrit ? (actor.CritDamage / 100f) : 1f;
            int finalDmg = Mathf.RoundToInt(preCritDmg * critMultiplier);

            return (Mathf.Max(1, finalDmg), isCrit);
        }

        public static (int damage, bool isCrit) ComputeUltimateDamage(UnitRuntimeState actor, UnitRuntimeState target, float skillMultiplier = 2.2f, IDeterministicRandom rng = null)
        {
            rng = rng ?? _defaultRandom;
            
            // Design doc formula: Base_DMG * Mitigation * Variance
            // Ultimate skill multiplier = 2.2 (default), but can be customized for per-hit calculations
            float baseDmg = actor.CurrentATK * skillMultiplier;
            float mitigation = 1000f / (1000f + target.CurrentDEF);
            float variance = rng.Range(0.95f, 1.05f);

            int preCritDmg = Mathf.RoundToInt(baseDmg * mitigation * variance);

            // CRIT roll (independent per call)
            bool isCrit = rng.Range(0f, 100f) < actor.CritRate;
            float critMultiplier = isCrit ? (actor.CritDamage / 100f) : 1f;
            int finalDmg = Mathf.RoundToInt(preCritDmg * critMultiplier);

            return (Mathf.Max(1, finalDmg), isCrit);
        }

        /// <summary>
        /// Computes heal amount for support ultimates (e.g. Lumina, Zephyr).
        /// Formula: Caster's ATK * skillMultiplier (default 1.5 for heals)
        /// </summary>
        public static int ComputeHealAmount(UnitRuntimeState caster, float skillMultiplier = 1.5f, IDeterministicRandom rng = null)
        {
            rng = rng ?? _defaultRandom;
            
            float baseHeal = caster.CurrentATK * skillMultiplier;
            float variance = rng.Range(0.95f, 1.05f);
            int finalHeal = Mathf.RoundToInt(baseHeal * variance);
            return Mathf.Max(1, finalHeal);
        }

        /// <summary>
        /// Computes shield amount for tank ultimates (e.g. Aegis).
        /// Formula: Caster's DEF * skillMultiplier (default 2.0 for shields)
        /// </summary>
        public static int ComputeShieldAmount(UnitRuntimeState caster, float skillMultiplier = 2.0f, IDeterministicRandom rng = null)
        {
            rng = rng ?? _defaultRandom;
            
            float baseShield = caster.CurrentDEF * skillMultiplier;
            float variance = rng.Range(0.95f, 1.05f);
            int finalShield = Mathf.RoundToInt(baseShield * variance);
            return Mathf.Max(1, finalShield);
        }
    }
}
