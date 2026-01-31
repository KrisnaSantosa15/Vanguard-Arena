using System.Collections.Generic;
using System.Linq;

namespace Project.Domain
{
    /// <summary>
    /// Possible outcomes of a battle simulation.
    /// </summary>
    public enum BattleOutcome
    {
        Victory,      // All enemies defeated
        Defeat,       // All players defeated
        TurnLimit,    // Max turns reached without victory/defeat
        InProgress    // Battle still ongoing
    }

    /// <summary>
    /// Result of a complete battle simulation.
    /// </summary>
    public class BattleResult
    {
        public BattleOutcome Outcome { get; set; }
        public int TurnsElapsed { get; set; }
        public List<UnitRuntimeState> SurvivingPlayers { get; set; }
        public List<UnitRuntimeState> SurvivingEnemies { get; set; }
        public int FinalPlayerEnergy { get; set; }
        public int FinalEnemyEnergy { get; set; }
        public List<string> BattleLog { get; set; } // Optional: detailed turn-by-turn log

        public BattleResult()
        {
            SurvivingPlayers = new List<UnitRuntimeState>();
            SurvivingEnemies = new List<UnitRuntimeState>();
            BattleLog = new List<string>();
        }
    }

    /// <summary>
    /// Simulates complete battles in memory (no Unity graphics/UI).
    /// Used for integration testing and AI development.
    /// Pure domain logic - no Unity dependencies except Mathf for rounding.
    /// </summary>
    public class BattleSimulator
    {
        private readonly List<UnitRuntimeState> _players;
        private readonly List<UnitRuntimeState> _enemies;
        private readonly IDeterministicRandom _rng;
        private readonly TurnManager _turnManager;
        private readonly TeamEnergyState _playerEnergy;
        private readonly TeamEnergyState _enemyEnergy;
        private readonly bool _enableLogging;

        private int _currentTurn = 0;
        private BattleOutcome _outcome = BattleOutcome.InProgress;

        /// <summary>
        /// Constructor for battle simulator.
        /// </summary>
        /// <param name="players">List of player units (will be modified during simulation)</param>
        /// <param name="enemies">List of enemy units (will be modified during simulation)</param>
        /// <param name="rng">Random number generator (use SeededRandom for deterministic tests)</param>
        /// <param name="enableLogging">If true, logs detailed battle information</param>
        public BattleSimulator(
            List<UnitRuntimeState> players,
            List<UnitRuntimeState> enemies,
            IDeterministicRandom rng,
            bool enableLogging = false)
        {
            _players = players;
            _enemies = enemies;
            _rng = rng;
            _enableLogging = enableLogging;

            _turnManager = new TurnManager();
            _playerEnergy = new TeamEnergyState(maxEnergy: 100, startEnergy: 0);
            _enemyEnergy = new TeamEnergyState(maxEnergy: 100, startEnergy: 0);

            // Initialize timeline
            var allUnits = new List<UnitRuntimeState>();
            allUnits.AddRange(_players);
            allUnits.AddRange(_enemies);
            _turnManager.RebuildTimeline(allUnits);
        }

        /// <summary>
        /// Simulate the entire battle until victory, defeat, or turn limit.
        /// </summary>
        /// <param name="maxTurns">Maximum number of turns before ending in TurnLimit outcome</param>
        /// <returns>BattleResult with outcome and statistics</returns>
        public BattleResult SimulateBattle(int maxTurns = 100)
        {
            // Check victory conditions before first turn (e.g., no units)
            _outcome = CheckBattleOutcome();
            if (_outcome != BattleOutcome.InProgress)
            {
                return CreateBattleResult();
            }

            while (_outcome == BattleOutcome.InProgress && _currentTurn < maxTurns)
            {
                SimulateSingleTurn();
                _currentTurn++;

                // Check victory conditions after each turn
                _outcome = CheckBattleOutcome();
            }

            // If we hit turn limit without victory/defeat
            if (_outcome == BattleOutcome.InProgress && _currentTurn >= maxTurns)
            {
                _outcome = BattleOutcome.TurnLimit;
            }

            return CreateBattleResult();
        }

        /// <summary>
        /// Simulate a single turn (all units act once in speed order).
        /// </summary>
        public void SimulateSingleTurn()
        {
            if (_enableLogging)
            {
                FileLogger.Log($"\n=== TURN {_currentTurn + 1} START ===", "BATTLE_SIM");
                FileLogger.Log($"Player Energy: {_playerEnergy.Energy}, Enemy Energy: {_enemyEnergy.Energy}", "BATTLE_SIM");
            }

            // Store units that need to act this turn
            var unitsToAct = new List<UnitRuntimeState>(_turnManager.Timeline);

            foreach (var actor in unitsToAct)
            {
                // Skip if unit died during this turn
                if (!actor.IsAlive)
                    continue;

                // Determine which team this unit belongs to
                bool isPlayerUnit = _players.Contains(actor);
                var allies = isPlayerUnit ? _players : _enemies;
                var enemies = isPlayerUnit ? _enemies : _players;
                var teamEnergy = isPlayerUnit ? _playerEnergy : _enemyEnergy;

                // Process burn damage at turn start
                int burnDamage = actor.ProcessBurnDamage();
                if (burnDamage > 0 && _enableLogging)
                {
                    FileLogger.Log($"🔥 {actor.DisplayName} took {burnDamage} burn damage (HP: {actor.CurrentHP})", "BATTLE_SIM");
                }

                // Skip turn if unit died from burn
                if (!actor.IsAlive)
                {
                    if (_enableLogging)
                    {
                        FileLogger.Log($"💀 {actor.DisplayName} died from burn damage!", "BATTLE_SIM");
                    }
                    continue;
                }

                // Tick down status effects and cooldowns
                actor.OnNewTurnTickCooldown();

                // Trigger OnTurnStart passives
                PassiveManager.TriggerOnTurnStart(actor, allies, _rng);

                // Decide action: Ultimate or Basic
                ActionResult actionResult = null;

                if (ActionExecutor.CanUseUltimate(actor, teamEnergy))
                {
                    // Use ultimate if available
                    actionResult = ActionExecutor.ExecuteUltimateAction(
                        actor, allies, enemies, teamEnergy, _rng);

                    if (_enableLogging && actionResult.Targets.Count > 0)
                    {
                        FileLogger.Log($"💥 {actor.DisplayName} used ULTIMATE on {actionResult.Targets.Count} targets! (Damage: {actionResult.DamageDealt.Sum()})", "BATTLE_SIM");
                    }
                }
                else
                {
                    // Use basic attack
                    actionResult = ActionExecutor.ExecuteBasicAction(
                        actor, allies, enemies, _rng);

                    if (_enableLogging && actionResult.Targets.Count > 0)
                    {
                        FileLogger.Log($"⚔️ {actor.DisplayName} used BASIC attack on {actionResult.Targets.Count} targets! (Damage: {actionResult.DamageDealt.Sum()})", "BATTLE_SIM");
                    }
                }

                // Gain energy from action
                if (actionResult.EnergyGenerated > 0)
                {
                    teamEnergy.GainEnergy(actionResult.EnergyGenerated);

                    if (_enableLogging)
                    {
                        FileLogger.Log($"⚡ {actor.DisplayName}'s team gained {actionResult.EnergyGenerated} energy (now {teamEnergy.Energy})", "BATTLE_SIM");
                    }
                }

                // Remove dead units from timeline
                _turnManager.RemoveDeadUnits();

                // Roll timeline after action
                _turnManager.RollAfterAction(actor);

                // Check if battle ended mid-turn
                var midTurnOutcome = CheckBattleOutcome();
                if (midTurnOutcome != BattleOutcome.InProgress)
                {
                    _outcome = midTurnOutcome;
                    break;
                }
            }

            // Increment turn counter in TurnManager
            _turnManager.IncrementTurn();

            if (_enableLogging)
            {
                FileLogger.Log($"=== TURN {_currentTurn + 1} END ===\n", "BATTLE_SIM");
                LogUnitStatuses();
            }
        }

        /// <summary>
        /// Check if the battle has reached a victory or defeat condition.
        /// </summary>
        private BattleOutcome CheckBattleOutcome()
        {
            bool anyPlayerAlive = _players.Any(u => u.IsAlive);
            bool anyEnemyAlive = _enemies.Any(u => u.IsAlive);

            if (!anyEnemyAlive && anyPlayerAlive)
                return BattleOutcome.Victory;

            if (!anyPlayerAlive && anyEnemyAlive)
                return BattleOutcome.Defeat;

            if (!anyPlayerAlive && !anyEnemyAlive)
                return BattleOutcome.Defeat; // Simultaneous death = defeat

            return BattleOutcome.InProgress;
        }

        /// <summary>
        /// Create the final battle result.
        /// </summary>
        private BattleResult CreateBattleResult()
        {
            return new BattleResult
            {
                Outcome = _outcome,
                TurnsElapsed = _currentTurn,
                SurvivingPlayers = _players.Where(u => u.IsAlive).ToList(),
                SurvivingEnemies = _enemies.Where(u => u.IsAlive).ToList(),
                FinalPlayerEnergy = _playerEnergy.Energy,
                FinalEnemyEnergy = _enemyEnergy.Energy
            };
        }

        /// <summary>
        /// Log current status of all units (for debugging).
        /// </summary>
        private void LogUnitStatuses()
        {
            FileLogger.Log("--- PLAYER STATUS ---", "BATTLE_SIM");
            foreach (var player in _players)
            {
                string status = player.IsAlive
                    ? $"{player.DisplayName}: {player.CurrentHP}/{player.MaxHP} HP"
                    : $"{player.DisplayName}: DEAD";
                FileLogger.Log(status, "BATTLE_SIM");
            }

            FileLogger.Log("--- ENEMY STATUS ---", "BATTLE_SIM");
            foreach (var enemy in _enemies)
            {
                string status = enemy.IsAlive
                    ? $"{enemy.DisplayName}: {enemy.CurrentHP}/{enemy.MaxHP} HP"
                    : $"{enemy.DisplayName}: DEAD";
                FileLogger.Log(status, "BATTLE_SIM");
            }
        }
    }
}
