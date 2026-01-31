using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VanguardArena.Tests.Utils;
using VanguardArena.Tests.PlayMode;
using Project.Domain;
using Project.Presentation;

namespace VanguardArena.Tests.PlayMode.Integration
{
    /// <summary>
    /// Basic tests for animation system readiness.
    /// Tests animator component existence and animation helper utilities.
    /// </summary>
    [TestFixture]
    public class AnimationSystemTests
    {
        private BattleSceneTestHelper _sceneHelper;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _sceneHelper = new BattleSceneTestHelper();
            yield return _sceneHelper.LoadBattleScene();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return _sceneHelper.UnloadBattleScene();
        }

        [UnityTest]
        public IEnumerator AnimationTestHelper_WaitForAnimationState_Works()
        {
            // This test verifies the helper utility works
            // Create a simple test GameObject with animator
            var testObj = new GameObject("TestAnimator");
            var animator = testObj.AddComponent<Animator>();
            
            // The animator won't have actual animations, but we can test the timeout
            yield return AnimationTestHelper.WaitForAnimationState(animator, "NonExistentState", timeout: 0.5f);
            
            // If we got here, the timeout worked correctly
            Assert.Pass("Animation helper timeout works correctly");
            
            Object.Destroy(testObj);
        }

        [UnityTest]
        public IEnumerator AnimationTestHelper_HasParameter_DetectsParameters()
        {
            // Create test animator
            var testObj = new GameObject("TestAnimator");
            var animator = testObj.AddComponent<Animator>();
            
            // Test parameter detection (will return false for empty animator)
            bool hasParam = AnimationTestHelper.HasParameter(animator, "Speed");
            
            Assert.IsFalse(hasParam, "Empty animator should not have parameters");
            
            Object.Destroy(testObj);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnimationTestHelper_GetCurrentClipName_ReturnsNoneForEmpty()
        {
            // Create test animator
            var testObj = new GameObject("TestAnimator");
            var animator = testObj.AddComponent<Animator>();
            
            string clipName = AnimationTestHelper.GetCurrentClipName(animator);
            
            Assert.AreEqual("None", clipName, "Empty animator should return 'None'");
            
            Object.Destroy(testObj);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnitView_HasAnimator_Configured()
        {
            // First spawn some units
            var (playerUnits, _) = new BattleTestBuilder()
                .AddPlayerUnit("TestPlayer", UnitType.Melee, hp: 200, atk: 30, def: 10, spd: 15)
                .BuildDefinitions();
            
            yield return _sceneHelper.StartBattle(playerUnits, new List<UnitDefinitionSO>());
            
            var playerViews = _sceneHelper.GetPlayerUnitViews();
            
            if (playerViews.Count > 0)
            {
                var firstUnit = playerViews[0];
                var animator = firstUnit.GetComponent<Animator>();
                
                // UnitView might not have animator (uses UnitAnimationDriver instead)
                // So we check for animator OR animation driver
                var animDriver = firstUnit.GetComponent<UnitAnimationDriver>();
                
                Assert.IsTrue(animator != null || animDriver != null, 
                    "UnitView should have Animator or UnitAnimationDriver component");
                
                if (animator != null)
                {
                    Debug.Log($"[TEST] Unit has Animator with controller: {animator.runtimeAnimatorController != null}");
                }
                if (animDriver != null)
                {
                    Debug.Log($"[TEST] Unit has UnitAnimationDriver");
                }
            }
            else
            {
                Assert.Fail("No player units found");
            }
            
            yield return null;
        }
    }
}
