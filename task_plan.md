# Ultimate Combo Animation Event Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement precise hit reactions for the 3-hit Ultimate combo by utilizing Animation Events instead of splitting clips or relying on delayed coroutines.

**Architecture:** 
We will leverage Unity's Animation Event system to trigger code at specific frames of the "Ultimate" animation. A receiver script (`UnitAnimationDriver` or new `AnimationEventHandler`) will catch these events and delegate them to the `BattleController` or `UnitRuntimeState` to apply damage/reactions instantly. This decouples visual timing from game loop timing while keeping them synchronized.

**Tech Stack:** Unity 6 (C#), Animation Events, Mecanim

---

### Task 1: Create Animation Event Receiver

**Files:**
- Modify: `Assets/_Project/Scripts/UnitAnimationDriver.cs`

**Step 1: Write failing test (Conceptual - Unity Test Runner not fully setup for Anim Events)**
*Since we can't easily unit test Animation Events without running the scene, we will define the method signature first.*

**Step 2: Add Event Handler Method to `UnitAnimationDriver`**

We need a public method that the Animation Event can call. Let's name it `OnAnimationHit`.

```csharp
// In Assets/_Project/Scripts/UnitAnimationDriver.cs

// Add this event definition
public event Action<int> OnHitTriggered;

// Add this method to be called by Animation Event
// index: 1, 2, 3 for combo hits
public void OnAnimationHit(int index) 
{
    Debug.Log($"[ANIM-EVENT] Hit Triggered: {index} on {gameObject.name}");
    OnHitTriggered?.Invoke(index);
}
```

**Step 3: Verification**
Verify the script compiles.

---

### Task 2: Integrate Receiver with BattleController

**Files:**
- Modify: `Assets/_Project/Scripts/Presentation/BattleController.cs`

**Step 1: Subscribe to Animation Events**

In `ExecuteUltimateMulti`, instead of using `PlayDelayedHit` coroutines, we will subscribe to the actor's `UnitAnimationDriver.OnHitTriggered`.

**Step 2: Refactor `ExecuteUltimateMulti` logic**

*   Remove `PlayDelayedHit` and `SpawnDelayedPopup` coroutines.
*   Subscribe to event before playing animation.
*   In the event handler, apply the damage/visuals for that specific hit index.
*   Unsubscribe after animation finishes.

```csharp
// Concept Logic for BattleController.cs

private void OnCombatHit(int hitIndex, List<HitInfo> hitInfos)
{
    // Apply visual effects for this specific hit index
    // For a 3-hit combo: 
    // Hit 1: Light reaction
    // Hit 2: Light reaction
    // Hit 3: Heavy reaction (Knockback) + Big Damage Popup?
    
    // OR simply split total damage:
    // If we want 1 big number at end, only show at index 3.
    // If we want 3 small numbers, divide damage.
    
    // CURRENT REQUIREMENT: "Light hit for attack 1,2... Ultimate animation is combined of A, B, C"
    
    foreach(var hit in hitInfos)
    {
       // Trigger Enemy "Hurt" animation
       // If index < 3: Trigger "LightHit"
       // If index == 3: Trigger "HeavyHit" (Knockback)
       
       PlayHitReaction(hit.target, isHeavy: index == 3);
    }
}
```

**Step 3: Update `UnitAnimationDriver` to support Light vs Heavy hit**

Modify `PlayHit(bool isUlt)` to something more flexible like `PlayHit(HitType type)`.

---

### Task 3: Unity Editor Configuration (Manual Steps)

**Files:**
- Unity Editor (Animation Window)

**Step 1: Open Animation Window**
User needs to:
1.  Select the Player Unit prefab.
2.  Open Window > Animation > Animation.
3.  Select "Ultimate" clip.

**Step 2: Add Events**
1.  Scrub to Hit 1 impact frame. Add Event `OnAnimationHit(1)`.
2.  Scrub to Hit 2 impact frame. Add Event `OnAnimationHit(2)`.
3.  Scrub to Hit 3 impact frame. Add Event `OnAnimationHit(3)`.

*Since I cannot do this via code, I will provide a script to strictly validate that these events exist on the AnimationClip if possible, or precise instructions.*

---

### Task 4: Refining the Hit Reaction Logic

**Files:**
- Modify: `Assets/_Project/Scripts/Presentation/BattleController.cs`
- Modify: `Assets/_Project/Scripts/UnitAnimationDriver.cs`

**Step 1: Define Hit Types**
Add enum or bools to `UnitAnimationDriver` to distinguish `TriggerLightHit` and `TriggerHeavyHit`.

**Step 2: Implement the "Light Hit" logic in BattleController**
When `OnAnimationHit(1)` or `(2)` fires:
- Call `targetView.AnimDriver.PlayHit(HitType.Light)`
- Spawn small damage popup (optional, or wait for final)

When `OnAnimationHit(3)` fires:
- Call `targetView.AnimDriver.PlayHit(HitType.Heavy)`
- Spawn BIG damage popup.

---

### Task 5: Cleanup and Verify

**Files:**
- Cleanup: Remove unused `PlayDelayedHit` coroutines in `BattleController.cs`.

**Step 1: Run functionality test**
Verify the sequence:
1. Ult starts.
2. Event 1 fires -> Enemies flinch (Light).
3. Event 2 fires -> Enemies flinch (Light).
4. Event 3 fires -> Enemies fly back (Heavy) + Damage shows.

