# Artemis Ultimate Damage Bug - Debug Test Instructions

## Bug Description
Ranged enemy unit **Artemis** uses ultimate (TargetPattern.AllEnemies), animation plays, but **ZERO damage** is dealt to player units. No damage popups appear, HP remains unchanged.

## 🆕 NEW: Automatic File Logging System

All debug logs are now **automatically written to a file** that you can directly read!

### Log File Location
```
D:\Projects\GameDevelopment\Unity\Vanguard Arena\battle_debug.log
```

The log file:
- ✅ **Replaces itself** every time you play the game (fresh log per session)
- ✅ **Includes timestamps** (HH:mm:ss.fff format)
- ✅ **Categorizes logs** (ULTIMATE, DAMAGE, GET-TARGETS, etc.)
- ✅ **Auto-flushes** to disk (no buffering delays)
- ✅ **Also appears in Unity Console** for real-time debugging

### How to Use

1. **Start the game** in Unity Editor (Play mode)
2. **Trigger Artemis ultimate** (let it build 2 energy and use Arrow Storm)
3. **Stop the game** (Exit Play mode)
4. **Open the log file**: `battle_debug.log` in the project root directory
5. **Copy the entire log contents** and share here

That's it! No more copying from Unity Console. Just read the file directly.

## Test Setup

### 1. Open Unity Project
- Open **Vanguard Arena** in Unity Editor
- Ensure you're in the Battle Scene

### 2. Create Test Battle with Artemis
- Spawn a battle that includes **Artemis** as an enemy unit
- Ensure you have at least 3-4 player units with HP

### 3. Trigger Artemis Ultimate
- Allow Artemis to build up 2 energy (ultimateCost = 2)
- Wait for Artemis to use ultimate (Arrow Storm - AllEnemies AOE)

## Expected Debug Logs (Check Unity Console)

### Phase 1: Target Selection (ExecuteUltimate wrapper)
```
[ULTIMATE] Artemis using AOE pattern: AllEnemies
[GET-TARGETS] Actor: Artemis (IsEnemy: True), Pattern: AllEnemies
[GET-TARGETS] Total units in battle: X
[GET-TARGETS] AllEnemies filter found X targets
[GET-TARGETS] Final result: X targets - PlayerName1(HP:70), PlayerName2(HP:65), ...
[ULTIMATE] GetValidTargets returned X targets
[ULTIMATE] Targets: PlayerName1(Alive:True), PlayerName2(Alive:True), ...
[ULTIMATE] Artemis proceeding with X target(s). Routing to Multi handler.
```

**CRITICAL CHECK:**
- If `GetValidTargets returned 0 targets` → **PROBLEM IS IN TARGETING LOGIC**
- If targets > 0, proceed to Phase 2

### Phase 2: Multi-Target Execution (ExecuteUltimateMulti)
```
💥 [ULTIMATE-MULTI] Artemis -> X targets (ATTACK)
[ULTIMATE-MULTI] Targets: PlayerName1, PlayerName2, ...
[ULTIMATE-MULTI] Actor transform found: Artemis_View(Clone)
[ULTIMATE-MULTI] No animation driver found for Artemis. Applying damage directly.
[ULTIMATE-MULTI] ValidTargets count for direct damage: X
```

**CRITICAL CHECK:**
- If `NO VIEW FOUND` → **PROBLEM IS VIEW REGISTRATION**
- If `(SUPPORT)` instead of `(ATTACK)` → **PROBLEM IS IsAllyTargeting logic**
- If driver != null → **UNEXPECTED - ranged units shouldn't have driver**

### Phase 3: Damage Application Loop
```
[ULTIMATE-MULTI] Computing damage for PlayerName1 (HP: 70/70)
[ULTIMATE-MULTI] Calculated damage: 45 (Crit: False)
[ULTIMATE-MULTI] After shield absorption: 45
[ULTIMATE-MULTI] HP change: 70 -> 25
[ULTIMATE-MULTI] Computing damage for PlayerName2 (HP: 65/70)
[ULTIMATE-MULTI] Calculated damage: 48 (Crit: True)
[ULTIMATE-MULTI] After shield absorption: 48
[ULTIMATE-MULTI] HP change: 65 -> 17
...
[ULTIMATE-MULTI] Damage loop complete. Damaged X targets.
```

**CRITICAL CHECK:**
- If `ValidTargets count for direct damage: 0` → **TARGETS FILTERED OUT BY IsAlive CHECK**
- If `Calculated damage: 0` → **PROBLEM IS DAMAGE CALCULATION**
- If `After shield absorption: 0` → **SHIELD IS BLOCKING ALL DAMAGE**
- If HP change shows same values (70 -> 70) → **PROBLEM IS HP ASSIGNMENT**

## Failure Scenarios & Root Causes

| Symptom | Root Cause | Investigation |
|---------|------------|---------------|
| `GetValidTargets returned 0 targets` | Targeting logic broken for enemies | Check if player units exist in `_units` list, verify `IsEnemy` flags |
| `NO VIEW FOUND` | View not registered for enemy | Check `SpawnTeam()` for enemies, verify `_views` dictionary |
| `(SUPPORT)` branch instead of `(ATTACK)` | IsAllyTargeting incorrectly returns true | Verify `TargetPatternHelper.IsAllyTargeting(AllEnemies)` returns false |
| `ValidTargets count for direct damage: 0` | Targets died or filtered between checks | Check timing, verify `IsAlive` status |
| `Calculated damage: 0` | Combat formula broken | Check `ComputeUltimateDamage()` with Artemis stats (ATK, multiplier) |
| `After shield absorption: 0` | Shield blocking all damage | Check if player shields are abnormally high |
| `HP change: 70 -> 70` | HP assignment not working | Check if `target.CurrentHP` setter is functioning |
| No logs at all | Ultimate not being called | Check energy system, verify Artemis energy reaches 2 |

## Files Modified (Session 3)
- **BattleController.cs** (lines 557-575, 782-946): Added extensive logging
- **BattleUnitManager.cs** (lines 176-227): Added target selection logging

## Next Steps After Log Analysis

### If targets = 0:
1. Check `_units` list population in BattleUnitManager
2. Verify `IsEnemy` flags on all units
3. Check if player team is spawned correctly

### If view not found:
1. Check `SpawnTeam()` in BattleController (line ~113)
2. Verify enemy view registration in `_views` dictionary
3. Check if enemy prefabs are being instantiated

### If damage = 0:
1. Check Artemis ATK stat (should be 15)
2. Verify `skillMultiplier` is 2.2
3. Check if mitigation formula is broken

### If damage loop never executes:
1. Check if `driver == null` condition is true
2. Verify `validTargets` list is not empty
3. Check if coroutine is yielding correctly

## Test Completion Checklist
- [ ] Artemis spawns correctly as enemy
- [ ] Artemis builds 2 energy
- [ ] Ultimate animation plays (Arrow Storm)
- [ ] All Phase 1 logs appear (target selection)
- [ ] All Phase 2 logs appear (multi-target routing)
- [ ] All Phase 3 logs appear (damage loop per target)
- [ ] Damage popups appear on player units
- [ ] Player HP bars decrease
- [ ] Battle log shows damage entries

## Report Template
When reporting results, copy/paste all logs that match the phases above and note:
1. **Did targets get found?** (Phase 1)
2. **Which branch was entered?** (ATTACK vs SUPPORT)
3. **Did damage loop execute?** (Phase 3)
4. **What were the calculated damage values?**
5. **Did HP actually change?**

---

**Last Updated:** Session 3, Jan 29 2026
**Status:** Debug instrumentation complete, awaiting test results
