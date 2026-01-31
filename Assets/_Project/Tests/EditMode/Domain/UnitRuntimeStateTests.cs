using NUnit.Framework;
using Project.Domain;
using VanguardArena.Tests.Utils;
using System.Linq;
using UnityEngine;

namespace VanguardArena.Tests.EditMode.Domain
{
    /// <summary>
    /// Unit tests for UnitRuntimeState status effect integration, computed stats, and state management.
    /// Tests cooldown ticking, status effect application/stacking, shield absorption, and burn damage.
    /// </summary>
    public class UnitRuntimeStateTests
    {
        private UnitRuntimeState _unit;

        [SetUp]
        public void SetUp()
        {
            // Create a test unit with known stats
            var testUnit = BattleTestBuilder.Simple1v1Melee()
                .BuildStates().players[0];
            _unit = testUnit;
        }

        #region Computed Stats Tests

        [Test]
        public void CurrentATK_NoBuffs_ReturnsBaseATK()
        {
            // Arrange: Unit has no status effects
            Assert.AreEqual(0, _unit.ActiveStatusEffects.Count, "Should start with no effects");

            // Act & Assert
            Assert.AreEqual(_unit.ATK, _unit.CurrentATK, "CurrentATK should equal base ATK with no buffs");
        }

        [Test]
        public void CurrentATK_WithATKUp_IncreasesCorrectly()
        {
            // Arrange: Apply +50% ATK buff
            _unit.ApplyStatusEffect(StatusEffectType.ATKUp, value: 0, modifier: 0.5f, duration: 3);

            // Act
            int buffedATK = _unit.CurrentATK;
            int expectedATK = Mathf.RoundToInt(_unit.ATK * 1.5f); // Base * 1.5

            // Assert
            Assert.AreEqual(expectedATK, buffedATK, "CurrentATK should increase by 50%");
        }

        [Test]
        public void CurrentATK_WithATKDown_DecreasesCorrectly()
        {
            // Arrange: Apply -30% ATK debuff
            _unit.ApplyStatusEffect(StatusEffectType.ATKDown, value: 0, modifier: 0.3f, duration: 2);

            // Act
            int debuffedATK = _unit.CurrentATK;
            int expectedATK = Mathf.RoundToInt(_unit.ATK * 0.7f); // Base * 0.7

            // Assert
            Assert.AreEqual(expectedATK, debuffedATK, "CurrentATK should decrease by 30%");
        }

        [Test]
        public void CurrentDEF_WithDEFUp_IncreasesCorrectly()
        {
            // Arrange: Apply +40% DEF buff
            _unit.ApplyStatusEffect(StatusEffectType.DEFUp, value: 0, modifier: 0.4f, duration: 3);

            // Act
            int buffedDEF = _unit.CurrentDEF;
            int expectedDEF = Mathf.RoundToInt(_unit.DEF * 1.4f);

            // Assert
            Assert.AreEqual(expectedDEF, buffedDEF, "CurrentDEF should increase by 40%");
        }

        [Test]
        public void CurrentDEF_WithDEFDown_DecreasesCorrectly()
        {
            // Arrange: Apply -50% DEF debuff
            _unit.ApplyStatusEffect(StatusEffectType.DEFDown, value: 0, modifier: 0.5f, duration: 2);

            // Act
            int debuffedDEF = _unit.CurrentDEF;
            int expectedDEF = Mathf.RoundToInt(_unit.DEF * 0.5f);

            // Assert
            Assert.AreEqual(expectedDEF, debuffedDEF, "CurrentDEF should decrease by 50%");
        }

        [Test]
        public void CurrentATK_MultipleBuffsAndDebuffs_StacksCorrectly()
        {
            // Arrange: Apply +30% ATKUp and -20% ATKDown (net +10%)
            _unit.ApplyStatusEffect(StatusEffectType.ATKUp, value: 0, modifier: 0.3f, duration: 3);
            _unit.ApplyStatusEffect(StatusEffectType.ATKDown, value: 0, modifier: 0.2f, duration: 3);

            // Act
            int netATK = _unit.CurrentATK;
            int expectedATK = Mathf.RoundToInt(_unit.ATK * 1.1f); // Base * (1 + 0.3 - 0.2)

            // Assert
            Assert.AreEqual(expectedATK, netATK, "Multiple stat modifiers should stack additively");
        }

        #endregion

        #region Status Effect Application Tests

        [Test]
        public void ApplyStatusEffect_Shield_Stacks()
        {
            // Arrange & Act
            _unit.ApplyStatusEffect(StatusEffectType.Shield, value: 50, modifier: 0f, duration: 3);
            _unit.ApplyStatusEffect(StatusEffectType.Shield, value: 30, modifier: 0f, duration: 2);

            // Assert
            Assert.AreEqual(80, _unit.ShieldAmount, "Shields should stack by value (50 + 30)");
            Assert.AreEqual(2, _unit.ActiveStatusEffects.Count(s => s.Type == StatusEffectType.Shield), "Should have 2 separate shield effects");
        }

        [Test]
        public void ApplyStatusEffect_Burn_RefreshesDuration()
        {
            // Arrange
            _unit.ApplyStatusEffect(StatusEffectType.Burn, value: 10, modifier: 0f, duration: 2);
            var firstBurn = _unit.ActiveStatusEffects.First(s => s.Type == StatusEffectType.Burn);

            // Act: Apply another burn (should refresh, not stack)
            _unit.ApplyStatusEffect(StatusEffectType.Burn, value: 15, modifier: 0f, duration: 4);

            // Assert
            Assert.AreEqual(1, _unit.ActiveStatusEffects.Count(s => s.Type == StatusEffectType.Burn), "Should only have 1 burn effect");
            Assert.AreEqual(4, firstBurn.DurationTurns, "Duration should be refreshed to 4");
            Assert.AreEqual(15, firstBurn.Value, "Damage should update to new value (15)");
        }

        [Test]
        public void ApplyStatusEffect_ATKUp_HigherMagnitudeWins()
        {
            // Arrange: Apply +30% ATK buff
            _unit.ApplyStatusEffect(StatusEffectType.ATKUp, value: 0, modifier: 0.3f, duration: 2);

            // Act: Apply stronger +50% buff (should replace)
            _unit.ApplyStatusEffect(StatusEffectType.ATKUp, value: 0, modifier: 0.5f, duration: 3);

            // Assert
            var atkBuff = _unit.ActiveStatusEffects.First(s => s.Type == StatusEffectType.ATKUp);
            Assert.AreEqual(0.5f, atkBuff.Modifier, "Higher magnitude should replace");
            Assert.AreEqual(3, atkBuff.DurationTurns, "Duration should be from new effect");
            Assert.AreEqual(1, _unit.ActiveStatusEffects.Count(s => s.Type == StatusEffectType.ATKUp), "Should only have 1 ATKUp");
        }

        [Test]
        public void ApplyStatusEffect_ATKUp_LowerMagnitudeIgnored()
        {
            // Arrange: Apply +50% ATK buff
            _unit.ApplyStatusEffect(StatusEffectType.ATKUp, value: 0, modifier: 0.5f, duration: 3);

            // Act: Apply weaker +30% buff (should be ignored)
            _unit.ApplyStatusEffect(StatusEffectType.ATKUp, value: 0, modifier: 0.3f, duration: 5);

            // Assert
            var atkBuff = _unit.ActiveStatusEffects.First(s => s.Type == StatusEffectType.ATKUp);
            Assert.AreEqual(0.5f, atkBuff.Modifier, "Original stronger modifier should remain");
            Assert.AreEqual(3, atkBuff.DurationTurns, "Original duration should remain");
        }

        #endregion

        #region Status Queries Tests

        [Test]
        public void IsStunned_ReturnsTrue_WhenStunActive()
        {
            // Arrange & Act
            _unit.ApplyStatusEffect(StatusEffectType.Stun, value: 0, modifier: 0f, duration: 2);

            // Assert
            Assert.IsTrue(_unit.IsStunned, "Unit should be stunned");
        }

        [Test]
        public void IsStunned_ReturnsFalse_WhenNoStun()
        {
            // Arrange: No stun applied
            Assert.AreEqual(0, _unit.ActiveStatusEffects.Count, "Should start with no effects");

            // Act & Assert
            Assert.IsFalse(_unit.IsStunned, "Unit should not be stunned");
        }

        [Test]
        public void ShieldAmount_SumsAllActiveShields()
        {
            // Arrange & Act
            _unit.ApplyStatusEffect(StatusEffectType.Shield, value: 40, modifier: 0f, duration: 3);
            _unit.ApplyStatusEffect(StatusEffectType.Shield, value: 25, modifier: 0f, duration: 2);
            _unit.ApplyStatusEffect(StatusEffectType.Shield, value: 15, modifier: 0f, duration: 1);

            // Assert
            Assert.AreEqual(80, _unit.ShieldAmount, "Should sum all shields (40 + 25 + 15)");
        }

        #endregion

        #region Cooldown Tests

        [Test]
        public void OnNewTurnTickCooldown_DecrementsCooldown()
        {
            // Arrange
            _unit.UltimateCooldownRemaining = 3;

            // Act
            _unit.OnNewTurnTickCooldown();

            // Assert
            Assert.AreEqual(2, _unit.UltimateCooldownRemaining, "Cooldown should decrement by 1");
        }

        [Test]
        public void OnNewTurnTickCooldown_StopsAtZero()
        {
            // Arrange
            _unit.UltimateCooldownRemaining = 1;

            // Act
            _unit.OnNewTurnTickCooldown(); // 1 -> 0
            _unit.OnNewTurnTickCooldown(); // 0 -> 0 (should not go negative)

            // Assert
            Assert.AreEqual(0, _unit.UltimateCooldownRemaining, "Cooldown should not go below 0");
        }

        [Test]
        public void OnNewTurnTickCooldown_TicksStatusEffectDurations()
        {
            // Arrange
            _unit.ApplyStatusEffect(StatusEffectType.ATKUp, value: 0, modifier: 0.3f, duration: 3);
            var effect = _unit.ActiveStatusEffects.First();

            // Act
            _unit.OnNewTurnTickCooldown();

            // Assert
            Assert.AreEqual(2, effect.DurationTurns, "Status effect duration should tick down");
        }

        [Test]
        public void OnNewTurnTickCooldown_RemovesExpiredEffects()
        {
            // Arrange
            _unit.ApplyStatusEffect(StatusEffectType.DEFUp, value: 0, modifier: 0.2f, duration: 1);
            Assert.AreEqual(1, _unit.ActiveStatusEffects.Count, "Should have 1 effect");

            // Act
            _unit.OnNewTurnTickCooldown(); // Duration: 1 -> 0 (expires)

            // Assert
            Assert.AreEqual(0, _unit.ActiveStatusEffects.Count, "Expired effect should be removed");
        }

        #endregion

        #region Shield Absorption Tests

        [Test]
        public void AbsorbDamageWithShield_FullAbsorption()
        {
            // Arrange
            _unit.ApplyStatusEffect(StatusEffectType.Shield, value: 100, modifier: 0f, duration: 3);

            // Act
            int remainingDamage = _unit.AbsorbDamageWithShield(50);

            // Assert
            Assert.AreEqual(0, remainingDamage, "Shield should absorb all damage");
            Assert.AreEqual(50, _unit.ShieldAmount, "Shield should be reduced to 50");
        }

        [Test]
        public void AbsorbDamageWithShield_PartialAbsorption()
        {
            // Arrange
            _unit.ApplyStatusEffect(StatusEffectType.Shield, value: 30, modifier: 0f, duration: 3);

            // Act
            int remainingDamage = _unit.AbsorbDamageWithShield(50);

            // Assert
            Assert.AreEqual(20, remainingDamage, "20 damage should penetrate (50 - 30)");
            Assert.AreEqual(0, _unit.ShieldAmount, "Shield should be completely broken");
        }

        [Test]
        public void AbsorbDamageWithShield_MultipleShields_FIFO()
        {
            // Arrange: Add shields in order (FIFO = process from end)
            _unit.ApplyStatusEffect(StatusEffectType.Shield, value: 20, modifier: 0f, duration: 3); // Shield 1 (index 0)
            _unit.ApplyStatusEffect(StatusEffectType.Shield, value: 15, modifier: 0f, duration: 2); // Shield 2 (index 1)

            // Act
            int remainingDamage = _unit.AbsorbDamageWithShield(30);

            // Assert
            // Shield 2 (added last, processed first): 15 absorbed, 15 damage remains
            // Shield 1: 15 absorbed, 0 damage remains
            Assert.AreEqual(0, remainingDamage, "Both shields should absorb all damage (15 + 15 = 30)");
            Assert.AreEqual(5, _unit.ShieldAmount, "Shield 1 should have 5 remaining (20 - 15)");
            Assert.AreEqual(1, _unit.ActiveStatusEffects.Count(s => s.Type == StatusEffectType.Shield), "Broken shield should be removed");
        }

        [Test]
        public void AbsorbDamageWithShield_NoDamage_NoChange()
        {
            // Arrange
            _unit.ApplyStatusEffect(StatusEffectType.Shield, value: 50, modifier: 0f, duration: 3);

            // Act
            int remainingDamage = _unit.AbsorbDamageWithShield(0);

            // Assert
            Assert.AreEqual(0, remainingDamage, "No damage should pass through");
            Assert.AreEqual(50, _unit.ShieldAmount, "Shield should remain intact");
        }

        #endregion

        #region Burn Damage Tests

        [Test]
        public void ProcessBurnDamage_AppliesDamageToHP()
        {
            // Arrange
            _unit.ApplyStatusEffect(StatusEffectType.Burn, value: 15, modifier: 0f, duration: 3);
            int initialHP = _unit.CurrentHP;

            // Act
            int burnDamage = _unit.ProcessBurnDamage();

            // Assert
            Assert.AreEqual(15, burnDamage, "Should return burn damage amount");
            Assert.AreEqual(initialHP - 15, _unit.CurrentHP, "HP should decrease by burn damage");
        }

        [Test]
        public void ProcessBurnDamage_MultipleBurns_StackDamage()
        {
            // Arrange: Add 2 burn effects (they should stack)
            _unit.ApplyStatusEffect(StatusEffectType.Burn, value: 10, modifier: 0f, duration: 2);
            // Burn refreshes, so apply to different "source" by directly adding
            var secondBurn = new StatusEffect(StatusEffectType.Burn, value: 8, modifier: 0f, duration: 3);
            _unit.ActiveStatusEffects.Add(secondBurn); // Force 2 burns
            
            int initialHP = _unit.CurrentHP;

            // Act
            int burnDamage = _unit.ProcessBurnDamage();

            // Assert
            // Note: Based on implementation, Burn refreshes. But if we manually add 2, they both trigger.
            // This tests the ProcessBurnDamage logic itself.
            Assert.AreEqual(18, burnDamage, "Should sum both burn effects (10 + 8)");
            Assert.AreEqual(initialHP - 18, _unit.CurrentHP, "HP should decrease by total burn");
        }

        [Test]
        public void ProcessBurnDamage_NoBurn_ReturnsZero()
        {
            // Arrange: No burn effects
            int initialHP = _unit.CurrentHP;

            // Act
            int burnDamage = _unit.ProcessBurnDamage();

            // Assert
            Assert.AreEqual(0, burnDamage, "Should return 0 damage");
            Assert.AreEqual(initialHP, _unit.CurrentHP, "HP should remain unchanged");
        }

        [Test]
        public void ProcessBurnDamage_ClampsAtZeroHP()
        {
            // Arrange
            _unit.CurrentHP = 5; // Low HP
            _unit.ApplyStatusEffect(StatusEffectType.Burn, value: 100, modifier: 0f, duration: 2);

            // Act
            int burnDamage = _unit.ProcessBurnDamage();

            // Assert
            Assert.AreEqual(100, burnDamage, "Should return full burn damage");
            Assert.AreEqual(0, _unit.CurrentHP, "HP should clamp at 0, not go negative");
            Assert.IsFalse(_unit.IsAlive, "Unit should be dead");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void FullTurnCycle_StatusEffectsTickAndExpire()
        {
            // Arrange: Apply multiple effects
            _unit.ApplyStatusEffect(StatusEffectType.ATKUp, value: 0, modifier: 0.3f, duration: 2);
            _unit.ApplyStatusEffect(StatusEffectType.Shield, value: 50, modifier: 0f, duration: 1);
            _unit.UltimateCooldownRemaining = 3;

            // Act: Simulate turn end
            _unit.OnNewTurnTickCooldown();

            // Assert
            var atkBuff = _unit.ActiveStatusEffects.FirstOrDefault(s => s.Type == StatusEffectType.ATKUp);
            var shield = _unit.ActiveStatusEffects.FirstOrDefault(s => s.Type == StatusEffectType.Shield);
            
            Assert.AreEqual(1, atkBuff?.DurationTurns, "ATKUp should tick to 1");
            Assert.IsNull(shield, "Shield (duration 1) should expire and be removed");
            Assert.AreEqual(2, _unit.UltimateCooldownRemaining, "Cooldown should tick to 2");
        }

        #endregion
    }
}
