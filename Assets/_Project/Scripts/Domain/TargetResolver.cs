using System.Collections.Generic;
using System.Linq;

namespace Project.Domain
{
    /// <summary>
    /// Resolves targeting patterns for actions.
    /// Pure domain logic - no Unity dependencies.
    /// </summary>
    public static class TargetResolver
    {
        /// <summary>
        /// Resolve targets based on pattern, actor, and available units.
        /// </summary>
        /// <param name="pattern">Targeting pattern to resolve</param>
        /// <param name="actor">Unit performing the action</param>
        /// <param name="allies">All allied units (including actor)</param>
        /// <param name="enemies">All enemy units</param>
        /// <param name="rng">Deterministic random number generator</param>
        /// <param name="manualTarget">Manually selected target (for single-target patterns)</param>
        /// <returns>List of valid targets</returns>
        public static List<UnitRuntimeState> ResolveTargets(
            TargetPattern pattern,
            UnitRuntimeState actor,
            List<UnitRuntimeState> allies,
            List<UnitRuntimeState> enemies,
            IDeterministicRandom rng,
            UnitRuntimeState manualTarget = null)
        {
            var targets = new List<UnitRuntimeState>();

            // Filter alive units
            var aliveAllies = allies.Where(u => u != null && u.IsAlive).ToList();
            var aliveEnemies = enemies.Where(u => u != null && u.IsAlive).ToList();

            switch (pattern)
            {
                case TargetPattern.Self:
                    if (actor != null && actor.IsAlive)
                        targets.Add(actor);
                    break;

                case TargetPattern.SingleEnemy:
                    if (manualTarget != null && manualTarget.IsAlive && aliveEnemies.Contains(manualTarget))
                    {
                        targets.Add(manualTarget);
                    }
                    else if (aliveEnemies.Count > 0)
                    {
                        // Default: Random enemy
                        targets.Add(aliveEnemies[rng.Next(0, aliveEnemies.Count)]);
                    }
                    break;

                case TargetPattern.SingleAlly:
                    var otherAllies = aliveAllies.Where(a => a != actor).ToList();
                    if (manualTarget != null && manualTarget.IsAlive && otherAllies.Contains(manualTarget))
                    {
                        targets.Add(manualTarget);
                    }
                    else if (otherAllies.Count > 0)
                    {
                        // Default: Random ally (excluding self)
                        targets.Add(otherAllies[rng.Next(0, otherAllies.Count)]);
                    }
                    break;

                case TargetPattern.AllEnemies:
                    targets.AddRange(aliveEnemies);
                    break;

                case TargetPattern.AllAllies:
                    targets.AddRange(aliveAllies.Where(a => a != actor));
                    break;

                case TargetPattern.LowestHpEnemy:
                    if (aliveEnemies.Count > 0)
                    {
                        var lowestHp = aliveEnemies.OrderBy(e => e.CurrentHP).First();
                        targets.Add(lowestHp);
                    }
                    break;

                case TargetPattern.HighestThreatEnemy:
                    if (aliveEnemies.Count > 0)
                    {
                        // Threat = ATK * HP% (high damage units with high HP)
                        var highestThreat = aliveEnemies.OrderByDescending(e => e.CurrentATK * (e.CurrentHP / (float)e.MaxHP)).First();
                        targets.Add(highestThreat);
                    }
                    break;

                case TargetPattern.FrontRowSingle:
                    var frontEnemies = GetFrontRow(aliveEnemies);
                    if (manualTarget != null && manualTarget.IsAlive && frontEnemies.Contains(manualTarget))
                    {
                        targets.Add(manualTarget);
                    }
                    else if (frontEnemies.Count > 0)
                    {
                        targets.Add(frontEnemies[rng.Next(0, frontEnemies.Count)]);
                    }
                    break;

                case TargetPattern.BackRowSingle:
                    var backEnemies = GetBackRow(aliveEnemies);
                    if (manualTarget != null && manualTarget.IsAlive && backEnemies.Contains(manualTarget))
                    {
                        targets.Add(manualTarget);
                    }
                    else if (backEnemies.Count > 0)
                    {
                        targets.Add(backEnemies[rng.Next(0, backEnemies.Count)]);
                    }
                    break;

                case TargetPattern.FrontRowAll:
                    targets.AddRange(GetFrontRow(aliveEnemies));
                    break;

                case TargetPattern.BackRowAll:
                    targets.AddRange(GetBackRow(aliveEnemies));
                    break;

                case TargetPattern.ColumnSingle:
                case TargetPattern.ColumnAll:
                    // TODO: Implement column targeting for 6v6 formation
                    // For now, fallback to single enemy
                    if (aliveEnemies.Count > 0)
                        targets.Add(aliveEnemies[rng.Next(0, aliveEnemies.Count)]);
                    break;
            }

            return targets;
        }

        /// <summary>
        /// Get front row units (Row = 0).
        /// </summary>
        private static List<UnitRuntimeState> GetFrontRow(List<UnitRuntimeState> units)
        {
            return units.Where(u => u.Row == 0).ToList();
        }

        /// <summary>
        /// Get back row units (Row = 1).
        /// </summary>
        private static List<UnitRuntimeState> GetBackRow(List<UnitRuntimeState> units)
        {
            return units.Where(u => u.Row == 1).ToList();
        }

        /// <summary>
        /// Check if a targeting pattern requires manual target selection.
        /// </summary>
        public static bool RequiresManualTarget(TargetPattern pattern)
        {
            switch (pattern)
            {
                case TargetPattern.SingleEnemy:
                case TargetPattern.SingleAlly:
                case TargetPattern.FrontRowSingle:
                case TargetPattern.BackRowSingle:
                case TargetPattern.ColumnSingle:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Check if a targeting pattern is ally-targeting (heals/buffs instead of damages).
        /// </summary>
        public static bool IsAllyTargeting(TargetPattern pattern)
        {
            return TargetPatternHelper.IsAllyTargeting(pattern);
        }
    }
}
