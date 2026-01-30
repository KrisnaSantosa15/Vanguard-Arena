# Development Roadmap — Vanguard Arena

> **Goal:** Ship a Vertical Slice (3v3 Turn-Based RPG) in Unity 6.3 LTS.
> **Philosophy:** Solo-Dev Optimized. Focus on core loop feel before content scale.

---

## Phase 1: Vertical Slice (Weeks 1-3)
**Objective:** A fully playable 3v3 battle with 6 units, one campaign node, and functional UI.

### Week 1: Core Tech & Pipeline
- [ ] **Project Setup:** Unity 6.3, URP, GitHub Repo, Folder Structure.
- [ ] **Domain Layer:** Implement `BattleState`, `TurnTimeline`, and `DamageFormula` (Pure C#).
- [ ] **Art Pipeline:** Import 1 placeholder character rig + 1 environment kit. Verify "Inverted Hull" outline shader.
- [ ] **Input:** Set up Unity Input System (Battle Actions).

### Week 2: Battle Implementation
- [ ] **Battle Loop:** Implement Start -> Turn -> Action -> End resolution.
- [ ] **Presentation:** Connect `BattleEvents` to Animator and VFX spawner.
- [ ] **UI MVP:** Unit Frames (HP/Energy), Action Buttons, Turn Order Strip (UI Toolkit).
- [ ] **VFX:** Create "Hit" and "Slash" particle systems.

### Week 3: Content & Polish (The "Slice")
- [ ] **Units:** Configure 6 `UnitDefinitionSO` (3 Heroes, 3 Enemies) with distinct stats.
- [ ] **Campaign:** Create `Chapter 1 - Node 1` encounter.
- [ ] **Save System:** Implement JSON save/load for player profile.
- [ ] **Playtest:** Verify 60 FPS on PC target. Tune animation timing.

---

## Phase 2: MVP Content Expansion (Weeks 4-8)
**Objective:** Turn the slice into a loop. Progression, rewards, and multiple stages.

### Week 4-5: Progression Loop
- [ ] **Meta UI:** Home Screen, Roster Screen, Unit Detail View.
- [ ] **Upgrade System:** Level Up (consumes XP items) and Skill Up logic.
- [ ] **Economy:** Implement Gold and Stamina counters.
- [ ] **Persistence:** Verify progression saves between sessions.

### Week 6-7: Content Scaling
- [ ] **Campaign:** Build Chapter 1 (7 Nodes + 1 Boss).
- [ ] **Dungeon:** Implement "Resource Dungeon" (Daily rotation logic).
- [ ] **Units:** Expand roster to 12 units (reuse animations where possible).
- [ ] **Audio:** Add SFX for UI and Skills. Add BGM.

### Week 8: Polish & Alpha Build
- [ ] **Tutorial:** 5-minute onboarding flow.
- [ ] **Settings:** Volume, Resolution, Quality toggles.
- [ ] **Build Pipeline:** Automate Windows build generation.

---

## Phase 3: Live Ops Foundation (Weeks 9-12)
**Objective:** Gacha, Shop, and Backend.

### Week 9-10: Backend Integration
- [ ] **Firebase:** Init Auth and Firestore.
- [ ] **Cloud Save:** Sync local JSON to Firestore.
- [ ] **Remote Config:** Move balance constants to server.

### Week 11-12: Monetization (Gacha)
- [ ] **Gacha System:** Server-authoritative pull logic (Cloud Functions).
- [ ] **UI:** Summon Banner, Animation, Pity Counter display.
- [ ] **Shop:** Exchange "Stardust" (dupes) for items.

---

## Post-Launch / Updates
- [ ] **Async Arena:** PvP using defender snapshots.
- [ ] **Team Expansion:** Move from 3v3 to 6v6.
- [ ] **New Chapters:** Story continuation.
