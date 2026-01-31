using NUnit.Framework;
using Project.Domain;

namespace VanguardArena.Tests.EditMode.Domain
{
    /// <summary>
    /// Regression tests for TeamEnergyState edge cases that could break in the future.
    /// These tests protect against bugs that have been fixed or potential future issues.
    /// </summary>
    public class TeamEnergyStateRegressionTests
    {
        private TeamEnergyState _teamEnergy;

        [SetUp]
        public void SetUp()
        {
            // Default: MaxEnergy=5, StartEnergy=0, StartBar=0%
            _teamEnergy = new TeamEnergyState(maxEnergy: 5, startEnergy: 0, startBarPct: 0f);
        }

        #region Energy Bar Extreme Cases

        [Test]
        public void Regression_EnergyBar_ExtremeOverflow_HandlesGracefully()
        {
            // Scenario: A unit with extreme combo hits (e.g., 10-hit ultimate) adds 500%+ to bar
            // Expected: Should convert properly and not overflow/crash
            
            // Arrange: Set energy to 4/5 (room for 1 more)
            _teamEnergy.GainEnergy(4);
            
            // Act: Add 500% to bar (should try to convert 5 energy, but only 1 fits)
            int converted = _teamEnergy.AddToBarAndConvert(500f);
            
            // Assert: Only 1 energy should convert (respects max)
            Assert.AreEqual(1, converted, "Should convert only what fits (1 energy)");
            Assert.AreEqual(5, _teamEnergy.Energy, "Energy should be at max (5)");
            Assert.AreEqual(0f, _teamEnergy.EnergyBarPct, "Excess bar % should be discarded");
        }

        [Test]
        public void Regression_EnergyBar_MultipleConversionsInOneTurn_AccumulatesCorrectly()
        {
            // Scenario: Multiple hits in same turn (e.g., 5-hit combo adding 25% each = 125%)
            // Expected: Should convert 1 energy and carry 25% remainder
            
            // Arrange
            Assert.AreEqual(0, _teamEnergy.Energy, "Starting at 0 energy");
            Assert.AreEqual(0f, _teamEnergy.EnergyBarPct, "Starting at 0% bar");
            
            // Act: Simulate 5 hits, each adding 25%
            for (int i = 0; i < 5; i++)
            {
                _teamEnergy.AddToBarAndConvert(25f);
            }
            
            // Assert: 125% total = 1 energy + 25% remainder
            Assert.AreEqual(1, _teamEnergy.Energy, "Should convert 1 energy from 125%");
            Assert.AreEqual(25f, _teamEnergy.EnergyBarPct, "Should carry 25% remainder");
        }

        [Test]
        public void Regression_EnergyBar_NegativeValues_DoesNotCorruptState()
        {
            // Scenario: Bug fix test - ensure negative bar values don't corrupt state
            // Expected: Negative values should be ignored/clamped
            
            // Arrange
            _teamEnergy.AddToBarAndConvert(30f); // Bar at 30%
            
            // Act: Try to add negative values (should be ignored)
            int negativeConverted = _teamEnergy.AddToBarAndConvert(-100f);
            
            // Assert: State should remain valid
            Assert.AreEqual(0, negativeConverted, "Negative values should not convert");
            Assert.AreEqual(30f, _teamEnergy.EnergyBarPct, "Bar should remain at 30%");
            Assert.AreEqual(0, _teamEnergy.Energy, "Energy should remain at 0");
            Assert.GreaterOrEqual(_teamEnergy.EnergyBarPct, 0f, "Bar % should never go negative");
        }

        #endregion

        #region Energy Spend Edge Cases

        [Test]
        public void Regression_Energy_SpendWhileAtZero_DoesNotUnderflow()
        {
            // Scenario: Try to spend energy when already at 0 (should fail gracefully)
            // Expected: Energy stays at 0, operation fails
            
            // Arrange
            Assert.AreEqual(0, _teamEnergy.Energy, "Starting at 0 energy");
            
            // Act: Try to spend 1 energy
            bool success = _teamEnergy.TrySpendEnergy(1);
            
            // Assert: Should fail and energy stays at 0
            Assert.IsFalse(success, "Spending should fail when insufficient energy");
            Assert.AreEqual(0, _teamEnergy.Energy, "Energy should remain at 0 (no underflow)");
        }

        [Test]
        public void Regression_Energy_SpendExactAmount_DoesNotLeaveResidue()
        {
            // Scenario: Spend exact amount of energy (e.g., have 3, spend 3)
            // Expected: Energy should be exactly 0, no floating point residue
            
            // Arrange
            _teamEnergy.GainEnergy(3);
            Assert.AreEqual(3, _teamEnergy.Energy, "Starting at 3 energy");
            
            // Act: Spend exact amount
            bool success = _teamEnergy.TrySpendEnergy(3);
            
            // Assert: Energy should be exactly 0
            Assert.IsTrue(success, "Spending should succeed");
            Assert.AreEqual(0, _teamEnergy.Energy, "Energy should be exactly 0 (no residue)");
        }

        #endregion

        #region Energy Gain Edge Cases

        [Test]
        public void Regression_Energy_GainAtMax_DoesNotOverflow()
        {
            // Scenario: Repeatedly gain energy when already at max
            // Expected: Energy stays at max, no overflow
            
            // Arrange: Set to max
            _teamEnergy.GainEnergy(5);
            Assert.AreEqual(5, _teamEnergy.Energy, "Starting at max (5)");
            
            // Act: Try to gain more energy multiple times
            for (int i = 0; i < 10; i++)
            {
                _teamEnergy.GainEnergy(1);
            }
            
            // Assert: Energy should remain at max
            Assert.AreEqual(5, _teamEnergy.Energy, "Energy should stay at max (5)");
            Assert.LessOrEqual(_teamEnergy.Energy, _teamEnergy.MaxEnergy, "Energy should never exceed max");
        }

        [Test]
        public void Regression_EnergyBar_AtMaxEnergy_DiscardsExcessCorrectly()
        {
            // Scenario: Energy bar converts when energy is already at max
            // Expected: Bar should be consumed but not convert (excess discarded)
            
            // Arrange: Set energy to max
            _teamEnergy.GainEnergy(5);
            Assert.AreEqual(5, _teamEnergy.Energy, "Starting at max (5)");
            
            // Act: Add to bar (should be discarded)
            int converted = _teamEnergy.AddToBarAndConvert(200f);
            
            // Assert: No conversion, bar consumed
            Assert.AreEqual(0, converted, "No energy should convert (already at max)");
            Assert.AreEqual(0f, _teamEnergy.EnergyBarPct, "Bar should be consumed (not accumulated)");
            Assert.AreEqual(5, _teamEnergy.Energy, "Energy should remain at max");
        }

        #endregion

        #region Reset Function Edge Cases

        [Test]
        public void Regression_Reset_ClearsBarCorrectly()
        {
            // Scenario: Reset energy while bar has accumulated %
            // Expected: Both energy and bar should reset
            
            // Arrange: Set energy and bar to non-zero values
            _teamEnergy.GainEnergy(3);
            _teamEnergy.AddToBarAndConvert(75f);
            Assert.AreEqual(3, _teamEnergy.Energy, "Energy at 3");
            Assert.AreEqual(75f, _teamEnergy.EnergyBarPct, "Bar at 75%");
            
            // Act: Reset to 2
            _teamEnergy.Reset(2);
            
            // Assert: Energy set to 2, bar cleared
            Assert.AreEqual(2, _teamEnergy.Energy, "Energy should be 2");
            Assert.AreEqual(0f, _teamEnergy.EnergyBarPct, "Bar should be cleared to 0%");
        }

        #endregion
    }
}
