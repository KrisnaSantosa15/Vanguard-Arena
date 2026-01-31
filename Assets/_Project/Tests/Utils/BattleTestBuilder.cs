using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Project.Domain;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VanguardArena.Tests.Utils
{
    /// <summary>
    /// Builder class for creating test battle scenarios programmatically.
    /// Allows creating units with custom configurations without requiring ScriptableObject assets.
    /// </summary>
    public class BattleTestBuilder
    {
        private readonly List<UnitDefinitionSO> _playerUnits = new();
        private readonly List<UnitDefinitionSO> _enemyUnits = new();
        private IDeterministicRandom _rng;
        private int _seed = 12345;

        public BattleTestBuilder WithSeed(int seed)
        {
            _seed = seed;
            return this;
        }

        public BattleTestBuilder WithRandom(IDeterministicRandom rng)
        {
            _rng = rng;
            return this;
        }

        /// <summary>
        /// Adds a player unit with minimal configuration for testing.
        /// </summary>
        public BattleTestBuilder AddPlayerUnit(
            string displayName,
            UnitType type = UnitType.Melee,
            int hp = 100,
            int atk = 10,
            int def = 5,
            int spd = 10,
            float critRate = 5f,
            float critDamage = 150f,
            int basicHitsMin = 1,
            int basicHitsMax = 1,
            TargetPattern basicTargetPattern = TargetPattern.SingleEnemy,
            TargetPattern ultimateTargetPattern = TargetPattern.AllEnemies,
            int ultimateEnergyCost = 2,
            int ultimateCooldown = 2,
            PassiveAbility passive = null)
        {
            var unit = CreateUnitDefinition(
                displayName, type, hp, atk, def, spd, critRate, critDamage,
                basicHitsMin, basicHitsMax, basicTargetPattern, 
                ultimateTargetPattern, ultimateEnergyCost, ultimateCooldown, passive);
            
            _playerUnits.Add(unit);
            return this;
        }

        /// <summary>
        /// Adds an enemy unit with minimal configuration for testing.
        /// </summary>
        public BattleTestBuilder AddEnemyUnit(
            string displayName,
            UnitType type = UnitType.Melee,
            int hp = 100,
            int atk = 10,
            int def = 5,
            int spd = 10,
            float critRate = 5f,
            float critDamage = 150f,
            int basicHitsMin = 1,
            int basicHitsMax = 1,
            TargetPattern basicTargetPattern = TargetPattern.SingleEnemy,
            TargetPattern ultimateTargetPattern = TargetPattern.AllEnemies,
            int ultimateEnergyCost = 2,
            int ultimateCooldown = 2,
            PassiveAbility passive = null)
        {
            var unit = CreateUnitDefinition(
                displayName, type, hp, atk, def, spd, critRate, critDamage,
                basicHitsMin, basicHitsMax, basicTargetPattern,
                ultimateTargetPattern, ultimateEnergyCost, ultimateCooldown, passive);
            
            _enemyUnits.Add(unit);
            return this;
        }

        /// <summary>
        /// Creates runtime states directly for unit testing (bypasses Unity scene).
        /// </summary>
        public (List<UnitRuntimeState> players, List<UnitRuntimeState> enemies, IDeterministicRandom rng) BuildStates()
        {
            if (_rng == null)
            {
                _rng = new SeededRandom(_seed);
            }

            var players = _playerUnits.Select((unit, index) => CreateRuntimeState(unit, index, isEnemy: false)).ToList();
            var enemies = _enemyUnits.Select((unit, index) => CreateRuntimeState(unit, index, isEnemy: true)).ToList();

            return (players, enemies, _rng);
        }

        /// <summary>
        /// Returns the UnitDefinitionSO objects directly (for PlayMode tests that need ScriptableObjects).
        /// </summary>
        public (List<UnitDefinitionSO> players, List<UnitDefinitionSO> enemies) BuildDefinitions()
        {
            return (_playerUnits, _enemyUnits);
        }

        /// <summary>
        /// Adds a passive ability to the last added unit (builder pattern).
        /// </summary>
        public BattleTestBuilder WithPassive(PassiveAbility passive)
        {
            if (_playerUnits.Count > 0 && _enemyUnits.Count == 0)
            {
                // Apply to last player unit
                _playerUnits[_playerUnits.Count - 1].Passive = passive;
            }
            else if (_enemyUnits.Count > 0)
            {
                // Apply to last enemy unit
                _enemyUnits[_enemyUnits.Count - 1].Passive = passive;
            }
            
            return this;
        }

        private UnitDefinitionSO CreateUnitDefinition(
            string displayName,
            UnitType type,
            int hp, int atk, int def, int spd,
            float critRate, float critDamage,
            int basicHitsMin, int basicHitsMax,
            TargetPattern basicTargetPattern,
            TargetPattern ultimateTargetPattern,
            int ultimateEnergyCost,
            int ultimateCooldown,
            PassiveAbility passive)
        {
            var unit = ScriptableObject.CreateInstance<UnitDefinitionSO>();
            unit.DisplayName = displayName;
            unit.Id = displayName.ToLower().Replace(" ", "_");
            unit.Type = type;
            unit.HP = hp;
            unit.ATK = atk;
            unit.DEF = def;
            unit.SPD = spd;
            unit.CritRate = critRate;
            unit.CritDamage = critDamage;
            unit.BasicHitsMin = basicHitsMin;
            unit.BasicHitsMax = basicHitsMax;
            unit.BasicTargetPattern = basicTargetPattern;
            unit.UltimateTargetPattern = ultimateTargetPattern;
            unit.UltimateEnergyCost = ultimateEnergyCost;
            unit.UltimateCooldownTurns = ultimateCooldown;
            unit.Passive = passive ?? new PassiveAbility();
            unit.BasicSkillDescription = "Test basic attack";
            unit.UltimateSkillDescription = "Test ultimate";
            unit.PassiveName = "Test Passive";
            unit.PassiveDescription = "Test passive description";
            
            // Load the default unit prefab for PlayMode tests
            unit.UnitViewPrefab = UnityEngine.Resources.Load<GameObject>("UnitAnimated");
            if (unit.UnitViewPrefab == null)
            {
                // Fallback: try loading from Assets path (for when Resources folder doesn't exist)
                #if UNITY_EDITOR
                unit.UnitViewPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Units/UnitAnimated.prefab");
                #endif
            }
            
            return unit;
        }

        private UnitRuntimeState CreateRuntimeState(UnitDefinitionSO def, int slotIndex, bool isEnemy)
        {
            return new UnitRuntimeState(def, isEnemy, slotIndex);
        }

        /// <summary>
        /// Creates a simple 1v1 melee battle for quick testing.
        /// </summary>
        public static BattleTestBuilder Simple1v1Melee()
        {
            return new BattleTestBuilder()
                .AddPlayerUnit("TestPlayer", UnitType.Melee, hp: 100, atk: 20, def: 5)
                .AddEnemyUnit("TestEnemy", UnitType.Melee, hp: 100, atk: 15, def: 5);
        }

        /// <summary>
        /// Creates a 2v2 battle with one melee and one ranged per side.
        /// </summary>
        public static BattleTestBuilder Simple2v2Mixed()
        {
            return new BattleTestBuilder()
                .AddPlayerUnit("PlayerMelee", UnitType.Melee, hp: 120, atk: 20, def: 8)
                .AddPlayerUnit("PlayerRanged", UnitType.Ranged, hp: 80, atk: 25, def: 3)
                .AddEnemyUnit("EnemyMelee", UnitType.Melee, hp: 110, atk: 18, def: 7)
                .AddEnemyUnit("EnemyRanged", UnitType.Ranged, hp: 70, atk: 22, def: 4);
        }
    }
}
