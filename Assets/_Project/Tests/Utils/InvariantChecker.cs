using System;
using System.Collections.Generic;
using System.Linq;
using Project.Domain;

namespace VanguardArena.Tests.Utils
{
    /// <summary>
    /// Represents a violation of a battle invariant.
    /// </summary>
    public class InvariantViolation
    {
        public string Category { get; set; }
        public string InvariantName { get; set; }
        public string Description { get; set; }
        public string UnitId { get; set; }
        public string Details { get; set; }

        public override string ToString()
        {
            return $"[{Category}] {InvariantName}: {Description} | Unit: {UnitId} | {Details}";
        }
    }

    /// <summary>
    /// Validates battle state against defined invariants.
    /// Based on the 27 invariants defined in the testing strategy document.
    /// </summary>
    public class InvariantChecker
    {
        public List<InvariantViolation> CheckAll(IEnumerable<UnitRuntimeState> allUnits, TeamEnergyState playerEnergy, TeamEnergyState enemyEnergy)
        {
            var violations = new List<InvariantViolation>();
            
            violations.AddRange(CheckHealthInvariants(allUnits));
            violations.AddRange(CheckShieldInvariants(allUnits));
            violations.AddRange(CheckStatusEffectInvariants(allUnits));
            violations.AddRange(CheckEnergyInvariants(allUnits, playerEnergy, enemyEnergy));
            violations.AddRange(CheckCooldownInvariants(allUnits));
            violations.AddRange(CheckTargetingInvariants(allUnits));
            
            return violations;
        }

        #region Health Invariants (H1-H5)

        private IEnumerable<InvariantViolation> CheckHealthInvariants(IEnumerable<UnitRuntimeState> allUnits)
        {
            foreach (var unit in allUnits)
            {
                // H1: CurrentHP must be within [0, MaxHP]
                if (unit.CurrentHP < 0 || unit.CurrentHP > unit.MaxHP)
                {
                    yield return new InvariantViolation
                    {
                        Category = "Health",
                        InvariantName = "H1: HP Bounds",
                        Description = "CurrentHP must be between 0 and MaxHP",
                        UnitId = unit.DisplayName,
                        Details = $"CurrentHP={unit.CurrentHP}, MaxHP={unit.MaxHP}"
                    };
                }

                // H2: Dead units have CurrentHP = 0
                if (!unit.IsAlive && unit.CurrentHP != 0)
                {
                    yield return new InvariantViolation
                    {
                        Category = "Health",
                        InvariantName = "H2: Dead Units Zero HP",
                        Description = "Dead units must have CurrentHP = 0",
                        UnitId = unit.DisplayName,
                        Details = $"IsAlive={unit.IsAlive}, CurrentHP={unit.CurrentHP}"
                    };
                }

                // H3: Alive units have CurrentHP > 0
                if (unit.IsAlive && unit.CurrentHP <= 0)
                {
                    yield return new InvariantViolation
                    {
                        Category = "Health",
                        InvariantName = "H3: Alive Units Positive HP",
                        Description = "Alive units must have CurrentHP > 0",
                        UnitId = unit.DisplayName,
                        Details = $"IsAlive={unit.IsAlive}, CurrentHP={unit.CurrentHP}"
                    };
                }

                // H4: MaxHP must be positive
                if (unit.MaxHP <= 0)
                {
                    yield return new InvariantViolation
                    {
                        Category = "Health",
                        InvariantName = "H4: MaxHP Positive",
                        Description = "MaxHP must be > 0",
                        UnitId = unit.DisplayName,
                        Details = $"MaxHP={unit.MaxHP}"
                    };
                }
            }
        }

        #endregion

        #region Shield Invariants (S1-S6)

        private IEnumerable<InvariantViolation> CheckShieldInvariants(IEnumerable<UnitRuntimeState> allUnits)
        {
            foreach (var unit in allUnits)
            {
                var shields = unit.ActiveStatusEffects.Where(s => s.Type == StatusEffectType.Shield && !s.IsExpired).ToList();
                
                // S1: Shield amount must be non-negative
                if (unit.ShieldAmount < 0)
                {
                    yield return new InvariantViolation
                    {
                        Category = "Shield",
                        InvariantName = "S1: Shield Non-Negative",
                        Description = "Shield amount must be >= 0",
                        UnitId = unit.DisplayName,
                        Details = $"ShieldAmount={unit.ShieldAmount}"
                    };
                }

                // S2: Shield value must match sum of active shield effects
                int expectedShield = shields.Sum(s => s.Value);
                if (unit.ShieldAmount != expectedShield)
                {
                    yield return new InvariantViolation
                    {
                        Category = "Shield",
                        InvariantName = "S2: Shield Sum Consistency",
                        Description = "Total shield must equal sum of active shield effects",
                        UnitId = unit.DisplayName,
                        Details = $"Actual={unit.ShieldAmount}, Expected={expectedShield}, ActiveShields={shields.Count}"
                    };
                }

                // S3: Shield effects must have modifier = 0
                foreach (var shield in shields)
                {
                    if (shield.Modifier != 0f)
                    {
                        yield return new InvariantViolation
                        {
                            Category = "Shield",
                            InvariantName = "S3: Shield Modifier Zero",
                            Description = "Shield effects must have modifier = 0",
                            UnitId = unit.DisplayName,
                            Details = $"ShieldValue={shield.Value}, Modifier={shield.Modifier}"
                        };
                    }
                }

                // S4: Shield Value must be positive
                foreach (var shield in shields)
                {
                    if (shield.Value <= 0)
                    {
                        yield return new InvariantViolation
                        {
                            Category = "Shield",
                            InvariantName = "S4: Shield Value Positive",
                            Description = "Active shield effects must have Value > 0",
                            UnitId = unit.DisplayName,
                            Details = $"ShieldValue={shield.Value}"
                        };
                    }
                }
            }
        }

        #endregion

        #region Status Effect Invariants (SE1-SE7)

        private IEnumerable<InvariantViolation> CheckStatusEffectInvariants(IEnumerable<UnitRuntimeState> allUnits)
        {
            foreach (var unit in allUnits)
            {
                foreach (var effect in unit.ActiveStatusEffects)
                {
                    // SE1: Duration must be non-negative
                    if (effect.DurationTurns < 0)
                    {
                        yield return new InvariantViolation
                        {
                            Category = "StatusEffect",
                            InvariantName = "SE1: Duration Non-Negative",
                            Description = "Status effect duration must be >= 0",
                            UnitId = unit.DisplayName,
                            Details = $"Effect={effect.Type}, DurationTurns={effect.DurationTurns}"
                        };
                    }

                    // SE2: Expired effects have DurationTurns = 0
                    if (effect.IsExpired && effect.DurationTurns != 0)
                    {
                        yield return new InvariantViolation
                        {
                            Category = "StatusEffect",
                            InvariantName = "SE2: Expired Zero Duration",
                            Description = "Expired effects must have DurationTurns = 0",
                            UnitId = unit.DisplayName,
                            Details = $"Effect={effect.Type}, DurationTurns={effect.DurationTurns}, IsExpired={effect.IsExpired}"
                        };
                    }

                    // SE3: Active effects have DurationTurns > 0
                    if (!effect.IsExpired && effect.DurationTurns <= 0)
                    {
                        yield return new InvariantViolation
                        {
                            Category = "StatusEffect",
                            InvariantName = "SE3: Active Positive Duration",
                            Description = "Active effects must have DurationTurns > 0",
                            UnitId = unit.DisplayName,
                            Details = $"Effect={effect.Type}, DurationTurns={effect.DurationTurns}, IsExpired={effect.IsExpired}"
                        };
                    }

                    // SE4: ATKUp/ATKDown modifier in range [0, 1]
                    if (effect.Type == StatusEffectType.ATKUp || effect.Type == StatusEffectType.ATKDown)
                    {
                        if (effect.Modifier < 0f || effect.Modifier > 1f)
                        {
                            yield return new InvariantViolation
                            {
                                Category = "StatusEffect",
                                InvariantName = "SE4: Modifier Range",
                                Description = "ATK modifiers must be in [0, 1]",
                                UnitId = unit.DisplayName,
                                Details = $"Effect={effect.Type}, Modifier={effect.Modifier}"
                            };
                        }
                    }

                    // SE5: Burn value must be positive
                    if (effect.Type == StatusEffectType.Burn && effect.Value <= 0)
                    {
                        yield return new InvariantViolation
                        {
                            Category = "StatusEffect",
                            InvariantName = "SE5: Burn Value Positive",
                            Description = "Burn effects must have Value > 0",
                            UnitId = unit.DisplayName,
                            Details = $"BurnValue={effect.Value}"
                        };
                    }
                }

                // SE6: Stunned units cannot act
                // (This is checked at action execution time, not state invariant)

                // SE7: Dead units should not accumulate new status effects
                // (This is a behavioral invariant, checked during effect application)
            }
        }

        #endregion

        #region Energy Invariants (E1-E6)

        private IEnumerable<InvariantViolation> CheckEnergyInvariants(
            IEnumerable<UnitRuntimeState> allUnits, 
            TeamEnergyState playerEnergy, 
            TeamEnergyState enemyEnergy)
        {
            // E1: Team energy must be in [0, MaxEnergy]
            if (playerEnergy != null)
            {
                if (playerEnergy.Energy < 0 || playerEnergy.Energy > playerEnergy.MaxEnergy)
                {
                    yield return new InvariantViolation
                    {
                        Category = "Energy",
                        InvariantName = "E1: Team Energy Bounds",
                        Description = "Team energy must be in [0, MaxEnergy]",
                        UnitId = "PlayerTeam",
                        Details = $"Energy={playerEnergy.Energy}, MaxEnergy={playerEnergy.MaxEnergy}"
                    };
                }
            }

            if (enemyEnergy != null)
            {
                if (enemyEnergy.Energy < 0 || enemyEnergy.Energy > enemyEnergy.MaxEnergy)
                {
                    yield return new InvariantViolation
                    {
                        Category = "Energy",
                        InvariantName = "E1: Team Energy Bounds",
                        Description = "Team energy must be in [0, MaxEnergy]",
                        UnitId = "EnemyTeam",
                        Details = $"Energy={enemyEnergy.Energy}, MaxEnergy={enemyEnergy.MaxEnergy}"
                    };
                }
            }

            // E2: Enemy per-unit energy must be in [0, UltimateEnergyCost]
            foreach (var enemy in allUnits.Where(u => u.IsEnemy))
            {
                if (enemy.CurrentEnergy < 0)
                {
                    yield return new InvariantViolation
                    {
                        Category = "Energy",
                        InvariantName = "E2: Enemy Energy Non-Negative",
                        Description = "Enemy per-unit energy must be >= 0",
                        UnitId = enemy.DisplayName,
                        Details = $"CurrentEnergy={enemy.CurrentEnergy}"
                    };
                }
            }

            // E3: Energy cost must not exceed MaxEnergy
            // (Checked at design time via UnitDefinitionSO validation)

            // E4: Using ultimate must consume energy
            // (Behavioral invariant, checked during ultimate execution)
        }

        #endregion

        #region Cooldown Invariants (C1-C3)

        private IEnumerable<InvariantViolation> CheckCooldownInvariants(IEnumerable<UnitRuntimeState> allUnits)
        {
            foreach (var unit in allUnits)
            {
                // C1: Cooldown remaining must be non-negative
                if (unit.UltimateCooldownRemaining < 0)
                {
                    yield return new InvariantViolation
                    {
                        Category = "Cooldown",
                        InvariantName = "C1: Cooldown Non-Negative",
                        Description = "UltimateCooldownRemaining must be >= 0",
                        UnitId = unit.DisplayName,
                        Details = $"Cooldown={unit.UltimateCooldownRemaining}"
                    };
                }

                // C2: Units with cooldown > 0 cannot use ultimate
                if (unit.UltimateCooldownRemaining > 0 && !unit.IsStunned && unit.IsAlive)
                {
                    // This is implicitly enforced by CanUseUltimate property
                    // We check it doesn't contradict itself
                    if (unit.IsEnemy && unit.CanUseUltimate && unit.UltimateCooldownRemaining > 0)
                    {
                        yield return new InvariantViolation
                        {
                            Category = "Cooldown",
                            InvariantName = "C2: Cooldown Blocks Ultimate",
                            Description = "Units on cooldown must not pass CanUseUltimate check",
                            UnitId = unit.DisplayName,
                            Details = $"Cooldown={unit.UltimateCooldownRemaining}, CanUseUltimate={unit.CanUseUltimate}"
                        };
                    }
                }
            }
        }

        #endregion

        #region Targeting Invariants (T1-T3)

        private IEnumerable<InvariantViolation> CheckTargetingInvariants(IEnumerable<UnitRuntimeState> allUnits)
        {
            // T1: Dead units should not be targeted
            // (Behavioral check during target selection)

            // T2: Target pattern must match action result
            // (Behavioral check during action execution)

            // T3: SlotIndex, Column, Row must be valid
            foreach (var unit in allUnits)
            {
                if (unit.SlotIndex < 0 || unit.SlotIndex > 5)
                {
                    yield return new InvariantViolation
                    {
                        Category = "Targeting",
                        InvariantName = "T3: Valid SlotIndex",
                        Description = "SlotIndex must be in [0, 5]",
                        UnitId = unit.DisplayName,
                        Details = $"SlotIndex={unit.SlotIndex}"
                    };
                }

                if (unit.Column < 0 || unit.Column > 2)
                {
                    yield return new InvariantViolation
                    {
                        Category = "Targeting",
                        InvariantName = "T3: Valid Column",
                        Description = "Column must be in [0, 2]",
                        UnitId = unit.DisplayName,
                        Details = $"Column={unit.Column}"
                    };
                }

                if (unit.Row < 0 || unit.Row > 1)
                {
                    yield return new InvariantViolation
                    {
                        Category = "Targeting",
                        InvariantName = "T3: Valid Row",
                        Description = "Row must be in [0, 1]",
                        UnitId = unit.DisplayName,
                        Details = $"Row={unit.Row}"
                    };
                }
            }
        }

        #endregion

        /// <summary>
        /// Quick check: returns true if no violations found.
        /// </summary>
        public bool IsValid(IEnumerable<UnitRuntimeState> allUnits, TeamEnergyState playerEnergy, TeamEnergyState enemyEnergy)
        {
            return !CheckAll(allUnits, playerEnergy, enemyEnergy).Any();
        }
    }
}
