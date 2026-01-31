using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Project.Domain;

namespace Project.Presentation
{
    /// <summary>
    /// Manages Unit Lifecycle, Spawning, and View Lookups.
    /// </summary>
    public class BattleUnitManager : MonoBehaviour
    {
        private readonly List<UnitRuntimeState> _units = new();
        private readonly Dictionary<UnitRuntimeState, UnitView> _views = new();

        public List<UnitRuntimeState> AllUnits => _units;

        public UnitRuntimeState Boss => _units.FirstOrDefault(u => u.IsEnemy && u.IsBoss)
                                     ?? _units.FirstOrDefault(u => u.IsEnemy);

        public List<UnitRuntimeState> PlayerSquad => _units.Where(u => u != null && !u.IsEnemy).ToList();

        public void SpawnTeam(UnitDefinitionSO[] lineup, Transform[] slots, bool isEnemy)
        {
            FileLogger.LogSeparator($"SPAWN TEAM: {(isEnemy ? "ENEMY" : "PLAYER")}");
            int count = Mathf.Min(lineup.Length, slots.Length);
            FileLogger.Log($"Spawning {count} units for {(isEnemy ? "ENEMY" : "PLAYER")} team", "SPAWN");

            for (int i = 0; i < count; i++)
            {
                var def = lineup[i];
                if (def == null || def.UnitViewPrefab == null)
                {
                    FileLogger.LogWarning($"Slot {i} has null definition or prefab. Skipping.", "SPAWN");
                    continue;
                }

                var state = new UnitRuntimeState(def, isEnemy, i); // Pass slot index
                _units.Add(state);

                FileLogger.Log($"{state.DisplayName} created: IsEnemy={state.IsEnemy}, Slot={i}, Column={state.Column}, Row={state.Row}, HP={state.CurrentHP}/{state.MaxHP}, ATK={state.ATK}", "SPAWN");

                Transform slot = slots[i];
                var go = Instantiate(def.UnitViewPrefab, slot.position, slot.rotation, slot);
                go.name = $"{(isEnemy ? "EN" : "PL")}_{def.DisplayName}_{i}";
                FileLogger.Log($"GameObject instantiated: {go.name}", "SPAWN");

                // Rotate enemies 180 degrees to face players
                if (isEnemy)
                {
                    go.transform.Rotate(0, 180, 0);
                }

                // Add Components if missing
                if (go.GetComponent<UnitClickable>() == null)
                    go.AddComponent<UnitClickable>();

                if (go.GetComponentInChildren<TargetIndicator>() == null)
                    go.AddComponent<TargetIndicator>();

                var view = go.GetComponent<UnitView>();
                if (view != null)
                {
                    view.Bind(state);
                    _views[state] = view;
                    FileLogger.Log($"View bound successfully for {state.DisplayName}", "SPAWN");
                }
                else
                {
                    FileLogger.LogError($"UnitView missing on prefab {def.DisplayName}!", "SPAWN");
                }
            }

        FileLogger.Log($"Team spawn complete. Total units in battle: {_units.Count}", "SPAWN");
    }

    /// <summary>
    /// Clear all units and destroy their GameObjects (for test cleanup).
    /// </summary>
    public void ClearAll()
    {
        FileLogger.Log($"Clearing {_units.Count} units and {_views.Count} views", "CLEAR");
        
        // Destroy all unit GameObjects
        foreach (var view in _views.Values)
        {
            if (view != null && view.gameObject != null)
            {
                Destroy(view.gameObject);
            }
        }
        
        // Clear internal collections
        _units.Clear();
        _views.Clear();
        
        FileLogger.Log("All units cleared", "CLEAR");
    }

    public bool TryGetView(UnitRuntimeState unit, out UnitView view)
        {
            if (unit == null)
            {
                view = null;
                return false;
            }
            return _views.TryGetValue(unit, out view);
        }

        public bool IsTeamDead(bool isEnemy)
        {
            return !_units.Any(u => u != null && u.IsEnemy == isEnemy && u.IsAlive);
        }

        public UnitRuntimeState SelectTarget(bool actorIsEnemy)
        {
            // Fallback for when we don't have actor context (shouldn't happen in new system)
            bool targetIsEnemy = !actorIsEnemy;
            return _units
                .Where(u => u != null && u.IsEnemy == targetIsEnemy && u.IsAlive)
                .OrderBy(u => u.CurrentHP)
                .FirstOrDefault();
        }

        /// <summary>
        /// NEW: Positional targeting - units attack same column, prioritize front row
        /// </summary>
        public UnitRuntimeState SelectTargetPositional(UnitRuntimeState actor)
        {
            if (actor == null || !actor.IsAlive) return null;

            bool targetIsEnemy = !actor.IsEnemy;
            int actorColumn = actor.Column;

            FileLogger.Log($"[TARGET] {actor.DisplayName} (Col {actorColumn}) selecting target...");

            // Step 1: Try front row in same column
            var frontRowTarget = _units
                .Where(u => u != null && u.IsAlive && u.IsEnemy == targetIsEnemy && u.Row == 0 && u.Column == actorColumn)
                .FirstOrDefault();

            if (frontRowTarget != null)
            {
                FileLogger.Log($"→ Found front row target: {frontRowTarget.DisplayName} (Col {frontRowTarget.Column}, Row {frontRowTarget.Row})", "TARGET");
                return frontRowTarget;
            }

            // Step 2: Try back row in same column
            var backRowTarget = _units
                .Where(u => u != null && u.IsAlive && u.IsEnemy == targetIsEnemy && u.Row == 1 && u.Column == actorColumn)
                .FirstOrDefault();

            if (backRowTarget != null)
            {
                FileLogger.Log($"→ Found back row target: {backRowTarget.DisplayName} (Col {backRowTarget.Column}, Row {backRowTarget.Row})", "TARGET");
                return backRowTarget;
            }

            // Step 3: Deterministic fallback - try other columns (right, then left, circularly)
            FileLogger.Log($"[TARGET] → Column {actorColumn} is empty. Trying fallback...");

            int[] columnOrder = GetColumnFallbackOrder(actorColumn);
            foreach (int col in columnOrder)
            {
                // Try front row first
                var fallbackFront = _units
                    .Where(u => u != null && u.IsAlive && u.IsEnemy == targetIsEnemy && u.Row == 0 && u.Column == col)
                    .FirstOrDefault();

                if (fallbackFront != null)
                {
                    FileLogger.Log($"[TARGET] → Fallback to front row: {fallbackFront.DisplayName} (Col {fallbackFront.Column}, Row {fallbackFront.Row})");
                    return fallbackFront;
                }

                // Try back row
                var fallbackBack = _units
                    .Where(u => u != null && u.IsAlive && u.IsEnemy == targetIsEnemy && u.Row == 1 && u.Column == col)
                    .FirstOrDefault();

                if (fallbackBack != null)
                {
                    FileLogger.Log($"[TARGET] → Fallback to back row: {fallbackBack.DisplayName} (Col {fallbackBack.Column}, Row {fallbackBack.Row})");
                    return fallbackBack;
                }
            }

            FileLogger.LogWarning($"[TARGET] → No valid targets found!");
            return null;
        }

        /// <summary>
        /// Deterministic fallback order: try right, then left (circular)
        /// Column 0 → [1, 2]
        /// Column 1 → [2, 0]
        /// Column 2 → [0, 1]
        /// </summary>
        private int[] GetColumnFallbackOrder(int currentColumn)
        {
            switch (currentColumn)
            {
                case 0: return new[] { 1, 2 }; // Right → Far Right
                case 1: return new[] { 2, 0 }; // Right → Left
                case 2: return new[] { 0, 1 }; // Wrap to Left → Center
                default: return new[] { 0, 1, 2 }; // Safety fallback
            }
        }

        public List<UnitRuntimeState> GetValidTargets(UnitRuntimeState actor, TargetPattern pattern)
        {
            var results = new List<UnitRuntimeState>();

            FileLogger.Log($"Actor: {actor.DisplayName} (IsEnemy: {actor.IsEnemy}), Pattern: {pattern}", "GET-TARGETS");
            FileLogger.Log($"Total units in battle: {_units.Count}", "GET-TARGETS");

            switch (pattern)
            {
                case TargetPattern.AllEnemies:
                    results.AddRange(_units.Where(u => u != null && u.IsAlive && u.IsEnemy != actor.IsEnemy));
                    FileLogger.Log($"AllEnemies filter found {results.Count} targets", "GET-TARGETS");
                    break;

                case TargetPattern.AllAllies:
                    results.AddRange(_units.Where(u => u != null && u.IsAlive && u.IsEnemy == actor.IsEnemy));
                    break;

                case TargetPattern.SingleEnemy:
                    results.AddRange(_units.Where(u => u != null && u.IsAlive && u.IsEnemy != actor.IsEnemy));
                    break;

                case TargetPattern.SingleAlly:
                    results.AddRange(_units.Where(u => u != null && u.IsAlive && u.IsEnemy == actor.IsEnemy && u != actor));
                    break;

                case TargetPattern.Self:
                    if (actor.IsAlive) results.Add(actor);
                    break;

                case TargetPattern.LowestHpEnemy:
                    var lowestHp = _units
                        .Where(u => u != null && u.IsAlive && u.IsEnemy != actor.IsEnemy)
                        .OrderBy(u => u.CurrentHP)
                        .FirstOrDefault();
                    if (lowestHp != null) results.Add(lowestHp);
                    break;

                case TargetPattern.HighestThreatEnemy:
                    var highestAtk = _units
                        .Where(u => u != null && u.IsAlive && u.IsEnemy != actor.IsEnemy)
                        .OrderByDescending(u => u.ATK)
                        .FirstOrDefault();
                    if (highestAtk != null) results.Add(highestAtk);
                    break;

                default:
                    // Fallback to enemies
                    results.AddRange(_units.Where(u => u != null && u.IsAlive && u.IsEnemy != actor.IsEnemy));
                    FileLogger.Log($"Default fallback found {results.Count} targets", "GET-TARGETS");
                    break;
            }

            FileLogger.Log($"Final result: {results.Count} targets - {string.Join(", ", results.Select(u => $"{u.DisplayName}(HP:{u.CurrentHP})"))}", "GET-TARGETS");
            return results;
        }

        public void RefreshAllViews()
        {
            foreach (var view in _views.Values)
            {
                if (view != null) view.Refresh();
            }
        }

        public void RefreshView(UnitRuntimeState unit)
        {
            if (unit != null && _views.TryGetValue(unit, out var view))
            {
                view.Refresh();
            }
        }

        // --- Visual Helpers ---

        public void ShowCurrentActorIndicator(UnitRuntimeState actor)
        {
            if (TryGetView(actor, out var view))
            {
                var indicator = view.GetComponentInChildren<TargetIndicator>();
                if (indicator != null) indicator.ShowCurrentActor();
            }
        }

        public void HideCurrentActorIndicator(UnitRuntimeState actor)
        {
            if (TryGetView(actor, out var view))
            {
                var indicator = view.GetComponentInChildren<TargetIndicator>();
                if (indicator != null) indicator.Hide();
            }
        }

        public void HighlightValidTargets(List<UnitRuntimeState> validTargets, UnitRuntimeState currentActor, TargetPattern pattern)
        {
            bool isAllyTargeting = TargetPatternHelper.IsAllyTargeting(pattern);

            foreach (var kvp in _views)
            {
                var unit = kvp.Key;
                var view = kvp.Value;

                if (unit == currentActor) continue; // Skip actor

                var indicator = view.GetComponentInChildren<TargetIndicator>();
                if (indicator == null) continue;

                if (validTargets.Contains(unit) && unit.IsAlive)
                {
                    if (isAllyTargeting)
                        indicator.ShowValidAlly();  // Blue ring for allies/self
                    else
                        indicator.ShowValidEnemy(); // Green ring for enemies
                }
                else
                    indicator.Hide();
            }
        }

        public void ClearAllHighlights(UnitRuntimeState currentActor)
        {
            foreach (var kvp in _views)
            {
                var unit = kvp.Key;
                var view = kvp.Value;

                if (unit == currentActor) continue;

                var indicator = view.GetComponentInChildren<TargetIndicator>();
                if (indicator != null) indicator.Hide();
            }
        }
    }
}
