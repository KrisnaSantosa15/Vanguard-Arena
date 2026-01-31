using NUnit.Framework;
using Project.Domain;

namespace VanguardArena.Tests.EditMode.Domain
{
    /// <summary>
    /// Unit tests for TeamEnergyState energy management system.
    /// Tests energy gain, spend, bar conversion, and max bounds.
    /// </summary>
    public class TeamEnergyStateTests
    {
        private TeamEnergyState _teamEnergy;

        [SetUp]
        public void SetUp()
        {
            // Default: MaxEnergy=5, StartEnergy=0, StartBar=0%
            _teamEnergy = new TeamEnergyState(maxEnergy: 5, startEnergy: 0, startBarPct: 0f);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_InitializesWithStartValues()
        {
            // Arrange & Act
            var energy = new TeamEnergyState(maxEnergy: 5, startEnergy: 3, startBarPct: 50f);

            // Assert
            Assert.AreEqual(5, energy.MaxEnergy, "MaxEnergy should match constructor");
            Assert.AreEqual(3, energy.Energy, "Energy should match start value");
            Assert.AreEqual(50f, energy.EnergyBarPct, "EnergyBarPct should match start value");
        }

        [Test]
        public void Constructor_ClampsValuesToValidRange()
        {
            // Arrange & Act: Test clamping of max, start energy, and bar %
            var energy = new TeamEnergyState(maxEnergy: 0, startEnergy: 10, startBarPct: 150f);

            // Assert
            Assert.AreEqual(1, energy.MaxEnergy, "MaxEnergy should clamp to minimum 1");
            Assert.AreEqual(1, energy.Energy, "Energy should clamp to MaxEnergy (1)");
            Assert.AreEqual(100f, energy.EnergyBarPct, "EnergyBarPct should clamp to 100%");
        }

        #endregion

        #region GainEnergy Tests

        [Test]
        public void GainEnergy_IncrementsCorrectly()
        {
            // Arrange
            Assert.AreEqual(0, _teamEnergy.Energy, "Starting energy should be 0");

            // Act
            _teamEnergy.GainEnergy(2);

            // Assert
            Assert.AreEqual(2, _teamEnergy.Energy, "Energy should increment by 2");
        }

        [Test]
        public void GainEnergy_ClampsAtMaxEnergy()
        {
            // Arrange
            _teamEnergy.GainEnergy(4); // Set to 4/5

            // Act
            _teamEnergy.GainEnergy(5); // Try to add 5 (would exceed max)

            // Assert
            Assert.AreEqual(5, _teamEnergy.Energy, "Energy should clamp at MaxEnergy (5)");
        }

        [Test]
        public void GainEnergy_IgnoresNegativeAndZeroValues()
        {
            // Arrange
            _teamEnergy.GainEnergy(3); // Set to 3

            // Act
            _teamEnergy.GainEnergy(-1);
            int afterNegative = _teamEnergy.Energy;
            _teamEnergy.GainEnergy(0);
            int afterZero = _teamEnergy.Energy;

            // Assert
            Assert.AreEqual(3, afterNegative, "Negative gain should be ignored");
            Assert.AreEqual(3, afterZero, "Zero gain should be ignored");
        }

        #endregion

        #region TrySpendEnergy Tests

        [Test]
        public void TrySpendEnergy_SucceedsWhenEnoughEnergy()
        {
            // Arrange
            _teamEnergy.GainEnergy(3); // Set to 3

            // Act
            bool success = _teamEnergy.TrySpendEnergy(2);

            // Assert
            Assert.IsTrue(success, "Spend should succeed with sufficient energy");
            Assert.AreEqual(1, _teamEnergy.Energy, "Energy should decrement by 2 (3 - 2 = 1)");
        }

        [Test]
        public void TrySpendEnergy_FailsWhenInsufficientEnergy()
        {
            // Arrange
            _teamEnergy.GainEnergy(1); // Set to 1

            // Act
            bool success = _teamEnergy.TrySpendEnergy(2); // Try to spend more than available

            // Assert
            Assert.IsFalse(success, "Spend should fail with insufficient energy");
            Assert.AreEqual(1, _teamEnergy.Energy, "Energy should remain unchanged");
        }

        [Test]
        public void TrySpendEnergy_AllowsZeroAndNegativeAmounts()
        {
            // Arrange
            _teamEnergy.GainEnergy(2);

            // Act
            bool zeroSuccess = _teamEnergy.TrySpendEnergy(0);
            bool negativeSuccess = _teamEnergy.TrySpendEnergy(-1);

            // Assert
            Assert.IsTrue(zeroSuccess, "Spending 0 should succeed");
            Assert.IsTrue(negativeSuccess, "Spending negative should succeed (no-op)");
            Assert.AreEqual(2, _teamEnergy.Energy, "Energy should remain unchanged");
        }

        #endregion

        #region AddToBarAndConvert Tests

        [Test]
        public void AddToBarAndConvert_AddsToBar_NoConversion()
        {
            // Arrange & Act
            int converted = _teamEnergy.AddToBarAndConvert(50f);

            // Assert
            Assert.AreEqual(0, converted, "No energy should convert (bar < 100)");
            Assert.AreEqual(50f, _teamEnergy.EnergyBarPct, "Bar should be 50%");
            Assert.AreEqual(0, _teamEnergy.Energy, "Energy should remain 0");
        }

        [Test]
        public void AddToBarAndConvert_ConvertsFullBar()
        {
            // Arrange & Act
            int converted = _teamEnergy.AddToBarAndConvert(100f);

            // Assert
            Assert.AreEqual(1, converted, "Should convert 100% bar to 1 energy");
            Assert.AreEqual(0f, _teamEnergy.EnergyBarPct, "Bar should reset to 0%");
            Assert.AreEqual(1, _teamEnergy.Energy, "Energy should be 1");
        }

        [Test]
        public void AddToBarAndConvert_ConvertsMultipleBars()
        {
            // Arrange & Act
            int converted = _teamEnergy.AddToBarAndConvert(250f); // 2.5 bars = 2 energy + 50% remainder

            // Assert
            Assert.AreEqual(2, converted, "Should convert 200% to 2 energy");
            Assert.AreEqual(50f, _teamEnergy.EnergyBarPct, "Bar should have 50% remainder");
            Assert.AreEqual(2, _teamEnergy.Energy, "Energy should be 2");
        }

        [Test]
        public void AddToBarAndConvert_CarriesRemainderCorrectly()
        {
            // Arrange
            _teamEnergy.AddToBarAndConvert(60f); // Bar at 60%

            // Act
            int converted = _teamEnergy.AddToBarAndConvert(50f); // Add 50% (total 110%)

            // Assert
            Assert.AreEqual(1, converted, "Should convert 100% to 1 energy");
            Assert.AreEqual(10f, _teamEnergy.EnergyBarPct, "Bar should have 10% remainder");
            Assert.AreEqual(1, _teamEnergy.Energy, "Energy should be 1");
        }

        [Test]
        public void AddToBarAndConvert_RespectsMaxEnergy()
        {
            // Arrange
            _teamEnergy.GainEnergy(5); // Set to max (5/5)

            // Act
            int converted = _teamEnergy.AddToBarAndConvert(200f); // Try to add 2 energy via bars

            // Assert
            Assert.AreEqual(0, converted, "No energy should convert (already at max)");
            Assert.AreEqual(0f, _teamEnergy.EnergyBarPct, "Bar should be consumed but not convert");
            Assert.AreEqual(5, _teamEnergy.Energy, "Energy should stay at max");
        }

        [Test]
        public void AddToBarAndConvert_PartialConversionAtMax()
        {
            // Arrange
            _teamEnergy.GainEnergy(4); // Set to 4/5 (room for 1 more)

            // Act
            int converted = _teamEnergy.AddToBarAndConvert(300f); // Try to add 3 energy via bars

            // Assert
            Assert.AreEqual(1, converted, "Only 1 energy should convert (max limit)");
            Assert.AreEqual(0f, _teamEnergy.EnergyBarPct, "Excess bar % should be lost");
            Assert.AreEqual(5, _teamEnergy.Energy, "Energy should be at max");
        }

        [Test]
        public void AddToBarAndConvert_IgnoresNegativeAndZeroValues()
        {
            // Arrange
            _teamEnergy.AddToBarAndConvert(30f); // Bar at 30%

            // Act
            int negativeConverted = _teamEnergy.AddToBarAndConvert(-50f);
            int zeroConverted = _teamEnergy.AddToBarAndConvert(0f);

            // Assert
            Assert.AreEqual(0, negativeConverted, "Negative add should not convert");
            Assert.AreEqual(0, zeroConverted, "Zero add should not convert");
            Assert.AreEqual(30f, _teamEnergy.EnergyBarPct, "Bar should remain at 30%");
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void CompleteEnergyFlow_GainSpendConvert()
        {
            // Simulate a complete turn flow
            
            // Act: Gain via bar
            _teamEnergy.AddToBarAndConvert(100f); // 1 energy
            Assert.AreEqual(1, _teamEnergy.Energy, "After bar conversion");

            // Act: Gain direct energy
            _teamEnergy.GainEnergy(1);
            Assert.AreEqual(2, _teamEnergy.Energy, "After direct gain");

            // Act: Spend energy
            bool success = _teamEnergy.TrySpendEnergy(2);
            Assert.IsTrue(success, "Spend should succeed");
            Assert.AreEqual(0, _teamEnergy.Energy, "After spending");

            // Act: Add to bar again
            _teamEnergy.AddToBarAndConvert(50f);
            Assert.AreEqual(50f, _teamEnergy.EnergyBarPct, "Bar accumulating again");
        }

        #endregion
    }
}
