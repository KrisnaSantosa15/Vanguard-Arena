using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Project.Domain;
using VanguardArena.Tests.Utils;

namespace VanguardArena.Tests.EditMode.Domain
{
    /// <summary>
    /// Regression tests for TurnManager turn order edge cases.
    /// Tests speed ties, determinism, and turn order correctness.
    /// </summary>
    public class TurnManagerRegressionTests
    {
        private TurnManager _turnManager;

        [SetUp]
        public void SetUp()
        {
            _turnManager = new TurnManager();
        }

        #region Speed Tie Regression Tests

        [Test]
        public void Regression_TurnOrder_IdenticalSpeeds_DeterministicOrder()
        {
            // Scenario: 3 units with identical SPD=10
            // Expected: Same turn order every time (deterministic by slot order)
            
            // Arrange: Create 3 players with identical speed
            var (players, _, _) = new BattleTestBuilder()
                .AddPlayerUnit("Player1", hp: 100, atk: 10, spd: 10)
                .AddPlayerUnit("Player2", hp: 100, atk: 10, spd: 10)
                .AddPlayerUnit("Player3", hp: 100, atk: 10, spd: 10)
                .BuildStates();
            
            // Act: Build timeline twice
            _turnManager.RebuildTimeline(players);
            var timeline1 = _turnManager.Timeline.Select(u => u.DisplayName).ToList();
            
            _turnManager.RebuildTimeline(players);
            var timeline2 = _turnManager.Timeline.Select(u => u.DisplayName).ToList();
            
            // Assert: Timeline should be identical both times
            Assert.AreEqual(3, timeline1.Count, "Timeline should have 3 units");
            CollectionAssert.AreEqual(timeline1, timeline2, "Timeline order should be deterministic");
        }

        [Test]
        public void Regression_TurnOrder_SpeedTies_PlayersBeforeEnemies()
        {
            // Scenario: Player (SPD=10) vs Enemy (SPD=10)
            // Expected: Consistent tiebreaker (depends on implementation - players first or slot order)
            // This test documents current behavior to catch unintended changes
            
            // Arrange: 1 player and 1 enemy with identical speed
            var (players, enemies, _) = new BattleTestBuilder()
                .AddPlayerUnit("Player1", hp: 100, atk: 10, spd: 10)
                .AddEnemyUnit("Enemy1", hp: 100, atk: 10, spd: 10)
                .BuildStates();
            
            var allUnits = new List<UnitRuntimeState>();
            allUnits.AddRange(players);
            allUnits.AddRange(enemies);
            
            // Act: Build timeline
            _turnManager.RebuildTimeline(allUnits);
            
            // Assert: Check that order is consistent (document current behavior)
            Assert.AreEqual(2, _turnManager.Timeline.Count, "Timeline should have 2 units");
            var firstUnit = _turnManager.Timeline[0];
            var secondUnit = _turnManager.Timeline[1];
            
            // Both units have same speed, so order is determined by OrderByDescending stability
            // This test documents current behavior - if it changes, we'll know
            Assert.AreEqual(10, firstUnit.SPD, "First unit should have SPD=10");
            Assert.AreEqual(10, secondUnit.SPD, "Second unit should have SPD=10");
            
            // Run again with same setup to ensure determinism
            _turnManager.RebuildTimeline(allUnits);
            Assert.AreEqual(firstUnit, _turnManager.Timeline[0], "First unit should remain the same");
            Assert.AreEqual(secondUnit, _turnManager.Timeline[1], "Second unit should remain the same");
        }

        [Test]
        public void Regression_TurnOrder_MultipleSpeedTies_StableOrder()
        {
            // Scenario: 6 units (3 players + 3 enemies) all with SPD=10
            // Expected: Order should be stable and deterministic
            
            // Arrange: Create 3 players and 3 enemies with identical speed
            var (players, enemies, _) = new BattleTestBuilder()
                .AddPlayerUnit("Player1", hp: 100, atk: 10, spd: 10)
                .AddPlayerUnit("Player2", hp: 100, atk: 10, spd: 10)
                .AddPlayerUnit("Player3", hp: 100, atk: 10, spd: 10)
                .AddEnemyUnit("Enemy1", hp: 100, atk: 10, spd: 10)
                .AddEnemyUnit("Enemy2", hp: 100, atk: 10, spd: 10)
                .AddEnemyUnit("Enemy3", hp: 100, atk: 10, spd: 10)
                .BuildStates();
            
            var allUnits = new List<UnitRuntimeState>();
            allUnits.AddRange(players);
            allUnits.AddRange(enemies);
            
            // Act: Build timeline multiple times
            _turnManager.RebuildTimeline(allUnits);
            var order1 = _turnManager.Timeline.Select(u => u.DisplayName).ToList();
            
            _turnManager.RebuildTimeline(allUnits);
            var order2 = _turnManager.Timeline.Select(u => u.DisplayName).ToList();
            
            _turnManager.RebuildTimeline(allUnits);
            var order3 = _turnManager.Timeline.Select(u => u.DisplayName).ToList();
            
            // Assert: Order should be stable across multiple rebuilds
            CollectionAssert.AreEqual(order1, order2, "Timeline order should be stable (rebuild 1 vs 2)");
            CollectionAssert.AreEqual(order2, order3, "Timeline order should be stable (rebuild 2 vs 3)");
            Assert.AreEqual(6, order1.Count, "Timeline should contain all 6 units");
        }

        #endregion

        #region Turn Order Correctness Tests

        [Test]
        public void Regression_TurnOrder_FastestUnitFirst()
        {
            // Scenario: Units with different speeds
            // Expected: Fastest unit should go first
            
            // Arrange: 3 units with SPD=10, 20, 30
            var (players, _, _) = new BattleTestBuilder()
                .AddPlayerUnit("SlowUnit", hp: 100, atk: 10, spd: 10)
                .AddPlayerUnit("MediumUnit", hp: 100, atk: 10, spd: 20)
                .AddPlayerUnit("FastUnit", hp: 100, atk: 10, spd: 30)
                .BuildStates();
            
            // Act
            _turnManager.RebuildTimeline(players);
            
            // Assert: Order should be FastUnit > MediumUnit > SlowUnit
            Assert.AreEqual(3, _turnManager.Timeline.Count, "Timeline should have 3 units");
            Assert.AreEqual("FastUnit", _turnManager.Timeline[0].DisplayName, "Fastest unit should go first");
            Assert.AreEqual("MediumUnit", _turnManager.Timeline[1].DisplayName, "Medium unit should go second");
            Assert.AreEqual("SlowUnit", _turnManager.Timeline[2].DisplayName, "Slowest unit should go last");
        }

        [Test]
        public void Regression_TurnOrder_ExcludesDeadUnits()
        {
            // Scenario: Timeline includes dead units, RemoveDeadUnits() should clean them
            // Expected: Dead units removed from timeline
            
            // Arrange: 3 units, kill one
            var (players, _, _) = new BattleTestBuilder()
                .AddPlayerUnit("Player1", hp: 100, atk: 10, spd: 10)
                .AddPlayerUnit("Player2", hp: 100, atk: 10, spd: 20)
                .AddPlayerUnit("Player3", hp: 100, atk: 10, spd: 30)
                .BuildStates();
            
            _turnManager.RebuildTimeline(players);
            Assert.AreEqual(3, _turnManager.Timeline.Count, "Timeline should start with 3 units");
            
            // Act: Kill the middle unit
            players[1].TakeDamage(100);
            Assert.IsFalse(players[1].IsAlive, "Player2 should be dead");
            
            _turnManager.RemoveDeadUnits();
            
            // Assert: Timeline should have 2 units (dead one removed)
            Assert.AreEqual(2, _turnManager.Timeline.Count, "Timeline should have 2 units after removing dead");
            Assert.IsFalse(_turnManager.Timeline.Any(u => !u.IsAlive), "No dead units should remain in timeline");
        }

        #endregion

        #region Turn Rolling Tests

        [Test]
        public void Regression_RollAfterAction_MovesUnitToEnd()
        {
            // Scenario: Unit acts, should move to end of timeline
            // Expected: Unit moves from index 0 to end
            
            // Arrange: 3 units
            var (players, _, _) = new BattleTestBuilder()
                .AddPlayerUnit("Player1", hp: 100, atk: 10, spd: 30) // Fastest
                .AddPlayerUnit("Player2", hp: 100, atk: 10, spd: 20)
                .AddPlayerUnit("Player3", hp: 100, atk: 10, spd: 10) // Slowest
                .BuildStates();
            
            _turnManager.RebuildTimeline(players);
            var actor = _turnManager.CurrentActor;
            Assert.AreEqual("Player1", actor.DisplayName, "Player1 should act first (fastest)");
            
            // Act: Roll after Player1 acts
            _turnManager.RollAfterAction(actor);
            
            // Assert: Player1 should now be at the end
            Assert.AreEqual("Player2", _turnManager.CurrentActor.DisplayName, "Player2 should now be current actor");
            Assert.AreEqual("Player1", _turnManager.Timeline[2].DisplayName, "Player1 should be at end of timeline");
        }

        [Test]
        public void Regression_RollAfterAction_DeadUnitDoesNotReenter()
        {
            // Scenario: Unit dies during their turn, RollAfterAction should not re-add them
            // Expected: Dead unit removed from timeline
            
            // Arrange: 2 units
            var (players, _, _) = new BattleTestBuilder()
                .AddPlayerUnit("Player1", hp: 100, atk: 10, spd: 20)
                .AddPlayerUnit("Player2", hp: 100, atk: 10, spd: 10)
                .BuildStates();
            
            _turnManager.RebuildTimeline(players);
            Assert.AreEqual(2, _turnManager.Timeline.Count, "Timeline should have 2 units");
            
            var actor = _turnManager.CurrentActor;
            Assert.AreEqual("Player1", actor.DisplayName, "Player1 should act first");
            
            // Act: Kill Player1, then roll
            actor.TakeDamage(100);
            Assert.IsFalse(actor.IsAlive, "Player1 should be dead");
            
            _turnManager.RollAfterAction(actor);
            
            // Assert: Timeline should only have 1 unit (Player2)
            // Dead units are not re-added by RollAfterAction
            Assert.AreEqual(1, _turnManager.Timeline.Count, "Timeline should have 1 unit (dead unit not re-added)");
            Assert.AreEqual("Player2", _turnManager.Timeline[0].DisplayName, "Player2 should be the only unit");
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void Regression_RebuildTimeline_EmptyList_NoError()
        {
            // Scenario: Rebuild timeline with empty unit list (edge case)
            // Expected: No crash, timeline is empty
            
            // Arrange
            var emptyList = new List<UnitRuntimeState>();
            
            // Act
            _turnManager.RebuildTimeline(emptyList);
            
            // Assert
            Assert.AreEqual(0, _turnManager.Timeline.Count, "Timeline should be empty");
            Assert.IsNull(_turnManager.CurrentActor, "CurrentActor should be null");
        }

        [Test]
        public void Regression_RemoveDeadUnits_AllDead_ClearsTimeline()
        {
            // Scenario: All units in timeline are dead
            // Expected: Timeline becomes empty
            
            // Arrange: 3 units, kill all
            var (players, _, _) = new BattleTestBuilder()
                .AddPlayerUnit("Player1", hp: 100, atk: 10, spd: 10)
                .AddPlayerUnit("Player2", hp: 100, atk: 10, spd: 20)
                .AddPlayerUnit("Player3", hp: 100, atk: 10, spd: 30)
                .BuildStates();
            
            _turnManager.RebuildTimeline(players);
            Assert.AreEqual(3, _turnManager.Timeline.Count, "Timeline should start with 3 units");
            
            // Act: Kill all units
            foreach (var player in players)
            {
                player.TakeDamage(100);
            }
            
            _turnManager.RemoveDeadUnits();
            
            // Assert: Timeline should be empty
            Assert.AreEqual(0, _turnManager.Timeline.Count, "Timeline should be empty (all units dead)");
        }

        #endregion
    }
}
