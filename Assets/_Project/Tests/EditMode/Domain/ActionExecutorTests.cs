using NUnit.Framework;
using Project.Domain;
using System.Collections.Generic;
using VanguardArena.Tests.Utils;

namespace VanguardArena.Tests.EditMode.Domain
{
    [TestFixture]
    public class ActionExecutorTests
    {
        private UnitRuntimeState _attacker;
        private UnitRuntimeState _target;
        private List<UnitRuntimeState> _allies;
        private List<UnitRuntimeState> _enemies;
        private TeamEnergyState _teamEnergy;
        private SeededRandom _rng;

        [SetUp]
        public void Setup()
        {
            // Create attacker
            var builder1 = new BattleTestBuilder();
            builder1.AddPlayerUnit("Attacker", atk: 50, hp: 100);
            var states1 = builder1.BuildStates();
            _attacker = states1.players[0];
            _allies = states1.players;

            // Create target
            var builder2 = new BattleTestBuilder();
            builder2.AddEnemyUnit("Target", atk: 30, hp: 100, def: 10);
            var states2 = builder2.BuildStates();
            _target = states2.enemies[0];
            _enemies = states2.enemies;

            _teamEnergy = new TeamEnergyState(maxEnergy: 100, startEnergy: 0);
            _rng = new SeededRandom(999);
        }

        #region Basic Attack Tests

        [Test]
        public void ExecuteBasicAction_DealsDamageToTarget()
        {
            // Act
            var result = ActionExecutor.ExecuteBasicAction(_attacker, _allies, _enemies, _rng, _target);

            // Assert
            Assert.AreEqual(_attacker, result.Actor, "Actor should be attacker");
            Assert.AreEqual(1, result.Targets.Count, "Should have 1 target");
            Assert.AreEqual(_target, result.Targets[0], "Target should match");
            Assert.Greater(result.DamageDealt[0], 0, "Should deal damage");
            Assert.Less(_target.CurrentHP, 100, "Target HP should decrease");
        }

        [Test]
        public void ExecuteBasicAction_TriggersMultipleHits()
        {
            // Arrange: Set attacker to have 3-5 hits
            _attacker.BasicHitsMin = 3;
            _attacker.BasicHitsMax = 5;
            int initialHP = _target.CurrentHP;

            // Act
            var result = ActionExecutor.ExecuteBasicAction(_attacker, _allies, _enemies, _rng, _target);

            // Assert
            Assert.AreEqual(1, result.DamageDealt.Count, "Should have damage for 1 target");
            int damageDealt = result.DamageDealt[0];
            Assert.Greater(damageDealt, 0, "Should deal damage");
            
            // HP cannot go below 0, so we check if it matches the clamped value
            int expectedHP = System.Math.Max(0, initialHP - damageDealt);
            Assert.AreEqual(expectedHP, _target.CurrentHP, "Target HP should decrease by damage amount (clamped to 0)");
        }

        [Test]
        public void ExecuteBasicAction_GeneratesEnergy()
        {
            // Act
            var result = ActionExecutor.ExecuteBasicAction(_attacker, _allies, _enemies, _rng, _target);

            // Assert
            Assert.Greater(result.EnergyGenerated, 0, "Should generate energy");
            Assert.IsFalse(result.WasUltimate, "Should not be ultimate");
        }

        [Test]
        public void ExecuteBasicAction_TriggersOnKillPassive()
        {
            // Arrange: Killer gains +40% ATK on kill
            var killer = new BattleTestBuilder()
                .AddPlayerUnit("Killer", atk: 100, passive: new PassiveAbility
                {
                    Type = PassiveType.OnKill,
                    EffectType = StatusEffectType.ATKUp,
                    Modifier = 0.4f,
                    Duration = 2,
                    TargetSelf = true
                })
                .BuildStates().players[0];

            var weakEnemy = new BattleTestBuilder()
                .AddEnemyUnit("Weak", hp: 1, def: 0)
                .BuildStates().enemies[0];

            var allies = new List<UnitRuntimeState> { killer };
            var enemies = new List<UnitRuntimeState> { weakEnemy };

            // Act: One-shot the weak enemy
            var result = ActionExecutor.ExecuteBasicAction(killer, allies, enemies, _rng, weakEnemy);

            // Assert
            Assert.IsFalse(weakEnemy.IsAlive, "Enemy should be dead");
            Assert.AreEqual(1, killer.ActiveStatusEffects.Count, "Killer should have OnKill buff");
            Assert.AreEqual(140, killer.CurrentATK, "Killer ATK should be 100 * 1.4 = 140");
        }

        [Test]
        public void ExecuteBasicAction_HitsAllEnemies_WithAOE()
        {
            // Arrange: AOE attacker with AllEnemies pattern
            var aoeAttacker = new BattleTestBuilder()
                .AddPlayerUnit("AOE", atk: 40, basicTargetPattern: TargetPattern.AllEnemies)
                .BuildStates().players[0];

            var enemy1 = new BattleTestBuilder().AddEnemyUnit("E1", hp: 100).BuildStates().enemies[0];
            var enemy2 = new BattleTestBuilder().AddEnemyUnit("E2", hp: 100).BuildStates().enemies[0];
            var enemy3 = new BattleTestBuilder().AddEnemyUnit("E3", hp: 100).BuildStates().enemies[0];

            var allies = new List<UnitRuntimeState> { aoeAttacker };
            var enemies = new List<UnitRuntimeState> { enemy1, enemy2, enemy3 };

            // Act
            var result = ActionExecutor.ExecuteBasicAction(aoeAttacker, allies, enemies, _rng);

            // Assert
            Assert.AreEqual(3, result.Targets.Count, "Should target all 3 enemies");
            Assert.AreEqual(3, result.DamageDealt.Count, "Should deal damage to all 3");
            Assert.Less(enemy1.CurrentHP, 100, "Enemy1 should take damage");
            Assert.Less(enemy2.CurrentHP, 100, "Enemy2 should take damage");
            Assert.Less(enemy3.CurrentHP, 100, "Enemy3 should take damage");
        }

        #endregion

        #region Ultimate Tests

        [Test]
        public void ExecuteUltimateAction_ConsumesEnergy()
        {
            // Arrange: Give enough energy
            _attacker.UltimateEnergyCost = 50;
            _teamEnergy.GainEnergy(50);

            // Act
            var result = ActionExecutor.ExecuteUltimateAction(_attacker, _allies, _enemies, _teamEnergy, _rng, _target);

            // Assert
            Assert.IsTrue(result.WasUltimate, "Should be ultimate");
            Assert.AreEqual(0, _teamEnergy.Energy, "Energy should be consumed (50 - 50 = 0)");
        }

        [Test]
        public void ExecuteUltimateAction_DealsDamageWithMultiplier()
        {
            // Arrange
            _attacker.UltimateEnergyCost = 50;
            _attacker.UltimateTargetPattern = TargetPattern.SingleEnemy;
            _teamEnergy.GainEnergy(50);

            // Act
            var result = ActionExecutor.ExecuteUltimateAction(_attacker, _allies, _enemies, _teamEnergy, _rng, _target);

            // Assert
            Assert.AreEqual(1, result.Targets.Count, "Should have 1 target");
            Assert.Greater(result.DamageDealt[0], 0, "Should deal damage");
            Assert.Less(_target.CurrentHP, 100, "Target HP should decrease");
        }

        [Test]
        public void ExecuteUltimateAction_StartsCooldown()
        {
            // Arrange
            _attacker.UltimateEnergyCost = 50;
            _attacker.UltimateCooldownTurns = 3;
            _teamEnergy.GainEnergy(50);

            // Act
            ActionExecutor.ExecuteUltimateAction(_attacker, _allies, _enemies, _teamEnergy, _rng, _target);

            // Assert
            Assert.AreEqual(3, _attacker.UltimateCooldownRemaining, "Cooldown should be 3 turns");
        }

        [Test]
        public void ExecuteUltimateAction_HealsAllies_WhenAllyTargeting()
        {
            // Arrange: Healer with AllAllies ultimate
            var healer = new BattleTestBuilder()
                .AddPlayerUnit("Healer", atk: 40, ultimateTargetPattern: TargetPattern.AllAllies, ultimateEnergyCost: 50)
                .BuildStates().players[0];

            var ally1 = new BattleTestBuilder().AddPlayerUnit("Ally1", hp: 100).BuildStates().players[0];
            ally1.CurrentHP = 50; // Wounded

            var ally2 = new BattleTestBuilder().AddPlayerUnit("Ally2", hp: 100).BuildStates().players[0];
            ally2.CurrentHP = 60; // Wounded

            var allies = new List<UnitRuntimeState> { healer, ally1, ally2 };
            var enemies = new List<UnitRuntimeState> { _target };

            _teamEnergy.GainEnergy(50);

            // Act
            var result = ActionExecutor.ExecuteUltimateAction(healer, allies, enemies, _teamEnergy, _rng);

            // Assert
            Assert.AreEqual(2, result.Targets.Count, "Should target 2 allies (excluding healer)");
            Assert.Greater(ally1.CurrentHP, 50, "Ally1 should be healed");
            Assert.Greater(ally2.CurrentHP, 60, "Ally2 should be healed");
        }

        [Test]
        public void CanUseUltimate_ReturnsFalse_WhenNotEnoughEnergy()
        {
            // Arrange
            _attacker.UltimateEnergyCost = 50;
            _teamEnergy.GainEnergy(30); // Not enough

            // Act
            bool canUse = ActionExecutor.CanUseUltimate(_attacker, _teamEnergy);

            // Assert
            Assert.IsFalse(canUse, "Should not be able to use ultimate");
        }

        [Test]
        public void CanUseUltimate_ReturnsFalse_WhenOnCooldown()
        {
            // Arrange
            _attacker.UltimateEnergyCost = 50;
            _attacker.UltimateCooldownTurns = 3;
            _teamEnergy.GainEnergy(50); // Enough energy
            _attacker.StartUltimateCooldown(); // But on cooldown

            // Act
            bool canUse = ActionExecutor.CanUseUltimate(_attacker, _teamEnergy);

            // Assert
            Assert.IsFalse(canUse, "Should not be able to use ultimate (on cooldown)");
        }

        [Test]
        public void CanUseUltimate_ReturnsTrue_WhenAvailable()
        {
            // Arrange
            _attacker.UltimateEnergyCost = 50;
            _teamEnergy.GainEnergy(50);

            // Act
            bool canUse = ActionExecutor.CanUseUltimate(_attacker, _teamEnergy);

            // Assert
            Assert.IsTrue(canUse, "Should be able to use ultimate");
        }

        #endregion
    }
}
