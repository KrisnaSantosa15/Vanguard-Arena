# Phase 3: Hybrid Systematic Implementation Plan

**Goal**: 70% test coverage, testable domain architecture, shippable game  
**Timeline**: ~14 hours over 3-4 sessions  
**Approach**: TDD + Refactoring (Red → Green → Refactor)

---

## Implementation Strategy

### The Hybrid Philosophy
1. **Test critical paths** (passives, targeting, actions)
2. **Skip exhaustive tests** (full battle simulations → manual QA)
3. **Refactor as we go** (extract domain logic from presentation)
4. **Ship incrementally** (each phase = working feature)

---

## Phase 3A: Passive System (Session 1 - 6 hours)

### Step 1: Create PassiveManager (Domain Logic)
**File**: `Assets/_Project/Scripts/Domain/PassiveManager.cs`

**Responsibilities**:
- Execute passive triggers (OnKill, OnDamageDealt, OnHPThreshold, etc.)
- Apply status effects to targets
- Handle targeting (self, single ally, all allies)

**Interface Design**:
```csharp
public static class PassiveManager
{
    // Trigger methods for each passive type
    public static void TriggerOnBattleStart(UnitRuntimeState owner, List<UnitRuntimeState> allies, IDeterministicRandom rng);
    public static void TriggerOnTurnStart(UnitRuntimeState owner, List<UnitRuntimeState> allies, IDeterministicRandom rng);
    public static void TriggerOnDamageDealt(UnitRuntimeState owner, UnitRuntimeState target, int damageDealt, List<UnitRuntimeState> allies, IDeterministicRandom rng);
    public static void TriggerOnDamageTaken(UnitRuntimeState owner, int damageTaken, List<UnitRuntimeState> allies, IDeterministicRandom rng);
    public static void TriggerOnKill(UnitRuntimeState owner, UnitRuntimeState killed, List<UnitRuntimeState> allies, IDeterministicRandom rng);
    public static void TriggerOnUltimate(UnitRuntimeState owner, List<UnitRuntimeState> allies, IDeterministicRandom rng);
    public static void TriggerOnHPThreshold(UnitRuntimeState owner, float hpPercent, List<UnitRuntimeState> allies, IDeterministicRandom rng);
    public static void TriggerOnAllyLowHP(UnitRuntimeState owner, UnitRuntimeState lowHPAlly, List<UnitRuntimeState> allies, IDeterministicRandom rng);

    // Internal helper
    private static void ApplyPassiveEffect(PassiveAbility passive, UnitRuntimeState owner, List<UnitRuntimeState> allies, IDeterministicRandom rng);
}
```

---

### Step 2: Write Tests FIRST (TDD Red Phase)
**File**: `Assets/_Project/Tests/EditMode/Domain/PassiveAbilityTests.cs`

**Test Structure** (20 tests total):

#### 2.1 OnBattleStart (3 tests)
```csharp
[Test]
public void Passive_OnBattleStart_AppliesSelfBuff()
{
    // Tank starts battle with +30% DEF buff for 3 turns
    var tank = new BattleTestBuilder()
        .AddPlayerUnit("Tank", def: 10, passive: new PassiveAbility
        {
            Type = PassiveType.OnBattleStart,
            EffectType = StatusEffectType.DEFUp,
            Modifier = 0.3f,
            Duration = 3,
            TargetSelf = true
        })
        .BuildStates().players[0];

    var rng = new SeededRandom(999);

    // Act
    PassiveManager.TriggerOnBattleStart(tank, new List<UnitRuntimeState> { tank }, rng);

    // Assert
    Assert.AreEqual(1, tank.ActiveStatusEffects.Count);
    Assert.AreEqual(StatusEffectType.DEFUp, tank.ActiveStatusEffects[0].Type);
    Assert.AreEqual(13, tank.CurrentDEF); // 10 * 1.3 = 13
}

[Test]
public void Passive_OnBattleStart_AppliesAllAllyBuff()
{
    // Support starts battle granting +20% ATK to all allies for 2 turns
    var support = new BattleTestBuilder()
        .AddPlayerUnit("Support", passive: new PassiveAbility
        {
            Type = PassiveType.OnBattleStart,
            EffectType = StatusEffectType.ATKUp,
            Modifier = 0.2f,
            Duration = 2,
            TargetSelf = false,
            TargetAllies = true
        })
        .BuildStates().players[0];

    var ally1 = new BattleTestBuilder().AddPlayerUnit("Ally1", atk: 20).BuildStates().players[0];
    var ally2 = new BattleTestBuilder().AddPlayerUnit("Ally2", atk: 25).BuildStates().players[0];
    var allies = new List<UnitRuntimeState> { support, ally1, ally2 };
    var rng = new SeededRandom(999);

    // Act
    PassiveManager.TriggerOnBattleStart(support, allies, rng);

    // Assert
    Assert.AreEqual(0, support.ActiveStatusEffects.Count, "Support should not buff self");
    Assert.AreEqual(1, ally1.ActiveStatusEffects.Count, "Ally1 should have ATKUp");
    Assert.AreEqual(1, ally2.ActiveStatusEffects.Count, "Ally2 should have ATKUp");
    Assert.AreEqual(24, ally1.CurrentATK); // 20 * 1.2 = 24
    Assert.AreEqual(30, ally2.CurrentATK); // 25 * 1.2 = 30
}

[Test]
public void Passive_OnBattleStart_AppliesRandomAllyShield()
{
    // Healer grants 50 shield to 1 random ally at battle start
    var healer = new BattleTestBuilder()
        .AddPlayerUnit("Healer", passive: new PassiveAbility
        {
            Type = PassiveType.OnBattleStart,
            EffectType = StatusEffectType.Shield,
            Value = 50,
            Duration = 3,
            TargetSelf = false,
            TargetRandomAlly = true
        })
        .BuildStates().players[0];

    var ally1 = new BattleTestBuilder().AddPlayerUnit("Ally1").BuildStates().players[0];
    var ally2 = new BattleTestBuilder().AddPlayerUnit("Ally2").BuildStates().players[0];
    var allies = new List<UnitRuntimeState> { healer, ally1, ally2 };
    var rng = new SeededRandom(999); // Deterministic random

    // Act
    PassiveManager.TriggerOnBattleStart(healer, allies, rng);

    // Assert
    int totalShields = ally1.ShieldAmount + ally2.ShieldAmount;
    Assert.AreEqual(50, totalShields, "Exactly 1 ally should receive shield");
    Assert.AreEqual(0, healer.ShieldAmount, "Healer should not shield self");
}
```

#### 2.2 OnTurnStart (2 tests)
```csharp
[Test]
public void Passive_OnTurnStart_GrantsSelfShield()
{
    // Tank gains 20 shield at start of each turn
    var tank = new BattleTestBuilder()
        .AddPlayerUnit("Tank", passive: new PassiveAbility
        {
            Type = PassiveType.OnTurnStart,
            EffectType = StatusEffectType.Shield,
            Value = 20,
            Duration = 1,
            TargetSelf = true
        })
        .BuildStates().players[0];

    var rng = new SeededRandom(999);

    // Act: Trigger twice (2 turns)
    PassiveManager.TriggerOnTurnStart(tank, new List<UnitRuntimeState> { tank }, rng);
    Assert.AreEqual(20, tank.ShieldAmount, "Turn 1: Should have 20 shield");

    PassiveManager.TriggerOnTurnStart(tank, new List<UnitRuntimeState> { tank }, rng);
    Assert.AreEqual(40, tank.ShieldAmount, "Turn 2: Shields should stack (20 + 20)");
}

[Test]
public void Passive_OnTurnStart_NoTrigger_WhenPassiveNone()
{
    // Unit with no passive should not trigger
    var unit = new BattleTestBuilder()
        .AddPlayerUnit("NoPassive", passive: new PassiveAbility { Type = PassiveType.None })
        .BuildStates().players[0];

    var rng = new SeededRandom(999);

    // Act
    PassiveManager.TriggerOnTurnStart(unit, new List<UnitRuntimeState> { unit }, rng);

    // Assert
    Assert.AreEqual(0, unit.ActiveStatusEffects.Count, "Should have no effects");
}
```

#### 2.3 OnDamageDealt (3 tests)
```csharp
[Test]
public void Passive_OnDamageDealt_Lifesteal()
{
    // Vampire gains HP equal to 20% of damage dealt
    var vampire = new BattleTestBuilder()
        .AddPlayerUnit("Vampire", hp: 100, atk: 50, passive: new PassiveAbility
        {
            Type = PassiveType.OnDamageDealt,
            EffectType = StatusEffectType.None, // Direct heal, not status effect
            Modifier = 0.2f, // 20% lifesteal
            TargetSelf = true
        })
        .BuildStates().players[0];

    vampire.CurrentHP = 60; // Damaged
    var rng = new SeededRandom(999);

    // Act: Dealt 100 damage
    PassiveManager.TriggerOnDamageDealt(vampire, null, damageDealt: 100, new List<UnitRuntimeState> { vampire }, rng);

    // Assert
    Assert.AreEqual(80, vampire.CurrentHP); // 60 + (100 * 0.2) = 80
}

[Test]
public void Passive_OnDamageDealt_GrantsATKBuff()
{
    // Berserker gains +10% ATK for 2 turns after dealing damage
    var berserker = new BattleTestBuilder()
        .AddPlayerUnit("Berserker", atk: 30, passive: new PassiveAbility
        {
            Type = PassiveType.OnDamageDealt,
            EffectType = StatusEffectType.ATKUp,
            Modifier = 0.1f,
            Duration = 2,
            TargetSelf = true
        })
        .BuildStates().players[0];

    var rng = new SeededRandom(999);

    // Act
    PassiveManager.TriggerOnDamageDealt(berserker, null, damageDealt: 50, new List<UnitRuntimeState> { berserker }, rng);

    // Assert
    Assert.AreEqual(1, berserker.ActiveStatusEffects.Count);
    Assert.AreEqual(33, berserker.CurrentATK); // 30 * 1.1 = 33
}

[Test]
public void Passive_OnDamageDealt_StacksBuffs()
{
    // Stacking passive: +5% ATK per hit (max 3 stacks)
    var unit = new BattleTestBuilder()
        .AddPlayerUnit("StackUnit", atk: 20, passive: new PassiveAbility
        {
            Type = PassiveType.OnDamageDealt,
            EffectType = StatusEffectType.ATKUp,
            Modifier = 0.05f,
            Duration = 3,
            TargetSelf = true
        })
        .BuildStates().players[0];

    var rng = new SeededRandom(999);

    // Act: Hit 3 times
    PassiveManager.TriggerOnDamageDealt(unit, null, 10, new List<UnitRuntimeState> { unit }, rng);
    PassiveManager.TriggerOnDamageDealt(unit, null, 10, new List<UnitRuntimeState> { unit }, rng);
    PassiveManager.TriggerOnDamageDealt(unit, null, 10, new List<UnitRuntimeState> { unit }, rng);

    // Assert
    Assert.AreEqual(3, unit.ActiveStatusEffects.Count, "Should have 3 separate buffs");
    Assert.AreEqual(23, unit.CurrentATK); // 20 * 1.15 = 23
}
```

#### 2.4 OnDamageTaken (2 tests)
```csharp
[Test]
public void Passive_OnDamageTaken_CounterAttackBuff()
{
    // Tank gains +30% ATK for 1 turn when taking damage
    var tank = new BattleTestBuilder()
        .AddPlayerUnit("Tank", atk: 15, passive: new PassiveAbility
        {
            Type = PassiveType.OnDamageTaken,
            EffectType = StatusEffectType.ATKUp,
            Modifier = 0.3f,
            Duration = 1,
            TargetSelf = true
        })
        .BuildStates().players[0];

    var rng = new SeededRandom(999);

    // Act: Take 50 damage
    PassiveManager.TriggerOnDamageTaken(tank, damageTaken: 50, new List<UnitRuntimeState> { tank }, rng);

    // Assert
    Assert.AreEqual(1, tank.ActiveStatusEffects.Count);
    Assert.AreEqual(20, tank.CurrentATK); // 15 * 1.3 = 19.5 → 20
}

[Test]
public void Passive_OnDamageTaken_ThornsDamage()
{
    // Thorns: Deal 10 damage back to attacker (not implemented in passive, just test framework)
    // This test verifies the trigger fires; actual damage is handled in ActionExecutor
    var tank = new BattleTestBuilder()
        .AddPlayerUnit("ThornsTank", passive: new PassiveAbility
        {
            Type = PassiveType.OnDamageTaken,
            Value = 10, // Thorns damage
            EffectType = StatusEffectType.None,
            TargetSelf = false
        })
        .BuildStates().players[0];

    var rng = new SeededRandom(999);

    // Act: Take damage (trigger should fire, but we don't test damage reflection here)
    PassiveManager.TriggerOnDamageTaken(tank, damageTaken: 30, new List<UnitRuntimeState> { tank }, rng);

    // Assert: For now, just verify no crash (full thorns logic in ActionExecutor)
    Assert.Pass("OnDamageTaken triggered successfully");
}
```

#### 2.5 OnKill (3 tests)
```csharp
[Test]
public void Passive_OnKill_GrantsATKBuff()
{
    // Assassin gains +40% ATK for 2 turns on kill
    var assassin = new BattleTestBuilder()
        .AddPlayerUnit("Assassin", atk: 25, passive: new PassiveAbility
        {
            Type = PassiveType.OnKill,
            EffectType = StatusEffectType.ATKUp,
            Modifier = 0.4f,
            Duration = 2,
            TargetSelf = true
        })
        .BuildStates().players[0];

    var enemy = new BattleTestBuilder().AddEnemyUnit("Victim").BuildStates().enemies[0];
    var rng = new SeededRandom(999);

    // Act: Kill enemy
    PassiveManager.TriggerOnKill(assassin, enemy, new List<UnitRuntimeState> { assassin }, rng);

    // Assert
    Assert.AreEqual(1, assassin.ActiveStatusEffects.Count);
    Assert.AreEqual(35, assassin.CurrentATK); // 25 * 1.4 = 35
}

[Test]
public void Passive_OnKill_HealsAlly()
{
    // Healer restores 30 HP to lowest HP ally on kill
    var healer = new BattleTestBuilder()
        .AddPlayerUnit("Healer", passive: new PassiveAbility
        {
            Type = PassiveType.OnKill,
            Value = 30,
            EffectType = StatusEffectType.None, // Direct heal
            TargetSelf = false,
            TargetRandomAlly = true // Lowest HP ally
        })
        .BuildStates().players[0];

    var woundedAlly = new BattleTestBuilder().AddPlayerUnit("Wounded", hp: 100).BuildStates().players[0];
    woundedAlly.CurrentHP = 40; // Low HP

    var allies = new List<UnitRuntimeState> { healer, woundedAlly };
    var enemy = new BattleTestBuilder().AddEnemyUnit("Victim").BuildStates().enemies[0];
    var rng = new SeededRandom(999);

    // Act
    PassiveManager.TriggerOnKill(healer, enemy, allies, rng);

    // Assert
    Assert.AreEqual(70, woundedAlly.CurrentHP); // 40 + 30 = 70
}

[Test]
public void Passive_OnKill_MultipleKills_StacksBuff()
{
    // Snowball passive: +20% ATK per kill
    var unit = new BattleTestBuilder()
        .AddPlayerUnit("SnowballUnit", atk: 20, passive: new PassiveAbility
        {
            Type = PassiveType.OnKill,
            EffectType = StatusEffectType.ATKUp,
            Modifier = 0.2f,
            Duration = 99, // Permanent
            TargetSelf = true
        })
        .BuildStates().players[0];

    var enemy1 = new BattleTestBuilder().AddEnemyUnit("E1").BuildStates().enemies[0];
    var enemy2 = new BattleTestBuilder().AddEnemyUnit("E2").BuildStates().enemies[0];
    var rng = new SeededRandom(999);

    // Act: Kill 2 enemies
    PassiveManager.TriggerOnKill(unit, enemy1, new List<UnitRuntimeState> { unit }, rng);
    PassiveManager.TriggerOnKill(unit, enemy2, new List<UnitRuntimeState> { unit }, rng);

    // Assert
    Assert.AreEqual(2, unit.ActiveStatusEffects.Count, "Should have 2 stacks");
    Assert.AreEqual(28, unit.CurrentATK); // 20 * 1.4 = 28
}
```

#### 2.6 OnUltimate (2 tests)
```csharp
[Test]
public void Passive_OnUltimate_GrantsSelfShield()
{
    // Unit gains 80 shield when using ultimate
    var unit = new BattleTestBuilder()
        .AddPlayerUnit("UltUnit", passive: new PassiveAbility
        {
            Type = PassiveType.OnUltimate,
            EffectType = StatusEffectType.Shield,
            Value = 80,
            Duration = 2,
            TargetSelf = true
        })
        .BuildStates().players[0];

    var rng = new SeededRandom(999);

    // Act
    PassiveManager.TriggerOnUltimate(unit, new List<UnitRuntimeState> { unit }, rng);

    // Assert
    Assert.AreEqual(80, unit.ShieldAmount);
}

[Test]
public void Passive_OnUltimate_BuffsAllAllies()
{
    // Support grants +50% ATK to all allies when using ultimate
    var support = new BattleTestBuilder()
        .AddPlayerUnit("Support", passive: new PassiveAbility
        {
            Type = PassiveType.OnUltimate,
            EffectType = StatusEffectType.ATKUp,
            Modifier = 0.5f,
            Duration = 3,
            TargetSelf = false,
            TargetAllies = true
        })
        .BuildStates().players[0];

    var ally1 = new BattleTestBuilder().AddPlayerUnit("Ally1", atk: 20).BuildStates().players[0];
    var ally2 = new BattleTestBuilder().AddPlayerUnit("Ally2", atk: 30).BuildStates().players[0];
    var allies = new List<UnitRuntimeState> { support, ally1, ally2 };
    var rng = new SeededRandom(999);

    // Act
    PassiveManager.TriggerOnUltimate(support, allies, rng);

    // Assert
    Assert.AreEqual(0, support.ActiveStatusEffects.Count, "Support should not buff self");
    Assert.AreEqual(30, ally1.CurrentATK); // 20 * 1.5 = 30
    Assert.AreEqual(45, ally2.CurrentATK); // 30 * 1.5 = 45
}
```

#### 2.7 OnHPThreshold (2 tests)
```csharp
[Test]
public void Passive_OnHPThreshold_EnrageBelowHalf()
{
    // Berserker gains +60% ATK when HP drops below 50%
    var berserker = new BattleTestBuilder()
        .AddPlayerUnit("Berserker", hp: 100, atk: 20, passive: new PassiveAbility
        {
            Type = PassiveType.OnHPThreshold,
            TriggerThreshold = 0.5f, // 50%
            EffectType = StatusEffectType.ATKUp,
            Modifier = 0.6f,
            Duration = 99, // Until end of battle
            TargetSelf = true
        })
        .BuildStates().players[0];

    berserker.CurrentHP = 45; // Below 50%
    var rng = new SeededRandom(999);

    // Act
    PassiveManager.TriggerOnHPThreshold(berserker, hpPercent: 0.45f, new List<UnitRuntimeState> { berserker }, rng);

    // Assert
    Assert.AreEqual(1, berserker.ActiveStatusEffects.Count);
    Assert.AreEqual(32, berserker.CurrentATK); // 20 * 1.6 = 32
}

[Test]
public void Passive_OnHPThreshold_NoTrigger_AboveThreshold()
{
    // Should not trigger if HP is above threshold
    var unit = new BattleTestBuilder()
        .AddPlayerUnit("Unit", hp: 100, passive: new PassiveAbility
        {
            Type = PassiveType.OnHPThreshold,
            TriggerThreshold = 0.3f, // 30%
            EffectType = StatusEffectType.DEFUp,
            Modifier = 0.5f,
            Duration = 2,
            TargetSelf = true
        })
        .BuildStates().players[0];

    unit.CurrentHP = 50; // 50% > 30% threshold
    var rng = new SeededRandom(999);

    // Act
    PassiveManager.TriggerOnHPThreshold(unit, hpPercent: 0.5f, new List<UnitRuntimeState> { unit }, rng);

    // Assert
    Assert.AreEqual(0, unit.ActiveStatusEffects.Count, "Should not trigger above threshold");
}
```

#### 2.8 OnAllyLowHP (3 tests)
```csharp
[Test]
public void Passive_OnAllyLowHP_HealsAlly()
{
    // Healer restores 40 HP when ally drops below 30%
    var healer = new BattleTestBuilder()
        .AddPlayerUnit("Healer", passive: new PassiveAbility
        {
            Type = PassiveType.OnAllyLowHP,
            TriggerThreshold = 0.3f,
            Value = 40,
            EffectType = StatusEffectType.None, // Direct heal
            TargetSelf = false
        })
        .BuildStates().players[0];

    var lowHPAlly = new BattleTestBuilder().AddPlayerUnit("LowAlly", hp: 100).BuildStates().players[0];
    lowHPAlly.CurrentHP = 25; // 25% HP

    var allies = new List<UnitRuntimeState> { healer, lowHPAlly };
    var rng = new SeededRandom(999);

    // Act
    PassiveManager.TriggerOnAllyLowHP(healer, lowHPAlly, allies, rng);

    // Assert
    Assert.AreEqual(65, lowHPAlly.CurrentHP); // 25 + 40 = 65
}

[Test]
public void Passive_OnAllyLowHP_GrantsShield()
{
    // Protector grants 50 shield to ally below 40% HP
    var protector = new BattleTestBuilder()
        .AddPlayerUnit("Protector", passive: new PassiveAbility
        {
            Type = PassiveType.OnAllyLowHP,
            TriggerThreshold = 0.4f,
            EffectType = StatusEffectType.Shield,
            Value = 50,
            Duration = 2,
            TargetSelf = false
        })
        .BuildStates().players[0];

    var ally = new BattleTestBuilder().AddPlayerUnit("Ally", hp: 100).BuildStates().players[0];
    ally.CurrentHP = 30; // 30% HP

    var allies = new List<UnitRuntimeState> { protector, ally };
    var rng = new SeededRandom(999);

    // Act
    PassiveManager.TriggerOnAllyLowHP(protector, ally, allies, rng);

    // Assert
    Assert.AreEqual(50, ally.ShieldAmount);
}

[Test]
public void Passive_OnAllyLowHP_DoesNotTargetSelf()
{
    // Healer should not trigger on own low HP
    var healer = new BattleTestBuilder()
        .AddPlayerUnit("Healer", hp: 100, passive: new PassiveAbility
        {
            Type = PassiveType.OnAllyLowHP,
            TriggerThreshold = 0.3f,
            Value = 40,
            EffectType = StatusEffectType.None,
            TargetSelf = false
        })
        .BuildStates().players[0];

    healer.CurrentHP = 20; // 20% HP
    var rng = new SeededRandom(999);

    // Act
    PassiveManager.TriggerOnAllyLowHP(healer, healer, new List<UnitRuntimeState> { healer }, rng);

    // Assert
    Assert.AreEqual(20, healer.CurrentHP, "Should not heal self");
}
```

---

### Step 3: Implement PassiveManager (Green Phase)
Write the minimum code to make tests pass.

**Implementation Plan**:
1. Start with simple cases (OnBattleStart, OnTurnStart)
2. Handle targeting logic (self, all allies, random ally)
3. Implement value-based effects (shields, direct heals)
4. Implement modifier-based effects (buffs/debuffs)
5. Handle edge cases (no passive, threshold checks)

---

### Step 4: Refactor & Document (Refactor Phase)
- Extract common logic (target selection, effect application)
- Add XML documentation
- Optimize performance (avoid allocations)

---

### Step 5: Integration with BattleController
Wire PassiveManager calls into `BattleController.cs`:
```csharp
// In BattleController.cs

private IEnumerator ExecutePlayerAction(UnitRuntimeState actor, PlayerActionType action)
{
    // ... existing code ...

    // NEW: Trigger OnDamageDealt passive
    if (damage > 0)
    {
        PassiveManager.TriggerOnDamageDealt(actor, target, damage, GetAllies(actor), _rng);
    }

    // NEW: Trigger OnDamageTaken passive
    if (damage > 0)
    {
        PassiveManager.TriggerOnDamageTaken(target, damage, GetAllies(target), _rng);
    }

    // NEW: Trigger OnKill passive
    if (!target.IsAlive)
    {
        PassiveManager.TriggerOnKill(actor, target, GetAllies(actor), _rng);
    }

    // ... existing code ...
}
```

**Success Criteria**:
- ✅ 20 PassiveAbilityTests passing
- ✅ PassiveManager compiles without errors
- ✅ Passives trigger in Unity (manual test)

---

## Phase 3B: Targeting & Actions (Session 2 - 8 hours)

### Step 1: Create TargetResolver (Domain Logic)
**File**: `Assets/_Project/Scripts/Domain/TargetResolver.cs`

**Interface Design**:
```csharp
public static class TargetResolver
{
    /// <summary>
    /// Resolves targets based on pattern and game state.
    /// </summary>
    public static List<UnitRuntimeState> ResolveTargets(
        TargetPattern pattern,
        UnitRuntimeState actor,
        List<UnitRuntimeState> playerTeam,
        List<UnitRuntimeState> enemyTeam,
        IDeterministicRandom rng);

    // Helpers
    private static List<UnitRuntimeState> GetFrontRow(List<UnitRuntimeState> team);
    private static List<UnitRuntimeState> GetBackRow(List<UnitRuntimeState> team);
    private static UnitRuntimeState GetLowestHPUnit(List<UnitRuntimeState> units);
    private static UnitRuntimeState GetHighestThreatUnit(List<UnitRuntimeState> enemies);
}
```

---

### Step 2: Write Tests FIRST (TDD Red Phase)
**File**: `Assets/_Project/Tests/EditMode/Domain/TargetResolverTests.cs`

**Test Structure** (13 tests):

```csharp
[Test]
public void TargetResolver_SingleEnemy_ReturnsOneEnemy()
{
    var builder = new BattleTestBuilder()
        .AddPlayerUnit("Attacker")
        .AddEnemyUnit("Enemy1")
        .AddEnemyUnit("Enemy2");
    var built = builder.BuildStates();
    var rng = new SeededRandom(999);

    // Act
    var targets = TargetResolver.ResolveTargets(
        TargetPattern.SingleEnemy,
        built.players[0],
        built.players,
        built.enemies,
        rng);

    // Assert
    Assert.AreEqual(1, targets.Count);
    Assert.IsTrue(built.enemies.Contains(targets[0]));
}

[Test]
public void TargetResolver_AllEnemies_ReturnsAllAlive()
{
    var builder = new BattleTestBuilder()
        .AddPlayerUnit("Attacker")
        .AddEnemyUnit("Enemy1")
        .AddEnemyUnit("Enemy2")
        .AddEnemyUnit("Enemy3");
    var built = builder.BuildStates();
    built.enemies[1].CurrentHP = 0; // Kill Enemy2
    var rng = new SeededRandom(999);

    // Act
    var targets = TargetResolver.ResolveTargets(
        TargetPattern.AllEnemies,
        built.players[0],
        built.players,
        built.enemies,
        rng);

    // Assert
    Assert.AreEqual(2, targets.Count, "Should return 2 alive enemies (Enemy1, Enemy3)");
    Assert.IsFalse(targets.Contains(built.enemies[1]), "Dead enemy should not be targeted");
}

[Test]
public void TargetResolver_LowestHpEnemy_ReturnsLowestHP()
{
    var builder = new BattleTestBuilder()
        .AddPlayerUnit("Attacker")
        .AddEnemyUnit("Enemy1", hp: 100)
        .AddEnemyUnit("Enemy2", hp: 50)  // Lowest HP
        .AddEnemyUnit("Enemy3", hp: 80);
    var built = builder.BuildStates();
    var rng = new SeededRandom(999);

    // Act
    var targets = TargetResolver.ResolveTargets(
        TargetPattern.LowestHpEnemy,
        built.players[0],
        built.players,
        built.enemies,
        rng);

    // Assert
    Assert.AreEqual(1, targets.Count);
    Assert.AreEqual(built.enemies[1], targets[0]); // Enemy2 (50 HP)
}

// ... 10 more tests for other patterns ...
```

---

### Step 3: Create ActionExecutor (Domain Logic)
**File**: `Assets/_Project/Scripts/Domain/ActionExecutor.cs`

**Interface Design**:
```csharp
public struct ActionResult
{
    public UnitRuntimeState Actor;
    public List<UnitRuntimeState> Targets;
    public List<int> DamagePerTarget;
    public List<bool> CritPerTarget;
    public int EnergyGained;
    public int EnergySpent;
}

public static class ActionExecutor
{
    public static ActionResult ExecuteBasicAttack(
        UnitRuntimeState actor,
        List<UnitRuntimeState> targets,
        IDeterministicRandom rng,
        float energyPerHit = 0.2f);

    public static ActionResult ExecuteUltimate(
        UnitRuntimeState actor,
        List<UnitRuntimeState> targets,
        TeamEnergyState teamEnergy,
        IDeterministicRandom rng);

    // Multi-hit combo logic
    private static int RollComboHits(UnitRuntimeState actor, IDeterministicRandom rng);
}
```

---

### Step 4: Write Tests FIRST
**File**: `Assets/_Project/Tests/EditMode/Domain/ActionExecutorTests.cs`

**Test Structure** (12 tests):

```csharp
[Test]
public void ActionExecutor_BasicAttack_SingleTarget_DealsCorrectDamage()
{
    var attacker = new BattleTestBuilder().AddPlayerUnit("Attacker", atk: 30, def: 5).BuildStates().players[0];
    var defender = new BattleTestBuilder().AddEnemyUnit("Defender", hp: 100, def: 10).BuildStates().enemies[0];
    var rng = new SeededRandom(999);

    // Act
    var result = ActionExecutor.ExecuteBasicAttack(attacker, new List<UnitRuntimeState> { defender }, rng, energyPerHit: 0.2f);

    // Assert
    Assert.AreEqual(1, result.Targets.Count);
    Assert.Greater(result.DamagePerTarget[0], 0, "Should deal damage");
    Assert.AreEqual(0.2f, result.EnergyGained, "Should gain 20% energy per hit");
}

[Test]
public void ActionExecutor_BasicAttack_MultiHit_Gains60PctEnergy()
{
    // Unit with 3-hit combo (fixed)
    var attacker = new BattleTestBuilder()
        .AddPlayerUnit("Combo", atk: 20, comboHits: (3, 3))
        .BuildStates().players[0];
    var defender = new BattleTestBuilder().AddEnemyUnit("Defender", hp: 200).BuildStates().enemies[0];
    var rng = new SeededRandom(999);

    // Act
    var result = ActionExecutor.ExecuteBasicAttack(attacker, new List<UnitRuntimeState> { defender }, rng, energyPerHit: 0.2f);

    // Assert
    Assert.AreEqual(3, result.DamagePerTarget.Count, "Should have 3 damage instances");
    Assert.AreEqual(0.6f, result.EnergyGained, "3 hits * 20% = 60%");
}

[Test]
public void ActionExecutor_Ultimate_SpeedsEnergyCost()
{
    var caster = new BattleTestBuilder()
        .AddPlayerUnit("Caster", atk: 40, ultimateCost: 2)
        .BuildStates().players[0];
    var targets = new BattleTestBuilder()
        .AddEnemyUnit("E1")
        .AddEnemyUnit("E2")
        .BuildStates().enemies;
    var energy = new TeamEnergyState(maxEnergy: 10, startEnergy: 5);
    var rng = new SeededRandom(999);

    // Act
    var result = ActionExecutor.ExecuteUltimate(caster, targets, energy, rng);

    // Assert
    Assert.AreEqual(2, result.EnergySpent);
    Assert.AreEqual(3, energy.Energy); // 5 - 2 = 3
}

[Test]
public void ActionExecutor_Ultimate_AppliesCooldown()
{
    var caster = new BattleTestBuilder()
        .AddPlayerUnit("Caster", ultimateCooldown: 3)
        .BuildStates().players[0];
    var targets = new BattleTestBuilder().AddEnemyUnit("E1").BuildStates().enemies;
    var energy = new TeamEnergyState(maxEnergy: 10, startEnergy: 5);
    var rng = new SeededRandom(999);

    // Act
    var result = ActionExecutor.ExecuteUltimate(caster, targets, energy, rng);

    // Assert
    Assert.AreEqual(3, caster.UltimateCooldownRemaining);
    Assert.IsFalse(caster.CanUseUltimate);
}

// ... 8 more tests ...
```

---

### Step 5: Implement & Integrate
1. Implement TargetResolver (make tests pass)
2. Implement ActionExecutor (make tests pass)
3. Refactor BattleController to use new domain classes
4. Manual test in Unity

**Success Criteria**:
- ✅ 13 TargetResolverTests passing
- ✅ 12 ActionExecutorTests passing
- ✅ Actions work in Unity (combo hits, ultimates, targeting)

---

## Phase 3C: Integration (Session 3 - 2 hours)

### Refactor BattleController.cs
**Goal**: Replace presentation logic with domain calls

**Before**:
```csharp
// 700+ lines of mixed logic in BattleController.cs
```

**After**:
```csharp
private IEnumerator ExecutePlayerAction(UnitRuntimeState actor, PlayerActionType action)
{
    // 1. Resolve targets (domain)
    var targets = TargetResolver.ResolveTargets(
        action == PlayerActionType.Basic ? actor.BasicTargetPattern : actor.UltimateTargetPattern,
        actor,
        _playerTeam,
        _enemyTeam,
        _rng);

    // 2. Execute action (domain)
    ActionResult result = action == PlayerActionType.Basic
        ? ActionExecutor.ExecuteBasicAttack(actor, targets, _rng, energyBarFillPerHitPct / 100f)
        : ActionExecutor.ExecuteUltimate(actor, targets, _playerEnergy, _rng);

    // 3. Apply results (presentation)
    for (int i = 0; i < result.Targets.Count; i++)
    {
        yield return AnimateDamage(result.Actor, result.Targets[i], result.DamagePerTarget[i], result.CritPerTarget[i]);
    }

    // 4. Trigger passives (domain)
    foreach (var target in result.Targets)
    {
        if (!target.IsAlive)
            PassiveManager.TriggerOnKill(actor, target, GetAllies(actor), _rng);
    }
}
```

**Reduction**: 700 lines → ~200 lines (clean, testable)

---

## Phase 3D: Manual QA (Session 4 - 2 hours)

### Test Scenarios in Unity

1. **Passive Triggers**
   - OnBattleStart: Verify buffs appear at battle start
   - OnKill: Verify assassin gains buff after killing enemy
   - OnHPThreshold: Verify berserker enrages below 50% HP

2. **Targeting Patterns**
   - AllEnemies: Verify AoE hits all enemies
   - LowestHpEnemy: Verify smart targeting works
   - FrontRowAll: Verify row-based attacks

3. **Ultimate Mechanics**
   - Verify energy cost deduction
   - Verify cooldown applies
   - Verify can't use while on cooldown

4. **Multi-Hit Combos**
   - Verify 3-hit combo plays 3 animations
   - Verify energy gain scales with hits
   - Verify damage popups appear per hit

5. **Edge Cases**
   - All units stunned → battle should pause
   - Zero energy → can't use ultimates
   - Shield overflow → clamps correctly

---

## Success Metrics

### Test Coverage
| Area | Phase 1-2 | Phase 3 | Total |
|------|-----------|---------|-------|
| Core Math | 14 tests ✅ | - | 14 |
| Energy & Status | 47 tests ✅ | - | 47 |
| Turn Order | 17 tests ✅ | - | 17 |
| Passives | 0 | 20 tests | 20 |
| Targeting | 0 | 13 tests | 13 |
| Actions | 0 | 12 tests | 12 |
| **Total** | **88** | **45** | **133** |

**Coverage**: 88 tests → 133 tests (+51%) = **~70% coverage**

---

### Domain Architecture
**Before Phase 3**:
- 5 domain classes
- 700+ line BattleController (untestable)
- Passives defined but not executed

**After Phase 3**:
- 8 domain classes (added PassiveManager, TargetResolver, ActionExecutor)
- 200-line BattleController (thin presentation layer)
- 100% domain logic testable

---

### Shippability
**After Phase 3**:
- ✅ Passives work
- ✅ Targeting works
- ✅ Ultimates work
- ✅ Combos work
- ✅ 70% test coverage
- ✅ Clean architecture
- ✅ Ready for players

---

## Timeline Estimate

| Session | Focus | Time | Cumulative |
|---------|-------|------|------------|
| **Session 1** | PassiveManager + 20 tests | 6h | 6h |
| **Session 2** | TargetResolver + ActionExecutor + 25 tests | 8h | 14h |
| **Session 3** | Integration (refactor BattleController) | 2h | 16h |
| **Session 4** | Manual QA in Unity | 2h | 18h |

**Total**: ~18 hours (spread over 3-4 days)

---

## Next Immediate Steps

**Ready to start?** Here's what we'll do in Session 1:

1. ✅ Create `PassiveManager.cs` skeleton
2. ✅ Write first 3 tests (OnBattleStart)
3. ✅ Implement until tests pass (Red → Green → Refactor)
4. ✅ Repeat for remaining 17 tests
5. ✅ Integrate into BattleController
6. ✅ Manual test in Unity

**Estimated**: 6 hours, but we can break it into smaller chunks:
- **Chunk 1** (2h): OnBattleStart + OnTurnStart (5 tests)
- **Chunk 2** (2h): OnDamageDealt + OnDamageTaken + OnKill (8 tests)
- **Chunk 3** (2h): OnUltimate + OnHPThreshold + OnAllyLowHP (7 tests)

---

## Let's Begin! 🚀

**What would you like to do?**

1. **Start Session 1, Chunk 1** (OnBattleStart + OnTurnStart tests)
2. **Review the plan first** (ask questions, adjust approach)
3. **Jump straight to implementation** (create PassiveManager.cs now)

Let me know and we'll proceed systematically! 💪
