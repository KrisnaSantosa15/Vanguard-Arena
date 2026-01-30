# Animation Fixes Summary

## Issues Fixed

### 1. **HitUltimate Knockback Recovery** ✅
**Problem**: When targets were hit by ultimate attacks, they would get knocked back by the animation physics but never return to their original position, breaking formation.

**Solution**: Added position save/restore system for targets:
- Save target's original position before playing HitUltimate animation
- After hit animation completes, smoothly move target back to original position
- Applied to both single-target (ExecuteUltimateSingle) and multi-target (ExecuteUltimateMulti) ultimates

**Code Changes**:
```csharp
// Save target original position for knockback recovery
Vector3 targetOriginalPos = Vector3.zero;
Transform targetTransform = null;
if (_views.TryGetValue(target, out var targetViewForKnockback) && targetViewForKnockback != null)
{
    targetTransform = targetViewForKnockback.transform;
    targetOriginalPos = targetTransform.position;
}

// Play hit animation
if (shieldedDmg > 0)
{
    PlayAnimation(target, true, true); // true = ultimate hit, true = isHit
    yield return new WaitForSeconds(hitAnimationDuration);
}

// Return target to original position after knockback
if (targetTransform != null)
{
    while (Vector3.Distance(targetTransform.position, targetOriginalPos) > 0.1f)
    {
        targetTransform.position = Vector3.MoveTowards(targetTransform.position, targetOriginalPos, meleeMovementSpeed * Time.deltaTime);
        yield return null;
    }
    targetTransform.position = targetOriginalPos; // Snap to exact position
}
```

### 2. **Dash vs Running Animation** ✅
**Problem**: When melee units moved to attack, the `IsMoving` bool parameter was triggering a running animation instead of a dash animation.

**Solution**: Renamed the animation parameter from `IsMoving` to `IsDashing` for clearer purpose and to allow separate dash/run animations in the Animator Controller.

**Code Changes**:

**UnitAnimationDriver.cs**:
```csharp
// Old
public void SetMoving(bool isMoving)
{
    _anim.SetBool("IsMoving", isMoving);
}

// New
public void SetDashing(bool isDashing)
{
    _anim.SetBool("IsDashing", isDashing);
}
```

**BattleController.cs**:
```csharp
// Old
private void SetMoving(UnitRuntimeState unit, bool isMoving)
{
    animator.SetMoving(isMoving);
}

// New
private void SetDashing(UnitRuntimeState unit, bool isDashing)
{
    animator.SetDashing(isDashing);
}
```

All calls updated from:
```csharp
SetMoving(actor, true);  // Enable movement animation
SetMoving(actor, false); // Disable movement animation
```

To:
```csharp
SetDashing(actor, true);  // Enable dash animation
SetDashing(actor, false); // Disable dash animation
```

## Files Modified

1. **UnitAnimationDriver.cs**
   - Changed `SetMoving(bool isMoving)` to `SetDashing(bool isDashing)`
   - Updated animator parameter from "IsMoving" to "IsDashing"

2. **BattleController.cs**
   - Renamed `SetMoving()` helper method to `SetDashing()`
   - Updated all 8 SetMoving calls across 4 Execute methods:
     - ExecuteBasicSingle (2 calls: move to target, return)
     - ExecuteUltimateSingle (2 calls: move to target, return)
     - ExecuteBasicMulti (2 calls: move to center, return)
     - ExecuteUltimateMulti (2 calls: move to center, return)
   - Added knockback recovery in ExecuteUltimateSingle (1 target)
   - Added knockback recovery in ExecuteUltimateMulti (multiple targets)

## Next Steps for Unity Setup

### Update Animator Controller

You need to update your Animator Controller to use the new parameter:

1. **Remove old parameter**:
   - Open your character's Animator Controller
   - In the Parameters tab, remove "IsMoving" (Bool)

2. **Add new parameter**:
   - Add "IsDashing" (Bool) parameter

3. **Update transitions**:
   - Find all transitions that used "IsMoving"
   - Update them to use "IsDashing" instead
   - Make sure transitions go from Idle → Dash (not Idle → Run)
   - Ensure your Dash animation is properly connected

4. **Transition Setup Example**:
   ```
   Idle → Dash
   Condition: IsDashing is true
   Has Exit Time: false
   Transition Duration: 0.1s
   
   Dash → Idle
   Condition: IsDashing is false
   Has Exit Time: false
   Transition Duration: 0.1s
   ```

## Testing Checklist

- [ ] Melee units dash (not run) when moving to attack
- [ ] Melee units dash back to original position after attacking
- [ ] Ranged units stay in place (no dashing)
- [ ] Targets hit by basic attacks stay in place
- [ ] Targets hit by ultimate attacks get knocked back
- [ ] Targets return to formation after ultimate knockback
- [ ] Multiple targets in AOE ultimates all return to position
- [ ] Death animations still play correctly after knockback

## Animation Parameter Reference

Current animator parameters:
- **IsDead** (Bool): Triggers death animation
- **IsDashing** (Bool): ✨ NEW - Enables dash animation during melee movement
- **MeleeAttack** (Trigger): Melee basic attack
- **MeleeUltimate** (Trigger): Melee ultimate attack
- **RangeAttack** (Trigger): Ranged basic attack
- **RangeUltimate** (Trigger): Ranged ultimate attack
- **HitBasic** (Trigger): Hit by basic attack
- **HitUltimate** (Trigger): Hit by ultimate attack (with knockback)
