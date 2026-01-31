using NUnit.Framework;
using Project.Domain;

namespace VanguardArena.Tests.EditMode.Domain
{
    /// <summary>
    /// Unit tests for StatusEffect lifecycle and behavior.
    /// Tests duration ticking, expiry, and stacking rules.
    /// </summary>
    public class StatusEffectTests
    {
        #region Constructor and Basic Properties

        [Test]
        public void Constructor_InitializesValuesCorrectly()
        {
            // Arrange & Act
            var effect = new StatusEffect(StatusEffectType.Shield, value: 50, modifier: 0f, duration: 3);

            // Assert
            Assert.AreEqual(StatusEffectType.Shield, effect.Type);
            Assert.AreEqual(50, effect.Value);
            Assert.AreEqual(0f, effect.Modifier);
            Assert.AreEqual(3, effect.DurationTurns);
            Assert.IsFalse(effect.IsExpired, "New effect should not be expired");
        }

        #endregion

        #region Duration and Expiry Tests

        [Test]
        public void TickDuration_DecrementsByOne()
        {
            // Arrange
            var effect = new StatusEffect(StatusEffectType.ATKUp, value: 0, modifier: 0.2f, duration: 3);

            // Act
            effect.TickDuration();

            // Assert
            Assert.AreEqual(2, effect.DurationTurns, "Duration should decrement by 1");
            Assert.IsFalse(effect.IsExpired, "Effect should not be expired yet");
        }

        [Test]
        public void TickDuration_MarksExpiredAtZero()
        {
            // Arrange
            var effect = new StatusEffect(StatusEffectType.Burn, value: 10, modifier: 0f, duration: 1);

            // Act
            effect.TickDuration(); // Duration: 1 -> 0

            // Assert
            Assert.AreEqual(0, effect.DurationTurns, "Duration should be 0");
            Assert.IsTrue(effect.IsExpired, "Effect should be expired at duration 0");
        }

        [Test]
        public void TickDuration_StaysAtZero()
        {
            // Arrange
            var effect = new StatusEffect(StatusEffectType.Stun, value: 0, modifier: 0f, duration: 1);
            effect.TickDuration(); // Duration: 1 -> 0

            // Act
            effect.TickDuration(); // Duration: 0 -> 0 (should not go negative)

            // Assert
            Assert.AreEqual(0, effect.DurationTurns, "Duration should stay at 0");
            Assert.IsTrue(effect.IsExpired, "Effect should still be expired");
        }

        [Test]
        public void IsExpired_ReturnsTrueForZeroDuration()
        {
            // Arrange
            var effect = new StatusEffect(StatusEffectType.DEFDown, value: 0, modifier: 0.3f, duration: 0);

            // Act & Assert
            Assert.IsTrue(effect.IsExpired, "Effect with 0 duration should be expired");
        }

        [Test]
        public void IsExpired_ReturnsFalseForPositiveDuration()
        {
            // Arrange
            var effect = new StatusEffect(StatusEffectType.DEFUp, value: 0, modifier: 0.25f, duration: 5);

            // Act & Assert
            Assert.IsFalse(effect.IsExpired, "Effect with positive duration should not be expired");
        }

        #endregion

        #region Multiple Tick Lifecycle Test

        [Test]
        public void StatusEffect_LifecycleFrom3To0()
        {
            // Arrange
            var effect = new StatusEffect(StatusEffectType.Shield, value: 100, modifier: 0f, duration: 3);

            // Assert initial state
            Assert.AreEqual(3, effect.DurationTurns);
            Assert.IsFalse(effect.IsExpired);

            // Act & Assert: Tick 1
            effect.TickDuration();
            Assert.AreEqual(2, effect.DurationTurns, "After tick 1");
            Assert.IsFalse(effect.IsExpired);

            // Act & Assert: Tick 2
            effect.TickDuration();
            Assert.AreEqual(1, effect.DurationTurns, "After tick 2");
            Assert.IsFalse(effect.IsExpired);

            // Act & Assert: Tick 3 (expires)
            effect.TickDuration();
            Assert.AreEqual(0, effect.DurationTurns, "After tick 3");
            Assert.IsTrue(effect.IsExpired, "Should be expired");
        }

        #endregion

        #region Value Modification Tests

        [Test]
        public void StatusEffect_ValueCanBeModified()
        {
            // Arrange
            var effect = new StatusEffect(StatusEffectType.Shield, value: 100, modifier: 0f, duration: 3);

            // Act
            effect.Value = 50; // Simulate shield absorbing damage

            // Assert
            Assert.AreEqual(50, effect.Value, "Value should be modifiable");
        }

        [Test]
        public void StatusEffect_DurationCanBeRefreshed()
        {
            // Arrange
            var effect = new StatusEffect(StatusEffectType.Burn, value: 20, modifier: 0f, duration: 2);
            effect.TickDuration(); // Duration: 2 -> 1

            // Act
            effect.DurationTurns = 3; // Refresh duration (simulates reapply)

            // Assert
            Assert.AreEqual(3, effect.DurationTurns, "Duration should be refreshed");
            Assert.IsFalse(effect.IsExpired, "Effect should no longer be expired");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void StatusEffect_NegativeDurationClampsToZero()
        {
            // Arrange
            var effect = new StatusEffect(StatusEffectType.ATKDown, value: 0, modifier: 0.1f, duration: -5);

            // Act & Assert
            // Note: Constructor doesn't clamp, but IsExpired should handle it
            Assert.IsTrue(effect.IsExpired, "Negative duration should be treated as expired");
        }

        [Test]
        public void StatusEffect_ZeroDurationImmediatelyExpired()
        {
            // Arrange & Act
            var effect = new StatusEffect(StatusEffectType.Stun, value: 0, modifier: 0f, duration: 0);

            // Assert
            Assert.IsTrue(effect.IsExpired, "Effect with 0 duration should be immediately expired");
        }

        #endregion

        #region Different Effect Types

        [Test]
        public void StatusEffect_ShieldType_UsesValueNotModifier()
        {
            // Arrange & Act
            var shield = new StatusEffect(StatusEffectType.Shield, value: 150, modifier: 0f, duration: 5);

            // Assert
            Assert.AreEqual(150, shield.Value, "Shield uses Value for absorption");
            Assert.AreEqual(0f, shield.Modifier, "Shield doesn't use Modifier");
        }

        [Test]
        public void StatusEffect_ATKUpType_UsesModifierNotValue()
        {
            // Arrange & Act
            var atkBuff = new StatusEffect(StatusEffectType.ATKUp, value: 0, modifier: 0.3f, duration: 3);

            // Assert
            Assert.AreEqual(0.3f, atkBuff.Modifier, "ATKUp uses Modifier (30%)");
            Assert.AreEqual(0, atkBuff.Value, "ATKUp typically has 0 Value");
        }

        [Test]
        public void StatusEffect_BurnType_UsesValueForDamage()
        {
            // Arrange & Act
            var burn = new StatusEffect(StatusEffectType.Burn, value: 25, modifier: 0f, duration: 4);

            // Assert
            Assert.AreEqual(25, burn.Value, "Burn uses Value for damage per turn");
            Assert.AreEqual(0f, burn.Modifier, "Burn doesn't use Modifier");
        }

        [Test]
        public void StatusEffect_StunType_OnlyUsesDuration()
        {
            // Arrange & Act
            var stun = new StatusEffect(StatusEffectType.Stun, value: 0, modifier: 0f, duration: 2);

            // Assert
            Assert.AreEqual(0, stun.Value, "Stun doesn't need Value");
            Assert.AreEqual(0f, stun.Modifier, "Stun doesn't need Modifier");
            Assert.AreEqual(2, stun.DurationTurns, "Stun only uses Duration");
        }

        #endregion
    }
}
