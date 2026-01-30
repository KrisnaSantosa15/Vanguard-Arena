using UnityEngine;
using Project.Domain;

namespace Project.Domain
{
    /// <summary>
    /// Encapsulates AI decision making logic.
    /// </summary>
    public static class BattleAI
    {
        public static bool ShouldUseUltimate(UnitRuntimeState actor, TeamEnergyState teamEnergy, System.Collections.Generic.List<UnitRuntimeState> allUnits)
        {
            if (!actor.CanUseUltimate || teamEnergy.Energy < actor.UltimateEnergyCost)
                return false;

            // Count alive enemies and allies
            int aliveEnemies = 0;
            int aliveAllies = 0;
            UnitRuntimeState lowestHPEnemy = null;
            UnitRuntimeState lowestHPAlly = null;

            foreach (var u in allUnits)
            {
                if (u == null || !u.IsAlive) continue;
                if (u.IsEnemy != actor.IsEnemy)
                {
                    aliveEnemies++;
                    if (lowestHPEnemy == null || u.CurrentHP < lowestHPEnemy.CurrentHP)
                        lowestHPEnemy = u;
                }
                else
                {
                    aliveAllies++;
                    if (lowestHPAlly == null || u.CurrentHP < lowestHPAlly.CurrentHP)
                        lowestHPAlly = u;
                }
            }

            // 1. Use ultimate if it can kill low HP enemies (AoE advantage)
            if (actor.UltimateTargetPattern == TargetPattern.AllEnemies && aliveEnemies >= 2)
            {
                if (lowestHPEnemy != null && lowestHPEnemy.CurrentHP < actor.CurrentATK * 1.5f)
                {
                    FileLogger.Log($"[SMART-AI] Use ULT: Can finish low HP enemies (AoE)");
                    return true;
                }
            }

            // 2. Use ultimate if outnumbered
            if (aliveEnemies > aliveAllies + 1)
            {
                FileLogger.Log($"[SMART-AI] Use ULT: Outnumbered ({aliveAllies} vs {aliveEnemies})");
                return true;
            }

            // 3. Save energy if ally is low and might need support (future: healing ults)
            if (lowestHPAlly != null && lowestHPAlly.CurrentHP < lowestHPAlly.MaxHP * 0.3f && teamEnergy.Energy < 5)
            {
                FileLogger.Log($"[SMART-AI] Save Energy: Ally critical HP ({lowestHPAlly.DisplayName}: {lowestHPAlly.CurrentHP}/{lowestHPAlly.MaxHP})");
                return false;
            }

            // 4. Use ultimate if we have plenty of energy (>= 5)
            if (teamEnergy.Energy >= 5)
            {
                FileLogger.Log($"[SMART-AI] Use ULT: High energy ({teamEnergy.Energy})");
                return true;
            }

            // 5. Default: use it if available
            FileLogger.Log($"[SMART-AI] Use ULT: Default behavior");
            return true;
        }
    }
}
