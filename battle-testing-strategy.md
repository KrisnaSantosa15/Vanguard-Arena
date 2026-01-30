# Battle Testing Strategy - Vanguard Arena

## Goal
Implement a scalable, comprehensive test strategy that covers the massive configuration space (target patterns, passives, effects, unit types) using combinatorial testing techniques, deterministic test execution, and automated verification.

## Current Implementation Summary

### Combat System Architecture
- **BattleController**: Orchestrates turn loop, action selection (Basic/Ultimate), manual/auto modes, passive triggers, victory checks
- **CombatCalculator**: Pure functions for damage/heal/shield with variance and crits via `UnityEngine.Random.Range`
- **UnitRuntimeState**: Unit state, computed stats with modifiers, status effects, cooldown tracking
- **TeamEnergyState**: Player team energy (shared), enemy per-unit energy, energy bar conversion
- **TargetPattern**: 13 variants (SingleEnemy, AllEnemies, FrontRow, Column, LowestHp, etc.)
- **PassiveAbility**: 8 trigger types, targeting options (Self/AllAllies/RandomAlly/Enemies)
- **StatusEffect**: 7 types with distinct stacking rules (Shield stacks, Burn refreshes, stat modifiers keep strongest)

### Key Testing Challenges
1. **Combinatorial explosion**: 2 UnitTypes × 13 TargetPatterns × 8 PassiveTypes × 7 StatusEffects = 1,456 base combinations
2. **Non-determinism**: Split RNG (`System.Random` for selection, `UnityEngine.Random` for variance/crit)
3. **Animation-driven damage**: Melee AOE ultimates use animation callbacks (harder to test)
4. **State transitions**: Status durations, cooldowns, energy bar, death removal
5. **Configuration in ScriptableObjects**: 12+ units with unique configs

---

## Testing Strategy (Research-Based)

### Approach: Combinatorial Testing + Invariant Checking + Log-Based Verification

**Why Combinatorial (Pairwise/3-way)?**
- NIST research: 70-90% of bugs found in 2-way interactions, 95%+ in 3-way
- Reduces test cases from thousands to 20-100 while maintaining high fault detection
- Industry standard for complex configuration testing (NASA, Microsoft, Lockheed Martin)

**Why Invariant-Based Testing?**
- No need for exact damage values (variance makes this brittle)
- Focus on "laws" that must always hold (HP bounds, shield absorption order, etc.)
- Easier to maintain, more robust to balance changes

**Why Log-Based Verification?**
- Existing comprehensive logging system provides execution trace
- Can parse logs to check coverage and invariants
- Enables automated regression testing

---

## Test Parameter Taxonomy

### Configuration Parameters (for combinatorial matrix)

| Parameter | Values | Notes |
|-----------|--------|-------|
| **UnitType** | Melee, Ranged | Affects movement, hit events |
| **ActionType** | Basic, Ultimate | Different damage calcs, energy cost |
| **TargetPattern** | SingleEnemy, AllEnemies, SingleAlly, AllAllies, FrontRowSingle, BackRowSingle, FrontRowAll, BackRowAll, ColumnSingle, LowestHpEnemy, HighestThreatEnemy, Self | 13 variants |
| **PassiveType** | None, OnBattleStart, OnTurnStart, OnDamageDealt, OnDamageTaken, OnAllyLowHP, OnKill, OnUltimate, OnHPThreshold | 9 variants |
| **StatusEffectType** | None, Shield, Burn, Stun, ATKUp, ATKDown, DEFUp, DEFDown | 8 variants |
| **HitCount** | 1, Multi (2-3) | BasicHitsMin/Max |
| **CritScenario** | NoCrit, Crit | For damage variance |
| **ShieldPresent** | None, Single, Stacked | Shield absorption testing |
| **StunActive** | No, Yes | Turn skip testing |
| **EnergyState** | Low (0-1), Mid (2-5), High (6-9), Max (10) | Energy gain blocking |
| **TargetAvailability** | AllAlive, SomeDead, AllDead | Edge case handling |
| **AnimationDriver** | Present, Absent | Melee AOE damage path |

### Derived Test Dimensions
- **Branch**: Damage vs Support (derived from IsAllyTargeting)
- **TargetCount**: Single vs Multi (derived from TargetPattern)
- **MovementRequired**: Yes (Melee) vs No (Ranged) (derived from UnitType)

---

## Test Invariants (Always-True Properties)

### Health & Death
1. `CurrentHP >= 0` (never negative)
2. `CurrentHP <= MaxHP` (no overhealing)
3. `IsAlive == (CurrentHP > 0)` (consistent state)
4. Dead units do not take turns
5. Dead units are not selectable as targets

### Shield Mechanics
6. Shield absorbs damage before HP loss
7. Multiple shields stack (sum of values)
8. Shield never negative
9. Shield expires when duration reaches 0

### Status Effects
10. Burn damage applies at turn start
11. Stun prevents action (turn skipped)
12. Stat modifiers apply to CurrentATK/CurrentDEF calculations
13. Status durations decrement each turn
14. Expired effects are removed

### Energy System
15. Energy never exceeds MaxEnergy
16. Energy bar conversion happens at 100%
17. Ultimate costs energy (player team energy or enemy per-unit)
18. Cooldown starts after ultimate use
19. Cooldown decrements each turn

### Targeting
20. Targets match pattern (Single/All/Row/Column/etc.)
21. No null/dead targets selected (except edge cases)
22. AllAllies pattern only targets allies
23. SingleEnemy pattern only targets enemies

### Combat Flow
24. Turn order follows SPD-based timeline
25. Battle ends when one team is dead
26. Victory/defeat logged with statistics
27. No infinite loops (safety counter in place)

---

## Test Suite Structure

### Layer 1: Unit Tests (Domain Logic - Fast)
**Location**: `Assets/_Project/Tests/EditMode/Domain/`

Focus on pure functions and state logic without Unity dependencies.

**Test Files:**
- `CombatCalculatorTests.cs` - Damage/heal/shield formulas
- `UnitRuntimeStateTests.cs` - Status effects, stat modifiers, cooldown
- `TeamEnergyStateTests.cs` - Energy gain/spend/bar conversion
- `StatusEffectTests.cs` - Stacking rules, expiry
- `TargetPatternHelperTests.cs` - IsAllyTargeting logic

**Coverage Goals:**
- All CombatCalculator formulas with controlled RNG
- Status effect stacking rules (Shield, Burn, StatModifiers)
- Energy cap and conversion logic
- Cooldown countdown and ready state

**Example Test:**
```csharp
[Test]
public void Shield_StacksCorrectly_WhenMultipleApplied()
{
    var unit = CreateMockUnit();
    unit.ApplyStatusEffect(StatusEffectType.Shield, 30, 0f, 2);
    unit.ApplyStatusEffect(StatusEffectType.Shield, 20, 0f, 1);
    
    Assert.AreEqual(50, unit.ShieldAmount); // Shields stack
    Assert.AreEqual(2, unit.ActiveStatusEffects.Count(s => s.Type == StatusEffectType.Shield));
}
```

---

### Layer 2: Integration Tests (Battle Flow - Medium)
**Location**: `Assets/_Project/Tests/PlayMode/Integration/`

Small battle scenarios (1v1, 2v2) with controlled configs.

**Test Files:**
- `BasicAttackFlowTests.cs` - Energy gain, multi-hit, crit
- `UltimateFlowTests.cs` - Cooldown, energy cost, damage/support branches
- `PassiveTriggerTests.cs` - OnBattleStart, OnTurnStart, targeting
- `StatusEffectFlowTests.cs` - Shield absorption, burn tick, stun skip
- `TargetingTests.cs` - All TargetPattern variants with valid targets
- `VictoryConditionTests.cs` - Battle end detection and logging

**Coverage Goals:**
- Each TargetPattern executes at least once
- Each PassiveType triggers at least once
- Each StatusEffect applies and expires
- Animation-driven vs direct damage paths
- Energy system full cycle (gain → spend → cooldown)

**Example Test:**
```csharp
[UnityTest]
public IEnumerator RangedUltimate_DealsDamage_ToAllEnemies()
{
    var battle = CreateTestBattle(
        playerUnits: new[] { CreateRangedUnit() },
        enemyUnits: new[] { CreateUnit(), CreateUnit() },
        seed: 12345
    );
    
    yield return battle.RunUntilPlayerUltimate();
    
    Assert.IsTrue(battle.AllEnemiesHaveTakenDamage());
    Assert.IsTrue(battle.LogContains("[ULTIMATE-MULTI] AllEnemies"));
}
```

---

### Layer 3: Combinatorial Test Matrix (Configuration Coverage - Long)
**Location**: `Assets/_Project/Tests/PlayMode/Combinatorial/`

Generated from pairwise/3-way parameter combinations.

**Approach:**
1. Define parameter set (see taxonomy above)
2. Generate pairwise covering array (20-40 test cases)
3. Create test scene config for each row
4. Run battle, validate invariants + coverage

**Test Files:**
- `CombinatorialTestRunner.cs` - Reads matrix, instantiates battles
- `InvariantChecker.cs` - Validates all 27 invariants
- `CoverageReporter.cs` - Parses logs, reports coverage %

**Matrix Example (Pairwise):**
| Test | UnitType | TargetPattern | PassiveType | StatusEffect | HitCount | Expected |
|------|----------|---------------|-------------|--------------|----------|----------|
| 1 | Melee | SingleEnemy | OnBattleStart | Shield | 1 | Shield applied at start |
| 2 | Ranged | AllEnemies | OnTurnStart | ATKUp | Multi | Buff each turn, multi-hit |
| 3 | Melee | AllAllies | None | Burn | 1 | Support heals, no burn |
| ... | ... | ... | ... | ... | ... | ... |

**Verification:**
- All invariants pass
- Coverage report shows 100% for: TargetPatterns, PassiveTypes, StatusEffects
- No crashes, deadlocks, or invalid states

---

### Layer 4: Regression Suite (Golden Logs - Fast)
**Location**: `Assets/_Project/Tests/PlayMode/Regression/`

Curated battles with known outcomes, validated against "golden" logs.

**Test Files:**
- `GoldenBattleTests.cs` - Replays scenarios with fixed seeds
- `GoldenLogValidator.cs` - Compares logs against expected patterns

**Golden Scenarios:**
- Player victory (4/6 survivors)
- Player defeat (total wipe)
- Passive heavy (Elara buffs, Aegis shields)
- Status heavy (burns, stuns, stat modifiers)
- Edge cases (0 HP survive via shield, all enemies dead in one ult)

**Example:**
```csharp
[UnityTest]
public IEnumerator GoldenBattle_PlayerVictory_4Survivors()
{
    var battle = LoadGoldenBattle("player_victory_4survivors.json");
    yield return battle.RunToCompletion();
    
    Assert.AreEqual(4, battle.PlayerSurvivors);
    Assert.IsTrue(battle.LogMatchesGolden("player_victory_4survivors.log"));
}
```

---

## Implementation Tasks

### Phase 1: Foundation (Test Infrastructure)
- [ ] **Task 1.1: Create test harness - BattleTestBuilder**
  - Utility class to spawn test battles with custom lineups
  - Methods: `CreateUnit(type, pattern, passive, effect, ...)`, `CreateBattle(seed, player[], enemy[])`
  - Verify: Can instantiate 1v1 battle programmatically

- [ ] **Task 1.2: Add deterministic RNG wrapper**
  - Create `IDeterministicRandom` interface
  - Implement `SeededRandom` (wraps System.Random + Unity.Random.state)
  - Inject into BattleController and CombatCalculator
  - Verify: Same seed produces identical damage/crit sequence

- [ ] **Task 1.3: Create invariant checker**
  - `InvariantChecker.cs` with methods for each of 27 invariants
  - `CheckAllInvariants(BattleState)` → List<InvariantViolation>
  - Verify: Detects violations (test with intentionally broken state)

- [ ] **Task 1.4: Create log parser**
  - `BattleLogParser.cs` parses battle_debug.log into structured events
  - Extract: PassiveTriggered, StatusApplied, DamageDealt, EnergyGained, etc.
  - Verify: Can extract all event types from sample log

- [ ] **Task 1.5: Create coverage reporter**
  - `CoverageReporter.cs` analyzes parsed logs
  - Report: "12/13 TargetPatterns covered", "7/8 PassiveTypes triggered", etc.
  - Verify: Generates HTML coverage report

---

### Phase 2: Unit Tests (Domain Layer)
- [ ] **Task 2.1: CombatCalculator tests**
  - Test damage formula with controlled variance/crit
  - Test heal/shield formulas
  - Edge cases: 0 ATK, 0 DEF, max crit
  - Verify: 100% code coverage on CombatCalculator

- [ ] **Task 2.2: UnitRuntimeState tests**
  - Test status effect stacking rules (Shield, Burn, StatModifiers)
  - Test cooldown countdown and CanUseUltimate logic
  - Test computed stats (CurrentATK/DEF with modifiers)
  - Verify: All stacking rules validated

- [ ] **Task 2.3: TeamEnergyState tests**
  - Test energy gain/spend/cap
  - Test energy bar conversion
  - Test blocked gain at max
  - Verify: All energy transitions covered

- [ ] **Task 2.4: StatusEffect tests**
  - Test TickDuration and IsExpired
  - Test duration 0 edge case
  - Verify: Expiry logic correct

---

### Phase 3: Integration Tests (Battle Flow)
- [ ] **Task 3.1: Basic attack flow tests**
  - Test single-target basic (Melee, Ranged)
  - Test multi-hit basic (2-3 hits)
  - Test energy gain per hit
  - Test shield absorption in basic
  - Verify: All basic attack paths execute correctly

- [ ] **Task 3.2: Ultimate flow tests**
  - Test single-target ultimate (damage, support)
  - Test multi-target ultimate (AOE, ranged, melee with driver)
  - Test cooldown starts after use
  - Test energy consumption
  - Verify: All ultimate branches covered

- [ ] **Task 3.3: Passive trigger tests**
  - Test OnBattleStart (Aegis shield)
  - Test OnTurnStart (Elara buff to random ally)
  - Test OnUltimate (if any unit has this)
  - Test targeting (Self, AllAllies, RandomAlly, Enemies)
  - Verify: All passive types trigger correctly

- [ ] **Task 3.4: Status effect flow tests**
  - Test shield absorption order (before HP)
  - Test burn tick at turn start
  - Test stun skips turn
  - Test stat modifiers apply to damage calc
  - Test status expiry after duration
  - Verify: All status mechanics work

- [ ] **Task 3.5: Targeting tests**
  - Test all 13 TargetPattern variants
  - Test with dead targets (should skip)
  - Test with no valid targets (should abort)
  - Test column/row targeting with 6v6 formation
  - Verify: All patterns select correct targets

- [ ] **Task 3.6: Victory condition tests**
  - Test player victory (all enemies dead)
  - Test player defeat (all players dead)
  - Test battle end logging (statistics, survivors)
  - Test mid-action victory (enemy dies during multi-hit)
  - Verify: Battle ends correctly in all scenarios

---

### Phase 4: Combinatorial Testing (Configuration Coverage)
- [ ] **Task 4.1: Define parameter set**
  - List all parameters from taxonomy
  - Assign values to each parameter
  - Document valid combinations (e.g., AllAllies requires support units)
  - Verify: Parameter set covers all config dimensions

- [ ] **Task 4.2: Generate pairwise covering array**
  - Use NIST ACTS tool or equivalent
  - Generate 20-40 test cases (pairwise)
  - Export as CSV/JSON
  - Verify: Every pair of parameters appears at least once

- [ ] **Task 4.3: Create test scene generator**
  - `CombinatorialTestGenerator.cs` reads matrix
  - For each row, create unit configs with specified parameters
  - Output: Test scene prefab or JSON config
  - Verify: Can generate 40 test configs from matrix

- [ ] **Task 4.4: Implement test runner**
  - `CombinatorialTestRunner.cs` loads each config
  - Instantiates battle with fixed seed
  - Runs to completion (max 10 turns or victory)
  - Collects logs and checks invariants
  - Verify: Can run 40 tests in batch

- [ ] **Task 4.5: Generate coverage report**
  - Parse all logs from combinatorial run
  - Check coverage for: TargetPatterns, PassiveTypes, StatusEffects, etc.
  - Generate HTML report with gaps highlighted
  - Verify: Report shows 95%+ coverage

---

### Phase 5: Regression Suite (Golden Logs)
- [ ] **Task 5.1: Capture golden scenarios**
  - Run curated battles (victory, defeat, edge cases)
  - Save logs as "golden" references
  - Document expected outcomes (survivors, turn count, key events)
  - Verify: 5-10 golden scenarios captured

- [ ] **Task 5.2: Implement golden log validator**
  - `GoldenLogValidator.cs` compares logs
  - Fuzzy matching for non-deterministic parts (timestamps, exact damage)
  - Exact matching for key events (passives, effects, victory)
  - Verify: Can detect regressions in golden scenarios

- [ ] **Task 5.3: Create regression test suite**
  - `GoldenBattleTests.cs` runs all golden scenarios
  - Asserts outcomes match expected
  - Validates logs match golden patterns
  - Verify: All golden tests pass

---

### Phase 6: Continuous Integration (Automation)
- [ ] **Task 6.1: Setup Unity Test Runner in CI**
  - Configure CI to run EditMode tests (fast)
  - Configure CI to run PlayMode tests (medium)
  - Configure CI to run Combinatorial suite (long, nightly)
  - Verify: Tests run automatically on commit

- [ ] **Task 6.2: Add test result reporting**
  - Export test results as JUnit XML
  - Integrate with CI dashboard (GitHub Actions, Jenkins, etc.)
  - Email/Slack notification on failures
  - Verify: Failures are visible and actionable

- [ ] **Task 6.3: Add coverage tracking**
  - Track test coverage over time (code coverage + config coverage)
  - Set minimum thresholds (e.g., 80% code, 90% config)
  - Block PRs if coverage drops
  - Verify: Coverage dashboard available

---

## Verification Criteria (Done When...)

### Phase 1 (Foundation)
- ✅ BattleTestBuilder can create 1v1 battles programmatically
- ✅ Deterministic RNG produces identical results with same seed
- ✅ InvariantChecker detects violations correctly
- ✅ Log parser extracts all event types
- ✅ Coverage reporter generates HTML report

### Phase 2 (Unit Tests)
- ✅ 100% code coverage on CombatCalculator
- ✅ All status effect stacking rules validated
- ✅ All energy transitions tested
- ✅ 50+ unit tests passing (EditMode)

### Phase 3 (Integration Tests)
- ✅ All basic/ultimate branches covered
- ✅ All 8 passive types triggered
- ✅ All 7 status effects applied and expired
- ✅ All 13 target patterns executed
- ✅ Victory/defeat detection works
- ✅ 100+ integration tests passing (PlayMode)

### Phase 4 (Combinatorial)
- ✅ 40+ combinatorial tests generated
- ✅ All tests pass with all invariants satisfied
- ✅ Coverage report shows 95%+ for all categories
- ✅ No crashes, deadlocks, or infinite loops

### Phase 5 (Regression)
- ✅ 5-10 golden scenarios captured
- ✅ All golden tests pass
- ✅ Regression detector catches intentional changes

### Phase 6 (CI)
- ✅ Tests run automatically on commit
- ✅ Test results visible in CI dashboard
- ✅ Coverage tracked over time

---

## Success Metrics

- **Test Count**: 200+ tests (50 unit, 100 integration, 40 combinatorial, 10 regression)
- **Execution Time**: Unit tests <5s, Integration <2min, Combinatorial <10min, Regression <30s
- **Code Coverage**: 80%+ on Domain layer, 60%+ on Presentation layer
- **Config Coverage**: 95%+ for TargetPatterns, PassiveTypes, StatusEffects
- **Invariant Violations**: 0 in CI
- **False Positives**: <5% (tests fail due to test issues, not real bugs)

---

## Notes

### Testing Philosophy
- **Test behavior, not implementation**: Focus on outcomes (HP changes, effects applied) not internal state
- **Determinism is key**: Fixed seeds enable reproducible failures
- **Logs are oracles**: Comprehensive logging enables automated verification
- **Combinatorial > Exhaustive**: Smart coverage beats brute force

### Risks & Mitigations
- **Risk**: Unity Random not fully deterministic across platforms
  - **Mitigation**: Use System.Random for critical paths, document variance
- **Risk**: Animation-driven damage is hard to test
  - **Mitigation**: Add "Test Mode" that forces direct damage path
- **Risk**: Combinatorial matrix too large
  - **Mitigation**: Start with pairwise, expand to 3-way only if needed
- **Risk**: Golden logs become stale
  - **Mitigation**: Automated golden log regeneration tool

### Future Enhancements
- **Performance testing**: 1000+ turn battles, stress test
- **Fuzzing**: Random unit configs, detect crashes
- **Property-based testing**: Generate random battles, check invariants
- **Visual regression**: Screenshot comparison for UI
- **AI testing**: Validate BattleAI decision quality

---

## References

### Academic Papers
- "Practical Combinatorial Testing" - NIST SP 800-142
- "Combinatorial Testing for Software: An Adaptation of Design of Experiments" - IEEE Computer 2013
- "All-pairs testing: A systematic approach to testing database interactions" - ICST 2010

### Industry Best Practices
- Unity Test Framework documentation
- NUnit best practices
- xUnit patterns (Gerard Meszaros)

### Tools
- NIST ACTS (covering array generator)
- Unity Test Framework (PlayMode + EditMode)
- NUnit 3.5 (assertion library)

---

**Document Version**: 1.0  
**Last Updated**: 2026-01-30  
**Owner**: Development Team  
**Status**: Ready for Implementation
