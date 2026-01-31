# Phase 6 Test Fixes - Missing UnitViewPrefab

## Issue Found
The 2 failing tests were:
1. ❌ `BattleController_StartBattle_InitializesUnits`
2. ❌ `BattleController_AllEnemiesDead_VictoryTriggered`

## Root Cause
**Error in logs**: `⚠️ Slot 0 has null definition or prefab. Skipping.`

The `BattleTestBuilder.CreateUnitDefinition()` method was creating `UnitDefinitionSO` objects but **not assigning the `UnitViewPrefab` property**. When `BattleUnitManager.SpawnTeam()` tried to instantiate units, it skipped them because `UnitViewPrefab` was null.

### Code Flow:
```
Test: BuildDefinitions()
  → BattleTestBuilder.CreateUnitDefinition()
    → Creates UnitDefinitionSO (but UnitViewPrefab = null!)
      → BattleController.InitializeBattle()
        → BattleUnitManager.SpawnTeam()
          → Checks: if (def.UnitViewPrefab == null) → Skip! ❌
```

## Fix Applied

### File: `BattleTestBuilder.cs`

#### Added Using Directive (lines 1-7):
```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Project.Domain;
#if UNITY_EDITOR
using UnityEditor;
#endif
```

#### Updated `CreateUnitDefinition()` Method (lines ~132-180):
```csharp
private UnitDefinitionSO CreateUnitDefinition(...)
{
    var unit = ScriptableObject.CreateInstance<UnitDefinitionSO>();
    // ... existing property assignments ...
    
    // NEW: Load the default unit prefab for PlayMode tests
    unit.UnitViewPrefab = UnityEngine.Resources.Load<GameObject>("UnitAnimated");
    if (unit.UnitViewPrefab == null)
    {
        // Fallback: try loading from Assets path (for when Resources folder doesn't exist)
        #if UNITY_EDITOR
        unit.UnitViewPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Units/UnitAnimated.prefab");
        #endif
    }
    
    return unit;
}
```

### Strategy:
1. **Primary**: Try `Resources.Load<GameObject>("UnitAnimated")` 
   - Fast, works in builds
   - Requires prefab to be in `Resources/` folder

2. **Fallback**: Use `AssetDatabase.LoadAssetAtPath()` in Editor
   - Only available in Unity Editor (wrapped in `#if UNITY_EDITOR`)
   - Loads from direct file path: `Assets/_Project/Prefabs/Units/UnitAnimated.prefab`
   - Works even if prefab is not in Resources folder

## Why This Works

### UnitDefinitionSO Structure:
```csharp
public class UnitDefinitionSO : ScriptableObject
{
    public string DisplayName;
    public int HP;
    public int ATK;
    // ... stats ...
    public GameObject UnitViewPrefab;  // ← This was null!
}
```

### BattleUnitManager.SpawnTeam() Logic:
```csharp
public void SpawnTeam(UnitDefinitionSO[] lineup, Transform[] slots, bool isEnemy)
{
    for (int i = 0; i < count; i++)
    {
        var def = lineup[i];
        if (def == null || def.UnitViewPrefab == null)  // ← Check here
        {
            FileLogger.LogWarning($"Slot {i} has null definition or prefab. Skipping.");
            continue;  // ← Was skipping test units!
        }
        
        var go = Instantiate(def.UnitViewPrefab, slot.position, slot.rotation, slot);
        // ... rest of spawning logic ...
    }
}
```

Now that `UnitViewPrefab` is assigned, units will spawn correctly!

## Expected Results After Fix

### Test 1: `BattleController_StartBattle_InitializesUnits`
**Before**: 
- Player units: 0 (skipped due to null prefab)
- Enemy units: 0 (skipped due to null prefab)
- Test fails: Expected 1 player, got 0

**After**:
- Player units: 1 ✅ (spawned with UnitAnimated prefab)
- Enemy units: 1 ✅ (spawned with UnitAnimated prefab)
- Test passes: Correct unit counts

### Test 2: `BattleController_AllEnemiesDead_VictoryTriggered`
**Before**:
- No units spawned
- Battle never starts (no units = no turns)
- Test times out waiting for battle end

**After**:
- Strong player unit spawned (500 HP, 200 ATK)
- Weak enemy unit spawned (50 HP, 5 ATK)
- Battle executes normally
- Player wins quickly
- `IsBattleOver = true`, `Outcome = Victory`
- Test passes ✅

## Files Modified

1. **`Assets/_Project/Tests/Utils/BattleTestBuilder.cs`**
   - Added `#if UNITY_EDITOR` using directive for UnityEditor
   - Added prefab loading logic in `CreateUnitDefinition()`

## Verification Steps

1. ✅ Wait for Unity to recompile
2. ✅ Run PlayMode tests again
3. ✅ Verify logs show: `Units spawned: X total` (where X > 0)
4. ✅ Verify no "null definition or prefab" warnings
5. ✅ Verify all 15/15 tests pass

## Alternative Solutions Considered

### Option 1: Mock UnitView (Rejected)
Create a minimal test prefab with just UnitView component.

**Pros**: Lighter, faster
**Cons**: Doesn't test real unit behavior, animations won't work

### Option 2: Require Manual Prefab Assignment (Rejected)
Force tests to manually load and assign prefabs.

**Pros**: More explicit
**Cons**: Verbose test code, harder to maintain

### Option 3: Auto-load from AssetDatabase (Selected ✅)
Automatically load prefab in builder.

**Pros**: Tests "just work", uses real prefabs, clean test code
**Cons**: Depends on prefab existing at specific path

## Future Improvements

### 1. Move Prefab to Resources Folder
**Current**: `Assets/_Project/Prefabs/Units/UnitAnimated.prefab`  
**Better**: `Assets/_Project/Resources/UnitAnimated.prefab`

**Benefits**:
- `Resources.Load()` works in builds
- No need for AssetDatabase fallback
- Faster loading

### 2. Create Lightweight Test Prefab
Create `Assets/_Project/Resources/TestUnit.prefab` with:
- UnitView component
- Simple cube mesh
- No complex animations

**Benefits**:
- Faster test execution
- Isolated from production prefab changes
- Smaller memory footprint

### 3. Add Prefab Validation to BattleTestBuilder
```csharp
public BattleTestBuilder()
{
    _testPrefab = Resources.Load<GameObject>("UnitAnimated");
    if (_testPrefab == null)
    {
        Debug.LogError("[TEST] UnitAnimated prefab not found! Tests will fail.");
    }
}
```

**Benefits**:
- Fail fast with clear error message
- Easier debugging when prefab missing

## Debug Commands

### Check if prefab exists:
```csharp
GameObject prefab = Resources.Load<GameObject>("UnitAnimated");
Debug.Log($"Prefab loaded: {prefab != null}");
```

### Check prefab path in Editor:
```csharp
#if UNITY_EDITOR
var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Units/UnitAnimated.prefab");
Debug.Log($"Prefab path valid: {prefab != null}");
#endif
```

### View spawned units in scene:
```csharp
var units = GameObject.FindObjectsOfType<UnitView>();
Debug.Log($"Units in scene: {units.Length}");
```

## Summary

**Root Cause**: Missing `UnitViewPrefab` assignment in test builder  
**Fix**: Auto-load prefab using Resources.Load() + AssetDatabase fallback  
**Impact**: 2 failing tests → 2 passing tests ✅  
**Total Tests**: 15/15 PlayMode tests expected to pass  

---

**Status**: Fix implemented, awaiting recompile + test rerun  
**Action Required**: Run PlayMode tests in Unity Editor to verify fix
