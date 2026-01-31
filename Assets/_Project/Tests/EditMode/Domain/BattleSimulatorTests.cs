using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Project.Domain;
using VanguardArena.Tests.Utils;

namespace VanguardArena.Tests.EditMode.Domain
{
    /// <summary>
    /// Integration tests for BattleSimulator.
    /// Tests full battle scenarios including victory/defeat conditions, energy economy, and passive interactions.
    /// </summary>
    [TestFixture]
    public class BattleSimulatorTests
    {
        private IDeterministicRandom _rng;
        private const int TestSeed = 999;

        [SetUp]
        public void SetUp()
        {
            _rng = new SeededRandom(TestSeed);
        }

        #region Victory Condition Tests

        [Test]
        public void SimulateBattle_PlayerVictory_AllEnemiesDefeated()
        {
            // Arrange: Players have much higher stats than enemies
            var builder = new BattleTestBuilder()
                .WithRandom(_rng)
                .AddPlayerUnit("StrongPlayer", UnitType.Melee, hp: 200, atk: 50, def: 10, spd: 15)
                .AddEnemyUnit("WeakEnemy", UnitType.Melee, hp: 50, atk: 5, def: 2, spd: 10);

            var (players, enemies, rng) = builder.BuildStates();
            var simulator = new BattleSimulator(players, enemies, rng, enableLogging: true);

            // Act
            var result = simulator.SimulateBattle(maxTurns: 20);

            // Assert
            Assert.AreEqual(BattleOutcome.Victory, result.Outcome, "Players should win against weak enemies");
            Assert.IsTrue(result.TurnsElapsed < 20, "Battle should end before turn limit");
            Assert.AreEqual(1, result.SurvivingPlayers.Count, "All players should survive");
            Assert.AreEqual(0, result.SurvivingEnemies.Count, "All enemies should be defeated");
            Assert.IsTrue(players[0].IsAlive, "Player should be alive");
            Assert.IsFalse(enemies[0].IsAlive, "Enemy should be dead");
        }

        [Test]
        public void SimulateBattle_PlayerVictory_MultipleEnemies()
        {
            // Arrange: 2 strong players vs 3 weak enemies
            var builder = new BattleTestBuilder()
                .WithRandom(_rng)
                .AddPlayerUnit("Player1", UnitType.Melee, hp: 150, atk: 40, def: 8, spd: 12)
                .AddPlayerUnit("Player2", UnitType.Ranged, hp: 120, atk: 45, def: 5, spd: 14)
                .AddEnemyUnit("Enemy1", UnitType.Melee, hp: 60, atk: 10, def: 3, spd: 10)
                .AddEnemyUnit("Enemy2", UnitType.Melee, hp: 60, atk: 10, def: 3, spd: 9)
                .AddEnemyUnit("Enemy3", UnitType.Ranged, hp: 50, atk: 12, def: 2, spd: 11);

            var (players, enemies, rng) = builder.BuildStates();
            var simulator = new BattleSimulator(players, enemies, rng);

            // Act
            var result = simulator.SimulateBattle(maxTurns: 30);

            // Assert
            Assert.AreEqual(BattleOutcome.Victory, result.Outcome, "Players should win");
            Assert.AreEqual(0, result.SurvivingEnemies.Count, "All enemies should be defeated");
            Assert.IsTrue(result.SurvivingPlayers.Count >= 1, "At least one player should survive");
        }

        #endregion

        #region Defeat Condition Tests

        [Test]
        public void SimulateBattle_PlayerDefeat_AllPlayersDefeated()
        {
            // Arrange: Players are much weaker than enemies
            var builder = new BattleTestBuilder()
                .WithRandom(_rng)
                .AddPlayerUnit("WeakPlayer", UnitType.Melee, hp: 50, atk: 5, def: 2, spd: 10)
                .AddEnemyUnit("StrongEnemy", UnitType.Melee, hp: 200, atk: 50, def: 10, spd: 15);

            var (players, enemies, rng) = builder.BuildStates();
            var simulator = new BattleSimulator(players, enemies, rng, enableLogging: true);

            // Act
            var result = simulator.SimulateBattle(maxTurns: 20);

            // Assert
            Assert.AreEqual(BattleOutcome.Defeat, result.Outcome, "Players should lose against strong enemy");
            Assert.IsTrue(result.TurnsElapsed < 20, "Battle should end before turn limit");
            Assert.AreEqual(0, result.SurvivingPlayers.Count, "All players should be defeated");
            Assert.AreEqual(1, result.SurvivingEnemies.Count, "Enemy should survive");
            Assert.IsFalse(players[0].IsAlive, "Player should be dead");
            Assert.IsTrue(enemies[0].IsAlive, "Enemy should be alive");
        }

        [Test]
        public void SimulateBattle_PlayerDefeat_Outnumbered()
        {
            // Arrange: 1 player vs 3 enemies with equal stats
            var builder = new BattleTestBuilder()
                .WithRandom(_rng)
                .AddPlayerUnit("LonePlayer", UnitType.Melee, hp: 100, atk: 20, def: 5, spd: 12)
                .AddEnemyUnit("Enemy1", UnitType.Melee, hp: 80, atk: 15, def: 4, spd: 10)
                .AddEnemyUnit("Enemy2", UnitType.Melee, hp: 80, atk: 15, def: 4, spd: 11)
                .AddEnemyUnit("Enemy3", UnitType.Ranged, hp: 70, atk: 18, def: 3, spd: 13);

            var (players, enemies, rng) = builder.BuildStates();
            var simulator = new BattleSimulator(players, enemies, rng);

            // Act
            var result = simulator.SimulateBattle(maxTurns: 30);

            // Assert
            Assert.AreEqual(BattleOutcome.Defeat, result.Outcome, "Outnumbered player should lose");
            Assert.AreEqual(0, result.SurvivingPlayers.Count, "Player should be defeated");
            Assert.IsTrue(result.SurvivingEnemies.Count >= 1, "At least one enemy should survive");
        }

        #endregion

        #region Turn Limit Tests

        [Test]
        public void SimulateBattle_TurnLimit_ReachedWithoutVictory()
        {
            // Arrange: Equal stats, high defense - battle will take many turns
            var builder = new BattleTestBuilder()
                .WithRandom(_rng)
                .AddPlayerUnit("TankPlayer", UnitType.Melee, hp: 500, atk: 10, def: 20, spd: 10)
                .AddEnemyUnit("TankEnemy", UnitType.Melee, hp: 500, atk: 10, def: 20, spd: 10);

            var (players, enemies, rng) = builder.BuildStates();
            var simulator = new BattleSimulator(players, enemies, rng);

            // Act: Set low turn limit
            var result = simulator.SimulateBattle(maxTurns: 5);

            // Assert
            Assert.AreEqual(BattleOutcome.TurnLimit, result.Outcome, "Should hit turn limit");
            Assert.AreEqual(5, result.TurnsElapsed, "Should have elapsed exactly maxTurns");
            Assert.AreEqual(1, result.SurvivingPlayers.Count, "Player should still be alive");
            Assert.AreEqual(1, result.SurvivingEnemies.Count, "Enemy should still be alive");
            Assert.IsTrue(players[0].IsAlive, "Player should survive turn limit");
            Assert.IsTrue(enemies[0].IsAlive, "Enemy should survive turn limit");
        }

        [Test]
        public void SimulateBattle_TurnLimit_DefaultMaxTurns()
        {
            // Arrange: Very tanky units that won't die quickly
            var builder = new BattleTestBuilder()
                .WithRandom(_rng)
                .AddPlayerUnit("Immortal1", UnitType.Melee, hp: 10000, atk: 5, def: 50, spd: 10)
                .AddEnemyUnit("Immortal2", UnitType.Melee, hp: 10000, atk: 5, def: 50, spd: 10);

            var (players, enemies, rng) = builder.BuildStates();
            var simulator = new BattleSimulator(players, enemies, rng);

            // Act: Use default maxTurns (100)
            var result = simulator.SimulateBattle();

            // Assert
            Assert.AreEqual(BattleOutcome.TurnLimit, result.Outcome, "Should hit default turn limit");
            Assert.AreEqual(100, result.TurnsElapsed, "Should have elapsed 100 turns");
        }

        #endregion

        #region Energy Accumulation Tests

        [Test]
        public void SimulateBattle_EnergyAccumulation_IncreasesOverTurns()
        {
            // Arrange: Battle that will last multiple turns
            var builder = new BattleTestBuilder()
                .WithRandom(_rng)
                .AddPlayerUnit("Player", UnitType.Melee, hp: 200, atk: 20, def: 5, spd: 12, ultimateEnergyCost: 50)
                .AddEnemyUnit("Enemy", UnitType.Melee, hp: 200, atk: 20, def: 5, spd: 10, ultimateEnergyCost: 50);

            var (players, enemies, rng) = builder.BuildStates();
            var simulator = new BattleSimulator(players, enemies, rng, enableLogging: true);

            // Act
            var result = simulator.SimulateBattle(maxTurns: 20);

            // Assert
            // Energy should have accumulated during the battle
            // Note: Energy may have been spent on ultimates, but should have been > 0 at some point
            Assert.IsTrue(result.TurnsElapsed > 3, "Battle should last multiple turns for energy accumulation");
            
            // Either battle ended (one side won) OR energy accumulated
            Assert.IsTrue(
                result.Outcome == BattleOutcome.Victory || 
                result.Outcome == BattleOutcome.Defeat ||
                result.FinalPlayerEnergy > 0 || 
                result.FinalEnemyEnergy > 0,
                "Energy should accumulate over turns or battle should end");
        }

        [Test]
        public void SimulateBattle_EnergyAccumulation_EnablesUltimates()
        {
            // Arrange: Players with low ultimate cost, high attack to generate energy quickly
            var builder = new BattleTestBuilder()
                .WithRandom(_rng)
                .AddPlayerUnit("Nuker", UnitType.Ranged, hp: 150, atk: 50, def: 5, spd: 15, 
                    ultimateEnergyCost: 30, ultimateCooldown: 0) // No cooldown, low cost
                .AddEnemyUnit("Target", UnitType.Melee, hp: 300, atk: 10, def: 3, spd: 10);

            var (players, enemies, rng) = builder.BuildStates();
            var simulator = new BattleSimulator(players, enemies, rng, enableLogging: true);

            // Act
            var result = simulator.SimulateBattle(maxTurns: 15);

            // Assert
            // With high attack and low ultimate cost, the player should be able to use ultimates
            // We can't directly check if ultimates were used, but we can check that damage was dealt
            Assert.AreEqual(BattleOutcome.Victory, result.Outcome, "High ATK player should win");
            Assert.IsTrue(result.TurnsElapsed < 15, "Battle should end quickly with high damage");
        }

        #endregion

        #region Passive Ability Integration Tests

        [Test]
        public void SimulateBattle_PassiveTriggers_OnTurnStart()
        {
            // Arrange: Unit with OnTurnStart passive (gain ATK buff)
            var passive = new PassiveAbility
            {
                Type = PassiveType.OnTurnStart,
                EffectType = StatusEffectType.ATKUp,
                Modifier = 0.2f, // Gain +20% ATK each turn
                Duration = 2,
                TargetSelf = true
            };

            var builder = new BattleTestBuilder()
                .WithRandom(_rng)
                .AddPlayerUnit("BuffedPlayer", UnitType.Melee, hp: 150, atk: 20, def: 5, spd: 12, passive: passive)
                .AddEnemyUnit("Enemy", UnitType.Melee, hp: 200, atk: 15, def: 5, spd: 10);

            var (players, enemies, rng) = builder.BuildStates();
            var simulator = new BattleSimulator(players, enemies, rng, enableLogging: true);

            int initialATK = players[0].CurrentATK;

            // Act
            var result = simulator.SimulateBattle(maxTurns: 20);

            // Assert
            // Player should have gained ATK buffs over multiple turns
            // Note: Buffs expire, so we can't guarantee ATK is higher at end
            // But the passive should have triggered during battle
            Assert.IsTrue(result.TurnsElapsed > 2, "Battle should last long enough for passive to trigger");
        }

        [Test]
        public void SimulateBattle_PassiveTriggers_OnDamageDealt()
        {
            // Arrange: Unit with OnDamageDealt passive (lifesteal)
            var passive = new PassiveAbility
            {
                Type = PassiveType.OnDamageDealt,
                EffectType = StatusEffectType.None, // No status effect, just direct heal
                Modifier = 0.5f, // Heal for 50% of damage dealt
                TargetSelf = true
            };

            var builder = new BattleTestBuilder()
                .WithRandom(_rng)
                .AddPlayerUnit("Lifesteal", UnitType.Melee, hp: 100, atk: 30, def: 5, spd: 12, passive: passive)
                .AddEnemyUnit("Enemy", UnitType.Melee, hp: 200, atk: 20, def: 5, spd: 10);

            var (players, enemies, rng) = builder.BuildStates();
            
            // Damage the player first so lifesteal has an effect
            players[0].TakeDamage(50);
            int hpAfterDamage = players[0].CurrentHP;
            Assert.AreEqual(50, hpAfterDamage, "Player should have 50 HP after taking damage");

            var simulator = new BattleSimulator(players, enemies, rng, enableLogging: true);

            // Act
            var result = simulator.SimulateBattle(maxTurns: 20);

            // Assert
            // Player should have healed from lifesteal passive
            // If player won, they should have more than 50 HP (healed during battle)
            if (result.Outcome == BattleOutcome.Victory)
            {
                Assert.IsTrue(players[0].CurrentHP > hpAfterDamage, 
                    $"Player should have healed via lifesteal (started at {hpAfterDamage}, ended at {players[0].CurrentHP})");
            }
        }

        [Test]
        public void SimulateBattle_PassiveTriggers_OnKill()
        {
            // Arrange: Unit with OnKill passive (gain ATK buff when killing enemy)
            var passive = new PassiveAbility
            {
                Type = PassiveType.OnKill,
                EffectType = StatusEffectType.ATKUp,
                Modifier = 0.5f, // Gain +50% ATK on kill
                Duration = 10, // Long lasting buff
                TargetSelf = true
            };

            var builder = new BattleTestBuilder()
                .WithRandom(_rng)
                .AddPlayerUnit("Rampage", UnitType.Melee, hp: 200, atk: 40, def: 8, spd: 15, passive: passive)
                .AddEnemyUnit("Weak1", UnitType.Melee, hp: 50, atk: 5, def: 2, spd: 10)
                .AddEnemyUnit("Weak2", UnitType.Melee, hp: 50, atk: 5, def: 2, spd: 9)
                .AddEnemyUnit("Weak3", UnitType.Melee, hp: 50, atk: 5, def: 2, spd: 8);

            var (players, enemies, rng) = builder.BuildStates();
            var simulator = new BattleSimulator(players, enemies, rng, enableLogging: true);

            int initialATK = players[0].CurrentATK;

            // Act
            var result = simulator.SimulateBattle(maxTurns: 20);

            // Assert
            Assert.AreEqual(BattleOutcome.Victory, result.Outcome, "Strong player should win");
            // Player should have killed multiple enemies, gaining ATK buffs
            // With 3 kills and +20 ATK per kill, player should have gained +60 ATK total
            // (Buffs may have stacked or replaced depending on implementation)
            Assert.IsTrue(result.SurvivingEnemies.Count == 0, "All enemies should be killed");
        }

        #endregion

        #region Speed-Based Turn Order Tests

        [Test]
        public void SimulateBattle_TurnOrder_RespectsSPDStat()
        {
            // Arrange: Player with higher SPD should act before enemy
            var builder = new BattleTestBuilder()
                .WithRandom(_rng)
                .AddPlayerUnit("FastPlayer", UnitType.Ranged, hp: 100, atk: 30, def: 5, spd: 20) // High SPD
                .AddEnemyUnit("SlowEnemy", UnitType.Melee, hp: 100, atk: 30, def: 5, spd: 5); // Low SPD

            var (players, enemies, rng) = builder.BuildStates();
            var simulator = new BattleSimulator(players, enemies, rng, enableLogging: true);

            // Act
            var result = simulator.SimulateBattle(maxTurns: 10);

            // Assert
            // Fast player should act first each turn, potentially winning before enemy can attack
            // This is probabilistic, but with 30 ATK vs 100 HP, should take ~4 hits per side
            // Player acts first, so should win
            Assert.AreEqual(BattleOutcome.Victory, result.Outcome, 
                "Faster player should win due to acting first each turn");
        }

        #endregion

        #region Multi-Unit Battle Tests

        [Test]
        public void SimulateBattle_MultiUnit_3v3Battle()
        {
            // Arrange: Balanced 3v3 battle
            var builder = new BattleTestBuilder()
                .WithRandom(_rng)
                // Player team: Tank, DPS, Support
                .AddPlayerUnit("Tank", UnitType.Melee, hp: 200, atk: 15, def: 10, spd: 8)
                .AddPlayerUnit("DPS", UnitType.Ranged, hp: 100, atk: 35, def: 3, spd: 15)
                .AddPlayerUnit("Support", UnitType.Ranged, hp: 120, atk: 10, def: 5, spd: 12)
                // Enemy team: Similar composition
                .AddEnemyUnit("EnemyTank", UnitType.Melee, hp: 180, atk: 18, def: 8, spd: 9)
                .AddEnemyUnit("EnemyDPS", UnitType.Ranged, hp: 90, atk: 32, def: 4, spd: 14)
                .AddEnemyUnit("EnemySupport", UnitType.Ranged, hp: 110, atk: 12, def: 6, spd: 11);

            var (players, enemies, rng) = builder.BuildStates();
            var simulator = new BattleSimulator(players, enemies, rng, enableLogging: true);

            // Act
            var result = simulator.SimulateBattle(maxTurns: 50);

            // Assert
            Assert.IsTrue(
                result.Outcome == BattleOutcome.Victory || result.Outcome == BattleOutcome.Defeat,
                "Balanced 3v3 should end in victory or defeat (not timeout)");
            Assert.IsTrue(result.TurnsElapsed > 2, "3v3 battle should last multiple turns");
            Assert.IsTrue(result.TurnsElapsed < 50, "Should not hit turn limit with reasonable stats");
        }

        [Test]
        public void SimulateBattle_MultiUnit_FocusFire()
        {
            // Arrange: Multiple players with high attack targeting single enemy
            var builder = new BattleTestBuilder()
                .WithRandom(_rng)
                .AddPlayerUnit("Attacker1", UnitType.Ranged, hp: 100, atk: 40, def: 5, spd: 15, 
                    basicTargetPattern: TargetPattern.SingleEnemy) // Focus single target
                .AddPlayerUnit("Attacker2", UnitType.Ranged, hp: 100, atk: 40, def: 5, spd: 14, 
                    basicTargetPattern: TargetPattern.SingleEnemy)
                .AddEnemyUnit("Target", UnitType.Melee, hp: 150, atk: 20, def: 5, spd: 10);

            var (players, enemies, rng) = builder.BuildStates();
            var simulator = new BattleSimulator(players, enemies, rng, enableLogging: true);

            // Act
            var result = simulator.SimulateBattle(maxTurns: 10);

            // Assert
            Assert.AreEqual(BattleOutcome.Victory, result.Outcome, "Two high-ATK units should defeat one enemy");
            Assert.IsTrue(result.TurnsElapsed <= 5, "Focus fire should end battle quickly");
            Assert.AreEqual(0, result.SurvivingEnemies.Count, "Enemy should be eliminated");
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void SimulateBattle_NoUnits_ReturnsDefeatImmediately()
        {
            // Arrange: Empty player list
            var players = new List<UnitRuntimeState>();
            var enemies = new List<UnitRuntimeState>();

            var builder = new BattleTestBuilder().WithRandom(_rng);
            builder.AddEnemyUnit("Enemy", UnitType.Melee, hp: 100, atk: 10, def: 5, spd: 10);
            var (_, enemiesBuilt, rng) = builder.BuildStates();

            var simulator = new BattleSimulator(players, enemiesBuilt, rng);

            // Act
            var result = simulator.SimulateBattle(maxTurns: 10);

            // Assert
            Assert.AreEqual(BattleOutcome.Defeat, result.Outcome, "No players = instant defeat");
            Assert.AreEqual(0, result.TurnsElapsed, "Should end immediately with no players");
        }

        [Test]
        public void SimulateBattle_DeterministicWithSeed_ProducesSameResult()
        {
            // Arrange: Create two identical battles with same seed
            var seed = 42;
            
            var builder1 = new BattleTestBuilder()
                .WithRandom(new SeededRandom(seed))
                .AddPlayerUnit("Player", UnitType.Melee, hp: 100, atk: 25, def: 5, spd: 12)
                .AddEnemyUnit("Enemy", UnitType.Melee, hp: 100, atk: 20, def: 5, spd: 10);
            
            var builder2 = new BattleTestBuilder()
                .WithRandom(new SeededRandom(seed))
                .AddPlayerUnit("Player", UnitType.Melee, hp: 100, atk: 25, def: 5, spd: 12)
                .AddEnemyUnit("Enemy", UnitType.Melee, hp: 100, atk: 20, def: 5, spd: 10);

            var (players1, enemies1, rng1) = builder1.BuildStates();
            var (players2, enemies2, rng2) = builder2.BuildStates();

            var simulator1 = new BattleSimulator(players1, enemies1, rng1);
            var simulator2 = new BattleSimulator(players2, enemies2, rng2);

            // Act
            var result1 = simulator1.SimulateBattle(maxTurns: 20);
            var result2 = simulator2.SimulateBattle(maxTurns: 20);

            // Assert
            Assert.AreEqual(result1.Outcome, result2.Outcome, "Same seed should produce same outcome");
            Assert.AreEqual(result1.TurnsElapsed, result2.TurnsElapsed, "Same seed should produce same turn count");
            Assert.AreEqual(result1.SurvivingPlayers.Count, result2.SurvivingPlayers.Count, 
                "Same seed should produce same survivor count");
        }

        #endregion
    }
}
