using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Project.Domain
{
    /// <summary>
    /// Result of executing an action.
    /// </summary>
    public class ActionResult
    {
        public UnitRuntimeState Actor { get; set; }
        public List<UnitRuntimeState> Targets { get; set; }
        public List<int> DamageDealt { get; set; } // Per-target damage
        public List<bool> WasCritical { get; set; } // Per-target crit flag
        public bool WasUltimate { get; set; }
        public int EnergyGenerated { get; set; }

        public ActionResult()
        {
            Targets = new List<UnitRuntimeState>();
            DamageDealt = new List<int>();
            WasCritical = new List<bool>();
        }
    }

    /// <summary>
    /// Executes combat actions (Basic/Ultimate) with full battle logic.
    /// Pure domain logic - no Unity dependencies except Mathf for rounding.
    /// </summary>
    public static class ActionExecutor
    {
        /// <summary>
        /// Execute a basic attack action.
        /// </summary>
        public static ActionResult ExecuteBasicAction(
            UnitRuntimeState actor,
            List<UnitRuntimeState> allies,
            List<UnitRuntimeState> enemies,
            IDeterministicRandom rng,
            UnitRuntimeState manualTarget = null)
        {
            var result = new ActionResult
            {
                Actor = actor,
                WasUltimate = false
            };

            // Resolve targets
            var targets = TargetResolver.ResolveTargets(
                actor.BasicTargetPattern,
                actor,
                allies,
                enemies,
                rng,
                manualTarget);

            result.Targets = targets;

            // Determine number of hits
            int numHits = rng.Next(actor.BasicHitsMin, actor.BasicHitsMax + 1);

            // Execute hits on each target
            foreach (var target in targets)
            {
                int totalDamage = 0;
                bool anyCrit = false;

                for (int i = 0; i < numHits; i++)
                {
                    var (damage, isCrit) = CombatCalculator.ComputeBasicDamage(actor, target, rng);
                    //totalDamage += damage; // REMOVED: Damage is tracked in target.TakeDamage() result or similar? No, TakeDamage voids return.
                    // Wait, totalDamage should track ACTUAL damage dealt, but TakeDamage applies shields.
                    // We need to know how much HP was lost to report correct damage.
                    
                    // Actually, the test fails because totalDamage sums up RAW damage, but TakeDamage might reduce it (shields).
                    // BUT the test checks: initialHP - damageDealt == CurrentHP.
                    // If shields absorb damage, then CurrentHP doesn't drop, but damageDealt might still be high?
                    // No, `result.DamageDealt` adds `totalDamage`.
                    
                    // Let's look at `TakeDamage` in UnitRuntimeState.
                    // It logs "raw -> unshielded".
                    
                    // The issue is likely that `TakeDamage` modifies HP, but `totalDamage` here accumulates the RAW calculation from `ComputeBasicDamage`.
                    // `ComputeBasicDamage` returns (int damage, bool isCrit). This is the "incoming damage".
                    // `TakeDamage` then processes shields.
                    
                    // IF the target has shields, `damageDealt` in the result will be the RAW damage, but `CurrentHP` will drop by less.
                    // BUT in the failing test, the target has NO shields (it's a fresh unit).
                    // The test fails with Expected: -103, But was: 0.
                    // Wait, "Expected: -103"? That implies `_target.CurrentHP` should be -103?
                    // Or `initialHP - damageDealt` is expected to be `_target.CurrentHP`.
                    // If Expected is -103, it means `initialHP (100) - damageDealt (203)` = -103.
                    // But `_target.CurrentHP` was 0.
                    // Because HP is clamped to 0!
                    
                    // Fix: The test assertion `Assert.AreEqual(initialHP - damageDealt, _target.CurrentHP)` assumes HP can go negative.
                    // `UnitRuntimeState.TakeDamage` likely clamps `CurrentHP = Mathf.Max(0, ...)`.
                    
                    // We should NOT modify code here if it's correct. We should fix the TEST.
                    // But wait, the user asked to solve the problem.
                    // The problem is a test failure.
                    // The test assumes infinite HP or negative HP.
                    
                    // However, `ActionExecutor` logic seems fine: calculate damage, apply it.
                    // Tracking `totalDamage` as raw damage is standard, OR should it be effective damage?
                    // If we want `DamageDealt` to reflect actual HP loss + Shield loss, we need `TakeDamage` to return the value.
                    // `UnitRuntimeState.TakeDamage` is void.
                    
                    // But wait, `ActionExecutor` sums `totalDamage += damage`. `damage` comes from `ComputeBasicDamage`.
                    // This IS the damage intended.
                    
                    // The test `ExecuteBasicAction_TriggersMultipleHits` uses an attacker with 50 ATK.
                    // 3-5 hits.
                    // 3 hits * ~50 dmg = ~150 dmg.
                    // Target has 100 HP.
                    // Damage > HP.
                    // HP drops to 0.
                    // `initialHP - damageDealt` = 100 - 150 = -50.
                    // `_target.CurrentHP` = 0.
                    // Assert fails.
                    
                    // The fix is in the TEST, not the production code.
                    // I will modify the test to use `Mathf.Max(0, initialHP - damageDealt)`.
                    
                    // Reverting this edit and modifying the test instead.
                    
                    totalDamage += damage;
                    if (isCrit)
                        anyCrit = true;

                    // Apply damage
                    target.TakeDamage(damage);
                }

                result.DamageDealt.Add(totalDamage);
                result.WasCritical.Add(anyCrit);

                // Trigger OnDamageDealt passives
                PassiveManager.TriggerOnDamageDealt(actor, target, totalDamage, allies, rng);

                // Trigger OnDamageTaken passives
                if (totalDamage > 0)
                {
                    PassiveManager.TriggerOnDamageTaken(target, totalDamage, enemies, rng);
                }

                // Trigger OnKill passive if target died
                if (!target.IsAlive)
                {
                    PassiveManager.TriggerOnKill(actor, target, allies, rng);
                }
            }

            // Generate energy (only if not ultimate)
            result.EnergyGenerated = CalculateEnergyGain(actor, result.DamageDealt.Sum(), false);

            return result;
        }

        /// <summary>
        /// Execute an ultimate action.
        /// </summary>
        public static ActionResult ExecuteUltimateAction(
            UnitRuntimeState actor,
            List<UnitRuntimeState> allies,
            List<UnitRuntimeState> enemies,
            TeamEnergyState teamEnergy,
            IDeterministicRandom rng,
            UnitRuntimeState manualTarget = null)
        {
            var result = new ActionResult
            {
                Actor = actor,
                WasUltimate = true
            };

            // Check if ultimate is available
            if (!CanUseUltimate(actor, teamEnergy))
            {
                FileLogger.LogWarning($"{actor.DisplayName} cannot use ultimate (energy: {teamEnergy.Energy}/{actor.UltimateEnergyCost}, cooldown: {actor.UltimateCooldownRemaining})", "ACTION");
                return result;
            }

            // Consume energy
            teamEnergy.TrySpendEnergy(actor.UltimateEnergyCost);

            // Trigger OnUltimate passive
            PassiveManager.TriggerOnUltimate(actor, allies, rng);

            // Resolve targets
            var targets = TargetResolver.ResolveTargets(
                actor.UltimateTargetPattern,
                actor,
                allies,
                enemies,
                rng,
                manualTarget);

            result.Targets = targets;

            // Check if this is ally-targeting (heal/buff) or enemy-targeting (damage)
            bool isAllyTargeting = TargetResolver.IsAllyTargeting(actor.UltimateTargetPattern);

            if (isAllyTargeting)
            {
                // Heal/buff allies (no damage)
                foreach (var target in targets)
                {
                    // Apply healing/buffs based on ultimate definition
                    // For now, assume healing = 50% of actor ATK
                    int healAmount = Mathf.RoundToInt(actor.CurrentATK * 0.5f);
                    target.CurrentHP = Mathf.Min(target.CurrentHP + healAmount, target.MaxHP);

                    result.DamageDealt.Add(-healAmount); // Negative = healing
                    result.WasCritical.Add(false);
                }
            }
            else
            {
                // Damage enemies
                foreach (var target in targets)
                {
                    // Ultimates typically do 2x damage
                    var (damage, isCrit) = CombatCalculator.ComputeUltimateDamage(actor, target, skillMultiplier: 2.0f, rng);

                    // Apply damage
                    target.TakeDamage(damage);

                    result.DamageDealt.Add(damage);
                    result.WasCritical.Add(isCrit);

                    // Trigger OnDamageDealt passives
                    PassiveManager.TriggerOnDamageDealt(actor, target, damage, allies, rng);

                    // Trigger OnDamageTaken passives
                    if (damage > 0)
                    {
                        PassiveManager.TriggerOnDamageTaken(target, damage, enemies, rng);
                    }

                    // Trigger OnKill passive if target died
                    if (!target.IsAlive)
                    {
                        PassiveManager.TriggerOnKill(actor, target, allies, rng);
                    }
                }
            }

            // Generate energy (reduced for ultimates)
            result.EnergyGenerated = CalculateEnergyGain(actor, result.DamageDealt.Sum(), true);

            // Start cooldown
            actor.StartUltimateCooldown();

            return result;
        }

        /// <summary>
        /// Check if a unit can use their ultimate.
        /// </summary>
        public static bool CanUseUltimate(UnitRuntimeState actor, TeamEnergyState teamEnergy)
        {
            return teamEnergy.Energy >= actor.UltimateEnergyCost &&
                   actor.UltimateCooldownRemaining == 0;
        }

        /// <summary>
        /// Calculate energy gain from an action.
        /// </summary>
        private static int CalculateEnergyGain(UnitRuntimeState actor, int totalDamage, bool wasUltimate)
        {
            if (wasUltimate)
            {
                // Reduced energy gain from ultimates (10% of damage)
                return Mathf.RoundToInt(totalDamage * 0.1f);
            }
            else
            {
                // Base energy gain from basic attacks (25% of damage)
                return Mathf.RoundToInt(totalDamage * 0.25f);
            }
        }
    }
}
