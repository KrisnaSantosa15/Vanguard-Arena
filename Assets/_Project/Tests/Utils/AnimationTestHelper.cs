using System.Collections;
using UnityEngine;

namespace VanguardArena.Tests.Utils
{
    /// <summary>
    /// Utilities for testing Unity animations in PlayMode.
    /// </summary>
    public static class AnimationTestHelper
    {
        /// <summary>
        /// Wait for animator to enter specific state.
        /// </summary>
        public static IEnumerator WaitForAnimationState(Animator animator, string stateName, int layer = 0, float timeout = 5f)
        {
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                if (animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName))
                {
                    yield break;
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            Debug.LogWarning($"[TEST] Animation state '{stateName}' not reached within {timeout}s");
        }

        /// <summary>
        /// Wait for animation to complete (normalized time >= 1).
        /// </summary>
        public static IEnumerator WaitForAnimationComplete(Animator animator, int layer = 0, float timeout = 10f)
        {
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
                
                if (stateInfo.normalizedTime >= 1.0f && !animator.IsInTransition(layer))
                {
                    yield break;
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            Debug.LogWarning($"[TEST] Animation did not complete within {timeout}s");
        }

        /// <summary>
        /// Check if animator parameter exists.
        /// </summary>
        public static bool HasParameter(Animator animator, string parameterName)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == parameterName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get current animation clip name.
        /// </summary>
        public static string GetCurrentClipName(Animator animator, int layer = 0)
        {
            var clipInfo = animator.GetCurrentAnimatorClipInfo(layer);
            return clipInfo.Length > 0 ? clipInfo[0].clip.name : "None";
        }
    }
}
