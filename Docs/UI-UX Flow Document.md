# UI/UX Flow Document - Vanguard Arena

## 1. Overview
This document outlines the user journey, screen flows, and interface layouts for the **Vertical Slice (3v3 Battle)** of Vanguard Arena. The focus is on a streamlined, mobile-first experience.

## 2. User Flows

### 2.1 FTUE (First Time User Experience)
The initial onboarding experience for a new player.
1.  **Splash Screen**: Logo & Loading.
2.  **Intro Cinematic**: Short 10s video or slideshow establishing the world.
3.  **Tutorial Battle**:
    *   Scripted 1v1 scenario.
    *   Prompts: "Move here", "Attack dummy", "Use Skill", "Use Ultimate".
    *   Goal: Destroy the enemy base.
4.  **Name Entry**: Player inputs Display Name.
5.  **Main Menu**: Landing state.

### 2.2 Core Loop (Battle)
The primary gameplay loop.
1.  **Main Menu**: Tap "BATTLE" (Big central button).
2.  **Hero Selection**:
    *   View available heroes.
    *   Select 1 Hero.
    *   Confirm "Ready".
3.  **Matchmaking / Loading**: Searching for 5 other players (or bots for VS).
4.  **Battle (3v3)**: The gameplay session (approx 3-5 mins).
5.  **Result Screen**:
    *   **Victory/Defeat** banner.
    *   MVP highlight.
    *   Rewards (Gold, XP).
    *   Button: "Home" or "Play Again".
6.  **Return to Main Menu**.

### 2.3 Upgrade Loop (Meta)
Improving hero strength outside of battle.
1.  **Main Menu**: Tap "Heroes" button.
2.  **Hero List**: Grid view of owned heroes. Warning indicators for available upgrades.
3.  **Hero Detail**:
    *   3D Model view.
    *   Stats panel (HP, Atk, Def).
    *   Skills description.
4.  **Upgrade Action**:
    *   Tap "Upgrade" button.
    *   Feedback: Particles/Sound, Stats increase.
    *   Cost: Gold + Hero Shards.
5.  **Back**: Return to Hero List -> Main Menu.

---

## 3. Wireframes & Layouts

### 3.1 Battle HUD (Unity UI Toolkit)
Targeting mobile landscape (1920x1080 ref).

**Top Region**
*   **Center**: Timer [ 03:00 ], Score [ Blue 12 - 10 Red ].
*   **Left**: Minimap (Square, showing team positions).
*   **Right**: Settings Icon (Pause menu), Emote/Ping Button.

**Bottom Left Region**
*   **Virtual Joystick**:
    *   Anchor: (200, 200).
    *   Dynamic radius.
    *   Controls character movement.

**Bottom Right Region (Action Buttons)**
*   **Attack (Main)**: Large button, bottom-right corner.
*   **Skill 1**: Arc position above Attack. Cooldown overlay.
*   **Skill 2**: Arc position above Skill 1. Cooldown overlay.
*   **Ultimate**: Top of the arc. Glows when charged.
*   **Summoner Spell / Dash**: Small button near the pivot.

**Floating Elements**
*   **Health Bars**: Above characters (Green = Self/Ally, Red = Enemy).
*   **Damage Numbers**: Pop-up text on hit.
*   **Kill Feed**: Top-center-right list (e.g., "Player1 slew Player2").

### 3.2 Main Menu
*   **Header**: Player Name, Currency (Gold, Gems), Stamina.
*   **Center**: Featured Hero 3D Model.
*   **Right**: "BATTLE" (Start).
*   **Bottom Nav**: Shop | Heroes | **Home** | Quests | Social.
