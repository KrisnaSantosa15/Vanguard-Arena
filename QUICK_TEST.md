# Quick Test Guide - Artemis Ultimate Bug

## What to Do

1. **Open Unity** → Play the battle scene
2. **Let Artemis use ultimate** (wait for 2 energy)
3. **Stop the game**
4. **Open this file:** `battle_debug.log` (in project root)
5. **Copy all contents** and share

## What the Log Shows

```
=== VANGUARD ARENA DEBUG LOG ===
Session Started: [timestamp]
...

================= ULTIMATE MULTI: Artemis ==================
[timestamp] [ULTIMATE] Artemis using AOE pattern: AllEnemies
[timestamp] [GET-TARGETS] Actor: Artemis (IsEnemy: True), Pattern: AllEnemies
[timestamp] [GET-TARGETS] AllEnemies filter found X targets  ← Check if X > 0
[timestamp] [ULTIMATE-MULTI] 💥 Artemis -> X targets (ATTACK)  ← Should be ATTACK not SUPPORT
[timestamp] [ULTIMATE-MULTI] No animation driver found for Artemis. Applying damage directly.
[timestamp] [DAMAGE] Computing damage for [player] (HP: X/X)
[timestamp] [DAMAGE] Calculated damage: X (Crit: True/False)  ← Should be > 0
[timestamp] [DAMAGE] HP change: X -> X  ← Should decrease
[timestamp] [ULTIMATE-MULTI] Damage loop complete. Damaged X targets.
```

## Critical Checks

| Log Line | Expected | If Wrong → Problem |
|----------|----------|-------------------|
| `AllEnemies filter found X targets` | X ≥ 1 | Targeting broken |
| `(ATTACK)` or `(SUPPORT)` | ATTACK | IsAllyTargeting bug |
| `No animation driver found` | Present | Driver check issue |
| `Calculated damage: X` | X > 0 | Damage formula broken |
| `HP change: 70 -> X` | X < 70 | HP not updating |
| `Damaged X targets` | X ≥ 1 | Loop not executing |

## File Location
```
D:\Projects\GameDevelopment\Unity\Vanguard Arena\battle_debug.log
```

---
**Just run → stop → copy the log file!**
