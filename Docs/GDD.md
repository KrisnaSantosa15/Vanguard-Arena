# Game Design Document (GDD) — 2.5D Turn-Based Squad RPG (Unity 6.3 LTS, URP)

> **Working title:** *Project: Vanguard Arena* (placeholder)
> **High concept:** Collect a roster of original heroes/villains, build a **3–6 unit** squad, and win fast, readable turn-based battles powered by an **Energy** resource and **synergy** passives—built as a **2.5D** game using **3D URP**, optimized for solo development, with **gacha monetization**, and PC-first launch.

---

## 0) Document control

* **Version:** 0.2 (Living Document)
* **Engine:** Unity **6.3 LTS**
* **Rendering:** **3D URP** (2.5D presentation)
* **Primary platform priority:** **PC (Windows) first**
* **Secondary platform (later):** Android
* **Target session length:** 10–25 minutes (PC); 5–12 minutes (mobile later)
* **Monetization:** **Gacha** (planned; implement post-core-loop stabilization)
* **Design goals:** Strategic depth with low cognitive load; high clarity; data-driven content creation; scalable to live ops.
* **Camera**: Orthographic (3D scene, fixed tactical framing)
* **Team size roadmap**: MVP 3v3 → Mid 4v4 → Late 6v6 (post-Arena stabilization)
* **Monetization**: Gacha planned, numbers deferred until Vertical Slice + MVP validation

---

## 1) Research-informed best-practice principles (applied)

### 1.1 Unity 6.3 LTS production approach

* Build on **Unity 6.3 LTS** and follow Unity’s best-practice guidance for project organization, profiling, and architecture. ([Unity Documentation][1])
* Use **ScriptableObjects** for shared game data (units, skills, items, encounters) for data-driven iteration. ([Unity][2])
* If/when content grows, use **Addressables** with clear grouping/label/update strategy to support live content updates. ([Unity][3])

### 1.2 GDD best practices

* Treat the GDD as a **living blueprint**: a single source of truth for scope, features, constraints, and implementation decisions. ([gitbook.com][4])

---

## 2) Vision

### 2.1 Elevator pitch

A turn-based squad RPG where players assemble a team, manage **Energy**, exploit **synergies**, and counter enemy formations. The game emphasizes **fast turns**, **high readability**, **meaningful progression**, and long-term collection via **gacha**, while remaining feasible for solo development.

### 2.2 Design pillars

1. **Readable Strategy** — Every action’s intent is visible (target lines, status icons, damage preview).
2. **Team Identity** — Synergies matter: faction/role passives and combo interactions.
3. **Short Iteration Loops** — Combat resolves quickly; upgrades are meaningful per session.
4. **Data-Driven Growth** — New units/modes added via assets (ScriptableObjects), not rewrites. ([Unity][2])
5. **2.5D Clarity** — 3D visuals, 2D readability: consistent silhouettes, strong VFX language, predictable camera.

### 2.3 Audience

* Players who enjoy squad-building, turn-based combat, collection/progression loops.
* Casual-to-midcore: auto-battle for farming; manual play for hard content.

---

## 3) Product scope (solo-dev optimized)

### 3.1 MVP (ship target — PC first)

* Campaign chapters (PvE)
* 1 repeatable “Resource Dungeon”
* Roster of **12–16** units (original IP)
* Upgrade loop (level, skills, gear)
* Simple daily quests
* Offline/local save (cloud optional later)
* **3v3 formation** only (upgrade to 6v6 later)

### 3.2 Post-MVP (expansion)

* **Gacha** (banners, pity, duplicates/shards)
* Async Arena (PvP vs snapshots)
* Events/rotations (live-ops)
* Additional dungeons, boss raids, seasonal ladders
* Shop layers (battle pass, cosmetics, bundles)

---

## 4) Gameplay overview

### 4.1 Core loop

1. **Select mode** (Campaign / Dungeon)
2. **Build squad** (formation + units)
3. **Battle** (turn-based)
4. **Rewards** (currency/materials/shards)
5. **Upgrade** (level/skills/gear)
6. **Repeat** (push progression gate)

### 4.2 Session loop (daily)

* Claim login rewards
* Complete 3–5 daily tasks
* Spend stamina in dungeon
* Push campaign or attempt challenge node

---

## 5) Combat design (turn-based squad RPG)

### 5.1 Battle format

* **Formation:** **3v3 (MVP)**; scalable to **6v6** later (front/back rows)
* **Win condition:** defeat all enemies
* **Time-to-resolution target:** 60–120 seconds per standard fight

### 5.2 Turn order

* Each unit has **SPD (Speed)**.
* Turn order computed by a timeline:

  * MVP: sort by SPD each round.
  * Later: initiative meter fill by SPD (supports interrupts).
* Determinism: outcomes reproducible from seed + inputs.

Here’s the **updated GDD text** for Energy + Actions, matching your final decisions:

* Team-shared energy
* **No turn-start energy gain**
* **Basic grants +1 energy**
* **Start battle with 2–3 energy** (configurable; default **2** to match minimum ult cost)
* **Combo hits fill Energy Bar** → at 100% converts to +1 energy (carry remainder)
* Ult costs 2–5 energy and has **cooldown** (per character; typical 2–3 turns)

---

### 5.3 Energy system (shared team resource)

**Team-shared Energy + Energy Bar**

* **Energy (0–10)** shared per side.
* **Start-of-battle energy:** each side starts with **2 Energy** (configurable; may be 3 for special modes/bosses).
* **Energy Bar (0–100%)** per side:

  * represents accumulated combat momentum from multi-hit attacks
  * when it reaches **≥ 100%**, it converts into **+1 Energy**, and **carries remainder** (e.g., 120% → +1 Energy + 20% bar)

**Gain**

* **+1 Energy on Basic Attack** (always).
* **Combo / multi-hit contribution:** each hit (including the first) adds **X%** to the team Energy Bar.

  * When Energy Bar crosses 100%, it grants +1 Energy (repeatable if >200%, etc.), then carries remainder.
  * If Energy is already at max, energy gain clamps (bar remainder still retained).

**Spend**

* **Ultimates cost 2–5 Energy** (varies by unit kit and role).
* **Ultimates do not generate Energy** (no energy gain and no bar fill) to prevent runaway loops.

**Purpose**

* Ensures a predictable baseline ultimate cadence driven by **basic attacks**, while letting combo-focused units accelerate energy slightly via Energy Bar conversion—creating clear “power turns” without making outcomes purely RNG.

---
### 5.4 Actions

Each unit has:

* **Basic**

  * no energy cost
  * grants **+1 team Energy** on use
  * may be multi-hit (combo) depending on the unit (some units are single-hit only)

* **Ultimate**

  * costs **2–5 team Energy** (unit-defined)
  * has a **cooldown** (unit-defined; typical **2–3 turns**)
  * does **not** generate energy (no +1 energy and no Energy Bar fill)

* **Passive** (always-on or trigger-based)

* Optional later: **Skill 2** (avoid in MVP unless needed)

---

### 5.5 Targeting shapes

* Single enemy
* Lowest HP / Highest ATK / Front row / Back row (rule-based)
* Row (front/back)
* All enemies (AoE)
* Ally single / all allies

### 5.6 Stats (MVP)

* **HP**
* **ATK**
* **DEF**
* **SPD**
* **CRIT% / CRIT DMG** (optional)
* **RESIST / ACC** (optional)

### 5.7 Damage model (baseline)

* One damage type in MVP.
* Example formula:

  * `Raw = ATK * SkillMultiplier`
  * `Reduced = Raw * (100 / (100 + DEF))`
  * Apply buffs/debuffs, crit, shields, clamp.

### 5.8 Buffs/debuffs (MVP set)

* **Shield**
* **Taunt**
* **Stun**
* **Burn**
* **ATK Up / DEF Down**

### 5.9 Status rules

* Duration measured in **turns**.
* Stacking policy:

  * Shield stacks by value
  * Burn refresh duration (or stacks value—choose one and standardize)
  * ATK Up / DEF Down: highest magnitude wins, duration refresh
* UI icons with countdown numbers.

### 5.10 AI (PvE)

* MVP priorities:

  1. If ultimate available and “good target exists” → ultimate
  2. Else basic attack lowest HP or front row
* Heuristics per archetype.

### 5.11 Auto-battle

* Enabled in farming stages.
* Same AI as enemies (or slightly smarter).
* Optional “manual override”: player chooses ultimates only.

### 5.12 Battle UX requirements

* Turn order strip (portraits)
* Energy bar per side
* Damage numbers + status popups
* Combat log (optional toggle)
* Speed: 1x / 2x / 3x (3x after first clear)

---

## 6) 2.5D presentation (URP, 3D pipeline)

### 6.1 Visual framing definition

* **2.5D** = **3D characters + 3D environment** presented with a **fixed or semi-fixed camera** and **tight readability constraints**.
* Preferred camera:

  * **Orthographic** camera for consistent silhouette scale (recommended for clean tactical readability), OR
  * **Perspective** with locked FOV and fixed angle to avoid distortion.

* **Camera (locked decision): Orthographic**
  * Fixed angle, fixed distance, consistent composition.
  * Z-axis reserved for layering (VFX depth, knockback offsets, hit reactions).

### 6.2 Battle stage layout

* Two lanes:

  * **Front row**: 3 slots
  * **Back row**: 3 slots (reserved for 6v6 expansion later; in MVP, keep hidden/unused)
* Anchors:

  * `PlayerSlots_Front[3]`, `PlayerSlots_Back[3]`
  * `EnemySlots_Front[3]`, `EnemySlots_Back[3]`

* **Slot grid evolution (same scene, expanding anchors):**

  * **3v3 (MVP):** Front row only (3 slots)
  * **4v4 (mid):** Front row 2 + Back row 2 (or Front 4 if you prefer a single row; pick one and standardize)
  * **6v6 (late):** Front 3 + Back 3
  
### 6.3 Readability constraints (hard rules)

* Characters must face the opponent side; minimal camera movement.
* VFX cannot obscure units > 0.35s at 1x speed.
* Status indicators remain legible at 1080p.

### 6.4 URP technical style

* Use URP Lit + simple shading:

  * Limit dynamic lights per scene
  * Prefer baked/real-time mixed lighting (PC-first flexibility)
* Post-processing: minimal (bloom, vignette), must not reduce UI readability.

---

## 7) Roster, roles, synergies

### 7.1 Unit taxonomy

* **Role:** Tank / DPS / Support / Control
* **Faction:** 4–6 factions (original lore)
* **Rarity:** Common / Rare / Epic / Legendary

### 7.2 Synergy system (MVP)

* **Faction synergy:** ≥2 same faction → small bonus; ≥4 → stronger bonus.
* **Role synergy:** Tank + Support + DPS → “Balanced” bonus.

### 7.3 Example kit templates

1. **Tank Guardian**

   * Basic: single hit + self shield
   * Ult: team shield + taunt 1 turn
   * Passive: DEF scaling when HP < 50%
2. **Burst DPS**

   * Ult: single target nuke, bonus vs DEF Down
3. **Control**

   * Ult: AoE chance stun (later balanced with ACC/RESIST)

---

## 8) Progression systems

### 8.1 Player progression

* Player Level increases max stamina and unlocks features.

### 8.2 Unit progression (MVP)

* **Unit Level**
* **Skill Levels**
* **Gear:** 3 slots (Weapon/Armor/Trinket)
* **Ascension:** shards/materials to increase level cap

### 8.3 Materials & currencies

* **Gold** (soft currency)
* **Stamina**
* **Upgrade mats** (skill books, gear ore, ascension tokens)
* **Premium currency** (for gacha + shop later)

---

## 9) Economy & rewards (design + tuning plan)

### 9.1 Reward principles

* Every session yields at least one meaningful upgrade route (level/skills/gear/shards).

### 9.2 Drop tables

* Stage rewards defined via ScriptableObjects.
* First-clear bonuses
* 3-star milestones (optional later)

### 9.3 Balancing methodology

* Automated battle simulator for thousands of fights.
* Add telemetry later for tuning (win rates, time-to-kill, ult usage).

---

## 10) Game modes

### 10.1 Campaign (PvE)

* Chapters of 10 nodes:

  * 7 normal fights
  * 2 elite fights
  * 1 boss (telegraphed mechanics)

### 10.2 Resource Dungeon (repeatable)

* Rotating daily:

  * Monday: gold
  * Tuesday: skill books
  * Wednesday: gear ore
  * Weekend: mixed
* Difficulty tiers + recommended power.

### 10.3 Challenge Tower (post-MVP)

* Endless floors with escalating modifiers.

### 10.4 Async Arena (post-MVP)

* Fight defender snapshots.
* Weekly rank rewards.
* No real-time networking.

---

## 11) UX / UI design (screens & requirements)

### 11.1 Navigation

* Home hub CTAs:

  * Campaign
  * Dungeon
  * Roster
  * Quests
  * **Summon** (post-MVP)
  * Shop (post-MVP)

### 11.2 Required screens (MVP)

1. Title/Load
2. Home
3. Campaign Map
4. Battle
5. Victory/Defeat
6. Roster
7. Unit Detail
8. Upgrade (level/skills/gear)
9. Dungeon Select
10. Daily Quests

### 11.3 Additional screens (Gacha phase)

11. Summon / Banner
12. Pity/Rate info modal
13. Duplicate conversion / Shards
14. Shop / Bundles
15. Mailbox (for compensation + event rewards)

### 11.4 UI rules

* Max 3 clicks from Home to “Start Battle”.
* Always show stamina cost, expected rewards, recommended power.
* Consistent status iconography + tooltips.

---

## 12) Art direction (2.5D, solo-feasible)

### 12.1 Visual style

* Stylized 3D characters (mid/low poly) with bold silhouettes.
* Environments: modular stage pieces, minimal clutter.
* VFX: readable, layered, and short-lived.

### 12.2 Asset list (MVP)

* 16 unit portraits (UI)
* 16 unit 3D models (or fewer if you reuse rigs heavily)
* 30 icons (statuses, currencies, UI)
* 10 VFX (hit/heal/shield/stun/burn/ult burst)
* 3 environments (campaign/dungeon/boss)

### 12.3 Animation scope

* Idle + basic attack + ultimate + hit reaction + KO
* Prefer shared rigs/retargeting.

* **Animation pipeline:** Hybrid

  * Prototype uses purchased humanoid rigs/animations for iteration speed.
  * Custom animation is reserved for **Ultimates**, signature moves, and key moments after the Vertical Slice.
---

## 13) Audio

* UI clicks, confirm/cancel
* 6 combat SFX categories (hit/crit/heal/shield/debuff/ult)
* 2 music tracks (menu/battle)

---

## 14) Narrative & world (original IP constraints)

* Original universe only.
* Chapter-level conflict + boss archetype.
* Short bios for collection appeal.

---

## 15) Technical design (Unity 6.3 LTS, URP)

### 15.1 Project organization

* Consistent folder/naming conventions. ([Unity][6])
  Suggested root:
* `Assets/_Project/`

  * `Art/`
  * `Audio/`
  * `Prefabs/`
  * `Scenes/`
  * `Scripts/`
  * `UI/`
  * `Data/` (ScriptableObjects)
  * `Addressables/` (when enabled)
  * `Tests/`
  * `ThirdParty/`

### 15.2 Data-driven architecture (ScriptableObjects)

Use ScriptableObjects for:

* `UnitDefinitionSO`
* `SkillDefinitionSO`
* `StatusEffectSO`
* `EncounterSO`
* `StageSO`
* `DropTableSO`
* `ItemDefinitionSO`
* `ProgressionCurveSO`

Runtime models (plain C#):

* `UnitRuntimeState`
* `BattleState`
* `PlayerProfile`

### 15.3 Addressables strategy (defer until needed)

* Move large content (models, textures, VFX, portraits, environments) to Addressables later.
* Plan groups/labels/update pipeline early. ([Unity][3])

### 15.4 Deterministic combat requirement

* One combat resolver:

  * input: `BattleState + Commands`
  * output: `BattleEvents` (for playback)
* Seeded RNG:

  * MVP offline: `seed = hash(stageId, runIndex)`
  * Online later: `seed = hash(stageId, playerId, serverNonce)`

### 15.5 Testing & validation

* Unit tests: damage, status stacking, turn order, energy rules.
* Golden tests: known seed → same battle log.

### 15.6 Performance targets (PC-first)

* PC target: stable 60 FPS in menus and battles at 1080p baseline.
* Avoid GC allocations in combat loop.
* Profile early and regularly. ([Unity][7])

---

## 16) Live-ops & analytics (post-MVP)

### 16.1 Events system

* Remote-configurable:

  * reward multipliers
  * rotating dungeon schedules
  * shop offers
* Seasonal resets.

### 16.2 Analytics

* Funnel: tutorial completion → chapter 1 clear → first upgrade → chapter 2 clear
* Battle: avg turns, TTK, ultimate usage
* Economy: currency inflow/outflow, bottlenecks
* Gacha: banner conversion, pity reach rates, duplicate rates

---

## 17) Gacha design (post-core-loop, but specified now)

### 17.1 Banner types

* **Standard banner** (permanent pool)
* **Featured banner** (limited time; featured unit rate-up)
* Optional later: **weapon/gear banner** (avoid initially)

### 17.2 Summon currencies

* **Premium currency** (paid/earned)
* **Tickets** (earned/event-specific)

### 17.3 Probability model (structure, not numbers yet)

* Rarity tier probabilities (C/R/E/L)
* “Featured” within top rarity
* Must include **rate disclosure UI** and pity rules.

### 17.4 Pity / guarantees (recommended)

* **Soft pity:** increased odds after N pulls without top rarity
* **Hard pity:** guaranteed top rarity at N pulls
* **Featured guarantee:** e.g., if you lose 50/50 once, next is guaranteed (optional, but retention-friendly)

### 17.5 Duplicates

* Duplicates convert to:

  * **Shards** for ascension/limit breaks
  * **Stardust** (shop currency) for targeted purchases

### 17.6 Anti-frustration safeguards (solo-friendly)

* First-week onboarding ensures at least 1 high-rarity unit.
* Early banner pool constrained to reduce “dead pulls”.

---

## 18) Content pipeline

### 18.1 Adding a new unit (goal: < 30 minutes)

1. Create `UnitDefinitionSO`
2. Create 2–3 `SkillDefinitionSO`
3. Assign VFX/SFX references
4. Add to banner/shop (later) or unlock table
5. Add to test encounter + run simulator batch

### 18.2 Encounter authoring

* Authored as assets:

  * enemy lineup + slot positions
  * AI profile
  * reward table
  * recommended power
  * modifiers (boss mechanics)

---

## 19) Tutorial & onboarding (MVP)

* 5-minute tutorial:

  1. Basic attack + turn order
  2. Energy + ultimate
  3. Status effect (stun/burn)
  4. Upgrade level
  5. Equip gear
* (Post-MVP) Gacha tutorial:

  * rate disclosure + pity explanation + first summon

---

## 20) Accessibility & localization

* Colorblind-friendly icons
* Scalable text
* Localization-ready strings

---

## 21) Production plan (solo dev)

### 21.1 Milestones

1. **Vertical Slice (2–3 weeks)**

   * 3v3 battle + energy + 6 units + one campaign node (2.5D URP baseline)
2. **MVP Content (4–8 weeks)**

   * 12–16 units, 2 chapters, dungeon, upgrade loop, quests
3. **Polish (2–4 weeks)**

   * VFX/audio pass, UX pass, optimization, bugfix
4. **Gacha Integration (2–4 weeks, after stability)**

   * summon UI + pity + duplicate conversion + shop basics
5. **Post-MVP**

   * Async Arena, events, more content, begin groundwork for 6v6

* **MVP Content:** 3v3 remains the default format.
* **Arena Integration:** ship Arena on 3v3 (or 4v4 if you’ve already stabilized it).
* **Team size expansion:**

  * **4v4** after core PvE + UI stability (internal milestone)
* **6v6** only **after Arena is stable and live** (balance + UX + performance pass complete)

### 21.2 Risks & mitigations

* **Scope creep:** no real-time PvP/co-op in MVP.
* **Balance drift (gacha):** simulator harness + controlled early pool; avoid too many systems at once.
* **Content burden in 3D:** reuse rigs, modular VFX, consistent camera to reduce animation load.

---

# 22) Open Questions (Resolved)

## 22.1 Camera choice: Orthographic vs Locked Perspective

**Decision:** **Orthographic camera in a 3D (URP) environment.**

**Rationale:**

* Matches anime / 2.5D expectations.
* Consistent character scale and cleaner UI composition.
* Lower performance overhead on mobile.
* Still supports 3D VFX, particles, and depth layering via Z-axis.

**Constraint:** Locked Perspective is considered only if the project shifts to cinematic 3D combat (out of MVP scope).

---

## 22.2 Art pipeline: purchased base rigs/animations vs fully custom

**Decision:** **Hybrid pipeline.**

**Early development / prototype:**

* Use purchased base rigs & animations (humanoid retargeting where applicable).
* Goal: speed, iteration, combat feel validation.

**Post-vertical slice / polish:**

* Gradual replacement with custom animations for:

  * Ultimate skills
  * Hero-defining moves
  * Key narrative moments

**Rationale:**

* Reduces upfront cost and production risk.
* Faster tuning of battle timing and VFX synchronization.
* Custom work is focused where players notice it most.

---

## 22.3 6v6 timing: when to upgrade from smaller team size

**Decision:** Upgrade to **6v6 after Arena is stable and live**.

**Proposed progression:**

* **Vertical Slice / MVP:** 3v3
* **Core PvE + UI stability:** 4v4
* **Arena launch + balance pass:** unlock 6v6

**Rationale:**

* Smaller team sizes improve readability, pacing, and early balance.
* 6v6 increases:

  * interaction complexity
  * turn duration
  * performance cost (especially mobile)
* Arena best justifies deeper composition and strategic depth.

---

## 22.4 Gacha numbers: rates, pity, currency income

**Decision:** **Deferred** until Vertical Slice + MVP feel test.

**Current stance:**

* Define rarity tiers first (**R / SR / SSR**).
* Validate:

  * pull excitement
  * acquisition pacing
  * progression friction

**Finalize after:**

* Core combat feel is locked
* Session length & early retention targets are measured
* Economy simulations are run

**Rationale:**

* Early gacha tuning without gameplay data causes rework.
* Rates, pity thresholds, and currency income must be tuned together.

---

## Appendix A — Concrete MVP feature checklist

* [ ] URP 2.5D battle scene with fixed camera + slot anchors
* [ ] Combat resolver + battle events playback
* [ ] 6 statuses (shield/taunt/stun/burn/atkUp/defDown)
* [ ] 12–16 units
* [ ] Campaign chapter structure + boss
* [ ] Dungeon rotation (simple)
* [ ] Upgrade UI (level/skills/gear)
* [ ] Save/load profile
* [ ] Auto-battle + speed controls
* [ ] Basic settings (audio, language stub)

---

### Sources (as provided)

[1]: https://docs.unity3d.com/6000.3/Documentation/Manual/UnityManual.html?utm_source=chatgpt.com "Unity - Manual: Unity 6.3 User Manual"
[2]: https://unity.com/how-to/architect-game-code-scriptable-objects?utm_source=chatgpt.com "Three ways to architect your game with ScriptableObjects"
[3]: https://unity.com/blog/engine-platform/addressables-planning-and-best-practices?utm_source=chatgpt.com "Addressables: Planning and best practices"
[4]: https://www.gitbook.com/blog/how-to-write-a-game-design-document?utm_source=chatgpt.com "How to write a game design document — with examples ..."
[5]: https://dl.acm.org/doi/abs/10.1145/3675807?utm_source=chatgpt.com "On Video Game Balancing: Joining Player- and Data- ..."
[6]: https://unity.com/how-to/organizing-your-project?utm_source=chatgpt.com "Best practices for organizing your Unity project"
[7]: https://unity.com/how-to?utm_source=chatgpt.com "Explore Unity's best practices"

If you answer the 4 open questions (camera, art pipeline, 6v6 timing, gacha numbers later), I can lock this into a **production-ready spec**: exact slot coordinates conventions, camera parameters, ScriptableObject field schemas, and a concrete Unity folder + scene architecture for the vertical slice.
