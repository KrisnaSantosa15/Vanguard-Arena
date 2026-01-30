# Animation Event Setup Instructions

To enable the multi-hit logic for the Ultimate ability, you must manually add Animation Events to the specific animation clips in the Unity Editor. This connects the visual animation timing to the code logic.

## 1. Open Animation Window
1. Go to the top menu: **Window > Animation > Animation** (or press `Ctrl+6`).
2. Dock this window somewhere convenient (e.g., bottom with Project/Console).

## 2. Select the Unit
1. In the **Hierarchy** or **Project** view, select the Unit Prefab or Instance you are configuring (e.g., `MeleeUnit`).
2. Ensure the GameObject has an `Animator` component and the `UnitAnimationDriver` script attached.

## 3. Select the Ultimate Animation Clip
1. In the Animation Window, click the dropdown menu in the top-left (which usually shows "Idle" or "Run").
2. Select the animation clip corresponding to the Ultimate ability.
   - For **Melee Units**, the code uses the trigger `MeleeUltimate`. The clip might be named `MeleeUltimate`, `Attack03`, `Spin`, or similar.
   - For **Ranged Units**, the code uses the trigger `RangeUltimate`.

## 4. Add Animation Events
You need to add events at the specific frames where you want the damage/hit to occur.

### First Hit
1. Drag the white timeline line (scrubber) to the frame where the first impact occurs (e.g., Frame 20).
2. Click the **Add Event** button (small white marker icon with a plus sign, usually on the left of the timeline toolbar).
3. In the Inspector (with the Event marker selected):
   - **Function:** `OnAnimationHit`
   - **Int:** `1`

### Second Hit
1. Scrub to the second impact frame (e.g., Frame 40).
2. Click **Add Event**.
3. In the Inspector:
   - **Function:** `OnAnimationHit`
   - **Int:** `2`

### Third Hit
1. Scrub to the third impact frame (e.g., Frame 60).
2. Click **Add Event**.
3. In the Inspector:
   - **Function:** `OnAnimationHit`
   - **Int:** `3`

## 5. Apply Changes
- If editing a Prefab, the changes save automatically.
- If editing an Instance in the scene, remember to click **Overrides > Apply All** in the Inspector to save changes to the Prefab.

## 6. Verification
- Play the game.
- Use the Ultimate ability.
- The `UnitAnimationDriver.OnAnimationHit(int index)` method will be called 3 times.
- If you have enabled debug logs or checking damage, you should see 3 distinct events corresponding to the animation timing.