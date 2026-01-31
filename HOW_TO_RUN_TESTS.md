# How to Run Tests for Vanguard Arena

## Quick Start

### 1. Open Unity Test Runner
1. Open the project in **Unity 6000.3.5f1** (or compatible version)
2. Go to **Window → General → Test Runner**
3. Click the **EditMode** tab

### 2. Run All Tests
- Click **"Run All"** to execute all ~165 EditMode tests
- Tests should complete in 5-15 seconds
- All tests should pass ✅

### 3. Run Specific Test Suites
You can run individual test files by expanding the tree:
- `CombatCalculatorTests` (14 tests) - Damage formulas
- `TeamEnergyStateTests` (~20 tests) - Energy management
- `StatusEffectTests` (~15 tests) - Status effects
- `UnitRuntimeStateTests` (~15 tests) - Unit state
- `TurnManagerTests` (~24 tests) - Turn management
- `PassiveAbilityTests` (~30 tests) - Passive abilities
- `TargetResolverTests` (~15 tests) - Targeting patterns
- `ActionExecutorTests` (~12 tests) - Action execution
- `BattleSimulatorTests` (20 tests) - **NEW: Full battle simulation**

---

## Test Structure

### EditMode Tests (Fast Unit Tests)
Located in: `Assets\_Project\Tests\EditMode\Domain\`

These tests run **without** Unity's PlayMode runtime:
- ✅ Pure C# logic testing
- ✅ Deterministic with seeded randomness
- ✅ Fast execution (~5-15 seconds for all tests)
- ✅ No Unity scene/GameObject dependencies

**Domain Classes Tested**:
- `CombatCalculator.cs` - Damage formulas
- `TeamEnergyState.cs` - Energy management
- `UnitRuntimeState.cs` - Unit state & status effects
- `TurnManager.cs` - Turn order management
- `PassiveManager.cs` - Passive ability triggers
- `TargetResolver.cs` - Targeting patterns
- `ActionExecutor.cs` - Action execution flow
- `BattleSimulator.cs` - **NEW: Full battle simulation**

### PlayMode Tests (Integration Tests)
Located in: `Assets\_Project\Tests\PlayMode\` *(To be implemented)*

These tests run **with** Unity's PlayMode runtime:
- Scene loading and Unity lifecycle
- MonoBehaviour components (`BattleController`, UI, etc.)
- Animation, visual effects, and audio
- Full end-to-end battle scenarios with Unity graphics

---

## New in This Update: BattleSimulator

### What is BattleSimulator?
`BattleSimulator.cs` is a new Domain class that runs **complete battles in memory** without Unity graphics. It's designed for:
- ✅ Fast integration testing
- ✅ AI development and training
- ✅ Balance testing (run 1000s of battles to test balance)
- ✅ Deterministic replay (seed-based randomness)

### BattleSimulator Test Coverage
The new `BattleSimulatorTests.cs` includes **20 integration tests**:

#### Victory Condition Tests (2 tests)
- `SimulateBattle_PlayerVictory_AllEnemiesDefeated`
- `SimulateBattle_PlayerVictory_MultipleEnemies`

#### Defeat Condition Tests (2 tests)
- `SimulateBattle_PlayerDefeat_AllPlayersDefeated`
- `SimulateBattle_PlayerDefeat_Outnumbered`

#### Turn Limit Tests (2 tests)
- `SimulateBattle_TurnLimit_ReachedWithoutVictory`
- `SimulateBattle_TurnLimit_DefaultMaxTurns`

#### Energy Accumulation Tests (2 tests)
- `SimulateBattle_EnergyAccumulation_IncreasesOverTurns`
- `SimulateBattle_EnergyAccumulation_EnablesUltimates`

#### Passive Ability Integration Tests (3 tests)
- `SimulateBattle_PassiveTriggers_OnTurnStart`
- `SimulateBattle_PassiveTriggers_OnDamageDealt`
- `SimulateBattle_PassiveTriggers_OnKill`

#### Speed-Based Turn Order Tests (1 test)
- `SimulateBattle_TurnOrder_RespectsSPDStat`

#### Multi-Unit Battle Tests (2 tests)
- `SimulateBattle_MultiUnit_3v3Battle`
- `SimulateBattle_MultiUnit_FocusFire`

#### Edge Case Tests (2 tests)
- `SimulateBattle_NoUnits_ReturnsDefeatImmediately`
- `SimulateBattle_DeterministicWithSeed_ProducesSameResult`

### Example: Running a Battle Simulation in Code

```csharp
using Project.Domain;
using VanguardArena.Tests.Utils;

// Create a 2v2 battle scenario
var builder = new BattleTestBuilder()
    .WithRandom(new SeededRandom(999))
    .AddPlayerUnit("Tank", UnitType.Melee, hp: 200, atk: 20, def: 10, spd: 10)
    .AddPlayerUnit("DPS", UnitType.Ranged, hp: 100, atk: 40, def: 5, spd: 15)
    .AddEnemyUnit("Enemy1", UnitType.Melee, hp: 150, atk: 25, def: 8, spd: 12)
    .AddEnemyUnit("Enemy2", UnitType.Ranged, hp: 80, atk: 30, def: 3, spd: 14);

var (players, enemies, rng) = builder.BuildStates();

// Run the battle simulation
var simulator = new BattleSimulator(players, enemies, rng, enableLogging: true);
var result = simulator.SimulateBattle(maxTurns: 50);

// Check the outcome
Debug.Log($"Battle Outcome: {result.Outcome}");
Debug.Log($"Turns Elapsed: {result.TurnsElapsed}");
Debug.Log($"Surviving Players: {result.SurvivingPlayers.Count}");
Debug.Log($"Surviving Enemies: {result.SurvivingEnemies.Count}");
```

---

## Troubleshooting

### Tests Not Appearing in Test Runner
1. Make sure you're in the **EditMode** tab (not PlayMode)
2. Click **"Refresh"** in the Test Runner window
3. Check that test files are in the correct directory: `Assets\_Project\Tests\EditMode\Domain\`
4. Verify test files have `.cs` extension and are properly named

### Tests Failing
1. Check the Unity Console (Ctrl+Shift+C) for compilation errors
2. Verify all Domain classes are present in `Assets\_Project\Scripts\Domain\`
3. Check that `BattleTestBuilder.cs` is in `Assets\_Project\Tests\Utils\`
4. Ensure you're using Unity 6000.3.5f1 or compatible version

### Slow Test Execution
- EditMode tests should complete in 5-15 seconds
- If tests are slow, check for infinite loops in Domain classes
- Use `enableLogging: true` in BattleSimulator to debug battle flow

---

## Running Tests from Command Line (Advanced)

If you need to run tests from command line (CI/CD pipelines):

```bash
# Windows (adjust Unity path to your installation)
"C:\Program Files\Unity\Hub\Editor\6000.3.5f1\Editor\Unity.exe" ^
  -runTests ^
  -batchmode ^
  -projectPath "D:\Projects\GameDevelopment\Unity\Vanguard Arena" ^
  -testResults "TestResults.xml" ^
  -testPlatform EditMode
```

```bash
# macOS
/Applications/Unity/Hub/Editor/6000.3.5f1/Unity.app/Contents/MacOS/Unity \
  -runTests \
  -batchmode \
  -projectPath "/path/to/Vanguard Arena" \
  -testResults "TestResults.xml" \
  -testPlatform EditMode
```

---

## Test Coverage Summary

### Current Coverage: ~95% of Domain Layer
- ✅ **Phase 1**: Core Math (14 tests)
- ✅ **Phase 2**: State & Turns (64 tests)
- ✅ **Phase 3**: Gameplay Logic (67 tests)
- ✅ **Phase 4**: Full Battle Simulation (20 tests)

**Total: ~165 EditMode tests**

### What's Tested
- Damage calculations (basic, critical, shields, healing)
- Energy management (gain, spend, overflow, caps)
- Status effects (buffs, debuffs, burn, shield, stun)
- Unit state (HP, stats, cooldowns, status effect ticks)
- Turn management (speed-based ordering, timeline rolling)
- Passive abilities (9 trigger types, all tested)
- Targeting patterns (13 patterns, all tested)
- Action execution (basic & ultimate, energy, cooldowns)
- Full battle simulation (victory/defeat, energy economy)

### What's NOT Tested Yet
- Unity UI layer (`BattleController.cs` presentation logic)
- Battle AI decision-making
- Animation and visual effects
- Audio system
- Save/Load system

---

## Next Development Steps

1. ✅ **Phase 4 Complete**: BattleSimulator implemented and tested
2. ⚠️ **Phase 5**: Refactor `BattleController.cs` to use Domain classes
3. ⚠️ **Phase 6**: Update AI to use `ActionExecutor`, `TargetResolver`, `PassiveManager`
4. ⚠️ **Phase 7**: Create PlayMode integration tests for Unity presentation layer

---

## Contact / Support

For questions about the test suite or Domain architecture:
- Check `TEST_COVERAGE_ANALYSIS.md` for detailed coverage breakdown
- Check `TESTING_STRATEGY.md` for testing philosophy and approach
- Review Domain class source code in `Assets\_Project\Scripts\Domain\`
