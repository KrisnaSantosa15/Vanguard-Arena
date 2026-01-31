using NUnit.Framework;
using Project.Domain;
using VanguardArena.Tests.Utils;

namespace VanguardArena.Tests.EditMode.Domain
{
    /// <summary>
    /// Unit tests for CombatCalculator damage/heal/shield formulas.
    /// Tests pure domain logic without Unity dependencies.
    /// </summary>
    public class CombatCalculatorTests
    {
        private IDeterministicRandom _rng;
        private UnitRuntimeState _attacker;
        private UnitRuntimeState _defender;

        [SetUp]
        public void SetUp()
        {
            // Use seeded RNG for deterministic tests
            _rng = new SeededRandom(42);

            // Create test units with minimal setup
            var builder = BattleTestBuilder.Simple1v1Melee().WithSeed(42);
            var (players, enemies, _) = builder.BuildStates();
            
            _attacker = players[0];
            _defender = enemies[0];
        }

        #region Basic Damage Tests

        [Test]
        public void ComputeBasicDamage_WithFixedRNG_ProducesConsistentDamage()
        {
            // Arrange: Same RNG seed should produce same result
            var rng1 = new SeededRandom(100);
            var rng2 = new SeededRandom(100);

            // Act
            var (damage1, crit1) = CombatCalculator.ComputeBasicDamage(_attacker, _defender, rng1);
            var (damage2, crit2) = CombatCalculator.ComputeBasicDamage(_attacker, _defender, rng2);

            // Assert
            Assert.AreEqual(damage1, damage2, "Same seed should produce same damage");
            Assert.AreEqual(crit1, crit2, "Same seed should produce same crit result");
        }

        [Test]
        public void ComputeBasicDamage_MinimumDamageIsOne()
        {
            // Arrange: Create weak attacker vs high DEF defender
            var weakAttacker = BattleTestBuilder.Simple1v1Melee()
                .AddPlayerUnit("Weak", atk: 1, def: 0)
                .AddEnemyUnit("Tank", hp: 100, atk: 10, def: 9999)
                .BuildStates().players[0];
            
            var tank = BattleTestBuilder.Simple1v1Melee()
                .AddPlayerUnit("Player", atk: 10, def: 0)
                .AddEnemyUnit("Tank", hp: 100, atk: 10, def: 9999)
                .BuildStates().enemies[0];

            // Act
            var (damage, _) = CombatCalculator.ComputeBasicDamage(weakAttacker, tank, _rng);

            // Assert
            Assert.GreaterOrEqual(damage, 1, "Damage should never be less than 1");
        }

        [Test]
        public void ComputeBasicDamage_HigherATK_ProducesHigherDamage()
        {
            // Arrange: Create both units in same builder for fair RNG comparison
            var testBuilder = new BattleTestBuilder()
                .AddPlayerUnit("Weak", atk: 10, def: 0, critRate: 0f)  // No crit for predictable results
                .AddPlayerUnit("Strong", atk: 50, def: 0, critRate: 0f)
                .AddEnemyUnit("Enemy", hp: 100, atk: 10, def: 5);  // Match _defender stats
            var units = testBuilder.BuildStates();
            var weakUnit = units.players[0];
            var strongUnit = units.players[1];

            // Act: Use same seed for fair comparison
            var rng1 = new SeededRandom(999);
            var rng2 = new SeededRandom(999);
            var (weakDamage, _) = CombatCalculator.ComputeBasicDamage(weakUnit, units.enemies[0], rng1);
            var (strongDamage, _) = CombatCalculator.ComputeBasicDamage(strongUnit, units.enemies[0], rng2);

            // Assert
            Assert.Greater(strongDamage, weakDamage, "Higher ATK should produce higher damage");
        }

        [Test]
        public void ComputeBasicDamage_HigherDEF_ReducesDamage()
        {
            // Arrange: Create both defenders in same builder
            var testBuilder = new BattleTestBuilder()
                .AddPlayerUnit("Attacker", atk: 20, def: 5, critRate: 0f)  // Match _attacker stats, no crit
                .AddEnemyUnit("WeakDef", hp: 100, atk: 10, def: 5)
                .AddEnemyUnit("StrongDef", hp: 100, atk: 10, def: 50);
            var units = testBuilder.BuildStates();
            var attacker = units.players[0];
            var weakDefender = units.enemies[0];
            var strongDefender = units.enemies[1];

            // Act: Use same seed for both
            var rng1 = new SeededRandom(999);
            var rng2 = new SeededRandom(999);
            var (weakDefDamage, _) = CombatCalculator.ComputeBasicDamage(attacker, weakDefender, rng1);
            var (strongDefDamage, _) = CombatCalculator.ComputeBasicDamage(attacker, strongDefender, rng2);

            // Assert
            Assert.Less(strongDefDamage, weakDefDamage, "Higher DEF should reduce damage");
        }

        [Test]
        public void ComputeBasicDamage_CritRate100_AlwaysCrits()
        {
            // Arrange: Create unit with 100% crit rate
            var testBuilder = new BattleTestBuilder()
                .AddPlayerUnit("Crit", atk: 20, def: 0, critRate: 100f, critDamage: 200f)
                .AddEnemyUnit("Enemy", hp: 100, atk: 10, def: 5);
            var units = testBuilder.BuildStates();
            var critUnit = units.players[0];
            var enemy = units.enemies[0];

            // Act: Test multiple times to ensure consistency
            bool allCrits = true;
            for (int i = 0; i < 20; i++)  // More iterations for better confidence
            {
                var rng = new SeededRandom(1000 + i);
                var (_, isCrit) = CombatCalculator.ComputeBasicDamage(critUnit, enemy, rng);
                if (!isCrit)
                {
                    allCrits = false;
                    break;
                }
            }

            // Assert
            Assert.IsTrue(allCrits, "100% crit rate should always crit");
        }

        [Test]
        public void ComputeBasicDamage_CritRate0_NeverCrits()
        {
            // Arrange: Create unit with 0% crit rate
            var testBuilder = new BattleTestBuilder()
                .AddPlayerUnit("NoCrit", atk: 20, def: 0, critRate: 0f)
                .AddEnemyUnit("Enemy", hp: 100, atk: 10, def: 5);
            var units = testBuilder.BuildStates();
            var noCritUnit = units.players[0];
            var enemy = units.enemies[0];

            // Act: Test multiple times
            bool anyCrits = false;
            for (int i = 0; i < 20; i++)  // More iterations for better confidence
            {
                var rng = new SeededRandom(2000 + i);
                var (_, isCrit) = CombatCalculator.ComputeBasicDamage(noCritUnit, enemy, rng);
                if (isCrit)
                {
                    anyCrits = true;
                    break;
                }
            }

            // Assert
            Assert.IsFalse(anyCrits, "0% crit rate should never crit");
        }

        [Test]
        public void ComputeBasicDamage_Crit_IncreasesActualDamage()
        {
            // Arrange: Create both units in same builder for fair comparison
            var testBuilder = new BattleTestBuilder()
                .AddPlayerUnit("Crit", atk: 20, def: 0, critRate: 100f, critDamage: 200f)
                .AddPlayerUnit("NoCrit", atk: 20, def: 0, critRate: 0f)
                .AddEnemyUnit("Enemy", hp: 100, atk: 10, def: 5);
            var units = testBuilder.BuildStates();
            var critUnit = units.players[0];
            var noCritUnit = units.players[1];
            var enemy = units.enemies[0];

            // Act: Use same seed for fair variance comparison
            var rng1 = new SeededRandom(999);
            var rng2 = new SeededRandom(999);
            var (critDamage, _) = CombatCalculator.ComputeBasicDamage(critUnit, enemy, rng1);
            var (normalDamage, _) = CombatCalculator.ComputeBasicDamage(noCritUnit, enemy, rng2);

            // Assert
            Assert.Greater(critDamage, normalDamage, "Crit damage should exceed normal damage");
        }

        #endregion

        #region Ultimate Damage Tests

        [Test]
        public void ComputeUltimateDamage_WithMultiplier_ProducesHigherDamage()
        {
            // Arrange: Default ultimate multiplier is 2.2x
            var rng1 = new SeededRandom(42);
            var rng2 = new SeededRandom(42);

            // Act
            var (basicDamage, _) = CombatCalculator.ComputeBasicDamage(_attacker, _defender, rng1);
            var (ultimateDamage, _) = CombatCalculator.ComputeUltimateDamage(_attacker, _defender, 2.2f, rng2);

            // Assert
            Assert.Greater(ultimateDamage, basicDamage, "Ultimate should deal more damage than basic");
        }

        [Test]
        public void ComputeUltimateDamage_CustomMultiplier_ScalesDamage()
        {
            // Arrange
            var rng1 = new SeededRandom(42);
            var rng2 = new SeededRandom(42);

            // Act
            var (damage1x, _) = CombatCalculator.ComputeUltimateDamage(_attacker, _defender, 1.0f, rng1);
            var (damage3x, _) = CombatCalculator.ComputeUltimateDamage(_attacker, _defender, 3.0f, rng2);

            // Assert
            Assert.Greater(damage3x, damage1x, "Higher multiplier should produce more damage");
        }

        #endregion

        #region Heal Tests

        [Test]
        public void ComputeHealAmount_MinimumIsOne()
        {
            // Arrange: Even with 0 ATK, heal should be at least 1
            var weakHealer = BattleTestBuilder.Simple1v1Melee()
                .AddPlayerUnit("Healer", atk: 0, def: 0)
                .BuildStates().players[0];

            // Act
            int heal = CombatCalculator.ComputeHealAmount(weakHealer, 1.5f, _rng);

            // Assert
            Assert.GreaterOrEqual(heal, 1, "Heal amount should never be less than 1");
        }

        [Test]
        public void ComputeHealAmount_ScalesWithATK()
        {
            // Arrange: Create both healers in same builder
            var testBuilder = new BattleTestBuilder()
                .AddPlayerUnit("Weak", atk: 10, def: 0)
                .AddPlayerUnit("Strong", atk: 50, def: 0);
            var units = testBuilder.BuildStates();
            var weakHealer = units.players[0];
            var strongHealer = units.players[1];

            // Act: Use same seed for fair comparison
            var rng1 = new SeededRandom(999);
            var rng2 = new SeededRandom(999);
            var weakHeal = CombatCalculator.ComputeHealAmount(weakHealer, 1.5f, rng1);
            var strongHeal = CombatCalculator.ComputeHealAmount(strongHealer, 1.5f, rng2);

            // Assert
            Assert.Greater(strongHeal, weakHeal, "Higher ATK should produce more healing");
        }

        #endregion

        #region Shield Tests

        [Test]
        public void ComputeShieldAmount_MinimumIsOne()
        {
            // Arrange: Even with 0 DEF, shield should be at least 1
            var weakTank = BattleTestBuilder.Simple1v1Melee()
                .AddPlayerUnit("Tank", atk: 10, def: 0)
                .BuildStates().players[0];

            // Act
            int shield = CombatCalculator.ComputeShieldAmount(weakTank, 2.0f, _rng);

            // Assert
            Assert.GreaterOrEqual(shield, 1, "Shield amount should never be less than 1");
        }

        [Test]
        public void ComputeShieldAmount_ScalesWithDEF()
        {
            // Arrange: Create both tanks in same builder
            var testBuilder = new BattleTestBuilder()
                .AddPlayerUnit("Weak", atk: 10, def: 5)
                .AddPlayerUnit("Strong", atk: 10, def: 50);
            var units = testBuilder.BuildStates();
            var weakTank = units.players[0];
            var strongTank = units.players[1];

            // Act: Use same seed for fair comparison
            var rng1 = new SeededRandom(999);
            var rng2 = new SeededRandom(999);
            var weakShield = CombatCalculator.ComputeShieldAmount(weakTank, 2.0f, rng1);
            var strongShield = CombatCalculator.ComputeShieldAmount(strongTank, 2.0f, rng2);

            // Assert
            Assert.Greater(strongShield, weakShield, "Higher DEF should produce more shield");
        }

        #endregion

        #region Variance Tests

        [Test]
        public void ComputeBasicDamage_VarianceBounds_AreMaintained()
        {
            // Arrange: Test variance is within 0.95-1.05 range
            // ATK=20, DEF=5 -> mitigation = 1000/(1000+5) = 0.995
            // Base damage = 20 * 0.995 = 19.9
            // With variance 0.95-1.05: 19.9*0.95=18.9, 19.9*1.05=20.9
            var testBuilder = new BattleTestBuilder()
                .AddPlayerUnit("Test", atk: 20, def: 0, critRate: 0f)  // NO CRIT
                .AddEnemyUnit("Enemy", hp: 100, atk: 10, def: 5);
            var units = testBuilder.BuildStates();

            int minDamage = int.MaxValue;
            int maxDamage = int.MinValue;

            // Act: Sample many times with different seeds to find actual bounds
            for (int i = 0; i < 100; i++)
            {
                var rng = new SeededRandom(5000 + i);
                var (damage, _) = CombatCalculator.ComputeBasicDamage(units.players[0], units.enemies[0], rng);
                minDamage = System.Math.Min(minDamage, damage);
                maxDamage = System.Math.Max(maxDamage, damage);
            }

            // Assert: Damage should vary but stay within reasonable bounds
            // Expected: 19.9 * 0.95 = 18.9 (rounds to 19), 19.9 * 1.05 = 20.9 (rounds to 21)
            // Allow ±1 tolerance for rounding edge cases
            Assert.GreaterOrEqual(minDamage, 18, "Minimum damage should respect variance lower bound");
            Assert.LessOrEqual(maxDamage, 22, "Maximum damage should respect variance upper bound (with rounding tolerance)");
        }

        #endregion
    }
}
