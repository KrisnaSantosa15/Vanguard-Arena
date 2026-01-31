using NUnit.Framework;
using Project.Domain;
using System.Collections.Generic;
using VanguardArena.Tests.Utils;

namespace VanguardArena.Tests.EditMode.Domain
{
    [TestFixture]
    public class TurnManagerTests
    {
        [Test]
        public void TurnManager_InitializesWithTurn1()
        {
            var tm = new TurnManager();
            Assert.AreEqual(1, tm.CurrentTurn, "Should start at turn 1");
            Assert.AreEqual(0, tm.Timeline.Count, "Timeline should start empty");
            Assert.IsNull(tm.CurrentActor, "No current actor when timeline is empty");
        }

        [Test]
        public void IncrementTurn_IncreasesTurnCounter()
        {
            var tm = new TurnManager();
            Assert.AreEqual(1, tm.CurrentTurn);
            
            tm.IncrementTurn();
            Assert.AreEqual(2, tm.CurrentTurn);
            
            tm.IncrementTurn();
            Assert.AreEqual(3, tm.CurrentTurn);
        }

        [Test]
        public void RebuildTimeline_SortsBySpeedDescending()
        {
            // Arrange
            var builder = new BattleTestBuilder()
                .AddPlayerUnit("Slow", spd: 5)
                .AddPlayerUnit("Fast", spd: 20)
                .AddPlayerUnit("Medium", spd: 10);
            var built = builder.BuildStates();
            var allUnits = new List<UnitRuntimeState>();
            allUnits.AddRange(built.players);
            allUnits.AddRange(built.enemies);

            var tm = new TurnManager();

            // Act
            tm.RebuildTimeline(allUnits);

            // Assert
            Assert.AreEqual(3, tm.Timeline.Count);
            Assert.AreEqual("Fast", tm.Timeline[0].DisplayName, "Fastest unit should be first");
            Assert.AreEqual("Medium", tm.Timeline[1].DisplayName, "Medium speed second");
            Assert.AreEqual("Slow", tm.Timeline[2].DisplayName, "Slowest unit last");
        }

        [Test]
        public void RebuildTimeline_FiltersDead()
        {
            // Arrange
            var builder = new BattleTestBuilder()
                .AddPlayerUnit("Alive1", hp: 100, spd: 20)
                .AddPlayerUnit("Dead", hp: 100, spd: 15)
                .AddPlayerUnit("Alive2", hp: 100, spd: 10);
            var built = builder.BuildStates();
            var allUnits = new List<UnitRuntimeState>();
            allUnits.AddRange(built.players);

            // Kill the middle unit
            allUnits[1].CurrentHP = 0;

            var tm = new TurnManager();

            // Act
            tm.RebuildTimeline(allUnits);

            // Assert
            Assert.AreEqual(2, tm.Timeline.Count, "Should only include alive units");
            Assert.AreEqual("Alive1", tm.Timeline[0].DisplayName);
            Assert.AreEqual("Alive2", tm.Timeline[1].DisplayName);
        }

        [Test]
        public void RebuildTimeline_HandlesNullUnits()
        {
            // Arrange
            var builder = new BattleTestBuilder()
                .AddPlayerUnit("Valid1", spd: 10)
                .AddPlayerUnit("Valid2", spd: 5);
            var built = builder.BuildStates();
            var allUnits = new List<UnitRuntimeState>();
            allUnits.AddRange(built.players);
            allUnits.Add(null); // Add null to the list

            var tm = new TurnManager();

            // Act
            tm.RebuildTimeline(allUnits);

            // Assert
            Assert.AreEqual(2, tm.Timeline.Count, "Should skip null units");
            Assert.AreEqual("Valid1", tm.Timeline[0].DisplayName);
            Assert.AreEqual("Valid2", tm.Timeline[1].DisplayName);
        }

        [Test]
        public void CurrentActor_ReturnsFirstInTimeline()
        {
            // Arrange
            var builder = new BattleTestBuilder()
                .AddPlayerUnit("First", spd: 20)
                .AddPlayerUnit("Second", spd: 10);
            var built = builder.BuildStates();
            var allUnits = new List<UnitRuntimeState>();
            allUnits.AddRange(built.players);

            var tm = new TurnManager();
            tm.RebuildTimeline(allUnits);

            // Act & Assert
            Assert.AreEqual("First", tm.CurrentActor.DisplayName, "CurrentActor should be first in timeline");
        }

        [Test]
        public void RollAfterAction_MovesActorToEnd()
        {
            // Arrange
            var builder = new BattleTestBuilder()
                .AddPlayerUnit("A", spd: 30)
                .AddPlayerUnit("B", spd: 20)
                .AddPlayerUnit("C", spd: 10);
            var built = builder.BuildStates();
            var allUnits = new List<UnitRuntimeState>();
            allUnits.AddRange(built.players);

            var tm = new TurnManager();
            tm.RebuildTimeline(allUnits);
            var actor = tm.CurrentActor;

            // Act
            tm.RollAfterAction(actor);

            // Assert
            Assert.AreEqual("B", tm.Timeline[0].DisplayName, "B should now be first");
            Assert.AreEqual("C", tm.Timeline[1].DisplayName, "C should be second");
            Assert.AreEqual("A", tm.Timeline[2].DisplayName, "A should move to end");
        }

        [Test]
        public void RollAfterAction_RemovesDeadActorFromTimeline()
        {
            // Arrange
            var builder = new BattleTestBuilder()
                .AddPlayerUnit("A", hp: 100, spd: 30)
                .AddPlayerUnit("B", hp: 100, spd: 20);
            var built = builder.BuildStates();
            var allUnits = new List<UnitRuntimeState>();
            allUnits.AddRange(built.players);

            var tm = new TurnManager();
            tm.RebuildTimeline(allUnits);
            var actor = tm.CurrentActor;

            // Kill the actor
            actor.CurrentHP = 0;

            // Act
            tm.RollAfterAction(actor);

            // Assert
            Assert.AreEqual(1, tm.Timeline.Count, "Dead actor should not be re-added");
            Assert.AreEqual("B", tm.Timeline[0].DisplayName, "Only B should remain");
        }

        [Test]
        public void RollAfterAction_HandlesActorNotAtFront()
        {
            // Arrange
            var builder = new BattleTestBuilder()
                .AddPlayerUnit("A", spd: 30)
                .AddPlayerUnit("B", spd: 20)
                .AddPlayerUnit("C", spd: 10);
            var built = builder.BuildStates();
            var allUnits = new List<UnitRuntimeState>();
            allUnits.AddRange(built.players);

            var tm = new TurnManager();
            tm.RebuildTimeline(allUnits);
            var middleActor = tm.Timeline[1]; // B

            // Act
            tm.RollAfterAction(middleActor);

            // Assert
            Assert.AreEqual(3, tm.Timeline.Count);
            Assert.AreEqual("A", tm.Timeline[0].DisplayName, "A stays first");
            Assert.AreEqual("C", tm.Timeline[1].DisplayName, "C moves up");
            Assert.AreEqual("B", tm.Timeline[2].DisplayName, "B moves to end");
        }

        [Test]
        public void RemoveDeadUnits_FiltersOutDead()
        {
            // Arrange
            var builder = new BattleTestBuilder()
                .AddPlayerUnit("A", hp: 100, spd: 30)
                .AddPlayerUnit("B", hp: 100, spd: 20)
                .AddPlayerUnit("C", hp: 100, spd: 10);
            var built = builder.BuildStates();
            var allUnits = new List<UnitRuntimeState>();
            allUnits.AddRange(built.players);

            var tm = new TurnManager();
            tm.RebuildTimeline(allUnits);

            // Kill B
            allUnits[1].CurrentHP = 0;

            // Act
            tm.RemoveDeadUnits();

            // Assert
            Assert.AreEqual(2, tm.Timeline.Count);
            Assert.AreEqual("A", tm.Timeline[0].DisplayName);
            Assert.AreEqual("C", tm.Timeline[1].DisplayName);
        }

        [Test]
        public void RemoveDeadUnits_HandlesNullUnits()
        {
            // Arrange
            var builder = new BattleTestBuilder()
                .AddPlayerUnit("A", spd: 30)
                .AddPlayerUnit("B", spd: 20);
            var built = builder.BuildStates();
            var allUnits = new List<UnitRuntimeState>();
            allUnits.AddRange(built.players);

            var tm = new TurnManager();
            tm.RebuildTimeline(allUnits);

            // Manually insert null (simulates edge case)
            tm.Timeline.Insert(1, null);

            // Act
            tm.RemoveDeadUnits();

            // Assert
            Assert.AreEqual(2, tm.Timeline.Count, "Null should be removed");
            Assert.AreEqual("A", tm.Timeline[0].DisplayName);
            Assert.AreEqual("B", tm.Timeline[1].DisplayName);
        }

        [Test]
        public void TurnManager_CompleteRoundSimulation()
        {
            // Arrange: 3 units, track 2 full rounds
            var builder = new BattleTestBuilder()
                .AddPlayerUnit("Fast", spd: 30)
                .AddPlayerUnit("Medium", spd: 20)
                .AddPlayerUnit("Slow", spd: 10);
            var built = builder.BuildStates();
            var allUnits = new List<UnitRuntimeState>();
            allUnits.AddRange(built.players);

            var tm = new TurnManager();
            tm.RebuildTimeline(allUnits);

            var actOrder = new List<string>();

            // Act: Simulate 2 complete rounds (6 actions)
            for (int i = 0; i < 6; i++)
            {
                var actor = tm.CurrentActor;
                actOrder.Add(actor.DisplayName);
                tm.RollAfterAction(actor);
            }

            // Assert: Speed order should repeat
            Assert.AreEqual("Fast", actOrder[0]);
            Assert.AreEqual("Medium", actOrder[1]);
            Assert.AreEqual("Slow", actOrder[2]);
            Assert.AreEqual("Fast", actOrder[3], "Round 2 should repeat order");
            Assert.AreEqual("Medium", actOrder[4]);
            Assert.AreEqual("Slow", actOrder[5]);
        }

        [Test]
        public void TurnManager_DeathMidRound()
        {
            // Arrange
            var builder = new BattleTestBuilder()
                .AddPlayerUnit("A", hp: 100, spd: 30)
                .AddPlayerUnit("B", hp: 100, spd: 20)
                .AddPlayerUnit("C", hp: 100, spd: 10);
            var built = builder.BuildStates();
            var allUnits = new List<UnitRuntimeState>();
            allUnits.AddRange(built.players);

            var tm = new TurnManager();
            tm.RebuildTimeline(allUnits);

            // Act: A acts, then B acts and dies
            tm.RollAfterAction(tm.CurrentActor); // A acts
            var bActor = tm.CurrentActor; // B
            bActor.CurrentHP = 0; // B dies
            tm.RollAfterAction(bActor); // B's turn ends
            tm.RemoveDeadUnits(); // Clean up

            // Assert: Only A and C remain, C should act next
            Assert.AreEqual(2, tm.Timeline.Count);
            Assert.AreEqual("C", tm.CurrentActor.DisplayName, "C should act next");
        }

        [Test]
        public void RebuildTimeline_EqualSpeed_MaintainsInsertionOrder()
        {
            // Arrange: Units with equal SPD
            var builder = new BattleTestBuilder()
                .AddPlayerUnit("First", spd: 10)
                .AddPlayerUnit("Second", spd: 10)
                .AddPlayerUnit("Third", spd: 10);
            var built = builder.BuildStates();
            var allUnits = new List<UnitRuntimeState>();
            allUnits.AddRange(built.players);

            var tm = new TurnManager();

            // Act
            tm.RebuildTimeline(allUnits);

            // Assert: OrderByDescending is stable sort in C#, should maintain insertion order
            Assert.AreEqual("First", tm.Timeline[0].DisplayName);
            Assert.AreEqual("Second", tm.Timeline[1].DisplayName);
            Assert.AreEqual("Third", tm.Timeline[2].DisplayName);
        }

        [Test]
        public void RebuildTimeline_ClearsExistingTimeline()
        {
            // Arrange
            var builder1 = new BattleTestBuilder()
                .AddPlayerUnit("Old1", spd: 10)
                .AddPlayerUnit("Old2", spd: 5);
            var built1 = builder1.BuildStates();
            var allUnits1 = new List<UnitRuntimeState>();
            allUnits1.AddRange(built1.players);

            var builder2 = new BattleTestBuilder()
                .AddPlayerUnit("New1", spd: 20);
            var built2 = builder2.BuildStates();
            var allUnits2 = new List<UnitRuntimeState>();
            allUnits2.AddRange(built2.players);

            var tm = new TurnManager();
            tm.RebuildTimeline(allUnits1);

            // Act
            tm.RebuildTimeline(allUnits2);

            // Assert
            Assert.AreEqual(1, tm.Timeline.Count, "Old timeline should be cleared");
            Assert.AreEqual("New1", tm.Timeline[0].DisplayName);
        }

        [Test]
        public void RollAfterAction_EmptyTimeline_DoesNotCrash()
        {
            // Arrange
            var tm = new TurnManager();
            var builder = new BattleTestBuilder().AddPlayerUnit("A", spd: 10);
            var built = builder.BuildStates();
            var actor = built.players[0];

            // Act & Assert
            Assert.DoesNotThrow(() => tm.RollAfterAction(actor), "Should handle empty timeline gracefully");
        }

        [Test]
        public void RemoveDeadUnits_EmptyTimeline_DoesNotCrash()
        {
            // Arrange
            var tm = new TurnManager();

            // Act & Assert
            Assert.DoesNotThrow(() => tm.RemoveDeadUnits(), "Should handle empty timeline gracefully");
        }
    }
}
