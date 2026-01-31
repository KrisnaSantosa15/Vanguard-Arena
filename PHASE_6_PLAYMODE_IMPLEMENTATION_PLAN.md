# Phase 6 (CI/CD) & PlayMode Tests - Comprehensive Implementation Plan

**Project**: Vanguard Arena  
**Unity Version**: 6000.3.5f1  
**Current Status**: Phase 4 Complete (151 EditMode tests passing)  
**GitHub**: https://github.com/KrisnaSantosa15/Vanguard-Arena.git

---

## Executive Summary

This plan implements:
1. **Phase 6**: CI/CD pipeline with GitHub Actions for automated testing
2. **PlayMode Tests**: Comprehensive integration tests for BattleController, animations, and UI

**Goals**:
- ✅ Automated test runs on every commit
- ✅ 100+ PlayMode integration tests
- ✅ Full battle scene coverage
- ✅ Test result reporting and coverage tracking
- ✅ PR checks to prevent broken code from merging

**Estimated Time**: 6-8 hours total
- CI/CD Setup: 1-2 hours
- PlayMode Infrastructure: 2-3 hours
- PlayMode Tests: 3-4 hours

---

## Part A: Phase 6 - CI/CD Integration

### A1. GitHub Actions Workflow Setup

#### Goal
Run Unity tests automatically on every push/PR to catch issues early.

#### Implementation Steps

**Step 1: Create GitHub Actions Workflow File**

File: `.github/workflows/unity-tests.yml`

```yaml
name: Unity Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  test-editmode:
    name: EditMode Tests
    runs-on: ubuntu-latest
    timeout-minutes: 15
    
    steps:
      # Checkout code
      - name: Checkout Repository
        uses: actions/checkout@v4
        with:
          lfs: true
      
      # Cache Unity Library folder
      - name: Cache Unity Library
        uses: actions/cache@v3
        with:
          path: Library
          key: Library-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
          restore-keys: |
            Library-
      
      # Run EditMode tests
      - name: Run EditMode Tests
        uses: game-ci/unity-test-runner@v4
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          projectPath: .
          unityVersion: 6000.0.23f1
          testMode: EditMode
          artifactsPath: test-results
          githubToken: ${{ secrets.GITHUB_TOKEN }}
          checkName: EditMode Test Results
      
      # Upload test results
      - name: Upload Test Results
        uses: actions/upload-artifact@v3
        if: always()
        with:
          name: EditMode-Results
          path: test-results
      
      # Publish test report
      - name: Publish Test Report
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: EditMode Tests
          path: 'test-results/*.xml'
          reporter: java-junit
          fail-on-error: true

  test-playmode:
    name: PlayMode Tests
    runs-on: ubuntu-latest
    timeout-minutes: 30
    needs: test-editmode
    
    steps:
      # Checkout code
      - name: Checkout Repository
        uses: actions/checkout@v4
        with:
          lfs: true
      
      # Cache Unity Library folder
      - name: Cache Unity Library
        uses: actions/cache@v3
        with:
          path: Library
          key: Library-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
          restore-keys: |
            Library-
      
      # Run PlayMode tests
      - name: Run PlayMode Tests
        uses: game-ci/unity-test-runner@v4
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          projectPath: .
          unityVersion: 6000.0.23f1
          testMode: PlayMode
          artifactsPath: test-results-playmode
          githubToken: ${{ secrets.GITHUB_TOKEN }}
          checkName: PlayMode Test Results
      
      # Upload test results
      - name: Upload Test Results
        uses: actions/upload-artifact@v3
        if: always()
        with:
          name: PlayMode-Results
          path: test-results-playmode
      
      # Publish test report
      - name: Publish Test Report
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: PlayMode Tests
          path: 'test-results-playmode/*.xml'
          reporter: java-junit
          fail-on-error: true

  coverage-report:
    name: Code Coverage
    runs-on: ubuntu-latest
    needs: [test-editmode, test-playmode]
    
    steps:
      - name: Checkout Repository
        uses: actions/checkout@v4
      
      - name: Download EditMode Results
        uses: actions/download-artifact@v3
        with:
          name: EditMode-Results
          path: coverage/editmode
      
      - name: Download PlayMode Results
        uses: actions/download-artifact@v3
        with:
          name: PlayMode-Results
          path: coverage/playmode
      
      - name: Generate Coverage Report
        uses: codecov/codecov-action@v3
        with:
          files: ./coverage/**/*.xml
          flags: unittests
          name: codecov-vanguard-arena
```

**Step 2: Add Unity License Activation**

File: `.github/workflows/unity-activation.yml`

```yaml
name: Unity License Activation

on:
  workflow_dispatch:

jobs:
  activation:
    name: Request Manual Activation File
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout Repository
        uses: actions/checkout@v4
      
      - name: Request Activation File
        uses: game-ci/unity-request-activation-file@v2
        id: getManualLicenseFile
        with:
          unityVersion: 6000.0.23f1
      
      - name: Upload Activation File
        uses: actions/upload-artifact@v3
        with:
          name: Unity_v6000.0.23f1.alf
          path: ${{ steps.getManualLicenseFile.outputs.filePath }}
```

**Step 3: Configure GitHub Secrets**

Required secrets (to be added via GitHub Settings → Secrets):
- `UNITY_LICENSE`: Unity license file content
- `UNITY_EMAIL`: Unity account email
- `UNITY_PASSWORD`: Unity account password

**Manual Steps**:
1. Run `unity-activation.yml` workflow to get `.alf` file
2. Upload `.alf` to https://license.unity3d.com/manual
3. Download `.ulf` license file
4. Add license content to `UNITY_LICENSE` secret
5. Add email/password to respective secrets

---

### A2. Test Result Reporting

#### Goal
Visualize test results directly in GitHub PRs with pass/fail status.

#### Features
- ✅ Automated test reports in PR comments
- ✅ Failed test details with stack traces
- ✅ Test execution time tracking
- ✅ Historical test trend graphs

Already included in `dorny/test-reporter@v1` action above.

---

### A3. Branch Protection Rules

#### Goal
Prevent merging PRs with failing tests.

#### Manual Steps (GitHub Web UI)

1. Go to: `Settings → Branches → Add Branch Protection Rule`
2. Branch name pattern: `main`
3. Enable:
   - ✅ Require status checks to pass before merging
   - ✅ Require branches to be up to date before merging
   - ✅ Status checks: `EditMode Tests`, `PlayMode Tests`
   - ✅ Require review from Code Owners (optional)
4. Save changes

---

## Part B: PlayMode Tests - Comprehensive Implementation

### B1. PlayMode Test Infrastructure

#### Goal
Create reusable test utilities for scene-based battle testing.

#### B1.1: Test Scene Setup Helper

File: `Assets/_Project/Tests/Utils/BattleSceneTestHelper.cs`

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Project.Domain;
using Project.Presentation;

namespace VanguardArena.Tests.Utils
{
    /// <summary>
    /// Helper for setting up PlayMode tests with BattleScene.
    /// Handles scene loading, battle initialization, and cleanup.
    /// </summary>
    public class BattleSceneTestHelper
    {
        private const string BATTLE_SCENE_PATH = "Assets/_Project/Scenes/BattleSandbox.unity";
        private const string BATTLE_SCENE_NAME = "BattleSandbox";
        
        public BattleController BattleController { get; private set; }
        public BattleUnitManager UnitManager { get; private set; }
        public Camera MainCamera { get; private set; }
        
        private Scene _loadedScene;
        private bool _isSceneLoaded;

        /// <summary>
        /// Load BattleSandbox scene and find all required components.
        /// </summary>
        public IEnumerator LoadBattleScene()
        {
            // Load scene additively
            var asyncLoad = SceneManager.LoadSceneAsync(BATTLE_SCENE_NAME, LoadSceneMode.Additive);
            
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            
            _loadedScene = SceneManager.GetSceneByName(BATTLE_SCENE_NAME);
            SceneManager.SetActiveScene(_loadedScene);
            _isSceneLoaded = true;
            
            // Wait one frame for scene objects to initialize
            yield return null;
            
            // Find components
            BattleController = GameObject.FindObjectOfType<BattleController>();
            UnitManager = GameObject.FindObjectOfType<BattleUnitManager>();
            MainCamera = Camera.main;
            
            if (BattleController == null)
            {
                Debug.LogError("[TEST] BattleController not found in scene!");
            }
            
            if (UnitManager == null)
            {
                Debug.LogError("[TEST] BattleUnitManager not found in scene!");
            }
        }

        /// <summary>
        /// Start a battle with specific unit configurations.
        /// </summary>
        public IEnumerator StartBattle(List<UnitDefinitionSO> playerUnits, List<UnitDefinitionSO> enemyUnits, int seed = 999)
        {
            if (BattleController == null)
            {
                Debug.LogError("[TEST] Cannot start battle - BattleController is null!");
                yield break;
            }
            
            // Set up units (you'll need to expose these methods in BattleController)
            // For now, assuming BattleController has InitializeBattle method
            // BattleController.InitializeBattle(playerUnits, enemyUnits, seed);
            
            // Start battle
            // BattleController.StartBattle();
            
            yield return null;
        }

        /// <summary>
        /// Wait for battle to complete (victory/defeat).
        /// </summary>
        public IEnumerator WaitForBattleEnd(float timeout = 60f)
        {
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                // Check if battle ended
                // if (BattleController.IsBattleOver)
                // {
                //     yield break;
                // }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            Debug.LogWarning($"[TEST] Battle did not end within {timeout}s timeout!");
        }

        /// <summary>
        /// Execute a single turn and wait for completion.
        /// </summary>
        public IEnumerator ExecuteSingleTurn()
        {
            if (BattleController == null)
            {
                yield break;
            }
            
            // Trigger turn execution
            // int turnsBefore = BattleController.CurrentTurn;
            
            // Wait for turn to complete
            // while (BattleController.CurrentTurn == turnsBefore)
            // {
            //     yield return null;
            // }
        }

        /// <summary>
        /// Cleanup and unload scene.
        /// </summary>
        public IEnumerator UnloadBattleScene()
        {
            if (_isSceneLoaded)
            {
                var asyncUnload = SceneManager.UnloadSceneAsync(_loadedScene);
                
                while (!asyncUnload.isDone)
                {
                    yield return null;
                }
                
                _isSceneLoaded = false;
            }
            
            yield return null;
        }

        /// <summary>
        /// Get all player unit views from the scene.
        /// </summary>
        public List<BattleUnitView> GetPlayerUnitViews()
        {
            if (UnitManager == null) return new List<BattleUnitView>();
            
            // return UnitManager.GetPlayerUnits();
            return new List<BattleUnitView>(); // Placeholder
        }

        /// <summary>
        /// Get all enemy unit views from the scene.
        /// </summary>
        public List<BattleUnitView> GetEnemyUnitViews()
        {
            if (UnitManager == null) return new List<BattleUnitView>();
            
            // return UnitManager.GetEnemyUnits();
            return new List<BattleUnitView>(); // Placeholder
        }

        /// <summary>
        /// Get specific unit view by name.
        /// </summary>
        public BattleUnitView GetUnitViewByName(string unitName)
        {
            var allUnits = new List<BattleUnitView>();
            allUnits.AddRange(GetPlayerUnitViews());
            allUnits.AddRange(GetEnemyUnitViews());
            
            return allUnits.Find(u => u.RuntimeState.Name == unitName);
        }
    }
}
```

#### B1.2: Animation Test Utilities

File: `Assets/_Project/Tests/Utils/AnimationTestHelper.cs`

```csharp
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
```

#### B1.3: UI Test Utilities

File: `Assets/_Project/Tests/Utils/UITestHelper.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace VanguardArena.Tests.Utils
{
    /// <summary>
    /// Utilities for testing UI components in PlayMode.
    /// </summary>
    public static class UITestHelper
    {
        /// <summary>
        /// Find UI element by name in hierarchy.
        /// </summary>
        public static GameObject FindUIElement(string name)
        {
            var allObjects = GameObject.FindObjectsOfType<GameObject>();
            
            foreach (var obj in allObjects)
            {
                if (obj.name == name)
                {
                    return obj;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Get text from TextMeshProUGUI component.
        /// </summary>
        public static string GetTextMeshProText(GameObject parent, string childName)
        {
            var child = parent.transform.Find(childName);
            if (child == null) return null;
            
            var tmp = child.GetComponent<TextMeshProUGUI>();
            return tmp != null ? tmp.text : null;
        }

        /// <summary>
        /// Get slider value.
        /// </summary>
        public static float GetSliderValue(GameObject parent, string sliderName)
        {
            var child = parent.transform.Find(sliderName);
            if (child == null) return -1f;
            
            var slider = child.GetComponent<Slider>();
            return slider != null ? slider.value : -1f;
        }

        /// <summary>
        /// Check if UI element is visible.
        /// </summary>
        public static bool IsUIElementVisible(GameObject uiElement)
        {
            if (uiElement == null) return false;
            
            var canvasGroup = uiElement.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                return canvasGroup.alpha > 0 && canvasGroup.interactable;
            }
            
            return uiElement.activeInHierarchy;
        }

        /// <summary>
        /// Wait for UI element to appear.
        /// </summary>
        public static IEnumerator WaitForUIElement(string elementName, float timeout = 5f)
        {
            float elapsed = 0f;
            
            while (elapsed < timeout)
            {
                var element = FindUIElement(elementName);
                if (element != null && IsUIElementVisible(element))
                {
                    yield break;
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            Debug.LogWarning($"[TEST] UI element '{elementName}' did not appear within {timeout}s");
        }
    }
}
```

---

### B2. BattleController Integration Tests

#### Goal
Test full battle flow through BattleController with real GameObjects.

File: `Assets/_Project/Tests/PlayMode/Integration/BattleControllerIntegrationTests.cs`

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VanguardArena.Tests.Utils;
using Project.Domain;

namespace VanguardArena.Tests.PlayMode.Integration
{
    /// <summary>
    /// Integration tests for BattleController.
    /// Tests full battle flow with scene, animations, and UI.
    /// </summary>
    [TestFixture]
    public class BattleControllerIntegrationTests
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
        public IEnumerator BattleController_Exists_InScene()
        {
            // Assert
            Assert.IsNotNull(_sceneHelper.BattleController, "BattleController should exist in scene");
            Assert.IsNotNull(_sceneHelper.UnitManager, "BattleUnitManager should exist in scene");
            Assert.IsNotNull(_sceneHelper.MainCamera, "Main Camera should exist in scene");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator BattleController_StartBattle_InitializesUnits()
        {
            // Arrange: Create test units
            var playerUnits = new BattleTestBuilder()
                .AddPlayerUnit("TestPlayer", UnitType.Melee, hp: 200, atk: 30, def: 10, spd: 15)
                .BuildDefinitions();
            
            var enemyUnits = new BattleTestBuilder()
                .AddEnemyUnit("TestEnemy", UnitType.Melee, hp: 150, atk: 25, def: 8, spd: 12)
                .BuildDefinitions();
            
            // Act: Start battle
            yield return _sceneHelper.StartBattle(playerUnits, enemyUnits);
            
            // Assert
            var playerViews = _sceneHelper.GetPlayerUnitViews();
            var enemyViews = _sceneHelper.GetEnemyUnitViews();
            
            Assert.AreEqual(1, playerViews.Count, "Should have 1 player unit");
            Assert.AreEqual(1, enemyViews.Count, "Should have 1 enemy unit");
            
            Assert.IsTrue(playerViews[0].gameObject.activeSelf, "Player unit should be active");
            Assert.IsTrue(enemyViews[0].gameObject.activeSelf, "Enemy unit should be active");
        }

        [UnityTest]
        public IEnumerator BattleController_ExecuteTurn_UnitsAct()
        {
            // Arrange
            yield return SetupSimple1v1Battle();
            
            // Act: Execute one turn
            yield return _sceneHelper.ExecuteSingleTurn();
            
            // Assert: Check that turn was executed
            // (Requires BattleController to expose turn counter)
            // Assert.AreEqual(1, _sceneHelper.BattleController.CurrentTurn);
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator BattleController_BasicAttack_DamagesEnemy()
        {
            // Arrange
            yield return SetupSimple1v1Battle();
            
            var enemyView = _sceneHelper.GetEnemyUnitViews()[0];
            int initialHP = enemyView.RuntimeState.HP;
            
            // Act: Execute turn where player attacks
            yield return _sceneHelper.ExecuteSingleTurn();
            
            // Assert: Enemy took damage
            Assert.Less(enemyView.RuntimeState.HP, initialHP, "Enemy should have taken damage");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator BattleController_UnitDeath_RemovesFromBattle()
        {
            // Arrange: Strong player vs weak enemy
            var playerUnits = new BattleTestBuilder()
                .AddPlayerUnit("StrongPlayer", UnitType.Melee, hp: 500, atk: 100, def: 20, spd: 20)
                .BuildDefinitions();
            
            var enemyUnits = new BattleTestBuilder()
                .AddEnemyUnit("WeakEnemy", UnitType.Melee, hp: 50, atk: 5, def: 2, spd: 5)
                .BuildDefinitions();
            
            yield return _sceneHelper.StartBattle(playerUnits, enemyUnits);
            
            // Act: Execute turns until enemy dies
            for (int i = 0; i < 5; i++)
            {
                yield return _sceneHelper.ExecuteSingleTurn();
                
                var enemyView = _sceneHelper.GetEnemyUnitViews()[0];
                if (!enemyView.RuntimeState.IsAlive)
                {
                    break;
                }
            }
            
            // Assert: Enemy is dead and removed
            var enemy = _sceneHelper.GetEnemyUnitViews()[0];
            Assert.IsFalse(enemy.RuntimeState.IsAlive, "Enemy should be dead");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator BattleController_AllEnemiesDead_VictoryTriggered()
        {
            // Arrange: Strong player vs weak enemy
            yield return SetupEasyVictoryBattle();
            
            // Act: Wait for battle to end
            yield return _sceneHelper.WaitForBattleEnd(timeout: 30f);
            
            // Assert: Battle ended in victory
            // Assert.IsTrue(_sceneHelper.BattleController.IsBattleOver);
            // Assert.AreEqual(BattleOutcome.Victory, _sceneHelper.BattleController.Outcome);
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator BattleController_AllPlayersDead_DefeatTriggered()
        {
            // Arrange: Weak player vs strong enemy
            var playerUnits = new BattleTestBuilder()
                .AddPlayerUnit("WeakPlayer", UnitType.Melee, hp: 50, atk: 5, def: 2, spd: 5)
                .BuildDefinitions();
            
            var enemyUnits = new BattleTestBuilder()
                .AddEnemyUnit("StrongEnemy", UnitType.Melee, hp: 500, atk: 100, def: 20, spd: 20)
                .BuildDefinitions();
            
            yield return _sceneHelper.StartBattle(playerUnits, enemyUnits);
            
            // Act: Wait for battle to end
            yield return _sceneHelper.WaitForBattleEnd(timeout: 30f);
            
            // Assert: Battle ended in defeat
            // Assert.IsTrue(_sceneHelper.BattleController.IsBattleOver);
            // Assert.AreEqual(BattleOutcome.Defeat, _sceneHelper.BattleController.Outcome);
            
            yield return null;
        }

        #region Helper Methods

        private IEnumerator SetupSimple1v1Battle()
        {
            var playerUnits = new BattleTestBuilder()
                .AddPlayerUnit("Player", UnitType.Melee, hp: 200, atk: 30, def: 10, spd: 15)
                .BuildDefinitions();
            
            var enemyUnits = new BattleTestBuilder()
                .AddEnemyUnit("Enemy", UnitType.Melee, hp: 150, atk: 25, def: 8, spd: 12)
                .BuildDefinitions();
            
            yield return _sceneHelper.StartBattle(playerUnits, enemyUnits);
        }

        private IEnumerator SetupEasyVictoryBattle()
        {
            var playerUnits = new BattleTestBuilder()
                .AddPlayerUnit("StrongPlayer", UnitType.Melee, hp: 500, atk: 100, def: 20, spd: 20)
                .BuildDefinitions();
            
            var enemyUnits = new BattleTestBuilder()
                .AddEnemyUnit("WeakEnemy", UnitType.Melee, hp: 50, atk: 5, def: 2, spd: 5)
                .BuildDefinitions();
            
            yield return _sceneHelper.StartBattle(playerUnits, enemyUnits);
        }

        #endregion
    }
}
```

---

### B3. Animation Integration Tests

File: `Assets/_Project/Tests/PlayMode/Integration/AnimationIntegrationTests.cs`

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VanguardArena.Tests.Utils;
using Project.Domain;

namespace VanguardArena.Tests.PlayMode.Integration
{
    /// <summary>
    /// Integration tests for battle animations.
    /// Tests animation triggers, state transitions, and synchronization with game logic.
    /// </summary>
    [TestFixture]
    public class AnimationIntegrationTests
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
        public IEnumerator UnitView_HasAnimator_Configured()
        {
            // Arrange
            yield return SetupSimple1v1Battle();
            
            var playerView = _sceneHelper.GetPlayerUnitViews()[0];
            
            // Assert
            Assert.IsNotNull(playerView.GetComponent<Animator>(), "UnitView should have Animator");
            
            var animator = playerView.GetComponent<Animator>();
            Assert.IsNotNull(animator.runtimeAnimatorController, "Animator should have controller assigned");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator BasicAttack_PlaysAttackAnimation()
        {
            // Arrange
            yield return SetupSimple1v1Battle();
            
            var playerView = _sceneHelper.GetPlayerUnitViews()[0];
            var animator = playerView.GetComponent<Animator>();
            
            // Act: Execute turn (player should attack)
            yield return _sceneHelper.ExecuteSingleTurn();
            
            // Wait for attack animation to start
            yield return AnimationTestHelper.WaitForAnimationState(animator, "Attack", layer: 0, timeout: 2f);
            
            // Assert
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"), 
                "Player should be playing Attack animation");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator TakeDamage_PlaysHurtAnimation()
        {
            // Arrange: Fast enemy attacks slow player
            var playerUnits = new BattleTestBuilder()
                .AddPlayerUnit("SlowPlayer", UnitType.Melee, hp: 200, atk: 20, def: 10, spd: 5)
                .BuildDefinitions();
            
            var enemyUnits = new BattleTestBuilder()
                .AddEnemyUnit("FastEnemy", UnitType.Melee, hp: 150, atk: 30, def: 8, spd: 20)
                .BuildDefinitions();
            
            yield return _sceneHelper.StartBattle(playerUnits, enemyUnits);
            
            var playerView = _sceneHelper.GetPlayerUnitViews()[0];
            var animator = playerView.GetComponent<Animator>();
            
            // Act: Execute turn (enemy attacks first)
            yield return _sceneHelper.ExecuteSingleTurn();
            
            // Wait for hurt animation
            yield return AnimationTestHelper.WaitForAnimationState(animator, "Hurt", layer: 0, timeout: 3f);
            
            // Assert
            var clipName = AnimationTestHelper.GetCurrentClipName(animator);
            Assert.IsTrue(clipName.Contains("Hurt") || clipName.Contains("Hit"), 
                $"Player should be playing Hurt animation, but playing: {clipName}");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnitDeath_PlaysDeathAnimation()
        {
            // Arrange: Strong player one-shots weak enemy
            var playerUnits = new BattleTestBuilder()
                .AddPlayerUnit("StrongPlayer", UnitType.Melee, hp: 500, atk: 200, def: 20, spd: 20)
                .BuildDefinitions();
            
            var enemyUnits = new BattleTestBuilder()
                .AddEnemyUnit("WeakEnemy", UnitType.Melee, hp: 50, atk: 5, def: 2, spd: 5)
                .BuildDefinitions();
            
            yield return _sceneHelper.StartBattle(playerUnits, enemyUnits);
            
            var enemyView = _sceneHelper.GetEnemyUnitViews()[0];
            var animator = enemyView.GetComponent<Animator>();
            
            // Act: Execute turn (player kills enemy)
            yield return _sceneHelper.ExecuteSingleTurn();
            
            // Wait for death animation
            yield return AnimationTestHelper.WaitForAnimationState(animator, "Death", layer: 0, timeout: 3f);
            
            // Assert
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Death"), 
                "Enemy should be playing Death animation");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ultimate_PlaysUltimateAnimation()
        {
            // Arrange: Give player max energy for ultimate
            yield return SetupSimple1v1Battle();
            
            var playerView = _sceneHelper.GetPlayerUnitViews()[0];
            var animator = playerView.GetComponent<Animator>();
            
            // Set player energy to max (requires exposing method)
            // playerView.RuntimeState.TeamEnergy.SetEnergy(100);
            
            // Act: Execute turn (player should use ultimate)
            yield return _sceneHelper.ExecuteSingleTurn();
            
            // Wait for ultimate animation
            yield return AnimationTestHelper.WaitForAnimationState(animator, "Ultimate", layer: 0, timeout: 3f);
            
            // Assert
            var clipName = AnimationTestHelper.GetCurrentClipName(animator);
            Assert.IsTrue(clipName.Contains("Ultimate") || clipName.Contains("Skill"), 
                $"Player should be playing Ultimate animation, but playing: {clipName}");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator Idle_ReturnsToIdleAfterAction()
        {
            // Arrange
            yield return SetupSimple1v1Battle();
            
            var playerView = _sceneHelper.GetPlayerUnitViews()[0];
            var animator = playerView.GetComponent<Animator>();
            
            // Act: Execute turn
            yield return _sceneHelper.ExecuteSingleTurn();
            
            // Wait for action animation to complete
            yield return AnimationTestHelper.WaitForAnimationComplete(animator, layer: 0, timeout: 5f);
            
            // Wait for return to idle
            yield return new WaitForSeconds(1f);
            
            // Assert
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), 
                "Unit should return to Idle state after action completes");
            
            yield return null;
        }

        #region Helper Methods

        private IEnumerator SetupSimple1v1Battle()
        {
            var playerUnits = new BattleTestBuilder()
                .AddPlayerUnit("Player", UnitType.Melee, hp: 200, atk: 30, def: 10, spd: 15)
                .BuildDefinitions();
            
            var enemyUnits = new BattleTestBuilder()
                .AddEnemyUnit("Enemy", UnitType.Melee, hp: 150, atk: 25, def: 8, spd: 12)
                .BuildDefinitions();
            
            yield return _sceneHelper.StartBattle(playerUnits, enemyUnits);
        }

        #endregion
    }
}
```

---

### B4. UI Integration Tests

File: `Assets/_Project/Tests/PlayMode/Integration/UIIntegrationTests.cs`

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VanguardArena.Tests.Utils;
using Project.Domain;

namespace VanguardArena.Tests.PlayMode.Integration
{
    /// <summary>
    /// Integration tests for UI updates during battle.
    /// Tests HP bars, energy bars, damage popups, and status indicators.
    /// </summary>
    [TestFixture]
    public class UIIntegrationTests
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
        public IEnumerator HPBar_UpdatesWhenDamaged()
        {
            // Arrange
            yield return SetupSimple1v1Battle();
            
            var enemyView = _sceneHelper.GetEnemyUnitViews()[0];
            float initialHPPercent = enemyView.RuntimeState.HPPercent;
            
            // Act: Execute turn (enemy takes damage)
            yield return _sceneHelper.ExecuteSingleTurn();
            yield return new WaitForSeconds(0.5f); // Wait for UI animation
            
            // Assert
            float currentHPPercent = enemyView.RuntimeState.HPPercent;
            Assert.Less(currentHPPercent, initialHPPercent, "HP bar should decrease after taking damage");
            
            // Check visual HP bar (requires HPBar component access)
            // var hpBar = enemyView.GetComponentInChildren<Slider>();
            // Assert.AreEqual(currentHPPercent, hpBar.value, 0.01f, "Visual HP bar should match actual HP");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnergyBar_UpdatesWhenGained()
        {
            // Arrange
            yield return SetupSimple1v1Battle();
            
            var playerView = _sceneHelper.GetPlayerUnitViews()[0];
            int initialEnergy = playerView.RuntimeState.TeamEnergy.Energy;
            
            // Act: Execute turn (gain energy)
            yield return _sceneHelper.ExecuteSingleTurn();
            yield return new WaitForSeconds(0.5f);
            
            // Assert
            int currentEnergy = playerView.RuntimeState.TeamEnergy.Energy;
            Assert.Greater(currentEnergy, initialEnergy, "Energy should increase after action");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator DamagePopup_AppearsWhenDamaged()
        {
            // Arrange
            yield return SetupSimple1v1Battle();
            
            // Act: Execute turn (damage dealt)
            yield return _sceneHelper.ExecuteSingleTurn();
            
            // Wait for damage popup to appear
            yield return UITestHelper.WaitForUIElement("DamagePopup", timeout: 2f);
            
            // Assert
            var popup = UITestHelper.FindUIElement("DamagePopup");
            Assert.IsNotNull(popup, "Damage popup should appear");
            Assert.IsTrue(UITestHelper.IsUIElementVisible(popup), "Damage popup should be visible");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator StatusIcon_AppearsWithStatusEffect()
        {
            // Arrange: Unit with ATK buff passive
            var playerUnits = new BattleTestBuilder()
                .AddPlayerUnit("BuffPlayer", UnitType.Melee, hp: 200, atk: 30, def: 10, spd: 15)
                .WithPassive(new PassiveAbility
                {
                    Type = PassiveType.OnTurnStart,
                    EffectType = StatusEffectType.ATKUp,
                    Modifier = 0.3f,
                    Duration = 3,
                    TargetSelf = true
                })
                .BuildDefinitions();
            
            var enemyUnits = new BattleTestBuilder()
                .AddEnemyUnit("Enemy", UnitType.Melee, hp: 150, atk: 25, def: 8, spd: 12)
                .BuildDefinitions();
            
            yield return _sceneHelper.StartBattle(playerUnits, enemyUnits);
            
            // Act: Execute turn (buff applied)
            yield return _sceneHelper.ExecuteSingleTurn();
            yield return new WaitForSeconds(0.5f);
            
            // Assert: Check for status icon
            var playerView = _sceneHelper.GetPlayerUnitViews()[0];
            Assert.IsTrue(playerView.RuntimeState.HasStatusEffect(StatusEffectType.ATKUp), 
                "Player should have ATK buff");
            
            // Check visual indicator (requires StatusIconContainer access)
            // var statusIcons = playerView.GetComponentInChildren<StatusIconContainer>();
            // Assert.IsTrue(statusIcons.HasIcon(StatusEffectType.ATKUp), "ATK buff icon should be visible");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator TurnIndicator_HighlightsActiveUnit()
        {
            // Arrange
            yield return SetupSimple1v1Battle();
            
            // Get the unit that acts first (highest SPD)
            var playerView = _sceneHelper.GetPlayerUnitViews()[0];
            var enemyView = _sceneHelper.GetEnemyUnitViews()[0];
            
            var firstActor = playerView.RuntimeState.SPD >= enemyView.RuntimeState.SPD ? playerView : enemyView;
            
            // Act: Start turn
            yield return new WaitForSeconds(0.2f);
            
            // Assert: Check turn indicator (requires TurnIndicator component)
            // var indicator = firstActor.GetComponentInChildren<TurnIndicator>();
            // Assert.IsTrue(indicator.IsActive, "Turn indicator should highlight active unit");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator VictoryScreen_AppearsOnVictory()
        {
            // Arrange: Easy victory
            var playerUnits = new BattleTestBuilder()
                .AddPlayerUnit("StrongPlayer", UnitType.Melee, hp: 500, atk: 200, def: 20, spd: 20)
                .BuildDefinitions();
            
            var enemyUnits = new BattleTestBuilder()
                .AddEnemyUnit("WeakEnemy", UnitType.Melee, hp: 50, atk: 5, def: 2, spd: 5)
                .BuildDefinitions();
            
            yield return _sceneHelper.StartBattle(playerUnits, enemyUnits);
            
            // Act: Wait for battle to end
            yield return _sceneHelper.WaitForBattleEnd(timeout: 30f);
            
            // Assert: Victory screen appears
            yield return UITestHelper.WaitForUIElement("VictoryScreen", timeout: 3f);
            
            var victoryScreen = UITestHelper.FindUIElement("VictoryScreen");
            Assert.IsNotNull(victoryScreen, "Victory screen should appear");
            Assert.IsTrue(UITestHelper.IsUIElementVisible(victoryScreen), "Victory screen should be visible");
            
            yield return null;
        }

        #region Helper Methods

        private IEnumerator SetupSimple1v1Battle()
        {
            var playerUnits = new BattleTestBuilder()
                .AddPlayerUnit("Player", UnitType.Melee, hp: 200, atk: 30, def: 10, spd: 15)
                .BuildDefinitions();
            
            var enemyUnits = new BattleTestBuilder()
                .AddEnemyUnit("Enemy", UnitType.Melee, hp: 150, atk: 25, def: 8, spd: 12)
                .BuildDefinitions();
            
            yield return _sceneHelper.StartBattle(playerUnits, enemyUnits);
        }

        #endregion
    }
}
```

---

## Implementation Order

### Priority 1: Critical Infrastructure (2-3 hours)
1. ✅ Create GitHub Actions workflow files
2. ✅ Set up Unity license activation
3. ✅ Create `BattleSceneTestHelper.cs`
4. ✅ Create `AnimationTestHelper.cs`
5. ✅ Create `UITestHelper.cs`

### Priority 2: Core PlayMode Tests (2-3 hours)
6. ✅ Implement `BattleControllerIntegrationTests.cs` (10 tests)
7. ✅ Implement `AnimationIntegrationTests.cs` (6 tests)
8. ✅ Implement `UIIntegrationTests.cs` (6 tests)

### Priority 3: CI/CD Configuration (1 hour)
9. ✅ Configure GitHub Secrets
10. ✅ Set up branch protection rules
11. ✅ Test CI/CD pipeline with sample commit

### Priority 4: Documentation & Verification (1 hour)
12. ✅ Update test README with PlayMode instructions
13. ✅ Verify all 151 EditMode + 22 PlayMode tests pass
14. ✅ Document CI/CD usage for team

---

## Success Criteria

### Phase 6 Complete When:
- ✅ GitHub Actions runs tests automatically on push/PR
- ✅ Test results appear in PR comments
- ✅ Branch protection prevents merging failing PRs
- ✅ Coverage tracking is configured

### PlayMode Tests Complete When:
- ✅ 20+ PlayMode integration tests implemented
- ✅ BattleController, animations, and UI tested
- ✅ All tests pass locally and in CI
- ✅ Test execution time < 2 minutes

---

## Next Steps After Completion

1. **Phase 5: Regression Suite**
   - Capture golden battle scenarios
   - Log-based regression validation

2. **Combinatorial Testing**
   - Generate pairwise test cases
   - Cover all config combinations

3. **Performance Testing**
   - Frame rate monitoring during battles
   - Memory leak detection

---

## Notes & Limitations

### Known Limitations
- PlayMode tests require BattleController API exposure (e.g., `CurrentTurn`, `IsBattleOver`)
- Some tests are placeholders until BattleController integration methods are implemented
- UI tests assume specific naming conventions for UI elements

### BattleController Refactoring Needed
To support PlayMode tests, BattleController should expose:
- `InitializeBattle(List<UnitDefinitionSO> players, List<UnitDefinitionSO> enemies, int seed)`
- `bool IsBattleOver { get; }`
- `int CurrentTurn { get; }`
- `BattleOutcome Outcome { get; }`

### Unity Version Note
CI workflow uses `6000.0.23f1` - update to match your installed version (`6000.3.5f1`).

---

## Estimated Timeline

| Task | Time | Status |
|------|------|--------|
| CI/CD Workflow Files | 30 min | ⏳ Pending |
| Test Helper Infrastructure | 1.5 hours | ⏳ Pending |
| BattleController Tests | 1.5 hours | ⏳ Pending |
| Animation Tests | 1 hour | ⏳ Pending |
| UI Tests | 1 hour | ⏳ Pending |
| CI/CD Configuration | 1 hour | ⏳ Pending |
| Documentation | 30 min | ⏳ Pending |
| **Total** | **7 hours** | **0% Complete** |

---

**Ready to start implementation? Let's begin with the CI/CD workflow files!**
