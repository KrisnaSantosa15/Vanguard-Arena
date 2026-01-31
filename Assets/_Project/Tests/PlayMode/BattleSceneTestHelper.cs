using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Project.Domain;
using Project.Presentation;

namespace VanguardArena.Tests.PlayMode
{
    /// <summary>
    /// Helper for setting up PlayMode tests with BattleScene.
    /// Handles scene loading, battle initialization, and cleanup.
    /// 
    /// NOTE: This is a simplified version. Full implementation requires:
    /// - BattleController API exposure (InitializeBattle, IsBattleOver, etc.)
    /// - UnitView/BattleUnitView component structure
    /// - Scene component references
    /// </summary>
    public class BattleSceneTestHelper
    {
        private const string BATTLE_SCENE_NAME = "BattleSandbox";
        
        public BattleController BattleController { get; private set; }
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
            BattleController = GameObject.FindFirstObjectByType<BattleController>();
            MainCamera = Camera.main;
            
            if (BattleController == null)
            {
                Debug.LogError("[TEST] BattleController not found in scene!");
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
            
            Debug.Log($"[TEST] Starting battle with {playerUnits.Count} players, {enemyUnits.Count} enemies, seed={seed}");
            BattleController.InitializeBattle(playerUnits, enemyUnits, seed);
            
            // Start the battle coroutine
            BattleController.StartTestBattle();
            
            // Wait a frame for initialization
            yield return null;
            
            Debug.Log($"[TEST] Battle started. IsBattleOver={BattleController.IsBattleOver}");
        }

        /// <summary>
        /// Wait for battle to complete (victory/defeat).
        /// </summary>
        public IEnumerator WaitForBattleEnd(float timeout = 60f)
        {
            if (BattleController == null)
            {
                Debug.LogWarning("[TEST] BattleController is null!");
                yield break;
            }
            
            float elapsed = 0f;
            Debug.Log($"[TEST] Waiting for battle to end (timeout={timeout}s)...");
            
            while (elapsed < timeout)
            {
                if (BattleController.IsBattleOver)
                {
                    Debug.Log($"[TEST] Battle ended after {elapsed:F1}s. Outcome: {BattleController.Outcome}");
                    yield break;
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            Debug.LogWarning($"[TEST] Battle did not end within {timeout}s timeout! Current turn: {BattleController.CurrentTurn}");
        }

        /// <summary>
        /// Execute a single turn and wait for completion.
        /// </summary>
        public IEnumerator ExecuteSingleTurn()
        {
            if (BattleController == null)
            {
                Debug.LogWarning("[TEST] BattleController is null!");
                yield break;
            }
            
            int startTurn = BattleController.CurrentTurn;
            Debug.Log($"[TEST] Waiting for turn {startTurn} to complete...");
            
            // Wait for turn to increment
            float elapsed = 0f;
            float timeout = 10f;
            
            while (elapsed < timeout)
            {
                // Check if turn incremented or battle ended
                if (BattleController.CurrentTurn > startTurn || BattleController.IsBattleOver)
                {
                    Debug.Log($"[TEST] Turn completed. Now on turn {BattleController.CurrentTurn}");
                    yield break;
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            Debug.LogWarning($"[TEST] Turn did not complete within {timeout}s!");
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
        public List<UnitView> GetPlayerUnitViews()
        {
            var views = new List<UnitView>();
            
            if (BattleController == null)
            {
                Debug.LogWarning("[TEST] BattleController is null, cannot get player units");
                return views;
            }
            
            var unitManager = BattleController.GetComponent<BattleUnitManager>();
            if (unitManager == null)
            {
                Debug.LogWarning("[TEST] BattleUnitManager not found");
                return views;
            }
            
            // Get all player units (IsEnemy = false)
            foreach (var unit in unitManager.AllUnits)
            {
                if (unit != null && !unit.IsEnemy)
                {
                    if (unitManager.TryGetView(unit, out var view))
                    {
                        views.Add(view);
                    }
                }
            }
            
            return views;
        }

        /// <summary>
        /// Get all enemy unit views from the scene.
        /// </summary>
        public List<UnitView> GetEnemyUnitViews()
        {
            var views = new List<UnitView>();
            
            if (BattleController == null)
            {
                Debug.LogWarning("[TEST] BattleController is null, cannot get enemy units");
                return views;
            }
            
            var unitManager = BattleController.GetComponent<BattleUnitManager>();
            if (unitManager == null)
            {
                Debug.LogWarning("[TEST] BattleUnitManager not found");
                return views;
            }
            
            // Get all enemy units (IsEnemy = true)
            foreach (var unit in unitManager.AllUnits)
            {
                if (unit != null && unit.IsEnemy)
                {
                    if (unitManager.TryGetView(unit, out var view))
                    {
                        views.Add(view);
                    }
                }
            }
            
            return views;
        }

        /// <summary>
        /// Get specific unit view by name.
        /// TODO: Requires UnitView structure.
        /// </summary>
        public UnitView GetUnitViewByName(string unitName)
        {
            // TODO: Implement
            return null;
        }
    }
}
