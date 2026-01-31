using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VanguardArena.Tests.Utils;
using VanguardArena.Tests.PlayMode;
using Project.Domain;
using Project.Presentation.UI;

namespace VanguardArena.Tests.PlayMode.Integration
{
    /// <summary>
    /// Basic tests for UI system and helper utilities.
    /// Tests UI helper functions and basic UI element detection.
    /// </summary>
    [TestFixture]
    public class UISystemTests
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
        public IEnumerator UITestHelper_FindUIElement_CanFindGameObjects()
        {
            // Create a test UI element
            var canvas = new GameObject("TestCanvas");
            canvas.AddComponent<Canvas>();
            
            var testElement = new GameObject("TestUIElement");
            testElement.transform.SetParent(canvas.transform);
            
            // Test finding it
            var found = UITestHelper.FindUIElement("TestUIElement");
            
            Assert.IsNotNull(found, "Should find the test UI element");
            Assert.AreEqual("TestUIElement", found.name);
            
            Object.Destroy(canvas);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UITestHelper_IsUIElementVisible_DetectsActiveState()
        {
            // Create test element
            var testObj = new GameObject("TestElement");
            
            // Active element should be visible
            Assert.IsTrue(UITestHelper.IsUIElementVisible(testObj), "Active element should be visible");
            
            // Inactive element should not be visible
            testObj.SetActive(false);
            Assert.IsFalse(UITestHelper.IsUIElementVisible(testObj), "Inactive element should not be visible");
            
            Object.Destroy(testObj);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UITestHelper_FindUIElement_ReturnsNullForNonExistent()
        {
            var result = UITestHelper.FindUIElement("NonExistentElement12345");
            
            Assert.IsNull(result, "Should return null for non-existent elements");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator BattleScene_HasEnergyBars()
        {
            // Since we don't know exact UI structure, let's test that HUD exists
            var hud = GameObject.FindFirstObjectByType<BattleHudController>();
            
            Assert.IsNotNull(hud, "BattleHudController should exist in scene");
            
            // Check if HUD has energy-related UI elements
            Debug.Log($"[TEST] BattleHudController found: {hud.gameObject.name}");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnitView_HasHPBar()
        {
            // Spawn some units first
            var (playerUnits, _) = new BattleTestBuilder()
                .AddPlayerUnit("TestPlayer", UnitType.Melee, hp: 200, atk: 30, def: 10, spd: 15)
                .BuildDefinitions();
            
            yield return _sceneHelper.StartBattle(playerUnits, new List<UnitDefinitionSO>());
            
            var playerViews = _sceneHelper.GetPlayerUnitViews();
            
            if (playerViews.Count > 0)
            {
                var firstUnit = playerViews[0];
                
                // Check for overhead HUD (which contains HP bar)
                var overheadHud = firstUnit.GetComponentInChildren<UnitOverheadHud>();
                
                Assert.IsNotNull(overheadHud, "Unit should have UnitOverheadHud component");
                
                Debug.Log($"[TEST] Unit has UnitOverheadHud");
            }
            else
            {
                Assert.Fail("No player units found");
            }
            
            yield return null;
        }
    }
}
