using NUnit.Framework;
using Project.Domain;
using System.Collections.Generic;
using VanguardArena.Tests.Utils;

namespace VanguardArena.Tests.EditMode.Domain
{
    [TestFixture]
    public class TargetResolverTests
    {
        private UnitRuntimeState _actor;
        private List<UnitRuntimeState> _allies;
        private List<UnitRuntimeState> _enemies;
        private SeededRandom _rng;

        [SetUp]
        public void Setup()
        {
            // Create allies (3 total including actor)
            var builder1 = new BattleTestBuilder();
            builder1.AddPlayerUnit("Actor", atk: 20, hp: 100);
            builder1.AddPlayerUnit("Ally1", atk: 15, hp: 80);
            builder1.AddPlayerUnit("Ally2", atk: 18, hp: 60);
            var states1 = builder1.BuildStates();
            _allies = states1.players;
            _actor = _allies[0]; // Actor is the first ally

            // Create enemies (3 total, with different HP/ATK for testing)
            var builder2 = new BattleTestBuilder();
            builder2.AddEnemyUnit("Enemy1", atk: 25, hp: 100); // Row 0 (front)
            builder2.AddEnemyUnit("Enemy2", atk: 30, hp: 50);  // Row 1 (front)
            builder2.AddEnemyUnit("Enemy3", atk: 20, hp: 70);  // Row 2 (front)
            var states2 = builder2.BuildStates();
            _enemies = states2.enemies;

            _rng = new SeededRandom(999);
        }

        #region Self Targeting

        [Test]
        public void ResolveTargets_Self_ReturnsSelf()
        {
            // Act
            var targets = TargetResolver.ResolveTargets(TargetPattern.Self, _actor, _allies, _enemies, _rng);

            // Assert
            Assert.AreEqual(1, targets.Count, "Should return 1 target");
            Assert.AreEqual(_actor, targets[0], "Should target self");
        }

        #endregion

        #region Single Enemy Targeting

        [Test]
        public void ResolveTargets_SingleEnemy_WithManualTarget_ReturnsManualTarget()
        {
            // Arrange
            var manualTarget = _enemies[1];

            // Act
            var targets = TargetResolver.ResolveTargets(TargetPattern.SingleEnemy, _actor, _allies, _enemies, _rng, manualTarget);

            // Assert
            Assert.AreEqual(1, targets.Count, "Should return 1 target");
            Assert.AreEqual(manualTarget, targets[0], "Should return manual target");
        }

        [Test]
        public void ResolveTargets_SingleEnemy_WithoutManualTarget_ReturnsRandomEnemy()
        {
            // Act
            var targets = TargetResolver.ResolveTargets(TargetPattern.SingleEnemy, _actor, _allies, _enemies, _rng);

            // Assert
            Assert.AreEqual(1, targets.Count, "Should return 1 target");
            Assert.IsTrue(_enemies.Contains(targets[0]), "Should return an enemy");
        }

        #endregion

        #region Single Ally Targeting

        [Test]
        public void ResolveTargets_SingleAlly_ExcludesSelf()
        {
            // Act
            var targets = TargetResolver.ResolveTargets(TargetPattern.SingleAlly, _actor, _allies, _enemies, _rng);

            // Assert
            Assert.AreEqual(1, targets.Count, "Should return 1 target");
            Assert.AreNotEqual(_actor, targets[0], "Should not target self");
            Assert.IsTrue(_allies.Contains(targets[0]), "Should return an ally");
        }

        #endregion

        #region All Enemies Targeting

        [Test]
        public void ResolveTargets_AllEnemies_ReturnsAllAliveEnemies()
        {
            // Arrange: Kill one enemy
            _enemies[1].CurrentHP = 0;

            // Act
            var targets = TargetResolver.ResolveTargets(TargetPattern.AllEnemies, _actor, _allies, _enemies, _rng);

            // Assert
            Assert.AreEqual(2, targets.Count, "Should return 2 alive enemies");
            Assert.IsTrue(targets.Contains(_enemies[0]), "Should include Enemy1");
            Assert.IsFalse(targets.Contains(_enemies[1]), "Should exclude dead Enemy2");
            Assert.IsTrue(targets.Contains(_enemies[2]), "Should include Enemy3");
        }

        #endregion

        #region All Allies Targeting

        [Test]
        public void ResolveTargets_AllAllies_ExcludesSelf()
        {
            // Act
            var targets = TargetResolver.ResolveTargets(TargetPattern.AllAllies, _actor, _allies, _enemies, _rng);

            // Assert
            Assert.AreEqual(2, targets.Count, "Should return 2 allies (excluding self)");
            Assert.IsFalse(targets.Contains(_actor), "Should not include self");
        }

        #endregion

        #region Lowest HP Enemy Targeting

        [Test]
        public void ResolveTargets_LowestHpEnemy_ReturnsLowestHpUnit()
        {
            // Arrange: Enemy2 has 50 HP (lowest)
            // Enemy1: 100 HP, Enemy2: 50 HP, Enemy3: 70 HP

            // Act
            var targets = TargetResolver.ResolveTargets(TargetPattern.LowestHpEnemy, _actor, _allies, _enemies, _rng);

            // Assert
            Assert.AreEqual(1, targets.Count, "Should return 1 target");
            Assert.AreEqual(_enemies[1], targets[0], "Should target Enemy2 (50 HP)");
        }

        #endregion

        #region Highest Threat Enemy Targeting

        [Test]
        public void ResolveTargets_HighestThreatEnemy_ReturnsHighestThreat()
        {
            // Arrange: 
            // Enemy1: ATK=25, HP=100/100 → Threat = 25 * 1.0 = 25
            // Enemy2: ATK=30, HP=50/50   → Threat = 30 * 1.0 = 30 (HIGHEST)
            // Enemy3: ATK=20, HP=70/70   → Threat = 20 * 1.0 = 20

            // Act
            var targets = TargetResolver.ResolveTargets(TargetPattern.HighestThreatEnemy, _actor, _allies, _enemies, _rng);

            // Assert
            Assert.AreEqual(1, targets.Count, "Should return 1 target");
            Assert.AreEqual(_enemies[1], targets[0], "Should target Enemy2 (highest threat: 30 ATK)");
        }

        #endregion

        #region Front Row Targeting

        [Test]
        public void ResolveTargets_FrontRowSingle_ReturnsOneFrontRowUnit()
        {
            // Arrange: All enemies are in front row (Row = 0)
            _enemies[0].Row = 0;
            _enemies[1].Row = 0;
            _enemies[2].Row = 0;

            // Act
            var targets = TargetResolver.ResolveTargets(TargetPattern.FrontRowSingle, _actor, _allies, _enemies, _rng);

            // Assert
            Assert.AreEqual(1, targets.Count, "Should return 1 target");
            Assert.IsTrue(_enemies.Contains(targets[0]), "Should be one of the enemies");
        }

        [Test]
        public void ResolveTargets_FrontRowAll_ReturnsAllFrontRowUnits()
        {
            // Arrange: 2 front row, 1 back row
            _enemies[0].Row = 0; // Front
            _enemies[1].Row = 0; // Front
            _enemies[2].Row = 1; // Back

            // Act
            var targets = TargetResolver.ResolveTargets(TargetPattern.FrontRowAll, _actor, _allies, _enemies, _rng);

            // Assert
            Assert.AreEqual(2, targets.Count, "Should return 2 front row units");
            Assert.IsTrue(targets.Contains(_enemies[0]), "Should include Enemy1 (Row 0)");
            Assert.IsTrue(targets.Contains(_enemies[1]), "Should include Enemy2 (Row 0)");
            Assert.IsFalse(targets.Contains(_enemies[2]), "Should exclude Enemy3 (Row 1, back row)");
        }

        #endregion

        #region Back Row Targeting

        [Test]
        public void ResolveTargets_BackRowAll_ReturnsAllBackRowUnits()
        {
            // Arrange: 1 front row, 2 back row
            _enemies[0].Row = 0; // Front
            _enemies[1].Row = 1; // Back
            _enemies[2].Row = 1; // Back

            // Act
            var targets = TargetResolver.ResolveTargets(TargetPattern.BackRowAll, _actor, _allies, _enemies, _rng);

            // Assert
            Assert.AreEqual(2, targets.Count, "Should return 2 back row units");
            Assert.IsFalse(targets.Contains(_enemies[0]), "Should exclude Enemy1 (Row 0, front row)");
            Assert.IsTrue(targets.Contains(_enemies[1]), "Should include Enemy2 (Row 1)");
            Assert.IsTrue(targets.Contains(_enemies[2]), "Should include Enemy3 (Row 1)");
        }

        #endregion

        #region Helper Method Tests

        [Test]
        public void RequiresManualTarget_SingleEnemy_ReturnsTrue()
        {
            Assert.IsTrue(TargetResolver.RequiresManualTarget(TargetPattern.SingleEnemy));
        }

        [Test]
        public void RequiresManualTarget_AllEnemies_ReturnsFalse()
        {
            Assert.IsFalse(TargetResolver.RequiresManualTarget(TargetPattern.AllEnemies));
        }

        [Test]
        public void IsAllyTargeting_Self_ReturnsTrue()
        {
            Assert.IsTrue(TargetResolver.IsAllyTargeting(TargetPattern.Self));
        }

        [Test]
        public void IsAllyTargeting_SingleEnemy_ReturnsFalse()
        {
            Assert.IsFalse(TargetResolver.IsAllyTargeting(TargetPattern.SingleEnemy));
        }

        #endregion
    }
}
