# QA Test Plan - Vanguard Arena

## 1. Overview
This test plan is designed for a **Solo Developer** workflow. It focuses on high-impact areas to ensure stability and playability for the Vertical Slice.

## 2. Smoke Test (Daily Build Checklist)
*Run this checklist before every commit or at the start of each dev day.*

*   [ ] **Boot**: Game launches without crashing to desktop.
*   [ ] **UI**: Main Menu loads; all buttons represent generic interaction (no dead clicks).
*   [ ] **Battle Entry**: Clicking "Battle" -> "Select Hero" -> "Start" successfully loads the map.
*   [ ] **Gameplay**: Character moves via joystick; Attack button triggers animation.
*   [ ] **Termination**: Completing a match (Win/Loss) returns to Main Menu properly.

## 3. Functional Tests (Combat & Logic)

### 3.1 Combat Math
*   **Damage**: Verify `Damage = Attacker.Atk - Defender.Def` (min 1 damage).
*   **Healing**: Verify Heals increase HP but do not exceed MaxHP.
*   **Death**: HP <= 0 triggers Death State (Respawn timer starts, Model hidden, Input disabled).

### 3.2 Status Effects
*   **Stun**: Input disabled for duration. Movement stops.
*   **Slow**: Movement speed reduced by X%.
*   **Burn/Poison**: Apply damage ticks every Y seconds.
*   **Buffs**: Verify stat increases (e.g., Speed Up) apply and expire correctly.

### 3.3 Game Modes (3v3)
*   **Spawning**: Players spawn at correct team bases.
*   **Objectives**: Zone capture logic (if applicable) or Kill count increments.
*   **End Condition**: Game ends immediately when Timer = 0 or Kill Cap reached.

## 4. Performance Targets

### 4.1 Frame Rate
*   **Target**: 60 FPS stable.
*   **Minimum**: 30 FPS (during heavy VFX/Ultimates).
*   **Resolution**: 1920x1080 (PC), Adaptive (Mobile).

### 4.2 Resources
*   **Memory (RAM)**: Keep under 500 MB to support mid-range mobile.
*   **Load Time**: < 5 seconds from Hero Select to Battle.
*   **Draw Calls**: Target < 150 per frame (Batching enabled).

## 5. Cheats & Debug Commands
*Implemented via hidden console or debug menu for testing.*

| Command | Effect | Use Case |
| :--- | :--- | :--- |
| `/win` | Force Victory immediately. | Test Result screen & rewards. |
| `/lose` | Force Defeat immediately. | Test Result screen. |
| `/addgold [n]` | Add `n` Gold. | Test Shop/Upgrades. |
| `/godmode` | Infinite HP / No Damage. | Test mechanics without dying. |
| `/killall` | Kill all enemies. | clear wave/enemies. |
| `/cooldown` | Toggle 0 cooldowns. | Test skill spam/VFX. |
| `/unlockall` | Unlock all heroes/items. | specific content access. |
