using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project.Domain;
using Project.Presentation.UI;

namespace Project.Presentation
{
    [RequireComponent(typeof(BattleUnitManager))]
    [RequireComponent(typeof(BattleInputController))]
    public sealed class BattleController : MonoBehaviour
    {
        [Header("Manual Targeting")]
        // LayerMask and Camera moved to BattleInputController, but keeping refs here if needed for legacy or cleanup

        [Header("Slots (Front Row MVP)")]
        [Tooltip("Player Slots: 0-2 = Front Row (L/C/R), 3-5 = Back Row (L/C/R)")]
        [SerializeField] private Transform[] playerSlots = Array.Empty<Transform>();
        [Tooltip("Enemy Slots: 0-2 = Front Row (L/C/R), 3-5 = Back Row (L/C/R)")]
        [SerializeField] private Transform[] enemySlots = Array.Empty<Transform>();

        [Header("Lineups (MVP)")]
        [SerializeField] private UnitDefinitionSO[] playerLineup = Array.Empty<UnitDefinitionSO>();
        [SerializeField] private UnitDefinitionSO[] enemyLineup = Array.Empty<UnitDefinitionSO>();

        [Header("Loop")]
        [SerializeField] private float secondsBetweenTurns = 0.25f;
        [SerializeField] private int maxTurns = 10;

        [Header("Damage Popups")]
        [SerializeField] private DamagePopup damagePopupPrefab;
        [SerializeField] private Vector3 damagePopupOffset = new Vector3(0, 2, 0);

        [Header("Animation Timings")]
        [SerializeField] private float attackAnimationDuration = 0.5f;
        [SerializeField] private float ultimateAnimationDuration = 1.5f;
        [SerializeField] private float hitAnimationDuration = 0.3f;
        [SerializeField] private float deathAnimationDuration = 1.0f;

        [Header("Melee Movement")]
        [SerializeField] private float meleeMovementSpeed = 10f;
        [SerializeField] private float meleeDistance = 1.5f;

        [Header("Energy System")]
        [SerializeField] private int maxEnergy = 10;
        [SerializeField] private int startEnergy = 2;
        [SerializeField, Range(1f, 50f)] private float energyBarFillPerHitPct = 20f;

        [Header("Determinism")]
        [SerializeField] private int battleSeed = 12345;

        [Header("Mode")]
        [SerializeField] private bool autoMode = false;

        [Header("Performance")]
        [SerializeField] private int damagePopupPoolSize = 15;

        // --- Modules ---
        private BattleUnitManager _unitManager;
        private BattleInputController _inputController;
        private TurnManager _turnManager;

        // --- State ---
        private TeamEnergyState _playerEnergy;
        private TeamEnergyState _enemyEnergy;
        private System.Random _rng;
        private BattleHudController _hud;

        // --- Object Pooling ---
        private readonly Queue<DamagePopup> _damagePopupPool = new();

        // --- Manual Input State ---
        private BattlePhase _battlePhase = BattlePhase.AutoResolve;
        private UnitRuntimeState _manualActor;
        private PlayerActionType _pendingAction = PlayerActionType.None;
        private readonly List<UnitRuntimeState> _validTargets = new();
        private UnitRuntimeState _selectedTarget;
        private bool _actionChanged;

        // Auto-per-unit preferences
        private readonly Dictionary<string, ActionPreference> _actionPreferences = new();

        // --- Public Accessors (Preserved for View binding) ---
        public int PlayerEnergyValue => _playerEnergy != null ? _playerEnergy.Energy : 0;
        public int EnemyEnergyValue => _enemyEnergy != null ? _enemyEnergy.Energy : 0;
        public bool AutoModeValue => autoMode;
        
        // --- Test/Debug API (for PlayMode tests) ---
        public bool IsBattleOver { get; private set; } = false;
        public int CurrentTurn => _turnManager != null ? _turnManager.CurrentTurn : 0;
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.InProgress;

        private void Awake()
        {
            // Initialize FileLogger for battle debugging
            FileLogger.Initialize("battle_debug.log");
            FileLogger.LogSeparator("BATTLE CONTROLLER AWAKE");
            
            _unitManager = GetComponent<BattleUnitManager>();
            if (_unitManager == null) _unitManager = gameObject.AddComponent<BattleUnitManager>();

            _inputController = GetComponent<BattleInputController>();
            if (_inputController == null) _inputController = gameObject.AddComponent<BattleInputController>();

            _turnManager = new TurnManager();
            
            FileLogger.Log("BattleController initialized", "INIT");
        }
        
        private void OnDestroy()
        {
            FileLogger.LogSeparator("BATTLE CONTROLLER DESTROYED");
            FileLogger.Shutdown();
        }

        private void Start()
        {
            FileLogger.LogSeparator("BATTLE START");
            _rng = new System.Random(battleSeed);

            _playerEnergy = new TeamEnergyState(maxEnergy, startEnergy);
            _enemyEnergy = new TeamEnergyState(maxEnergy, startEnergy);

            // Initialize DamagePopup Pool
            InitializeDamagePopupPool();

            // 1. Spawn Units
            FileLogger.Log($"Player lineup count: {playerLineup.Length}, Enemy lineup count: {enemyLineup.Length}", "INIT");
            _unitManager.SpawnTeam(playerLineup, playerSlots, isEnemy: false);
            _unitManager.SpawnTeam(enemyLineup, enemySlots, isEnemy: true);
            
            FileLogger.Log($"All units spawned. Total units: {_unitManager.AllUnits.Count()}", "INIT");
            FileLogger.Log($"Player units: {string.Join(", ", _unitManager.AllUnits.Where(u => !u.IsEnemy).Select(u => u.DisplayName))}", "INIT");
            FileLogger.Log($"Enemy units: {string.Join(", ", _unitManager.AllUnits.Where(u => u.IsEnemy).Select(u => u.DisplayName))}", "INIT");

            // 1b. Trigger OnBattleStart passives for all units
            foreach (var unit in _unitManager.AllUnits)
            {
                if (unit != null && unit.IsAlive && unit.Passive != null)
                {
                    TriggerPassive(unit, PassiveType.OnBattleStart);
                }
            }

            // 2. Setup Input
            if (_inputController != null)
            {
                _inputController.OnUnitClicked += TrySelectTarget;
                _inputController.OnCancel += CancelTargetSelection;
            }

            // 3. Bind UI
            BindHud();
            if (autoMode && _hud != null)
                _hud.ShowAutoSquadPanel();

            // 4. Start Loop
            FileLogger.Log(" Start() called. Spawning done. Starting coroutine...");
            StartCoroutine(BattleCoroutine());
        }

        private void BindHud()
        {
            _hud = FindFirstObjectByType<BattleHudController>();
            if (_hud == null) return;

            _hud.OnBasicClicked += () => OnPlayerChoseAction(PlayerActionType.Basic);
            _hud.OnUltClicked += () => OnPlayerChoseAction(PlayerActionType.Ultimate);
            _hud.OnSquadAvatarClicked += OnSquadAvatarClicked;

            _hud.Bind(
                maxEnergy,
                getRound: () => _turnManager.CurrentTurn,
                getRoundMax: () => maxTurns,
                getAuto: () => autoMode,
                setAuto: v =>
                {
                    autoMode = v;
                    if (autoMode)
                    {
                        // Switch to Auto
                        _battlePhase = BattlePhase.AutoResolve;
                        _manualActor = null;
                        _pendingAction = PlayerActionType.None;
                        _validTargets.Clear();
                        _selectedTarget = null;
                        _unitManager.ClearAllHighlights(null);

                        _inputController.EnableInput(false);

                        if (_hud != null)
                        {
                            _hud.HideManualActionPanel();
                            _hud.ShowAutoSquadPanel();
                        }
                    }
                    else
                    {
                        // Switch to Manual
                        if (_hud != null) _hud.HideAutoSquadPanel();
                    }
                },
                getPlayerEnergy: () => _playerEnergy,
                getEnemyEnergy: () => _enemyEnergy,
                getBoss: () => _unitManager.Boss,
                getRollingTimeline: () => _turnManager.Timeline,
                getCurrentActor: () => _turnManager.CurrentActor,
                getUnitPreference: GetActionPreference,
                getPlayerSquad: () => _unitManager.PlayerSquad
            );
        }

        private IEnumerator BattleCoroutine()
        {
            while (_turnManager.CurrentTurn <= maxTurns)
            {
                // Win/Loss Check
                FileLogger.Log($"Checking victory conditions (Turn Start) - Player team dead: {_unitManager.IsTeamDead(false)}, Enemy team dead: {_unitManager.IsTeamDead(true)}", "DEBUG");
                if (_unitManager.IsTeamDead(false))
                {
                    LogBattleEnd(false, _turnManager.CurrentTurn);
                    yield break;
                }
                if (_unitManager.IsTeamDead(true))
                {
                    LogBattleEnd(true, _turnManager.CurrentTurn);
                    yield break;
                }

                // New Turn Setup
                _turnManager.RebuildTimeline(_unitManager.AllUnits);
                FileLogger.LogSeparator($"TURN {_turnManager.CurrentTurn}");
                FileLogger.Log($"Timeline: {string.Join(" -> ", _turnManager.Timeline.Select(u => u.DisplayName))}", "TURN");

                // Trigger Passives
                foreach (var unit in _unitManager.AllUnits.Where(u => u != null && u.IsAlive))
                {
                    TriggerPassive(unit, PassiveType.OnTurnStart);
                }

                int actionsThisTurn = _turnManager.Timeline.Count;
                int safety = 0;

                // Process Action Queue
                for (int step = 0; step < actionsThisTurn; step++)
                {
                    if (_turnManager.Timeline.Count == 0) break;

                    safety++;
                    if (safety > 50) { FileLogger.LogWarning("Safety break"); break; }

                    var actor = _turnManager.CurrentActor;
                    if (actor == null || !actor.IsAlive)
                    {
                        _turnManager.Timeline.RemoveAt(0);
                        step--;
                        continue;
                    }

                    // --- Actor Turn Start ---
                    LogTurnStart(actor);

                    // Burn Processing
                    int burnDmg = actor.ProcessBurnDamage();
                    if (burnDmg > 0)
                    {
                        FileLogger.Log($" ❤️‍🔥 {actor.DisplayName} takes {burnDmg} burn damage");
                        _unitManager.RefreshView(actor);
                    }

                    actor.OnNewTurnTickCooldown();

                    // Energy Gain (Enemy)
                    if (actor.IsEnemy)
                        actor.CurrentEnergy = Mathf.Min(actor.UltimateEnergyCost, actor.CurrentEnergy + 1);

                    // Stun Check
                    if (actor.IsStunned)
                    {
                        FileLogger.Log($" {actor.DisplayName} is stunned!");
                        _turnManager.RollAfterAction(actor);
                        yield return new WaitForSeconds(secondsBetweenTurns * 0.5f);
                        continue;
                    }

                    // --- Action Decision ---
                    if (!autoMode && !actor.IsEnemy)
                    {
                        // Manual Player Turn
                        yield return ManualResolve(actor);

                        _turnManager.RemoveDeadUnits();
                        
                        // Check for battle end after manual action
                        FileLogger.Log($"Checking victory conditions (Manual) - Player team dead: {_unitManager.IsTeamDead(false)}, Enemy team dead: {_unitManager.IsTeamDead(true)}", "DEBUG");
                        if (_unitManager.IsTeamDead(false))
                        {
                            LogBattleEnd(false, _turnManager.CurrentTurn);
                            yield break;
                        }
                        if (_unitManager.IsTeamDead(true))
                        {
                            LogBattleEnd(true, _turnManager.CurrentTurn);
                            yield break;
                        }

                        _turnManager.RollAfterAction(actor);
                        yield return new WaitForSeconds(secondsBetweenTurns);
                        continue;
                    }

                    // Auto / Enemy Turn
                    var teamEnergy = actor.IsEnemy ? _enemyEnergy : _playerEnergy;
                    bool canUlt = actor.IsEnemy
                        ? actor.CanUseUltimate
                        : (actor.CanUseUltimate && teamEnergy.Energy >= actor.UltimateEnergyCost);

                    bool useUlt = false;

                    if (!actor.IsEnemy)
                    {
                        // Player Auto Logic
                        var pref = GetActionPreference(actor.UnitId);
                        useUlt = pref switch
                        {
                            ActionPreference.Basic => false,
                            ActionPreference.Ultimate => canUlt,
                            ActionPreference.SmartAI => canUlt && BattleAI.ShouldUseUltimate(actor, teamEnergy, _unitManager.AllUnits),
                            _ => canUlt
                        };
                    }
                    else
                    {
                        // Enemy Logic
                        useUlt = canUlt;
                    }

                    // Execute
                    if (useUlt) yield return StartCoroutine(ExecuteUltimate(actor, teamEnergy));
                    else yield return StartCoroutine(ExecuteBasic(actor, teamEnergy));

                    _turnManager.RemoveDeadUnits();
                    
                    // Check for battle end after each action
                    FileLogger.Log($"Checking victory conditions - Player team dead: {_unitManager.IsTeamDead(false)}, Enemy team dead: {_unitManager.IsTeamDead(true)}", "DEBUG");
                    if (_unitManager.IsTeamDead(false))
                    {
                        LogBattleEnd(false, _turnManager.CurrentTurn);
                        yield break;
                    }
                    if (_unitManager.IsTeamDead(true))
                    {
                        LogBattleEnd(true, _turnManager.CurrentTurn);
                        yield break;
                    }

                    _turnManager.RollAfterAction(actor);
                    yield return new WaitForSeconds(secondsBetweenTurns);
                }

                _turnManager.IncrementTurn();
                yield return null;
            }
        }

        private void LogTurnStart(UnitRuntimeState actor)
        {
            var teamEnergy = actor.IsEnemy ? _enemyEnergy : _playerEnergy;
            FileLogger.LogSeparator($"{actor.DisplayName}'s Turn (Turn {_turnManager.CurrentTurn})");
            FileLogger.Log($"Unit: {actor.DisplayName}, IsEnemy: {actor.IsEnemy}, HP: {actor.CurrentHP}/{actor.MaxHP}, Energy: {(actor.IsEnemy ? actor.CurrentEnergy : teamEnergy.Energy)}", "TURN");
            FileLogger.Log($"ATK: {actor.ATK}, DEF: {actor.DEF}, SPD: {actor.SPD}, Type: {actor.Type}", "TURN");
        }

        private void LogBattleEnd(bool playerWon, int currentTurn)
        {
            FileLogger.LogSeparator("BATTLE END");
            
            // Set battle outcome flags
            IsBattleOver = true;
            Outcome = playerWon ? BattleOutcome.Victory : BattleOutcome.Defeat;
            
            // Victory/Defeat announcement
            if (playerWon)
            {
                FileLogger.Log("🏆 VICTORY! Player team wins!", "BATTLE-END");
            }
            else
            {
                FileLogger.Log("☠️ DEFEAT! Enemy team wins!", "BATTLE-END");
            }
            
            // Battle statistics
            FileLogger.Log($"Total turns: {currentTurn}", "BATTLE-END");
            
            // Player team status
            var playerUnits = _unitManager.AllUnits.Where(u => !u.IsEnemy).ToList();
            var playerAlive = playerUnits.Where(u => u.IsAlive).ToList();
            var playerDead = playerUnits.Where(u => !u.IsAlive).ToList();
            
            FileLogger.LogSeparator("PLAYER TEAM STATUS");
            FileLogger.Log($"Survivors: {playerAlive.Count}/{playerUnits.Count}", "BATTLE-END");
            
            if (playerAlive.Any())
            {
                FileLogger.Log("Surviving units:", "BATTLE-END");
                foreach (var unit in playerAlive)
                {
                    FileLogger.Log($"  • {unit.DisplayName}: {unit.CurrentHP}/{unit.MaxHP} HP ({(unit.CurrentHP * 100 / unit.MaxHP)}%)", "BATTLE-END");
                }
            }
            
            if (playerDead.Any())
            {
                FileLogger.Log($"Casualties: {playerDead.Count}", "BATTLE-END");
                foreach (var unit in playerDead)
                {
                    FileLogger.Log($"  • {unit.DisplayName} (KIA)", "BATTLE-END");
                }
            }
            
            // Enemy team status
            var enemyUnits = _unitManager.AllUnits.Where(u => u.IsEnemy).ToList();
            var enemyAlive = enemyUnits.Where(u => u.IsAlive).ToList();
            var enemyDead = enemyUnits.Where(u => !u.IsAlive).ToList();
            
            FileLogger.LogSeparator("ENEMY TEAM STATUS");
            FileLogger.Log($"Survivors: {enemyAlive.Count}/{enemyUnits.Count}", "BATTLE-END");
            
            if (enemyAlive.Any())
            {
                FileLogger.Log("Surviving units:", "BATTLE-END");
                foreach (var unit in enemyAlive)
                {
                    FileLogger.Log($"  • {unit.DisplayName}: {unit.CurrentHP}/{unit.MaxHP} HP ({(unit.CurrentHP * 100 / unit.MaxHP)}%)", "BATTLE-END");
                }
            }
            
            if (enemyDead.Any())
            {
                FileLogger.Log($"Eliminated: {enemyDead.Count}", "BATTLE-END");
                foreach (var unit in enemyDead)
                {
                    FileLogger.Log($"  • {unit.DisplayName} (KIA)", "BATTLE-END");
                }
            }
            
            FileLogger.LogSeparator("BATTLE END");
        }

        // ----------------------------------------------------------------------------------
        // Manual Resolution Logic
        // ----------------------------------------------------------------------------------
        private IEnumerator ManualResolve(UnitRuntimeState actor)
        {
            _manualActor = actor;
            var teamEnergy = _playerEnergy;
            _inputController.EnableInput(true);

            while (true)
            {
                _pendingAction = PlayerActionType.None;
                _selectedTarget = null;
                _validTargets.Clear();
                _actionChanged = false;

                bool canUlt = actor.CanUseUltimate && teamEnergy.Energy >= actor.UltimateEnergyCost;
                _battlePhase = BattlePhase.WaitingForPlayerAction;

                _unitManager.ShowCurrentActorIndicator(actor);

                if (_hud != null)
                {
                    _hud.ShowManualActionPanel(actor, canUlt);
                    _hud.SetActionHint($"Acting: {actor.DisplayName}");
                }

                // Wait for Action Selection (Basic/Ult)
                while (_battlePhase == BattlePhase.WaitingForPlayerAction && !autoMode)
                    yield return null;

                // If switched to auto mode, execute a default basic attack instead of skipping
                if (autoMode)
                {
                    FileLogger.Log($" Switched to Auto during {actor.DisplayName}'s turn. Executing Basic attack.");
                    AbortManual();
                    var autoTarget = _unitManager.SelectTargetPositional(actor);
                    if (autoTarget != null)
                        yield return StartCoroutine(ExecuteBasicSingle(actor, teamEnergy, autoTarget));
                    yield break;
                }

                // Inner Loop: Target Selection
                while (true)
                {
                    _actionChanged = false;

                    // Calculate Valid Targets based on pending action
                    bool isUlt = _pendingAction == PlayerActionType.Ultimate;
                    TargetPattern pattern = isUlt ? actor.UltimateTargetPattern : actor.BasicTargetPattern;

                    _validTargets.Clear();
                    // Using helper method logic here since we moved GetValidTargets logic
                    // Re-implementing simplified target fetch locally using UnitManager
                    _validTargets.AddRange(GetValidTargets(actor, pattern));

                    if (_validTargets.Count == 0) { AbortManual(); yield break; }

                    _battlePhase = BattlePhase.WaitingForTargetSelection;
                    bool isAoe = IsAoePattern(pattern);

                    if (_hud != null)
                        _hud.SetActionHint(isAoe ? "Click any enemy for AOE" : "Click target");

                    _unitManager.HighlightValidTargets(_validTargets, actor, pattern);

                    // Wait for Click or Cancel or Switch Action
                    while (_battlePhase == BattlePhase.WaitingForTargetSelection && !autoMode && !_actionChanged)
                        yield return null;

                    _unitManager.ClearAllHighlights(actor);

                    // If switched to auto mode, execute a default basic attack instead of skipping
                    if (autoMode)
                    {
                        FileLogger.Log($" Switched to Auto during {actor.DisplayName}'s target selection. Executing Basic attack.");
                        AbortManual();
                        var autoTarget = _unitManager.SelectTargetPositional(actor);
                        if (autoTarget != null)
                            yield return StartCoroutine(ExecuteBasicSingle(actor, teamEnergy, autoTarget));
                        yield break;
                    }

                    if (_actionChanged) continue; // Loop back to target calc

                    if (_battlePhase == BattlePhase.WaitingForPlayerAction) break; // ESC pressed, go back to Action Select

                    if (_selectedTarget != null)
                    {
                        _unitManager.HideCurrentActorIndicator(actor);
                        _inputController.EnableInput(false);
                        goto ExecuteAction;
                    }
                }
            }

        ExecuteAction:
            _battlePhase = BattlePhase.ExecutingAction;
            bool isUltFinal = _pendingAction == PlayerActionType.Ultimate;
            TargetPattern finalPattern = isUltFinal ? actor.UltimateTargetPattern : actor.BasicTargetPattern;
            bool isAoeFinal = IsAoePattern(finalPattern);

            if (isAoeFinal)
            {
                if (isUltFinal)
                {
                    // Consume energy before executing ultimate
                    if (!teamEnergy.TrySpendEnergy(actor.UltimateEnergyCost))
                    {
                        FileLogger.LogWarning($" Failed to spend energy for {actor.DisplayName}'s Ultimate!");
                        yield break;
                    }
                    FileLogger.Log($" Player energy consumed: {actor.UltimateEnergyCost}. Current: {teamEnergy.Energy}");
                    yield return StartCoroutine(ExecuteUltimateMulti(actor, teamEnergy, _validTargets));
                }
                else yield return StartCoroutine(ExecuteBasicMulti(actor, teamEnergy, _validTargets));
            }
            else
            {
                if (isUltFinal)
                {
                    // Consume energy before executing ultimate
                    if (!teamEnergy.TrySpendEnergy(actor.UltimateEnergyCost))
                    {
                        FileLogger.LogWarning($" Failed to spend energy for {actor.DisplayName}'s Ultimate!");
                        yield break;
                    }
                    FileLogger.Log($" Player energy consumed: {actor.UltimateEnergyCost}. Current: {teamEnergy.Energy}");
                    yield return StartCoroutine(ExecuteUltimateSingle(actor, teamEnergy, _selectedTarget));
                }
                else yield return StartCoroutine(ExecuteBasicSingle(actor, teamEnergy, _selectedTarget));
            }

            if (_hud != null) _hud.HideManualActionPanel();
        }

        private void AbortManual()
        {
            _inputController.EnableInput(false);
            if (_manualActor != null) _unitManager.HideCurrentActorIndicator(_manualActor);
            if (_hud != null) _hud.HideManualActionPanel();
        }

        // ----------------------------------------------------------------------------------
        // Input Callbacks
        // ----------------------------------------------------------------------------------
        private void OnPlayerChoseAction(PlayerActionType action)
        {
            if (_battlePhase != BattlePhase.WaitingForPlayerAction && _battlePhase != BattlePhase.WaitingForTargetSelection) return;

            if (_battlePhase == BattlePhase.WaitingForTargetSelection)
            {
                _unitManager.ClearAllHighlights(_manualActor);
                _validTargets.Clear();
                _selectedTarget = null;
                _actionChanged = true;
            }

            if (action == PlayerActionType.Ultimate)
            {
                var teamEnergy = _playerEnergy;
                bool canUlt = _manualActor != null && _manualActor.CanUseUltimate && teamEnergy.Energy >= _manualActor.UltimateEnergyCost;
                if (!canUlt) return;
            }

            _pendingAction = action;
            _battlePhase = BattlePhase.WaitingForTargetSelection;
        }

        public void TrySelectTarget(UnitRuntimeState clicked)
        {
            if (_battlePhase != BattlePhase.WaitingForTargetSelection) return;
            if (clicked == null || !_validTargets.Contains(clicked)) return;

            _selectedTarget = clicked;
            _battlePhase = BattlePhase.ExecutingAction;
        }

        private void CancelTargetSelection()
        {
            _unitManager.ClearAllHighlights(_manualActor);
            _validTargets.Clear();
            _selectedTarget = null;
            _pendingAction = PlayerActionType.None;
            _battlePhase = BattlePhase.WaitingForPlayerAction;

            if (_hud != null) _hud.SetActionHint($"Acting: {_manualActor?.DisplayName} - Choose action");
        }

        // ----------------------------------------------------------------------------------
        // Action Sequencing (Coroutines) - Kept here for orchestration
        // ----------------------------------------------------------------------------------
        private IEnumerator ExecuteBasic(UnitRuntimeState actor, TeamEnergyState teamEnergy)
        {
            var target = _unitManager.SelectTargetPositional(actor);
            if (target == null) yield break;
            yield return StartCoroutine(ExecuteBasicSingle(actor, teamEnergy, target));
        }

        private IEnumerator ExecuteUltimate(UnitRuntimeState actor, TeamEnergyState teamEnergy)
        {
            // Trigger OnUltimate passive before execution
            if (actor != null && actor.Passive != null)
            {
                TriggerPassive(actor, PassiveType.OnUltimate);
            }

            if (actor.IsEnemy)
            {
                if (actor.CurrentEnergy < actor.UltimateEnergyCost)
                {
                    yield return StartCoroutine(ExecuteBasic(actor, teamEnergy));
                    yield break;
                }

                FileLogger.Log($" Enemy {actor.DisplayName} energy consumed: {actor.UltimateEnergyCost}. Before: {actor.CurrentEnergy}");
                actor.CurrentEnergy = 0; // Consume Energy immediately for enemy
            }
            else
            {
                FileLogger.Log($" Player energy BEFORE spending: {teamEnergy.Energy}");
                if (!teamEnergy.TrySpendEnergy(actor.UltimateEnergyCost))
                {
                    FileLogger.LogWarning($" Failed to spend {actor.UltimateEnergyCost} energy. Current: {teamEnergy.Energy}");
                    // Fallback for player if energy somehow lost
                    yield break;
                }
                FileLogger.Log($" Player energy AFTER spending: {teamEnergy.Energy}");
            }

            // Determine Targets
            List<UnitRuntimeState> targets = new List<UnitRuntimeState>();
            if (IsAoePattern(actor.UltimateTargetPattern))
            {
                FileLogger.Log($"{actor.DisplayName} using AOE pattern: {actor.UltimateTargetPattern}", "ULTIMATE");
                targets = GetValidTargets(actor, actor.UltimateTargetPattern);
                FileLogger.Log($"GetValidTargets returned {targets.Count} targets", "ULTIMATE");
                if (targets.Count > 0)
                {
                    FileLogger.Log($"Targets: {string.Join(", ", targets.Select(t => $"{t.DisplayName}(Alive:{t.IsAlive})"))}", "ULTIMATE");
                }
            }
            else
            {
                var singleTarget = _unitManager.SelectTargetPositional(actor);
                if (singleTarget != null) targets.Add(singleTarget);
            }

            if (targets.Count == 0)
            {
                FileLogger.LogWarning($"{actor.DisplayName} found ZERO targets for pattern {actor.UltimateTargetPattern}! Aborting.", "ULTIMATE");
                yield break;
            }

            FileLogger.Log($"{actor.DisplayName} proceeding with {targets.Count} target(s). Routing to {(targets.Count > 1 ? "Multi" : "Single")} handler.", "ULTIMATE");
            if (targets.Count > 1)
                yield return StartCoroutine(ExecuteUltimateMulti(actor, teamEnergy, targets));
            else
                yield return StartCoroutine(ExecuteUltimateSingle(actor, teamEnergy, targets[0]));
        }

        private IEnumerator ExecuteBasicSingle(UnitRuntimeState actor, TeamEnergyState teamEnergy, UnitRuntimeState target)
        {
            if (target == null || !target.IsAlive) yield break;

            FileLogger.LogSeparator($"BASIC ATTACK: {actor.DisplayName}");
            FileLogger.Log($"⚔️ {actor.DisplayName} (ATK:{actor.CurrentATK}, Type:{actor.Type}) -> {target.DisplayName} (HP:{target.CurrentHP}/{target.MaxHP}, DEF:{target.CurrentDEF})", "BASIC");

            // Movement Logic
            Vector3 originalPos = Vector3.zero;
            Transform actorTransform = null;
            if (_unitManager.TryGetView(actor, out var actorView))
            {
                actorTransform = actorView.transform;
                originalPos = actorTransform.position;
            }

            if (actor.Type == UnitType.Melee && actorTransform != null && _unitManager.TryGetView(target, out var targetView))
            {
                FileLogger.Log($"Moving {actor.DisplayName} to target position (Melee)", "BASIC");
                yield return MoveToTarget(actor, actorTransform, targetView.transform.position);
            }
            else if (actor.Type == UnitType.Ranged)
            {
                FileLogger.Log($"Ranged unit - no movement", "BASIC");
            }

            teamEnergy.GainEnergy(1);
            int hits = _rng.Next(actor.BasicHitsMin, actor.BasicHitsMax + 1);
            FileLogger.Log($"Number of hits: {hits} (Range: {actor.BasicHitsMin}-{actor.BasicHitsMax})", "BASIC");

            for (int h = 1; h <= hits; h++)
            {
                FileLogger.Log($"--- Hit {h}/{hits} ---", "BASIC");
                
                PlayAnimation(actor, false);
                yield return new WaitForSeconds(attackAnimationDuration * 0.6f);

                teamEnergy.AddToBarAndConvert(energyBarFillPerHitPct);

                bool targetWasAlive = target.IsAlive;
                
                if (!targetWasAlive)
                {
                    FileLogger.Log($"Target already dead, skipping damage", "BASIC");
                    continue;
                }
                
                // Damage calculation
                var (dmg, isCrit) = CombatCalculator.ComputeBasicDamage(actor, target);
                FileLogger.Log($"Calculated damage: {dmg} (Crit: {isCrit})", "BASIC");

                int hpBefore = target.CurrentHP;
                int shieldedDmg = target.AbsorbDamageWithShield(dmg);
                FileLogger.Log($"After shield: {shieldedDmg} damage penetrates", "BASIC");

                target.CurrentHP = Mathf.Max(0, target.CurrentHP - shieldedDmg);
                FileLogger.Log($"HP change: {hpBefore} -> {target.CurrentHP} (Damage dealt: {shieldedDmg})", "BASIC");

                if (shieldedDmg > 0)
                {
                    PlayAnimation(target, false, true);
                    SpawnDamagePopup(target, shieldedDmg, isCrit, false);
                    yield return new WaitForSeconds(hitAnimationDuration);
                }
                else
                {
                    FileLogger.Log($"No damage dealt (fully absorbed by shield)", "BASIC");
                    SpawnDamagePopup(target, 0, isCrit, false);
                }

                if (h < hits) yield return new WaitForSeconds(0.2f);
            }

            _unitManager.RefreshView(target);
            
            if (!target.IsAlive)
            {
                FileLogger.Log($"💀 {target.DisplayName} has died!", "BASIC");
                yield return new WaitForSeconds(deathAnimationDuration);
            }

            if (actor.Type == UnitType.Melee && actorTransform != null)
            {
                FileLogger.Log($"Moving back to original position", "BASIC");
                yield return MoveBack(actorTransform, originalPos);
            }
            
            FileLogger.Log($"Basic attack complete", "BASIC");
        }

        private IEnumerator ExecuteUltimateSingle(UnitRuntimeState actor, TeamEnergyState teamEnergy, UnitRuntimeState target)
        {
            if (target == null || !target.IsAlive)
            {
                FileLogger.Log($"Target is null or dead, aborting ultimate", "ULTIMATE-SINGLE");
                yield break;
            }

            // Note: Energy consumption handled by caller (ExecuteUltimate wrapper or ManualResolve)

            bool isAllyTargeting = TargetPatternHelper.IsAllyTargeting(actor.UltimateTargetPattern);
            string emoji = isAllyTargeting ? "💚" : "💥";
            FileLogger.LogSeparator($"ULTIMATE SINGLE: {actor.DisplayName}");
            FileLogger.Log($"{emoji} {actor.DisplayName} -> {target.DisplayName} ({(isAllyTargeting ? "SUPPORT" : "ATTACK")})", "ULTIMATE-SINGLE");
            FileLogger.Log($"Actor: ATK={actor.CurrentATK}, Type={actor.Type}", "ULTIMATE-SINGLE");
            FileLogger.Log($"Target: HP={target.CurrentHP}/{target.MaxHP}, DEF={target.CurrentDEF}", "ULTIMATE-SINGLE");

            Vector3 originalPos = Vector3.zero;
            Transform actorTransform = null;
            if (_unitManager.TryGetView(actor, out var actorView))
            {
                actorTransform = actorView.transform;
                originalPos = actorTransform.position;
                FileLogger.Log($"Actor transform found: {actorView.gameObject.name}", "ULTIMATE-SINGLE");
            }

            // Only move to target for melee attacks (not for ally support)
            if (!isAllyTargeting && actor.Type == UnitType.Melee && actorTransform != null && _unitManager.TryGetView(target, out var targetView))
            {
                FileLogger.Log($"Moving melee unit to target", "ULTIMATE-SINGLE");
                yield return MoveToTarget(actor, actorTransform, targetView.transform.position);
            }
            else if (actor.Type == UnitType.Ranged)
            {
                FileLogger.Log($"Ranged unit - no movement", "ULTIMATE-SINGLE");
            }

            PlayAnimation(actor, true);
            yield return new WaitForSeconds(ultimateAnimationDuration * 0.7f);

            actor.StartUltimateCooldown();
            FileLogger.Log($"Ultimate cooldown started: {actor.UltimateCooldownRemaining} turns", "ULTIMATE-SINGLE");

            if (isAllyTargeting)
            {
                FileLogger.Log($"Entering SUPPORT branch", "ULTIMATE-SINGLE");
                
                // SUPPORT LOGIC: Heal or Shield
                // Determine if this is a heal or shield based on unit role (simplified heuristic)
                bool isShieldEffect = actor.DisplayName.Contains("Aegis") || actor.DisplayName.Contains("Tank");

                if (isShieldEffect)
                {
                    // Apply Shield
                    int shieldAmount = CombatCalculator.ComputeShieldAmount(actor);
                    FileLogger.Log($"Applying shield: {shieldAmount} HP", "ULTIMATE-SINGLE");
                    target.ApplyStatusEffect(StatusEffectType.Shield, shieldAmount, 0f, 2);
                    
                    SpawnDamagePopup(target, shieldAmount, false, true); // Green popup for shield
                    FileLogger.Log($"✅ Shield applied successfully", "ULTIMATE-SINGLE");
                }
                else
                {
                    // Apply Heal
                    int healAmount = CombatCalculator.ComputeHealAmount(actor);
                    int actualHeal = Mathf.Min(healAmount, target.MaxHP - target.CurrentHP);
                    int hpBefore = target.CurrentHP;
                    target.CurrentHP = Mathf.Min(target.MaxHP, target.CurrentHP + actualHeal);
                    
                    FileLogger.Log($"Healing: {healAmount} calculated, {actualHeal} actual", "ULTIMATE-SINGLE");
                    FileLogger.Log($"HP change: {hpBefore} -> {target.CurrentHP}", "ULTIMATE-SINGLE");
                    
                    SpawnDamagePopup(target, actualHeal, false, true); // Green popup for heal
                    FileLogger.Log($"✅ Heal applied successfully", "ULTIMATE-SINGLE");
                }

                yield return new WaitForSeconds(ultimateAnimationDuration * 0.3f);
            }
            else
            {
                FileLogger.Log($"Entering ATTACK/DAMAGE branch", "ULTIMATE-SINGLE");
                
                // Capture target position BEFORE any animation/knockback
                Vector3 targetOriginalPos = Vector3.zero;
                Transform targetTransform = null;
                if (_unitManager.TryGetView(target, out var tView))
                {
                    targetTransform = tView.transform;
                    targetOriginalPos = targetTransform.position;
                    FileLogger.Log($"Target position captured BEFORE knockback: {targetOriginalPos}", "ULTIMATE-SINGLE");
                }
                
                // DAMAGE LOGIC: Original behavior
                var (dmg, isCrit) = CombatCalculator.ComputeUltimateDamage(actor, target);
                FileLogger.Log($"Calculated damage: {dmg} (Crit: {isCrit})", "ULTIMATE-SINGLE");
                
                int hpBefore = target.CurrentHP;
                int shieldedDmg = target.AbsorbDamageWithShield(dmg);
                FileLogger.Log($"After shield: {shieldedDmg} damage penetrates", "ULTIMATE-SINGLE");
                
                target.CurrentHP = Mathf.Max(0, target.CurrentHP - shieldedDmg);
                FileLogger.Log($"HP change: {hpBefore} -> {target.CurrentHP} (Damage dealt: {shieldedDmg})", "ULTIMATE-SINGLE");

                if (shieldedDmg > 0)
                {
                    FileLogger.Log($"Damage dealt: playing hit animation", "ULTIMATE-SINGLE");
                    PlayAnimation(target, true, true, true);
                    SpawnDamagePopup(target, shieldedDmg, isCrit, false);
                    yield return new WaitForSeconds(hitAnimationDuration + 0.3f);
                }
                else
                {
                    FileLogger.Log($"No damage dealt (fully absorbed by shield)", "ULTIMATE-SINGLE");
                    SpawnDamagePopup(target, 0, isCrit, false);
                    yield return new WaitForSeconds(ultimateAnimationDuration * 0.3f);
                }

                // Restore target position AFTER knockback animation completes
                if (targetTransform != null)
                {
                    // Force target to idle state before restoring position
                    // This prevents animation curves from interfering with position restoration
                    var animator = targetTransform.GetComponentInChildren<Animator>();
                    if (animator != null)
                    {
                        FileLogger.Log("Forcing target to idle state before position restore", "ULTIMATE-SINGLE");
                        animator.ResetTrigger("HitUltimate");
                        animator.ResetTrigger("HitBasic");
                    }
                    
                    // Small buffer to ensure animation state change takes effect
                    yield return new WaitForSeconds(0.1f);
                    
                    targetTransform.position = targetOriginalPos;
                    FileLogger.Log($"Target position restored to: {targetOriginalPos}", "ULTIMATE-SINGLE");
                }
            }

            if (actor.Type == UnitType.Melee && actorTransform != null)
            {
                FileLogger.Log($"Moving back to original position", "ULTIMATE-SINGLE");
                yield return MoveBack(actorTransform, originalPos);
            }

            _unitManager.RefreshView(target);
            
            if (!target.IsAlive)
            {
                FileLogger.Log($"💀 {target.DisplayName} has died!", "ULTIMATE-SINGLE");
                yield return new WaitForSeconds(deathAnimationDuration);
            }
            
            FileLogger.Log($"Ultimate single complete", "ULTIMATE-SINGLE");
        }

        private IEnumerator ExecuteBasicMulti(UnitRuntimeState actor, TeamEnergyState teamEnergy, List<UnitRuntimeState> targets)
        {
            if (targets == null || targets.Count == 0) yield break;

            Vector3 originalPos = Vector3.zero;
            Transform actorTransform = null;
            if (_unitManager.TryGetView(actor, out var view)) { actorTransform = view.transform; originalPos = actorTransform.position; }

            // Move to center
            if (actor.Type == UnitType.Melee && actorTransform != null)
            {
                // Calculate center
                Vector3 center = Vector3.zero;
                int count = 0;
                foreach (var t in targets)
                {
                    if (_unitManager.TryGetView(t, out var tv)) { center += tv.transform.position; count++; }
                }
                if (count > 0) yield return MoveToTarget(actor, actorTransform, center / count);
            }

            PlayAnimation(actor, false);
            yield return new WaitForSeconds(attackAnimationDuration * 0.6f);
            teamEnergy.GainEnergy(1);

            foreach (var t in targets)
            {
                if (t == null || !t.IsAlive) continue;
                teamEnergy.AddToBarAndConvert(energyBarFillPerHitPct);

                var (dmg, isCrit) = CombatCalculator.ComputeBasicDamage(actor, t);
                int shieldedDmg = t.AbsorbDamageWithShield(dmg);
                t.CurrentHP = Mathf.Max(0, t.CurrentHP - shieldedDmg);

                if (shieldedDmg > 0)
                {
                    PlayAnimation(t, false, true);
                    SpawnDamagePopup(t, shieldedDmg, isCrit, false);
                    yield return new WaitForSeconds(hitAnimationDuration);
                }
                else
                {
                    SpawnDamagePopup(t, shieldedDmg, isCrit, false);
                }
                _unitManager.RefreshView(t);
                if (!t.IsAlive) yield return new WaitForSeconds(deathAnimationDuration);
            }

            if (actor.Type == UnitType.Melee && actorTransform != null) yield return MoveBack(actorTransform, originalPos);
        }

        private IEnumerator ExecuteUltimateMulti(UnitRuntimeState actor, TeamEnergyState teamEnergy, List<UnitRuntimeState> targets)
        {
            FileLogger.LogSeparator($"ULTIMATE MULTI: {actor.DisplayName}");
            
            if (targets == null || targets.Count == 0)
            {
                FileLogger.LogWarning($"{actor.DisplayName} has NO TARGETS!", "ULTIMATE-MULTI");
                yield break;
            }
            
            // Energy already consumed in ExecuteUltimate wrapper for consistency

            bool isAllyTargeting = TargetPatternHelper.IsAllyTargeting(actor.UltimateTargetPattern);
            string emoji = isAllyTargeting ? "💚" : "💥";
            FileLogger.Log($"{emoji} {actor.DisplayName} -> {targets.Count} targets ({(isAllyTargeting ? "SUPPORT" : "ATTACK")})", "ULTIMATE-MULTI");
            FileLogger.Log($"Targets: {string.Join(", ", targets.Select(t => t.DisplayName))}", "ULTIMATE-MULTI");

            Vector3 originalPos = Vector3.zero;
            Transform actorTransform = null;
            if (_unitManager.TryGetView(actor, out var view))
            {
                actorTransform = view.transform;
                originalPos = actorTransform.position;
                FileLogger.Log($"Actor transform found: {actorTransform.name}", "ULTIMATE-MULTI");
            }
            else
            {
                FileLogger.LogWarning($"NO VIEW FOUND for {actor.DisplayName}!", "ULTIMATE-MULTI");
            }

            // CAPTURE TARGET POSITIONS **BEFORE** ANY MOVEMENT OR ANIMATION (FIX FOR KNOCKBACK BUG)
            FileLogger.Log("Capturing target positions BEFORE movement/animation", "ULTIMATE-MULTI");
            var restoreData = new List<(Transform t, Vector3 p)>();
            foreach (var t in targets)
            {
                if (_unitManager.TryGetView(t, out var tv))
                {
                    restoreData.Add((tv.transform, tv.transform.position));
                }
            }
            FileLogger.Log($"Restore data captured. Count: {restoreData.Count}", "ULTIMATE-MULTI");

            // Only move to center for melee attacks (not for ally support)
            if (!isAllyTargeting && actor.Type == UnitType.Melee && actorTransform != null)
            {
                FileLogger.Log("Melee movement initiated", "ULTIMATE-MULTI");
                Vector3 center = Vector3.zero;
                int count = 0;
                foreach (var t in targets)
                    if (_unitManager.TryGetView(t, out var tv)) { center += tv.transform.position; count++; }
                if (count > 0) yield return MoveToTarget(actor, actorTransform, center / count);
            }
            else
            {
                FileLogger.Log($"Skipping movement: IsAllyTargeting={isAllyTargeting}, Type={actor.Type}, HasTransform={actorTransform != null}", "ULTIMATE-MULTI");
            }

            // Filter valid targets only (alive targets)
            FileLogger.Log($"Filtering targets. Initial count: {targets.Count}", "ULTIMATE-MULTI");
            var validTargets = targets.Where(t => t != null && t.IsAlive).ToList();
            FileLogger.Log($"ValidTargets after filter: {validTargets.Count}", "ULTIMATE-MULTI");
            
            FileLogger.Log($"Checking branch: isAllyTargeting={isAllyTargeting}", "ULTIMATE-MULTI");

            if (isAllyTargeting)
            {
                FileLogger.Log("Entering SUPPORT branch (heal/shield)", "ULTIMATE-MULTI");
                // ===== SUPPORT LOGIC: Heal or Shield All Allies =====
                PlayAnimation(actor, true);
                actor.StartUltimateCooldown();
                yield return new WaitForSeconds(ultimateAnimationDuration * 0.7f);

                bool isShieldEffect = actor.DisplayName.Contains("Aegis") || actor.DisplayName.Contains("Tank");

                foreach (var target in validTargets)
                {
                    if (target == null || !target.IsAlive) continue;

                    if (isShieldEffect)
                    {
                        // Apply Shield
                        int shieldAmount = CombatCalculator.ComputeShieldAmount(actor);
                        target.ApplyStatusEffect(StatusEffectType.Shield, shieldAmount, 0f, 2);
                        
                        SpawnDamagePopup(target, shieldAmount, false, true); // Green popup for shield
                        FileLogger.Log($" {actor.DisplayName} shielded {target.DisplayName} for {shieldAmount}");
                    }
                    else
                    {
                        // Apply Heal
                        int healAmount = CombatCalculator.ComputeHealAmount(actor);
                        int actualHeal = Mathf.Min(healAmount, target.MaxHP - target.CurrentHP);
                        target.CurrentHP = Mathf.Min(target.MaxHP, target.CurrentHP + actualHeal);
                        
                        SpawnDamagePopup(target, actualHeal, false, true); // Green popup for heal
                        FileLogger.Log($" {actor.DisplayName} healed {target.DisplayName} for {actualHeal} HP");
                    }

                    _unitManager.RefreshView(target);
                }

                yield return new WaitForSeconds(ultimateAnimationDuration * 0.3f);
            }
            else
            {
                FileLogger.Log("Entering ATTACK/DAMAGE branch", "ULTIMATE-MULTI");
                // ===== DAMAGE LOGIC: Original multi-hit behavior =====
                // (Restore data already built before movement - see line ~1020)

                // Setup Event Listener for per-hit damage calculation
                UnitAnimationDriver driver = null;
                if (actorTransform != null) driver = actorTransform.GetComponent<UnitAnimationDriver>();
                FileLogger.Log($"Driver check: actorTransform={actorTransform != null}, driver={driver != null}, UnitType={actor.Type}", "ULTIMATE-MULTI");

                // Per-hit multipliers: Hit 1 = 0.44, Hit 2 = 0.44, Hit 3 = 1.32 (totals 2.2)
                float[] hitMultipliers = { 0.44f, 0.44f, 1.32f };

                Action<int> onHit = (hitIndex) =>
                {
                    // Ensure hitIndex is valid (1-based from animation events)
                    int arrayIndex = hitIndex - 1;
                    if (arrayIndex < 0 || arrayIndex >= hitMultipliers.Length)
                    {
                        FileLogger.LogWarning($"Invalid hit index: {hitIndex} (array index: {arrayIndex})", "ULTIMATE-MULTI");
                        return;
                    }

                    float multiplier = hitMultipliers[arrayIndex];
                    FileLogger.Log($"--- Hit {hitIndex}/{hitMultipliers.Length} (Multiplier: {multiplier:F2}x) ---", "ULTIMATE-MULTI");

                    foreach (var target in validTargets)
                    {
                        if (target == null || !target.IsAlive)
                        {
                            FileLogger.Log($"Skipping null/dead target in hit {hitIndex}", "ULTIMATE-MULTI");
                            continue;
                        }

                        // Calculate damage with per-hit multiplier and independent crit roll
                        int hpBefore = target.CurrentHP;
                        var (dmg, isCrit) = CombatCalculator.ComputeUltimateDamage(actor, target, multiplier);
                        FileLogger.Log($"  {target.DisplayName}: Calculated damage={dmg} (Crit:{isCrit}), HP before={hpBefore}/{target.MaxHP}", "ULTIMATE-MULTI");
                        
                        int shieldedDmg = target.AbsorbDamageWithShield(dmg);
                        FileLogger.Log($"  After shield: {shieldedDmg} damage penetrates", "ULTIMATE-MULTI");

                        // Apply damage to HP
                        target.CurrentHP = Mathf.Max(0, target.CurrentHP - shieldedDmg);
                        FileLogger.Log($"  HP change: {hpBefore} -> {target.CurrentHP} (Damage dealt: {shieldedDmg})", "ULTIMATE-MULTI");

                        // Visual feedback
                        bool isHeavyHit = (arrayIndex == 2); // Hit 3 is heavy
                        if (shieldedDmg > 0)
                        {
                            PlayAnimation(target, true, true, isHeavyHit);
                            SpawnDamagePopup(target, shieldedDmg, isCrit, false);
                        }
                        else
                        {
                            FileLogger.Log($"  No damage dealt (fully absorbed)", "ULTIMATE-MULTI");
                            SpawnDamagePopup(target, 0, false, false);
                        }

                        _unitManager.RefreshView(target);
                        
                        if (!target.IsAlive)
                        {
                            FileLogger.Log($"  💀 {target.DisplayName} has died!", "ULTIMATE-MULTI");
                        }
                    }
                };

                if (driver != null) driver.OnHitTriggered += onHit;

                PlayAnimation(actor, true);
                actor.StartUltimateCooldown();

                // If no animation driver OR ranged unit (ranged units don't fire hit events), apply damage directly
                bool useDirectDamage = (driver == null || actor.Type == UnitType.Ranged);
                FileLogger.Log($"Damage path decision: useDirectDamage={useDirectDamage} (driver={driver != null}, type={actor.Type})", "ULTIMATE-MULTI");
                
                if (useDirectDamage)
                {
                    FileLogger.Log($"Using direct damage application for {actor.DisplayName} (Type: {actor.Type})", "ULTIMATE-MULTI");
                    FileLogger.Log($"ValidTargets count for direct damage: {validTargets.Count}", "ULTIMATE-MULTI");
                    yield return new WaitForSeconds(ultimateAnimationDuration * 0.5f);
                    
                    // Apply full damage (use 2.2 multiplier for single-hit AOE)
                    int damageCount = 0;
                    foreach (var target in validTargets)
                    {
                        if (target == null || !target.IsAlive)
                        {
                            FileLogger.Log("Skipping null/dead target in damage loop", "ULTIMATE-MULTI");
                            continue;
                        }

                        FileLogger.Log($"Computing damage for {target.DisplayName} (HP: {target.CurrentHP}/{target.MaxHP})", "DAMAGE");
                        var (dmg, isCrit) = CombatCalculator.ComputeUltimateDamage(actor, target, 2.2f);
                        FileLogger.Log($"Calculated damage: {dmg} (Crit: {isCrit})", "DAMAGE");
                        
                        int shieldedDmg = target.AbsorbDamageWithShield(dmg);
                        FileLogger.Log($"After shield absorption: {shieldedDmg}", "DAMAGE");
                        
                        int oldHP = target.CurrentHP;
                        target.CurrentHP = Mathf.Max(0, target.CurrentHP - shieldedDmg);
                        FileLogger.Log($"HP change: {oldHP} -> {target.CurrentHP}", "DAMAGE");

                        if (shieldedDmg > 0)
                        {
                            PlayAnimation(target, true, true, true);
                            SpawnDamagePopup(target, shieldedDmg, isCrit, false);
                            damageCount++;
                        }
                        else
                        {
                            SpawnDamagePopup(target, 0, false, false);
                        }

                        _unitManager.RefreshView(target);
                    }
                    
                    FileLogger.Log($"Damage loop complete. Damaged {damageCount} targets.", "ULTIMATE-MULTI");
                    yield return new WaitForSeconds(ultimateAnimationDuration * 0.5f);
                }
                else
                {
                    // Wait for Animation events (melee units with animation driver)
                    FileLogger.Log("Using animation-driven damage (melee with driver). Waiting for hit events...", "ULTIMATE-MULTI");
                    // Add buffer to ensure we don't unsubscribe before the last event (Frame 46) if duration is tight
                    yield return new WaitForSeconds(ultimateAnimationDuration + 0.5f);

                    // Unsubscribe
                    driver.OnHitTriggered -= onHit;
                }

                // Force all targets to idle state before restoring positions
                // This prevents animation curves from interfering with position restoration
                FileLogger.Log("Forcing targets to idle state before position restore", "ULTIMATE-MULTI");
                foreach (var t in targets)
                {
                    if (_unitManager.TryGetView(t, out var tv))
                    {
                        var animator = tv.GetComponentInChildren<Animator>();
                        if (animator != null)
                        {
                            animator.ResetTrigger("HitUltimate");
                            animator.ResetTrigger("HitBasic");
                        }
                    }
                }
                
                // Small buffer to ensure animation state change takes effect
                yield return new WaitForSeconds(0.1f);
                
                // Restore positions
                FileLogger.Log("Restoring target positions", "ULTIMATE-MULTI");
                foreach (var data in restoreData)
                {
                    if (data.t != null)
                    {
                        data.t.position = data.p;
                        FileLogger.Log($"Position restored to: {data.p}", "ULTIMATE-MULTI");
                    }
                }
            }

            // Move back for melee units
            if (actor.Type == UnitType.Melee && actorTransform != null) yield return MoveBack(actorTransform, originalPos);
        }

        // --- Movement Helpers ---
        private IEnumerator MoveToTarget(UnitRuntimeState actor, Transform actorT, Vector3 targetPos)
        {
            // Use "IsDashing" parameter
            SetDashing(actor, true);
            Vector3 direction = (targetPos - actorT.position).normalized;
            Vector3 attackPos = targetPos - direction * meleeDistance;

            while (Vector3.Distance(actorT.position, attackPos) > 0.1f)
            {
                actorT.position = Vector3.MoveTowards(actorT.position, attackPos, meleeMovementSpeed * Time.deltaTime);
                yield return null;
            }
            SetDashing(actor, false);
        }

        private IEnumerator MoveBack(Transform actorT, Vector3 originalPos)
        {
            while (Vector3.Distance(actorT.position, originalPos) > 0.1f)
            {
                actorT.position = Vector3.MoveTowards(actorT.position, originalPos, meleeMovementSpeed * Time.deltaTime);
                yield return null;
            }
            actorT.position = originalPos;
        }

        private void SetDashing(UnitRuntimeState unit, bool isDashing)
        {
            if (_unitManager.TryGetView(unit, out var view))
            {
                var anim = view.GetComponent<UnitAnimationDriver>();
                if (anim != null) anim.SetDashing(isDashing);
            }
        }

        // --- Visual Helpers ---
        private void PlayAnimation(UnitRuntimeState unit, bool isUlt, bool isHit = false, bool isHeavy = false)
        {
            if (_unitManager.TryGetView(unit, out var view))
            {
                var anim = view.GetComponent<UnitAnimationDriver>();
                if (anim != null)
                {
                    if (isHit) anim.PlayHit(isUlt, isHeavy);
                    else anim.PlayAttack(unit.Type, isUlt);
                }
            }
        }

        private void SpawnDamagePopup(UnitRuntimeState target, int dmg, bool isCrit, bool isHeal)
        {
            if (damagePopupPrefab == null)
            {
                FileLogger.LogWarning($" DamagePopup prefab is NULL. Cannot spawn popup for {dmg} damage. Please assign it in the Inspector.");
                return;
            }

            if (_unitManager.TryGetView(target, out var view))
            {
                Vector3 spawnPos = view.transform.position + damagePopupOffset;
                var popup = GetPooledDamagePopup();
                popup.Initialize(dmg, isCrit, isHeal, spawnPos, isPooled: true);
            }
        }

        // --- Object Pooling ---
        private void InitializeDamagePopupPool()
        {
            if (damagePopupPrefab == null)
            {
                FileLogger.LogWarning(" DamagePopup prefab is NULL. Cannot initialize pool.");
                return;
            }

            // Register return callback
            DamagePopup.SetReturnToPoolCallback(ReturnDamagePopupToPool);

            for (int i = 0; i < damagePopupPoolSize; i++)
            {
                var popup = Instantiate(damagePopupPrefab);
                popup.gameObject.SetActive(false);
                popup.transform.SetParent(transform); // Keep organized
                _damagePopupPool.Enqueue(popup);
            }

            FileLogger.Log($" Initialized DamagePopup pool with {damagePopupPoolSize} instances.");
        }

        private DamagePopup GetPooledDamagePopup()
        {
            if (_damagePopupPool.Count > 0)
            {
                return _damagePopupPool.Dequeue();
            }

            // Pool exhausted - create a new one (fallback)
            FileLogger.LogWarning(" DamagePopup pool exhausted! Creating new instance.");
            var popup = Instantiate(damagePopupPrefab);
            popup.transform.SetParent(transform);
            return popup;
        }

        private void ReturnDamagePopupToPool(DamagePopup popup)
        {
            _damagePopupPool.Enqueue(popup);
        }

        // --- Helpers ---
        private void TriggerPassive(UnitRuntimeState unit, PassiveType type)
        {
            if (unit == null || unit.Passive == null) return;
            if (!unit.ShouldTriggerPassive(type, (float)unit.CurrentHP / unit.MaxHP)) return;

            FileLogger.LogSeparator($"PASSIVE: {unit.PassiveName}");
            FileLogger.Log($"🌟 {unit.DisplayName} triggers '{unit.PassiveName}' ({type})", "PASSIVE");
            FileLogger.Log($"   Effect: {unit.Passive.EffectType}, Value: {unit.Passive.Value}, Modifier: {unit.Passive.Modifier}, Duration: {unit.Passive.Duration}", "PASSIVE");
            
            // Effect application logic...
            if (unit.Passive.TargetRandomAlly)
            {
                // Select 1 random ally (excluding self if not TargetSelf)
                var allies = _unitManager.AllUnits.Where(u => u.IsEnemy == unit.IsEnemy && u.IsAlive).ToList();
                if (!unit.Passive.TargetSelf)
                {
                    allies = allies.Where(a => a != unit).ToList();
                }
                
                if (allies.Count > 0)
                {
                    var randomAlly = allies[_rng.Next(allies.Count)];
                    FileLogger.Log($"   Targeting 1 random ally: {randomAlly.DisplayName} (from {allies.Count} candidates)", "PASSIVE");
                    randomAlly.ApplyStatusEffect(unit.Passive.EffectType, unit.Passive.Value, unit.Passive.Modifier, unit.Passive.Duration);
                }
                else
                {
                    FileLogger.Log($"   No valid allies to target", "PASSIVE");
                }
            }
            else if (unit.Passive.TargetAllies)
            {
                var allies = _unitManager.AllUnits.Where(u => u.IsEnemy == unit.IsEnemy && u.IsAlive).ToList();
                FileLogger.Log($"   Targeting {allies.Count} allies: {string.Join(", ", allies.Select(a => a.DisplayName))}", "PASSIVE");
                
                foreach (var ally in allies)
                {
                    ally.ApplyStatusEffect(unit.Passive.EffectType, unit.Passive.Value, unit.Passive.Modifier, unit.Passive.Duration);
                }
            }
            else if (unit.Passive.TargetSelf)
            {
                FileLogger.Log($"   Targeting self: {unit.DisplayName}", "PASSIVE");
                unit.ApplyStatusEffect(unit.Passive.EffectType, unit.Passive.Value, unit.Passive.Modifier, unit.Passive.Duration);
            }
            else
            {
                // If neither TargetAllies nor TargetSelf, assume it targets enemies
                var enemies = _unitManager.AllUnits.Where(u => u.IsEnemy != unit.IsEnemy && u.IsAlive).ToList();
                FileLogger.Log($"   Targeting {enemies.Count} enemies: {string.Join(", ", enemies.Select(e => e.DisplayName))}", "PASSIVE");
                
                foreach (var enemy in enemies)
                {
                    enemy.ApplyStatusEffect(unit.Passive.EffectType, unit.Passive.Value, unit.Passive.Modifier, unit.Passive.Duration);
                }
            }
            
            FileLogger.Log($"✅ Passive '{unit.PassiveName}' complete", "PASSIVE");
        }

        private List<UnitRuntimeState> GetValidTargets(UnitRuntimeState actor, TargetPattern pattern)
        {
            return _unitManager.GetValidTargets(actor, pattern);
        }

        private bool IsAoePattern(TargetPattern p)
        {
            return p == TargetPattern.AllEnemies || p == TargetPattern.AllAllies;
        }

        private void OnSquadAvatarClicked(string id)
        {
            if (!autoMode) return;
            var pref = GetActionPreference(id);
            var next = pref switch { ActionPreference.SmartAI => ActionPreference.Basic, ActionPreference.Basic => ActionPreference.Ultimate, _ => ActionPreference.SmartAI };
            _actionPreferences[id] = next;
            if (_hud != null) _hud.ShowAutoSquadPanel();
        }

        public ActionPreference GetActionPreference(string id) => _actionPreferences.TryGetValue(id, out var p) ? p : ActionPreference.SmartAI;
        
        // --- Test/Debug API ---
        
        /// <summary>
        /// Initialize a battle with custom units (for testing).
        /// This bypasses the normal Start() lineup and spawns units directly.
        /// </summary>
        public void InitializeBattle(List<UnitDefinitionSO> players, List<UnitDefinitionSO> enemies, int seed)
        {
            FileLogger.LogSeparator("TEST BATTLE INITIALIZATION");
            FileLogger.Log($"Initializing test battle with {players.Count} players, {enemies.Count} enemies, seed={seed}", "TEST-INIT");
            
            // Reset state
            IsBattleOver = false;
            Outcome = BattleOutcome.InProgress;
            
        // Initialize RNG with test seed
        _rng = new System.Random(seed);
        
        // Clear existing units
        if (_unitManager != null)
        {
            // Stop existing battle coroutine if running
            StopAllCoroutines();
            
            // Clear all existing units from scene
            _unitManager.ClearAll();
        }
        else
        {
            _unitManager = GetComponent<BattleUnitManager>();
            if (_unitManager == null) _unitManager = gameObject.AddComponent<BattleUnitManager>();
        }
        
        // Initialize energy
            if (_playerEnergy == null) _playerEnergy = new TeamEnergyState(maxEnergy, startEnergy);
            if (_enemyEnergy == null) _enemyEnergy = new TeamEnergyState(maxEnergy, startEnergy);
            
            _playerEnergy.Reset(startEnergy);
            _enemyEnergy.Reset(startEnergy);
            
            // Spawn units
            _unitManager.SpawnTeam(players.ToArray(), playerSlots, isEnemy: false);
            _unitManager.SpawnTeam(enemies.ToArray(), enemySlots, isEnemy: true);
            
            FileLogger.Log($"Units spawned: {_unitManager.AllUnits.Count} total", "TEST-INIT");
            
            // Don't auto-start battle - let tests control execution
            FileLogger.Log("Test battle initialized. Call StartTestBattle() to begin.", "TEST-INIT");
        }
        
        /// <summary>
        /// Start the battle coroutine (for tests that initialized via InitializeBattle).
        /// </summary>
        public void StartTestBattle()
        {
            if (_unitManager == null || _unitManager.AllUnits.Count == 0)
            {
                FileLogger.LogWarning("Cannot start battle - no units spawned!");
                return;
            }
            
            FileLogger.Log("Starting test battle coroutine", "TEST");
            StartCoroutine(BattleCoroutine());
        }
        
        /// <summary>
        /// Get all player unit views (for testing).
        /// </summary>
        public List<UnitView> GetPlayerUnits()
        {
            var views = new List<UnitView>();
            if (_unitManager == null) return views;
            
            foreach (var unit in _unitManager.AllUnits)
            {
                if (unit != null && !unit.IsEnemy)
                {
                    if (_unitManager.TryGetView(unit, out var view))
                    {
                        views.Add(view);
                    }
                }
            }
            
            return views;
        }
        
        /// <summary>
        /// Get all enemy unit views (for testing).
        /// </summary>
        public List<UnitView> GetEnemyUnits()
        {
            var views = new List<UnitView>();
            if (_unitManager == null) return views;
            
            foreach (var unit in _unitManager.AllUnits)
            {
                if (unit != null && unit.IsEnemy)
                {
                    if (_unitManager.TryGetView(unit, out var view))
                    {
                        views.Add(view);
                    }
                }
            }
            
            return views;
        }
    }
}
