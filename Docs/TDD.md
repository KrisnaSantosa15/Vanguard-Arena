# Tech Design Document (TDD) — 2.5D Turn-Based Squad RPG

**Unity 6.3 LTS • URP • Orthographic Camera • PC-First (Steam) • Addressables-First • Firebase Backend • UI Toolkit**

---

## 0) Document control

* **Version:** 0.1 (Living Document)
* **Owner:** Solo Dev
* **Engine:** Unity **6.3 LTS**
* **Renderer:** **URP** (3D)
* **Camera:** **Orthographic** + slight dynamic framing
* **UI:** **UI Toolkit** (runtime) + custom animation approach

  * Follow Unity’s UI Toolkit best-practice guide and performance guidance. ([Unity Documentation][1])
* **Input:** New **Unity Input System** ([Unity Documentation][2])
* **Asset delivery:** **Addressables from day 1** + content update workflow ([Unity][3])
* **Backend (planned early):** **Firebase** (Auth + Firestore + Storage + Remote Config + Analytics) ([Firebase][4])
* **Distribution:** Steam primary (Windows)
* **Save format:** JSON (offline-first) + cloud sync later (hard requirement)
* **Gacha:** Server-authoritative pulls (Firebase-backed) — rates/pity deferred

---

## 1) Goals and non-goals

### 1.1 Goals

* **Deterministic combat core** (reproducible battle logs via seed + commands).
* **Data-driven content** via ScriptableObjects + Addressables.
* **UI Toolkit-native UI architecture** with performant styling and animation rules. ([Unity Documentation][5])
* **Offline-first** playable MVP with **optional login**; later supports **cross-device sync (PC ↔ mobile)**.
* **Server-authoritative gacha** (anti-tamper) with auditability and analytics.
* **Steam-ready build pipeline** and CI via GitHub Actions.

### 1.2 Non-goals (MVP)

* Real-time multiplayer / co-op.
* Fully cinematic camera system.
* Live content authoring in runtime editor.
* Anti-cheat beyond server-authoritative actions (gacha/purchases) + basic integrity checks.

---

## 2) Target environment and budgets

### 2.1 PC baseline spec (performance target)

* CPU: i5-8400 / Ryzen 5 2600
* GPU: GTX 1060 / RX 580
* RAM: 8 GB
* Target: **1080p @ 60 FPS** menus and battles.

### 2.2 Quality tiers (URP)

* **Tier 0 (Low / later mobile):** minimal post, baked lighting where possible.
* **Tier 1 (PC baseline):** lightweight post (bloom), modest particles, limited realtime lights.
* **Tier 2 (High):** enhanced shadows / effects; optional.

---

## 3) Technology stack

### 3.1 Unity packages (core)

* **URP**
* **Input System** ([Unity Documentation][2])
* **UI Toolkit** (runtime) best-practice guide reference ([Unity Documentation][1])
* **Addressables** + Content Update workflow ([Unity][3])
* **Test Framework** (EditMode + PlayMode)
* **Localization** (post-MVP or early stub)
* **Newtonsoft.Json** (or System.Text.Json if feasible; Newtonsoft is common in Unity pipelines)

### 3.2 Backend (Firebase)

* **Firebase Auth** (optional login early) ([Firebase][6])
* **Cloud Firestore** (profiles, inventory, gacha ledger) ([Firebase][7])
* **Cloud Storage** (optional: screenshots, cosmetics metadata, remote assets metadata) ([Firebase][8])
* **Remote Config** (tuning knobs; flags; A/B segments) ([Firebase][9])
* **Analytics** (basic events in MVP) ([Firebase][10])

> Note: Firebase Unity desktop support is commonly used for dev/testing; plan validation for your shipping target early. ([Firebase][4])

---

## 4) High-level architecture

### 4.1 Architectural style

* **Layered + event-driven**:

  * **Presentation** (UI Toolkit / VFX playback)
  * **Application** (use-cases / orchestration)
  * **Domain** (combat rules, progression rules)
  * **Infrastructure** (save/load, addressables, firebase, analytics)
* Combat follows a **“simulation + playback”** pattern:

  * **Simulation:** pure deterministic logic, no Unity objects.
  * **Playback:** consumes emitted battle events to drive animations/VFX.

### 4.2 Dependency rules

* Domain has **no dependency** on UnityEngine (except minimal math structs if needed; prefer plain C#).
* Infrastructure depends on Unity and Firebase SDKs.
* Presentation depends on Unity objects, Addressables, UI Toolkit.

---

## 5) Project structure (Unity)

### 5.1 Folder layout

`Assets/_Project/`

* `Art/` (models, textures, shaders, VFX graph if used)
* `Audio/`
* `Prefabs/`
* `Scenes/`
* `UI/` (UXML/USS, UI assets, UI controllers)
* `Scripts/`

  * `Domain/` (combat, rules, formulas)
  * `Application/` (use cases, services interfaces)
  * `Infrastructure/`

    * `Addressables/`
    * `Save/`
    * `Firebase/`
    * `Analytics/`
  * `Presentation/` (battle view, VFX bindings, UI presenters)
* `Data/` (ScriptableObjects)
* `Addressables/` (profiles/groups configs, labels strategy docs)
* `Tests/` (EditMode/PlayMode)
* `ThirdParty/`

### 5.2 Assembly Definitions (asmdef)

Create assemblies:

* `_Project.Domain`
* `_Project.Application`
* `_Project.Infrastructure`
* `_Project.Presentation`
* `_Project.Tests`

This keeps compile times manageable and enforces boundaries.

---

## 6) Scene architecture

### 6.1 Scenes (MVP)

1. `Boot`

   * Initializes: Addressables, Firebase (optional), Save system, Analytics, Remote Config.
2. `MainMenu`
3. `Home` (hub)
4. `CampaignMap`
5. `Battle`
6. `DungeonSelect`

### 6.2 Boot flow

* Show small loading UI.
* Initialize in order:

  1. Logging + crash capture (if used)
  2. Addressables init (profiles; catalog) ([Unity User Manual][11])
  3. Load local JSON save
  4. If user opted login: Firebase init + auth restore ([Firebase][4])
  5. Remote Config fetch + apply defaults fallback ([Firebase][9])
  6. Analytics session start ([Firebase][10])
  7. Enter menu

---

## 7) Data model and content system

### 7.1 ScriptableObjects (authoring)

* `UnitDefinitionSO`

  * id (string GUID)
  * displayNameKey (loc key)
  * role, faction, rarity
  * baseStats (HP/ATK/DEF/SPD)
  * growthCurves references
  * skillRefs (basic/ult/passive)
  * prefabRef (Addressable key)
  * portraitRef (Addressable key)
* `SkillDefinitionSO`

  * id, nameKey, descriptionKey
  * targetRule (enum)
  * costEnergy, cooldownTurns
  * effectList (list of EffectDefinition)
  * vfxRef, sfxRef, animationTag
* `StatusEffectSO`

  * id, iconRef, stackingRule, durationType
  * tickTiming (turnStart/turnEnd)
* `StageSO`

  * stageId, staminaCost, recommendedPower
  * encounterRefs (list)
  * dropTableRef
* `DropTableSO`

  * weighted rewards + pity-like guarantees (for drops, not gacha)

### 7.2 Runtime state (pure C#)

* `PlayerProfile`

  * playerId (local GUID), accountId (firebase uid optional)
  * currencies, stamina timers
  * roster inventory (unit instances)
  * progression (campaign clears, dungeon tiers)
* `UnitInstance`

  * unitDefId, level, skillLevels, gear, ascension
* `BattleState`

  * seed, rngState
  * team states, energy, timeline, active statuses
* `BattleCommand`

  * actorId, actionType, targetSelector, chosenTargets (optional)
* `BattleEvent`

  * eventType (Damage/Heal/ApplyStatus/SpendEnergy/etc.)
  * payload (ids, values, status, duration)

---

## 8) Deterministic combat system

### 8.1 Core requirements

* Same inputs (seed + commands) → same results.
* Combat logic runs in **fixed steps**:

  1. Start battle (apply passives, init energy)
  2. Determine next actor by SPD rules
  3. Resolve action
  4. Emit BattleEvents
  5. Update timeline/status durations
  6. End battle check

### 8.2 RNG strategy

* Use a custom deterministic RNG (e.g., Xorshift/PCG) implemented in Domain layer.
* Store RNG state in `BattleState`.

### 8.3 Simulation vs Playback

* **Simulation outputs events** only.
* **Playback** consumes events to:

  * animate character
  * spawn VFX
  * update UI numbers
* This enables:

  * fast auto-sim for balance
  * replays
  * headless tests

---

## 9) 2.5D battle presentation (URP + orthographic)

### 9.1 Camera spec (initial)

* Orthographic camera
* Slight dynamic framing:

  * small zoom-in on Ult (lerp ortho size)
  * limited screen shake (amplitude capped)
  * never moves UI anchor points

### 9.2 Slot anchoring

* Create transforms:

  * `PlayerSlots_Front[3]`, `PlayerSlots_Back[3]`
  * `EnemySlots_Front[3]`, `EnemySlots_Back[3]`
* MVP uses Front[3] only; Back slots disabled.
* Later:

  * 4v4 uses Front[2]+Back[2] (configurable)
  * 6v6 uses all 6

### 9.3 VFX layering

* Depth via Z offsets:

  * front-row characters slightly closer than back-row
  * VFX spawned at dedicated “VFX anchor bones” on rigs

---

## 10) UI system (UI Toolkit)

### 10.1 UI architecture

* **UXML + USS as source of truth**; avoid heavy inline styles for memory/perf. ([Unity Documentation][5])
* Separate UI into **Screens** (full pages) and **Panels** (embedded components).
* Recommended layers:

  * `UIScreenController` (navigation)
  * `Presenter` (binds model ↔ view)
  * `ViewModel` (pure C# data, observable pattern)

### 10.2 Performance rules (UI Toolkit)

* Prefer USS files; minimize per-element inline styles. ([Unity Documentation][5])
* Avoid animating layout-heavy properties (size/position) if it triggers costly layout; prefer transforms/opacity patterns per Unity guidance. ([Unity Documentation][12])
* Use templates and reusable components; keep UXML attributes primitive and data binding clean. ([Unity Discussions][13])

### 10.3 Custom animation approach

* Implement a lightweight UI animation service:

  * property tweens for opacity/scale/translate
  * timeline sequences for screen transitions
* Keep animation state in Presenter layer; view only exposes hooks.

---

## 11) Input system

### 11.1 Input Actions

* Use **Input System** with action maps:

  * `UI` map: navigate, confirm, cancel, pointer
  * `Battle` map: select target, confirm action, toggle speed, toggle auto
* PC: keyboard/mouse baseline; controller later.

### 11.2 Routing

* Central `InputRouter` reads actions and forwards to active screen/battle controller.
* No direct Input polling inside Domain.

(Reference: Unity Input System package docs.) ([Unity Documentation][2])

---

## 12) Save/Load and cross-device sync

### 12.1 Local save (offline-first)

* **JSON** save stored under persistent data path.
* Versioned schema:

  * `saveVersion`
  * migration functions: `Migrate_vN_to_vN+1`
* Split files:

  * `profile.json` (core)
  * `inventory.json` (units/items)
  * `settings.json`

### 12.2 Cloud sync (hard requirement)

* On login:

  * Load local profile
  * Fetch cloud profile
  * Merge strategy (deterministic):

    * Use “lastWriteTime” + conflict rules per field
    * Always preserve purchase/gacha ledger from server
* Store in **Firestore**:

  * `/players/{uid}/profile`
  * `/players/{uid}/inventory`
  * `/players/{uid}/progress`

(Firestore + setup refs.) ([Firebase][7])

---

## 13) Firebase backend design (Auth + Firestore + Remote Config + Analytics)

### 13.1 Authentication (optional early)

* Support:

  * Anonymous or email/password for PC MVP
  * Google sign-in later (mobile) if desired ([Firebase][6])
* Auth restore on boot; allow “Play Offline”.

### 13.2 Remote Config (tuning + feature flags)

* Use Remote Config for:

  * drop rates (non-gacha)
  * stamina regen values
  * event flags
  * UX experiments
* Always ship **in-app defaults** and override from server. ([Firebase][9])
* Use Analytics audiences/properties to segment experiments. ([Firebase][10])

### 13.3 Analytics (MVP basic events)

Minimum event set:

* `session_start`, `session_end`
* `battle_start`, `battle_end` (result, turns, duration)
* `stage_start`, `stage_complete`
* `upgrade_unit`, `upgrade_skill`, `equip_gear`
* `login_method` (offline/anonymous/account)
* Later gacha:

  * `gacha_open`, `gacha_pull` (bannerId, pullCount, resultRarity)

(Analytics + Remote Config integration ref.) ([Firebase][10])

---

## 14) Addressables strategy (immediate adoption)

### 14.1 Principles

* Plan groups/labels early to support patching and minimize redownloads. ([Unity][3])
* Use profiles for local vs remote paths. ([Unity User Manual][11])

### 14.2 Grouping (recommended initial)

* `core_ui` (UXML/USS/icons needed at boot) — local
* `core_battle` (shared VFX/SFX/basic rigs) — local
* `units_common` (shared humanoid rig/animations) — local
* `units_heroes_pack01` — remote
* `units_villains_pack01` — remote
* `environments_pack01` — remote
* `events_packXX` — remote

### 14.3 Labels

* `boot_required`
* `battle_required`
* `unit:<id>`
* `env:<id>`
* `vfx:<type>`

### 14.4 Content updates workflow

* Use Addressables content update workflow and restriction checks before patching. ([Unity Documentation][14])
* Maintain build layout reports for bundle analysis. ([Unity User Manual][11])

---

## 15) Gacha system (server-authoritative)

### 15.1 Core rule

* The client **never** determines pull outcomes.
* Client requests:

  * `bannerId`, `pullCount`, `clientSessionId`
* Server returns:

  * pull results + updated pity counters + ledger entry id

### 15.2 Firebase implementation approach

* Use **Cloud Functions** (recommended) to:

  * validate request
  * check currency balance
  * apply pity logic
  * write immutable ledger
  * return results
* Data model (Firestore):

  * `/banners/{bannerId}` (rates, pool, rules) — remote config or firestore
  * `/players/{uid}/wallet`
  * `/players/{uid}/gachaState` (pity counters)
  * `/players/{uid}/gachaLedger/{pullId}` (append-only)

### 15.3 Anti-tamper essentials

* Wallet updates only by server (functions).
* Ledger is append-only; client reads but cannot write without rules.
* Analytics records both request and result summary.

> Rates/pity values remain deferred; only structure is implemented now.

---

## 16) Build, distribution, and CI (GitHub Actions)

### 16.1 Version control

* Git + **Git LFS** for binaries (models/textures/audio). ([Unity Learn][15])

### 16.2 Branching

* `main` = stable
* `dev` = integration
* `feature/*` branches

### 16.3 GitHub Actions pipeline (Windows)

Stages:

1. Checkout + LFS fetch
2. Cache Unity Library (keyed by Unity version + manifest)
3. Run EditMode tests
4. Run PlayMode smoke tests (optional)
5. Build Windows player
6. Upload artifacts (zip)
7. Optional: upload to Steam depot (later with SteamPipe secrets)

### 16.4 Build profiles

* `Development` (logs, debug overlays)
* `ReleaseCandidate`
* `Release`

---

## 17) Testing strategy

### 17.1 Domain tests (highest ROI)

* Damage formula tests
* Status stacking tests
* Turn order tests
* Energy economy tests
* Golden seed replay tests (same log)

### 17.2 Integration tests

* Save/load roundtrip
* Addressables load critical assets
* Firebase init (mocked where possible)

### 17.3 Performance testing

* Automated scene benchmark:

  * battle with 6 units + heavy VFX
  * measure frame time + GC allocs

---

## 18) Logging, diagnostics, and error handling

* Structured logs with categories:

  * `BOOT`, `ADDR`, `SAVE`, `BATTLE`, `UI`, `NET`, `GACHA`
* Crash logging strategy:

  * local logs + optional Crashlytics later (if you add it; not mandatory now)

---

## 19) Security and data integrity

* Local JSON is user-modifiable; treat as “untrusted”.
* Server authoritative for:

  * wallet/premium currency
  * gacha pulls
  * purchase receipts (later)
* Use Remote Config for tuning, but always fallback to defaults. ([Firebase][9])

---

## 20) Performance guidelines (PC-first, mobile later)

### 20.1 Combat loop

* No per-frame allocations during simulation or playback.
* Pool frequently spawned objects (damage numbers, VFX instances).

### 20.2 UI Toolkit

* Minimize inline styles; reuse USS; avoid layout-costly animations. ([Unity Documentation][5])

### 20.3 Addressables

* Avoid loading large packs at boot; load by label on demand.
* Prewarm core battle assets using `battle_required` label.

---

## 21) Implementation roadmap (engineering)

### Phase 1 — Vertical Slice (3v3)

* Domain combat resolver + battle event stream
* Battle playback system (3D slots + VFX)
* UI Toolkit battle HUD
* Local JSON save
* Addressables baseline groups/labels and boot-required pack
* CI builds + tests

### Phase 2 — MVP Content

* Campaign + dungeon flows
* Upgrade systems + inventory
* Firebase optional login + cloud sync (profile/inventory)
* Remote Config for tuning
* Analytics events baseline

### Phase 3 — Gacha Integration

* Cloud Functions pull endpoint
* Banner UI + inventory ingestion
* Ledger + wallet server authority

### Phase 4 — Arena + Team Expansion

* Async arena snapshots
* Expand to 4v4 then 6v6 once arena stable

---

## 22) References (best-practice sources used)

* Unity UI Toolkit best-practice guide + performance guidance. ([Unity Documentation][1])
* Unity Input System package documentation. ([Unity Documentation][2])
* Addressables planning, groups, overview, and content update workflow. ([Unity][3])
* Firebase Unity setup, Auth, Firestore, Storage, Remote Config, and Analytics integration. ([Firebase][4])
* Unity Learn guidance for Git LFS with Unity projects. ([Unity Learn][15])

---

If you want, I can follow this TDD with a **“Unity Implementation Blueprint”**: concrete ScriptableObject field schemas (exact C#), Addressables group/label table, Boot scene initialization order code skeleton, and a GitHub Actions YAML template tuned for Unity 6.3 LTS.

[1]: https://docs.unity3d.com/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/bpg-uiad-index.html?utm_source=chatgpt.com "Manual: UI Toolkit for advanced Unity developers"
[2]: https://docs.unity3d.com/Packages/com.unity.inputsystem%40latest/?utm_source=chatgpt.com "Input System | 1.18.0"
[3]: https://unity.com/blog/engine-platform/addressables-planning-and-best-practices?utm_source=chatgpt.com "Addressables: Planning and best practices"
[4]: https://firebase.google.com/docs/unity/setup?utm_source=chatgpt.com "Add Firebase to your Unity project"
[5]: https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-USS-WritingStyleSheets.html?utm_source=chatgpt.com "Best practices for USS"
[6]: https://firebase.google.com/docs/auth/unity/start?utm_source=chatgpt.com "Get Started with Firebase Authentication in Unity - Google"
[7]: https://firebase.google.com/docs/firestore/quickstart?utm_source=chatgpt.com "Get started with Cloud Firestore Standard edition - Firebase"
[8]: https://firebase.google.com/docs/storage/unity/start?utm_source=chatgpt.com "Get started with Cloud Storage for Unity - Firebase"
[9]: https://firebase.google.com/docs/remote-config?utm_source=chatgpt.com "Firebase Remote Config - Google"
[10]: https://firebase.google.com/docs/remote-config/config-analytics?utm_source=chatgpt.com "Use Firebase Remote Config with Analytics - Google"
[11]: https://docs.unity.cn/Packages/com.unity.addressables%402.2/manual/AddressableAssetsOverview.html?utm_source=chatgpt.com "Addressables overview | Addressables | 2.2.2"
[12]: https://docs.unity3d.com/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/optimizing-performance.html?utm_source=chatgpt.com "Manual: Optimizing performance"
[13]: https://discussions.unity.com/t/ui-toolkit-uxml-first-approach-for-runtime-doesnt-exist/1505433?utm_source=chatgpt.com "UI Toolkit UXML-first approach for runtime doesn't exist"
[14]: https://docs.unity3d.com/Packages/com.unity.addressables%401.20/manual/ContentUpdateWorkflow.html?utm_source=chatgpt.com "Content update builds | Addressables | 1.20.5"
[15]: https://learn.unity.com/course/collaborate-on-a-project-with-github-desktop/tutorial/set-up-git-lfs-to-manage-large-files-in-unity?version=6.2&utm_source=chatgpt.com "Set Up Git LFS to Manage Large Files in Unity"
