using UnityEngine;

namespace Project.Domain
{
    public enum UnitType { Melee, Ranged }
    public enum UnitRole { Tank, DPS, Support, Control }
    public enum UnitElement { Physical, Tech, Mystic, Esper }

    [CreateAssetMenu(menuName = "Project/Data/Unit Definition", fileName = "Unit_")]
    public sealed class UnitDefinitionSO : ScriptableObject
    {
        [Header("UI")]
        public Sprite Portrait;

        [Header("Boss")]
        public bool IsBoss;
        public Sprite BossTypeIcon; // optional, can be null

        [Header("Identity")]
        public string Id;              // e.g. "unit_guardian_001"
        public string DisplayName;     // MVP: plain string (later loc key)

        [Header("Classification")]
        public UnitType Type = UnitType.Melee; // Melee or Ranged
        public UnitRole Role;
        public UnitElement Element;
        public int Rarity;             // MVP: 1..4 (later enum)

        [Header("Base Stats")]
        public int HP = 100;
        public int ATK = 10;
        public int DEF = 5;
        public int SPD = 10;

        [Header("Crit Stats")]
        [Range(0f, 100f)]
        [Tooltip("Critical hit chance percentage (0-100). Default: 5%")]
        public float CritRate = 5f;

        [Range(100f, 300f)]
        [Tooltip("Critical damage multiplier percentage (100-300). Default: 150% = 1.5x damage")]
        public float CritDamage = 150f;

        [Header("Basic (MVP)")]
        [Tooltip("Total hits per Basic Attack. If Min=Max=1, this unit has no combo.")]
        public int BasicHitsMin = 1;

        public int BasicHitsMax = 1;

        [Tooltip("Targeting pattern for Basic Attack")]
        public TargetPattern BasicTargetPattern = TargetPattern.SingleEnemy;

        [TextArea(2, 4)]
        [Tooltip("Description of Basic Attack skill (shown in UI)")]
        public string BasicSkillDescription = "Basic attack that deals damage to a single enemy.";

        [Header("Ultimate (MVP)")]
        public int UltimateEnergyCost = 2;

        [Tooltip("Cooldown in turns after using Ultimate. Typical 2–3.")]
        public int UltimateCooldownTurns = 2;

        [Tooltip("Targeting pattern for Ultimate")]
        public TargetPattern UltimateTargetPattern = TargetPattern.AllEnemies;

        [TextArea(2, 4)]
        [Tooltip("Description of Ultimate skill (shown in UI)")]
        public string UltimateSkillDescription = "Ultimate attack that deals massive damage to all enemies.";

        [Header("Passive (Info Only)")]
        [Tooltip("Passive ability name shown in UI")]
        public string PassiveName = "Passive Ability";

        [TextArea(3, 6)]
        [Tooltip("Passive ability description")]
        public string PassiveDescription = "This unit has a special passive ability.";

        [Tooltip("Actual passive ability implementation")]
        public PassiveAbility Passive = new PassiveAbility();


        [Header("Presentation (MVP)")]
        public GameObject UnitViewPrefab; // drag prefab here for now (later Addressables)
    }
}
