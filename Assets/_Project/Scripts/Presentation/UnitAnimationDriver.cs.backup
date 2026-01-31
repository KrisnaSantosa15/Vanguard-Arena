using System;
using UnityEngine;
using Project.Domain;

namespace Project.Presentation
{
    [RequireComponent(typeof(Animator))]
    public sealed class UnitAnimationDriver : MonoBehaviour
    {
        public event Action<int> OnHitTriggered;

        private Animator _anim;
        private UnitView _view;

        private void Awake()
        {
            _anim = GetComponent<Animator>();
            _view = GetComponent<UnitView>();
        }

        public void OnAnimationHit(int index)
        {
            OnHitTriggered?.Invoke(index);
        }

        public void PlayAttack(UnitType unitType, bool isUltimate)
        {
            string triggerName = isUltimate
                ? (unitType == UnitType.Melee ? "MeleeUltimate" : "RangeUltimate")
                : (unitType == UnitType.Melee ? "MeleeAttack" : "RangeAttack");
            ForcePlayAnimation(triggerName);
        }

        public void PlayHit(bool isUltimateHit, bool isHeavy)
        {
            string triggerName = (isUltimateHit && isHeavy) ? "HitUltimate" : "HitBasic";
            ForcePlayAnimation(triggerName);
        }

        public void PlayDeath()
        {
            FileLogger.Log($"Setting IsDead=true for {gameObject.name}", "ANIM-DEATH");
            _anim.SetBool("IsDead", true);
        }

        public void SetDashing(bool isDashing)
        {
            _anim.SetBool("IsDashing", isDashing);
        }

        /// <summary>
        /// Force play animation by resetting pending triggers and setting the desired one.
        /// This ensures clean state transitions without relying on specific state names.
        /// </summary>
        private void ForcePlayAnimation(string triggerName)
        {
            // Reset all triggers to clear any queued animations
            _anim.ResetTrigger("MeleeAttack");
            _anim.ResetTrigger("MeleeUltimate");
            _anim.ResetTrigger("RangeAttack");
            _anim.ResetTrigger("RangeUltimate");
            _anim.ResetTrigger("HitBasic");
            _anim.ResetTrigger("HitUltimate");

            // Set the trigger for the desired animation
            _anim.SetTrigger(triggerName);
        }
    }
}