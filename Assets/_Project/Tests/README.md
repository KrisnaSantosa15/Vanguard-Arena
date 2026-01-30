# Vanguard Arena - Testing Infrastructure

## Phase 1: Foundation Complete ✅

This directory contains the test infrastructure for Vanguard Arena's battle system.

### Created Files

#### Test Infrastructure (`Assets/_Project/Tests/`)
- **Assembly Definitions**
  - `VanguardArena.Tests.EditMode.asmdef` - EditMode tests (fast, no Unity runtime)
  - `VanguardArena.Tests.PlayMode.asmdef` - PlayMode tests (integration, requires scene)

#### Test Utilities (`Assets/_Project/Tests/Utils/`)
- **BattleTestBuilder.cs** - Programmatic battle setup without ScriptableObjects
  - Simple factories: `Simple1v1Melee()`, `Simple2v2Mixed()`
  - Fluent API: `AddPlayerUnit()`, `AddEnemyUnit()`, `WithSeed()`
  - Returns `(players, enemies, rng)` for pure unit tests

- **InvariantChecker.cs** - Automated battle state validation
  - 27 invariants across 6 categories (Health, Shield, Status Effects, Energy, Cooldown, Targeting)
  - `CheckAll()` returns list of violations
  - `IsValid()` quick boolean check

- **BattleLogParser.cs** - Structured log analysis
  - Parses `battle_debug.log` into `BattleLogEvent` objects
  - Extracts structured data from log messages
  - Query helpers: `ContainsEvent()`, `GetTurnOrder()`, `CountPassiveTargets()`

#### Domain Refactoring (`Assets/_Project/Scripts/Domain/`)
- **IDeterministicRandom.cs** - Interface for testable RNG
  - `Next(min, max)` - Integer range
  - `Range(min, max)` - Float range
  - `Value()` - 0-1 float

- **SeededRandom.cs** - Deterministic RNG for tests
  - Uses `System.Random` with fixed seed
  - Produces identical results for same seed

- **UnityRandomWrapper.cs** - Production RNG wrapper
  - Delegates to `UnityEngine.Random`
  - Used by default in `CombatCalculator`

- **CombatCalculator.cs** (REFACTORED)
  - All methods now accept optional `IDeterministicRandom rng` parameter
  - Backward compatible: defaults to `UnityRandomWrapper` if not provided
  - Testable: inject `SeededRandom` for deterministic tests

#### Unit Tests (`Assets/_Project/Tests/EditMode/Domain/`)
- **CombatCalculatorTests.cs** - 15 tests covering damage/heal/shield formulas
  - Determinism tests (same seed = same result)
  - Boundary tests (minimum damage = 1)
  - Scaling tests (ATK/DEF/multipliers)
  - Crit rate tests (0%, 100%, damage increase)
  - Variance bounds tests (0.95-1.05 range)

---

## Test Categories

### ✅ EditMode Tests (Unit Tests)
- **Fast**: Run in <1 second
- **No Unity Runtime**: Pure C# logic
- **Deterministic**: Seeded RNG
- **Coverage**: Domain logic (CombatCalculator, StatusEffects, Energy)

**Location**: `Assets/_Project/Tests/EditMode/`

**Run**: Unity Test Runner → EditMode

### 🚧 PlayMode Tests (Integration Tests)
- **Slower**: Require Unity runtime (~2 min total)
- **Scene-based**: Full battle simulation
- **Coverage**: BattleController, BattleUnitManager, turn flow

**Location**: `Assets/_Project/Tests/PlayMode/`

**Run**: Unity Test Runner → PlayMode

---

## How to Run Tests

### In Unity Editor
1. Open **Window > General > Test Runner**
2. Click **EditMode** tab
3. Click **Run All** (should see 15 tests pass in CombatCalculatorTests)

### Expected Results (Phase 1)
```
✓ EditMode Tests: 15/15 passing
  ✓ CombatCalculatorTests (15 tests)
    ✓ Determinism tests (2)
    ✓ Boundary tests (3)
    ✓ Scaling tests (5)
    ✓ Crit tests (3)
    ✓ Variance tests (2)

✗ PlayMode Tests: 0 tests (not implemented yet)
```

---

## Next Steps (Phase 2-6)

### Phase 2: More Unit Tests
- [ ] `TeamEnergyStateTests.cs` - Energy system tests
- [ ] `UnitRuntimeStateTests.cs` - Status effect stacking
- [ ] `StatusEffectTests.cs` - Duration, expiry, modifiers

### Phase 3: Integration Tests
- [ ] `BasicAttackFlowTests.cs` - Energy gain, multi-hit, crit
- [ ] `UltimateFlowTests.cs` - Cooldown, energy spend, AOE
- [ ] `PassiveTriggerTests.cs` - All 8 passive types
- [ ] `StatusEffectFlowTests.cs` - Shield absorption, burn DoT, stun
- [ ] `TargetingTests.cs` - All 13 target patterns
- [ ] `VictoryConditionTests.cs` - Battle end detection

### Phase 4: Combinatorial Testing
- [ ] Generate pairwise parameter matrix (NIST ACTS tool)
- [ ] Create 20-40 test cases covering all config combinations
- [ ] Automated invariant checking per test
- [ ] Coverage report generation

### Phase 5: Regression Suite
- [ ] Capture 5-10 golden battle scenarios
- [ ] Log-based regression validation
- [ ] Detect unintended behavior changes

### Phase 6: CI/CD Integration
- [ ] GitHub Actions / GitLab CI setup
- [ ] Automated test runs on commit
- [ ] Coverage tracking over time

---

## Architecture Notes

### Determinism Strategy
**Problem**: Two RNG sources caused non-reproducible tests
- `UnityEngine.Random` in `CombatCalculator` (damage variance, crit)
- `System.Random _rng` in `BattleController` (target selection)

**Solution**: `IDeterministicRandom` interface
- Production: `UnityRandomWrapper` delegates to Unity
- Tests: `SeededRandom` uses `System.Random(seed)`
- All RNG calls now injectable and testable

### Test Data Strategy
**Problem**: Tests require complex `UnitDefinitionSO` ScriptableObjects

**Solution**: `BattleTestBuilder`
- Creates `UnitDefinitionSO` programmatically via `ScriptableObject.CreateInstance<T>()`
- No asset files needed for tests
- Fluent API for readable test setup

### Invariant Checking Strategy
**Problem**: 1,456+ config combinations, can't manually verify each

**Solution**: Define universal invariants
- 27 invariants that must ALWAYS be true
- Checked automatically after every test
- Violations logged with unit name and details

### Log-Based Verification
**Problem**: Integration tests need to verify complex sequences

**Solution**: Parse existing comprehensive logs
- `battle_debug.log` already captures every event
- `BattleLogParser` extracts structured data
- Tests verify expected events occurred in correct order

---

## Test Metrics (Goals)

### Coverage Targets
- **Code Coverage**: 80%+ on Domain layer
- **Config Coverage**: 95%+ (target patterns, passives, effects)
- **Execution Time**: <10 min total
  - EditMode: <5s
  - PlayMode: <2min
  - Combinatorial: <10min

### Invariant Violations
- **Goal**: 0 violations in CI
- **Reality Check**: Expect some violations initially, fix systematically

### Test Count (Final)
- **Unit Tests**: 50+ (EditMode)
- **Integration Tests**: 100+ (PlayMode)
- **Combinatorial Tests**: 40 (PlayMode)
- **Regression Tests**: 10 (PlayMode)
- **Total**: 200+ tests

---

## Useful Commands

### View Battle Logs
```bash
# On Windows
type battle_debug.log

# On macOS/Linux
cat battle_debug.log
```

### Run Tests from Command Line (Unity Cloud Build)
```bash
# EditMode tests only
unity -runTests -batchmode -projectPath . -testPlatform EditMode -testResults EditModeResults.xml

# PlayMode tests only
unity -runTests -batchmode -projectPath . -testPlatform PlayMode -testResults PlayModeResults.xml
```

---

## Key Insights from Testing Strategy

### Why Combinatorial Testing?
**NIST Research**: 70-90% of bugs found with 2-way parameter interactions
- **Exhaustive**: 13 patterns × 8 passives × 7 effects = 728 combinations (per unit type)
- **Pairwise**: 20-40 test cases cover all pairs
- **Industry Standard**: NASA, Microsoft, Lockheed Martin use this

### Why Invariants?
**Formal Verification Lite**: Define "laws" that must always hold
- Easier than writing thousands of assertions
- Catches edge cases humans miss
- Self-documenting (invariant names explain expected behavior)

### Why Log-Based Testing?
**Comprehensive Observability**: Logs already capture everything
- No need to instrument code with test hooks
- Tests are non-invasive
- Can add tests retroactively by analyzing logs

---

## Files Modified This Session

### New Files (9)
1. `Assets/_Project/Tests/EditMode/VanguardArena.Tests.EditMode.asmdef`
2. `Assets/_Project/Tests/PlayMode/VanguardArena.Tests.PlayMode.asmdef`
3. `Assets/_Project/Tests/Utils/BattleTestBuilder.cs`
4. `Assets/_Project/Tests/Utils/InvariantChecker.cs`
5. `Assets/_Project/Tests/Utils/BattleLogParser.cs`
6. `Assets/_Project/Scripts/Domain/IDeterministicRandom.cs`
7. `Assets/_Project/Scripts/Domain/SeededRandom.cs`
8. `Assets/_Project/Scripts/Domain/UnityRandomWrapper.cs`
9. `Assets/_Project/Tests/EditMode/Domain/CombatCalculatorTests.cs`

### Modified Files (1)
1. `Assets/_Project/Scripts/Domain/CombatCalculator.cs` - Added optional `rng` parameter to all methods

### Test Directories Created
```
Assets/_Project/Tests/
├── EditMode/
│   ├── Domain/              # Unit tests
│   │   └── CombatCalculatorTests.cs ✅
│   └── Utils/               # Future utility tests
├── PlayMode/
│   ├── Integration/         # Integration tests (TBD)
│   ├── Combinatorial/       # Generated tests (TBD)
│   └── Regression/          # Golden scenario tests (TBD)
└── Utils/                   # Shared test utilities
    ├── BattleTestBuilder.cs
    ├── InvariantChecker.cs
    └── BattleLogParser.cs
```

---

## Important Notes

### Unity Test Framework Version
- **Installed**: `com.unity.test-framework` v1.6.0
- **NUnit Version**: 3.13.x (bundled)
- **Compatibility**: Unity 6.3 LTS (6000.3.5f1)

### Before Running Tests
1. Unity must be in Edit Mode (not Play Mode)
2. Test Runner window: `Window > General > Test Runner`
3. First run may take 5-10s for compilation
4. Subsequent runs: <1s for EditMode tests

### Known Limitations
- `BattleTestBuilder` creates runtime states only (no GameObjects/Views)
- Integration tests require Unity runtime (slower)
- Log parser assumes specific log format (fragile to format changes)

---

## Success Criteria (Phase 1) ✅

- [x] BattleTestBuilder can create 1v1 battles programmatically
- [x] Deterministic RNG produces identical results with same seed
- [x] InvariantChecker detects violations correctly
- [x] Log parser extracts all event types
- [x] 15+ unit tests passing for CombatCalculator
- [x] Zero compilation errors
- [x] README documents approach

**Status**: Phase 1 Complete! Ready for Phase 2 (More Unit Tests).

---

## Contact / Questions

For testing strategy questions, see:
- `TESTING_STRATEGY.md` - Comprehensive testing overview
- `battle-testing-strategy.md` - Detailed implementation plan
- `UNIT_CONFIGURATIONS.md` - All unit configs exported

For bugs in tests:
1. Check Unity Console for errors
2. Verify Test Runner is in correct mode (EditMode vs PlayMode)
3. Check assembly definition references
