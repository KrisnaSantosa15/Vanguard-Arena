# Vanguard Arena - Comprehensive Testing Strategy

## Current State Analysis

### Units in Game (From battle_debug.log)
**Player Team:**
1. Aurelius (Esper/Tank)
2. Elara (Mystic/Support)
3. Malzahar (Mystic/DPS)
4. Aegis (Physical/Tank)
5. Gorok (Physical/DPS)
6. Artemis (Tech/Ranged)

**Enemy Team:**
1. Lumina (Esper/Support)
2. Merlin (Mystic/Ranged)
3. Mortis (Mystic/DPS)
4. Kaito (Physical/DPS)
5. Kuro (Physical/Tank)
6. Draven (Tech/Melee)

---

## Test Matrix

### 1. Unit Type Coverage

| Unit Type | Player Units | Enemy Units | Test Status |
|-----------|-------------|-------------|-------------|
| **Melee** | Aurelius, Malzahar, Aegis, Gorok | Mortis, Kaito, Kuro, Draven | ⚠️ Needs testing |
| **Ranged** | Elara, Artemis | Lumina, Merlin | ✅ Artemis tested (fixed) |

**Tests Needed:**
- [ ] Melee basic attack (movement to target)
- [ ] Melee ultimate (movement + damage)
- [ ] Ranged basic attack (no movement)
- [ ] Ranged ultimate (no movement) ✅ FIXED
- [ ] Multi-hit basic attacks (BasicHitsMin/Max > 1)

---

### 2. Target Pattern Coverage

| Pattern | Basic Attacks | Ultimates | Test Status |
|---------|--------------|-----------|-------------|
| **SingleEnemy** | Most units | Some units | ⚠️ Partial |
| **AllEnemies** | - | AOE ultimates | ⚠️ Needs explicit testing |
| **SingleAlly** | - | Healing ultimates | ❌ Not tested |
| **AllAllies** | - | Team buffs | ❌ Not tested |
| **RandomEnemy** | - | Random targeting | ❌ Not tested |

**Tests Needed:**
- [ ] Single target damage (basic + ultimate)
- [ ] AOE damage to all enemies
- [ ] Single ally healing/buffing
- [ ] Team-wide buffs/shields
- [ ] Random targeting consistency

---

### 3. Passive Ability Coverage

| Passive Type | Expected Behavior | Test Status | Issues Found |
|--------------|-------------------|-------------|--------------|
| **OnBattleStart** | Triggers once at start | ⚠️ Partial | Aegis shield has 0.2 modifier (should be 0) |
| **OnTurnStart** | Triggers each turn | ⚠️ Partial | **BUG**: Elara buffs ALL 6 allies, not 1 random |
| **OnDamageDealt** | Triggers after damage | ❌ Not tested | - |
| **OnDamageTaken** | Triggers after taking damage | ❌ Not tested | - |
| **OnAllyLowHP** | Triggers when ally < threshold | ❌ Not tested | - |
| **OnKill** | Triggers after killing | ❌ Not tested | - |
| **OnUltimate** | Triggers when using ult | ❌ Not tested | - |
| **OnHPThreshold** | Triggers at HP threshold | ❌ Not tested | - |

**Known Issues:**
1. ⚠️ **Elara's "Combat Boost" passive** (OnTurnStart):
   - Expected: Buff 1 random ally with +20% ATK
   - Actual: Buffs ALL 6 allies
   - Location: `BattleController.cs:1215-1254` (TriggerPassive method)

2. ⚠️ **Aegis's "Protective Shield" passive** (OnBattleStart):
   - Has modifier 0.2 (should be 0 for shields)
   - Shield value: 30 HP for 99 turns

---

### 4. Status Effect Coverage

| Effect Type | Applied By | Test Status | Issues Found |
|-------------|------------|-------------|--------------|
| **Shield** | Aegis passive, ultimates | ✅ Working | Multiple shields stack correctly |
| **Stun** | - | ❌ Not tested | - |
| **Burn** | - | ✅ Working | DoT applies per turn |
| **ATKUp** | Elara passive | ⚠️ Partial | Targets all instead of random |
| **ATKDown** | - | ❌ Not tested | - |
| **DEFUp** | - | ❌ Not tested | - |
| **DEFDown** | - | ❌ Not tested | - |

**Tests Needed:**
- [ ] Stun prevents turn action
- [ ] Burn damage per turn (multiple stacks)
- [ ] ATK/DEF stat modifiers apply correctly
- [ ] Stat modifiers expire after duration
- [ ] Multiple effects of same type (stacking rules)

---

### 5. Combat Mechanics Coverage

| Mechanic | Test Status | Issues Found |
|----------|-------------|--------------|
| **Damage Calculation** | ✅ Logged | Working correctly |
| **Shield Absorption** | ✅ Logged | Multiple shields sum correctly |
| **Critical Hits** | ✅ Logged | Working (CritRate, CritDamage) |
| **Multi-Hit Attacks** | ⚠️ Partial | Need to test all combo units |
| **Death Detection** | ✅ Working | Properly removes dead units |
| **Energy Gain** | ⚠️ Partial | **BUG**: Some attacks missing energy gain logs |
| **Energy Bar Fill** | ✅ Working | 100% converts to +1 energy |
| **Ultimate Cooldown** | ✅ Logged | Counts down correctly |

**Known Issues:**
1. ⚠️ **Missing Energy Gain Logs**:
   - Some basic attacks don't log energy gain
   - Expected: `[ENERGY] ⚡ Energy gained: X -> Y (+1)`
   - Location: After hit processing in basic attacks

2. ⚠️ **Melee Multi-Target Ultimate Damage**:
   - Uses animation-driven damage (no explicit damage logs)
   - Only see shield absorption logs
   - Example: Gorok's ultimate (Turn 2 in log)

---

### 6. Ultimate Type Coverage

| Ultimate Branch | Expected Behavior | Test Status |
|----------------|-------------------|-------------|
| **ATTACK (Single)** | Damage to 1 enemy | ✅ Working |
| **ATTACK (Multi)** | Damage to multiple enemies | ⚠️ Melee lacks logs |
| **SUPPORT (Heal)** | Restore HP to ally/allies | ❌ Not tested |
| **SUPPORT (Shield)** | Apply shield to ally/allies | ❌ Not tested |
| **SUPPORT (Buff)** | Apply stat buffs | ❌ Not tested |

**Tests Needed:**
- [ ] Single-target ultimates (all units with UltimateCost=2-3)
- [ ] AOE ultimates (AllEnemies pattern)
- [ ] Healing ultimates (verify HP restoration)
- [ ] Shield ultimates (verify shield application)
- [ ] Buff ultimates (verify stat changes)

---

## Priority Test Plan

### Phase 1: Critical Fixes (IMMEDIATE)
1. ✅ **Fix Auto Mode Toggle** - Show all 6 units (COMPLETED)
2. **Fix Elara Passive** - Target 1 random ally, not all
3. **Fix Aegis Passive** - Modifier should be 0
4. **Fix Missing Energy Logs** - Ensure all hits log energy gain

### Phase 2: Coverage Gaps (HIGH PRIORITY)
1. **Test Healing/Support Ultimates**
   - Create test unit with healing ultimate
   - Verify HP restoration calculation
   - Verify can't overheal (HP cap)

2. **Test Stun Mechanic**
   - Create unit with stun ability
   - Verify stunned unit skips turn
   - Verify stun duration countdown

3. **Test All Passive Types**
   - OnDamageDealt, OnDamageTaken, OnKill, OnUltimate, OnHPThreshold, OnAllyLowHP
   - Create test units for each passive type
   - Verify trigger conditions

4. **Test Stat Modifiers (DEF, ATK)**
   - Apply ATKUp/Down, DEFUp/Down
   - Verify damage calculations use modified stats
   - Verify modifiers stack/replace correctly

### Phase 3: Edge Cases (MEDIUM PRIORITY)
1. **Multi-Hit Combos**
   - Test units with BasicHitsMin != BasicHitsMax
   - Verify hit count RNG
   - Verify energy gain per hit

2. **Target Pattern Edge Cases**
   - Target dead units (should skip)
   - Target full HP units with heal (should still work)
   - RandomEnemy with 1 enemy left

3. **Status Effect Stacking**
   - Multiple shields from different sources
   - Multiple burns (currently stacks)
   - Stat modifiers (currently highest magnitude wins)

### Phase 4: Automation (FUTURE)
1. **Automated Battle Tests**
   - Create test scenes with specific unit configs
   - Parse battle_debug.log programmatically
   - Assert expected outcomes (damage, deaths, victory)

2. **Unit Test Suite**
   - Test CombatCalculator in isolation
   - Test UnitRuntimeState methods
   - Test StatusEffect logic

---

## Recommended Test Approach

### Manual Testing (Current Phase)
**Setup:**
1. Create test scene: "TestScene_PassiveAbilities"
2. Configure BattleController with specific unit lineups
3. Set `battleSeed` to fixed value for reproducibility
4. Enable Auto Mode for consistency

**Process:**
1. Run battle to completion
2. Review `battle_debug.log` for:
   - Expected passive triggers
   - Correct damage calculations
   - Proper status effect application
   - Victory/defeat detection
3. Document any discrepancies
4. Fix bugs
5. Repeat

### Automated Testing (Future Phase)
**Framework:**
- Unity Test Framework (Play Mode tests)
- Create `BattleTestRunner.cs` helper
- Mock/stub random number generation
- Assert log output or battle state

**Example Test:**
```csharp
[UnityTest]
public IEnumerator TestElara_PassiveShouldTargetOneRandomAlly()
{
    // Arrange
    var battle = SetupBattle(
        playerUnits: new[] { "Aurelius", "Elara", "Aegis", "Gorok", "Artemis", "Malzahar" },
        enemyUnits: new[] { "Merlin" },
        seed: 12345
    );
    
    // Act
    yield return battle.RunUntilTurn(1);
    
    // Assert
    var buffedAllies = battle.PlayerTeam.Where(u => u.HasEffect(StatusEffectType.ATKUp)).Count();
    Assert.AreEqual(1, buffedAllies, "Elara passive should buff only 1 ally, not all");
}
```

---

## Unit Configuration Audit Needed

To complete testing, we need to **document each unit's configuration**:

### Audit Checklist (for each unit):
- [ ] **Identity**: DisplayName, ID, Element, Role
- [ ] **Type**: Melee or Ranged
- [ ] **Stats**: HP, ATK, DEF, SPD, CritRate, CritDamage
- [ ] **Basic Attack**: HitsMin/Max, TargetPattern, Description
- [ ] **Ultimate**: EnergyCost, CooldownTurns, TargetPattern, Description
- [ ] **Passive**: Type, Threshold, Value, Modifier, Duration, EffectType, Targets

### How to Extract:
1. Open Unity Editor
2. Navigate to `Assets/_Project/Data/Units/Generated/`
3. Select each unit asset
4. Document Inspector values
5. Create unit matrix spreadsheet

**OR** use Editor script to auto-generate:
```csharp
// Create: Assets/_Project/Scripts/Editor/UnitConfigurationExporter.cs
[MenuItem("Tools/Export Unit Configurations")]
static void ExportUnitConfigs()
{
    var units = Resources.FindObjectsOfTypeAll<UnitDefinitionSO>();
    var output = new StringBuilder();
    
    foreach (var unit in units)
    {
        output.AppendLine($"## {unit.DisplayName}");
        output.AppendLine($"- Type: {unit.Type}");
        output.AppendLine($"- Basic: {unit.BasicTargetPattern}, Hits: {unit.BasicHitsMin}-{unit.BasicHitsMax}");
        output.AppendLine($"- Ultimate: {unit.UltimateTargetPattern}, Cost: {unit.UltimateEnergyCost}");
        output.AppendLine($"- Passive: {unit.Passive.Type}, Effect: {unit.Passive.EffectType}");
        output.AppendLine();
    }
    
    File.WriteAllText("UnitConfigurations.md", output.ToString());
    Debug.Log("Exported to UnitConfigurations.md");
}
```

---

## Success Criteria

### Minimum Viable Testing:
- ✅ All unit types tested (Melee, Ranged)
- ✅ All target patterns tested (Single, All, Random)
- ✅ All passive types triggered at least once
- ✅ All status effects applied and verified
- ✅ No critical bugs in combat flow
- ✅ Victory/defeat detection works in all scenarios

### Complete Testing:
- All 12 units have documented configurations
- All 8 passive types have dedicated test cases
- All 7 status effects tested in isolation
- All edge cases handled gracefully
- Automated test suite covers core mechanics
- Performance profiling (1000+ turn battles)

---

## Next Steps

1. ✅ **Fix Auto Mode Toggle** (COMPLETED)
2. **Run Battle with Current Logging** - Generate fresh log
3. **Fix Elara Passive Bug** - Target selection logic
4. **Fix Missing Energy Logs** - Ensure all hits log energy
5. **Create Unit Configuration Export Tool**
6. **Document All 12 Unit Configs**
7. **Create Test Scenes for Untested Mechanics**
8. **Implement Automated Test Framework**

---

## Notes

- Current logging is **EXCELLENT** - makes debugging easy
- Battle flow is solid, issues are in edge cases
- Need more test coverage for support/healing mechanics
- Consider adding "Test Mode" toggle to force specific scenarios
- Consider replay system (save RNG seed + actions → reproducible battles)
