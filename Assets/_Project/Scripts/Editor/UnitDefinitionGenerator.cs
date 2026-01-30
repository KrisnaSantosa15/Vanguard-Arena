#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Project.Domain;
using System.Collections.Generic;

namespace Project.Editor
{
    /// <summary>
    /// Editor tool to generate varied and balanced unit definitions
    /// </summary>
    public class UnitDefinitionGenerator : EditorWindow
    {
        private GameObject unitPrefab;

        [MenuItem("Project/Tools/Unit Generator")]
        private static void ShowWindow()
        {
            var window = GetWindow<UnitDefinitionGenerator>();
            window.titleContent = new GUIContent("Unit Generator");
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Unit Definition Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            unitPrefab = EditorGUILayout.ObjectField("Unit Prefab", unitPrefab, typeof(GameObject), false) as GameObject;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This will generate all 16 unique character definitions.\n\n" +
                "Units will be created in Assets/_Project/Data/Units/Generated/",
                MessageType.Info);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate All Units (16)", GUILayout.Height(40)))
            {
                GenerateUnits();
            }
        }

        private void GenerateUnits()
        {
            if (unitPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Unit Prefab!", "OK");
                return;
            }

            string outputPath = "Assets/_Project/Data/Units/Generated";
            if (!AssetDatabase.IsValidFolder(outputPath))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Data/Units", "Generated");
            }

            // Create element subfolders
            string[] elements = { "Physical", "Tech", "Mystic", "Esper" };
            foreach (var element in elements)
            {
                string elementPath = $"{outputPath}/{element}";
                if (!AssetDatabase.IsValidFolder(elementPath))
                {
                    AssetDatabase.CreateFolder(outputPath, element);
                }
            }

            int generatedCount = 0;
            var unitTemplates = GetUnitTemplates();

            foreach (var template in unitTemplates)
            {
                // Create element-specific path
                string elementFolder = $"{outputPath}/{template.element}";
                var unit = CreateUnitFromTemplate(template, elementFolder);
                if (unit != null)
                {
                    generatedCount++;
                    // Note: Editor scripts use FileLogger.Log for Unity Editor console
                    FileLogger.Log($"Generated: {unit.DisplayName} ({unit.Element})");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", $"Generated {generatedCount} unique characters!\n\nOrganized by element in:\nAssets/_Project/Data/Units/Generated/[Element]/", "OK");
        }

        private UnitDefinitionSO CreateUnitFromTemplate(UnitTemplate template, string path)
        {
            var unit = ScriptableObject.CreateInstance<UnitDefinitionSO>();

            // Identity - Each unit is unique with its own name
            unit.Id = $"unit_{template.uniqueName.ToLower().Replace(" ", "_")}";
            unit.DisplayName = template.uniqueName;
            
            // Classification
            unit.Type = template.type;
            unit.Role = template.role;
            unit.Element = template.element;
            unit.Rarity = template.rarity;
            unit.IsBoss = template.isBoss;

            // Base Stats (already balanced per unit)
            unit.HP = template.baseHP;
            unit.ATK = template.baseATK;
            unit.DEF = template.baseDEF;
            unit.SPD = template.baseSPD;

            // Crit Stats
            unit.CritRate = template.critRate;
            unit.CritDamage = template.critDamage;

            // Basic Attack
            unit.BasicHitsMin = template.basicHitsMin;
            unit.BasicHitsMax = template.basicHitsMax;
            unit.BasicTargetPattern = template.basicPattern;
            unit.BasicSkillDescription = template.basicDescription;

            // Ultimate
            unit.UltimateEnergyCost = template.ultimateCost;
            unit.UltimateCooldownTurns = template.ultimateCooldown;
            unit.UltimateTargetPattern = template.ultimatePattern;
            unit.UltimateSkillDescription = template.ultimateDescription;

            // Passive
            unit.PassiveName = template.passiveName;
            unit.PassiveDescription = template.passiveDescription;
            unit.Passive = template.passive;

            // Prefab
            unit.UnitViewPrefab = unitPrefab;

            // Save with unique name
            string fileName = $"Unit_{template.uniqueName.Replace(" ", "")}.asset";
            string fullPath = $"{path}/{fileName}";
            
            AssetDatabase.CreateAsset(unit, fullPath);
            return unit;
        }

        private List<UnitTemplate> GetUnitTemplates()
        {
            var templates = new List<UnitTemplate>();

            // === HEROES (Player Units) === //
            // Inspired by anime/fantasy naming conventions with balanced stats
            
            // Tank - Immovable Shield
            templates.Add(new UnitTemplate
            {
                uniqueName = "Aegis",
                className = "Guardian",
                type = UnitType.Melee,
                role = UnitRole.Tank,
                element = UnitElement.Physical,
                rarity = 3,
                baseHP = 150,
                baseATK = 8,
                baseDEF = 15,
                baseSPD = 8,
                critRate = 3f,
                critDamage = 130f,
                basicHitsMin = 1,
                basicHitsMax = 1,
                basicPattern = TargetPattern.SingleEnemy,
                basicDescription = "Strikes a single enemy with a defensive blow.",
                ultimateCost = 3,
                ultimateCooldown = 3,
                ultimatePattern = TargetPattern.AllAllies,
                ultimateDescription = "Raises shields, protecting all allies from harm.",
                passiveName = "Protective Shield",
                passiveDescription = "At battle start, grants shield to all allies equal to 20% of max HP.",
                passive = new PassiveAbility
                {
                    Type = PassiveType.OnBattleStart,
                    EffectType = StatusEffectType.Shield,
                    Value = 30,
                    TargetAllies = true,
                    Duration = 99
                }
            });

            // DPS - Lightning Blade
            templates.Add(new UnitTemplate
            {
                uniqueName = "Raiden",
                className = "Striker",
                type = UnitType.Melee,
                role = UnitRole.DPS,
                element = UnitElement.Esper,
                rarity = 4,
                baseHP = 100,
                baseATK = 18,
                baseDEF = 6,
                baseSPD = 14,
                critRate = 15f,
                critDamage = 200f,
                basicHitsMin = 2,
                basicHitsMax = 3,
                basicPattern = TargetPattern.SingleEnemy,
                basicDescription = "Lightning-fast combo that strikes 2-3 times in quick succession.",
                ultimateCost = 2,
                ultimateCooldown = 2,
                ultimatePattern = TargetPattern.SingleEnemy,
                ultimateDescription = "Thunder Strike - Channels lightning to deliver a devastating blow to a single enemy.",
                passiveName = "Combo Master",
                passiveDescription = "After dealing damage, gain +30% ATK for 2 turns.",
                passive = new PassiveAbility
                {
                    Type = PassiveType.OnDamageDealt,
                    EffectType = StatusEffectType.ATKUp,
                    Modifier = 0.3f,
                    TargetSelf = true,
                    Duration = 2
                }
            });

            // Support - Divine Healer
            templates.Add(new UnitTemplate
            {
                uniqueName = "Lumina",
                className = "Medic",
                type = UnitType.Ranged,
                role = UnitRole.Support,
                element = UnitElement.Esper,
                rarity = 3,
                baseHP = 95,
                baseATK = 9,
                baseDEF = 8,
                baseSPD = 12,
                critRate = 5f,
                critDamage = 140f,
                basicHitsMin = 1,
                basicHitsMax = 1,
                basicPattern = TargetPattern.SingleEnemy,
                basicDescription = "Fires a light bolt at a single enemy.",
                ultimateCost = 2,
                ultimateCooldown = 2,
                ultimatePattern = TargetPattern.AllAllies,
                ultimateDescription = "Divine Blessing - Heals and shields all allies, restoring their fighting spirit.",
                passiveName = "Emergency Heal",
                passiveDescription = "When an ally falls below 30% HP, grants them a shield for 2 turns.",
                passive = new PassiveAbility
                {
                    Type = PassiveType.OnAllyLowHP,
                    TriggerThreshold = 0.3f,
                    EffectType = StatusEffectType.Shield,
                    Value = 25,
                    TargetAllies = true,
                    Duration = 2
                }
            });

            // DPS - Shadow Assassin
            templates.Add(new UnitTemplate
            {
                uniqueName = "Kaito",
                className = "Assassin",
                type = UnitType.Melee,
                role = UnitRole.DPS,
                element = UnitElement.Physical,
                rarity = 4,
                baseHP = 90,
                baseATK = 20,
                baseDEF = 5,
                baseSPD = 17,
                critRate = 25f,
                critDamage = 220f,
                basicHitsMin = 1,
                basicHitsMax = 2,
                basicPattern = TargetPattern.LowestHpEnemy,
                basicDescription = "Shadow Strike - Targets the weakest enemy with precise strikes, hitting 1-2 times.",
                ultimateCost = 2,
                ultimateCooldown = 2,
                ultimatePattern = TargetPattern.LowestHpEnemy,
                ultimateDescription = "Phantom Execution - Emerges from shadows to deliver a lethal blow to the most wounded enemy.",
                passiveName = "Execute",
                passiveDescription = "On kill, immediately gain +50% ATK for 1 turn.",
                passive = new PassiveAbility
                {
                    Type = PassiveType.OnKill,
                    EffectType = StatusEffectType.ATKUp,
                    Modifier = 0.5f,
                    TargetSelf = true,
                    Duration = 1
                }
            });

            // Control - Archmage
            templates.Add(new UnitTemplate
            {
                uniqueName = "Merlin",
                className = "Mage",
                type = UnitType.Ranged,
                role = UnitRole.Control,
                element = UnitElement.Mystic,
                rarity = 3,
                baseHP = 85,
                baseATK = 15,
                baseDEF = 6,
                baseSPD = 11,
                critRate = 10f,
                critDamage = 160f,
                basicHitsMin = 1,
                basicHitsMax = 1,
                basicPattern = TargetPattern.SingleEnemy,
                basicDescription = "Arcane Bolt - Fires a concentrated magical projectile at a single enemy.",
                ultimateCost = 3,
                ultimateCooldown = 3,
                ultimatePattern = TargetPattern.AllEnemies,
                ultimateDescription = "Inferno Catastrophe - Unleashes a devastating firestorm that engulfs all enemies in flames.",
                passiveName = "Burn Master",
                passiveDescription = "Ultimate applies Burn to all enemies for 3 turns.",
                passive = new PassiveAbility
                {
                    Type = PassiveType.OnUltimate,
                    EffectType = StatusEffectType.Burn,
                    Value = 10,
                    Duration = 3
                }
            });

            // Tank - Holy Paladin
            templates.Add(new UnitTemplate
            {
                uniqueName = "Aurelius",
                className = "Paladin",
                type = UnitType.Melee,
                role = UnitRole.Tank,
                element = UnitElement.Esper,
                rarity = 4,
                baseHP = 165,
                baseATK = 10,
                baseDEF = 18,
                baseSPD = 7,
                critRate = 5f,
                critDamage = 140f,
                basicHitsMin = 1,
                basicHitsMax = 1,
                basicPattern = TargetPattern.SingleEnemy,
                basicDescription = "Holy Strike - Channels divine energy into a righteous blow against a single foe.",
                ultimateCost = 3,
                ultimateCooldown = 3,
                ultimatePattern = TargetPattern.Self,
                ultimateDescription = "Divine Fortress - Wraps himself in radiant holy armor, becoming nearly indestructible.",
                passiveName = "Last Stand",
                passiveDescription = "When HP falls below 25%, gain +100% DEF for 2 turns.",
                passive = new PassiveAbility
                {
                    Type = PassiveType.OnHPThreshold,
                    TriggerThreshold = 0.25f,
                    EffectType = StatusEffectType.DEFUp,
                    Modifier = 1.0f,
                    TargetSelf = true,
                    Duration = 2
                }
            });

            // Support - Battle Enchanter  
            templates.Add(new UnitTemplate
            {
                uniqueName = "Elara",
                className = "Enchanter",
                type = UnitType.Ranged,
                role = UnitRole.Support,
                element = UnitElement.Mystic,
                rarity = 2,
                baseHP = 80,
                baseATK = 8,
                baseDEF = 7,
                baseSPD = 13,
                critRate = 5f,
                critDamage = 140f,
                basicHitsMin = 1,
                basicHitsMax = 1,
                basicPattern = TargetPattern.SingleEnemy,
                basicDescription = "Enchanted Arrow - Fires a magic-infused projectile at a single enemy.",
                ultimateCost = 2,
                ultimateCooldown = 2,
                ultimatePattern = TargetPattern.AllAllies,
                ultimateDescription = "Battle Hymn - Chants an empowering spell that strengthens all allies for combat.",
                passiveName = "Combat Boost",
                passiveDescription = "At turn start, grant random ally +20% ATK for 1 turn.",
                passive = new PassiveAbility
                {
                    Type = PassiveType.OnTurnStart,
                    EffectType = StatusEffectType.ATKUp,
                    Modifier = 0.2f,
                    TargetAllies = true,
                    Duration = 1
                }
            });

            // === ENEMIES === //
            // Balanced stats for challenging encounters
            
            // DPS - Agile Thief
            templates.Add(new UnitTemplate
            {
                uniqueName = "Kuro",
                className = "Bandit",
                type = UnitType.Melee,
                role = UnitRole.DPS,
                element = UnitElement.Physical,
                rarity = 1,
                isEnemy = true,
                baseHP = 75,
                baseATK = 12,
                baseDEF = 4,
                baseSPD = 13,
                critRate = 8f,
                critDamage = 150f,
                basicHitsMin = 1,
                basicHitsMax = 2,
                basicPattern = TargetPattern.SingleEnemy,
                basicDescription = "Dual Slash - Swift knife attacks that can strike 1-2 times in rapid succession.",
                ultimateCost = 2,
                ultimateCooldown = 2,
                ultimatePattern = TargetPattern.SingleEnemy,
                ultimateDescription = "Thieves' Rush - A lightning-fast assault combining speed and ferocity against a single target.",
                passiveName = "Quick Strike",
                passiveDescription = "First attack in battle deals +20% damage.",
                passive = new PassiveAbility
                {
                    Type = PassiveType.OnBattleStart,
                    EffectType = StatusEffectType.ATKUp,
                    Modifier = 0.2f,
                    TargetSelf = true,
                    Duration = 1
                }
            });

            // Tank - Mountain Tank
            templates.Add(new UnitTemplate
            {
                uniqueName = "Gorok",
                className = "Bruiser",
                type = UnitType.Melee,
                role = UnitRole.Tank,
                element = UnitElement.Physical,
                rarity = 2,
                isEnemy = true,
                baseHP = 135,
                baseATK = 9,
                baseDEF = 13,
                baseSPD = 6,
                critRate = 3f,
                critDamage = 130f,
                basicHitsMin = 1,
                basicHitsMax = 1,
                basicPattern = TargetPattern.SingleEnemy,
                basicDescription = "Heavy Smash - Crushes a single enemy with overwhelming brute force.",
                ultimateCost = 3,
                ultimateCooldown = 3,
                ultimatePattern = TargetPattern.AllEnemies,
                ultimateDescription = "Ground Pound - Slams the earth with tremendous force, creating shockwaves that damage all foes.",
                passiveName = "Thick Skin",
                passiveDescription = "When taking damage, gain +15% DEF for 2 turns.",
                passive = new PassiveAbility
                {
                    Type = PassiveType.OnDamageTaken,
                    EffectType = StatusEffectType.DEFUp,
                    Modifier = 0.15f,
                    TargetSelf = true,
                    Duration = 2
                }
            });

            // Support - Dark Priest
            templates.Add(new UnitTemplate
            {
                uniqueName = "Zephyr",
                className = "Shaman",
                type = UnitType.Ranged,
                role = UnitRole.Support,
                element = UnitElement.Mystic,
                rarity = 2,
                isEnemy = true,
                baseHP = 90,
                baseATK = 10,
                baseDEF = 7,
                baseSPD = 10,
                critRate = 5f,
                critDamage = 140f,
                basicHitsMin = 1,
                basicHitsMax = 1,
                basicPattern = TargetPattern.SingleEnemy,
                basicDescription = "Shadow Curse - Hurls dark energy at a single enemy, draining their vitality.",
                ultimateCost = 2,
                ultimateCooldown = 2,
                ultimatePattern = TargetPattern.AllAllies,
                ultimateDescription = "Ritual of Renewal - Performs a dark ritual that channels life force to heal all allies.",
                passiveName = "Dark Blessing",
                passiveDescription = "At turn start, heal all allies for 5% of their max HP.",
                passive = new PassiveAbility
                {
                    Type = PassiveType.OnTurnStart,
                    EffectType = StatusEffectType.Shield,
                    Value = 5,
                    TargetAllies = true,
                    Duration = 1
                }
            });

            // DPS - Eagle-Eye Marksman
            templates.Add(new UnitTemplate
            {
                uniqueName = "Artemis",
                className = "Archer",
                type = UnitType.Ranged,
                role = UnitRole.DPS,
                element = UnitElement.Tech,
                rarity = 2,
                isEnemy = true,
                baseHP = 70,
                baseATK = 15,
                baseDEF = 4,
                baseSPD = 14,
                critRate = 12f,
                critDamage = 170f,
                basicHitsMin = 1,
                basicHitsMax = 1,
                basicPattern = TargetPattern.SingleEnemy,
                basicDescription = "Precision Shot - Takes careful aim and fires a high-powered arrow at a single target.",
                ultimateCost = 2,
                ultimateCooldown = 2,
                ultimatePattern = TargetPattern.AllEnemies,
                ultimateDescription = "Arrow Storm - Fires a barrage of arrows that rain down on all enemies simultaneously.",
                passiveName = "Aimed Shot",
                passiveDescription = "Basic attacks have +10% crit rate against low HP targets.",
                passive = new PassiveAbility
                {
                    Type = PassiveType.OnDamageDealt,
                    EffectType = StatusEffectType.None,
                    Value = 10
                }
            });

            // Control - Death Lord
            templates.Add(new UnitTemplate
            {
                uniqueName = "Mortis",
                className = "Necromancer",
                type = UnitType.Ranged,
                role = UnitRole.Control,
                element = UnitElement.Mystic,
                rarity = 3,
                isEnemy = true,
                baseHP = 85,
                baseATK = 14,
                baseDEF = 6,
                baseSPD = 9,
                critRate = 7f,
                critDamage = 150f,
                basicHitsMin = 1,
                basicHitsMax = 1,
                basicPattern = TargetPattern.SingleEnemy,
                basicDescription = "Death Bolt - Fires a projectile of necrotic energy at a single enemy, weakening their defenses.",
                ultimateCost = 3,
                ultimateCooldown = 3,
                ultimatePattern = TargetPattern.AllEnemies,
                ultimateDescription = "Plague of Undeath - Spreads a virulent curse across all enemies, rotting their armor and flesh.",
                passiveName = "Curse",
                passiveDescription = "Attacks apply -20% DEF debuff to target for 2 turns.",
                passive = new PassiveAbility
                {
                    Type = PassiveType.OnDamageDealt,
                    EffectType = StatusEffectType.DEFDown,
                    Modifier = 0.2f,
                    Duration = 2
                }
            });

            // Tank - Supreme Commander (BOSS)
            templates.Add(new UnitTemplate
            {
                uniqueName = "Draven",
                className = "Warlord",
                type = UnitType.Melee,
                role = UnitRole.Tank,
                element = UnitElement.Tech,
                rarity = 4,
                isEnemy = true,
                isBoss = true,
                baseHP = 210,
                baseATK = 16,
                baseDEF = 20,
                baseSPD = 8,
                critRate = 10f,
                critDamage = 180f,
                basicHitsMin = 1,
                basicHitsMax = 2,
                basicPattern = TargetPattern.SingleEnemy,
                basicDescription = "Tactical Strike - Executes a calculated attack with military precision, hitting 1-2 times.",
                ultimateCost = 3,
                ultimateCooldown = 2,
                ultimatePattern = TargetPattern.AllEnemies,
                ultimateDescription = "Total Assault - Commands a devastating full-scale attack that crushes all opposition.",
                passiveName = "Warlord's Fury",
                passiveDescription = "When HP falls below 50%, gain +50% ATK and +30% DEF for rest of battle.",
                passive = new PassiveAbility
                {
                    Type = PassiveType.OnHPThreshold,
                    TriggerThreshold = 0.5f,
                    EffectType = StatusEffectType.ATKUp,
                    Modifier = 0.5f,
                    TargetSelf = true,
                    Duration = 99
                }
            });

            // DPS - Silent Blade
            templates.Add(new UnitTemplate
            {
                uniqueName = "Vex",
                className = "Rogue",
                type = UnitType.Melee,
                role = UnitRole.DPS,
                element = UnitElement.Tech,
                rarity = 2,
                isEnemy = true,
                baseHP = 65,
                baseATK = 17,
                baseDEF = 3,
                baseSPD = 16,
                critRate = 18f,
                critDamage = 190f,
                basicHitsMin = 2,
                basicHitsMax = 3,
                basicPattern = TargetPattern.SingleEnemy,
                basicDescription = "Blade Flurry - Unleashes a whirlwind of precise cuts, striking 2-3 times with deadly efficiency.",
                ultimateCost = 2,
                ultimateCooldown = 2,
                ultimatePattern = TargetPattern.SingleEnemy,
                ultimateDescription = "Silent Death - Vanishes and reappears behind the target for a lethal critical strike.",
                passiveName = "Backstab",
                passiveDescription = "Critical hits reduce target's DEF by 15% for 2 turns.",
                passive = new PassiveAbility
                {
                    Type = PassiveType.OnDamageDealt,
                    EffectType = StatusEffectType.DEFDown,
                    Modifier = 0.15f,
                    Duration = 2
                }
            });

            // Control - Cursed Sorcerer
            templates.Add(new UnitTemplate
            {
                uniqueName = "Malzahar",
                className = "Warlock",
                type = UnitType.Ranged,
                role = UnitRole.Control,
                element = UnitElement.Mystic,
                rarity = 3,
                isEnemy = true,
                baseHP = 80,
                baseATK = 16,
                baseDEF = 5,
                baseSPD = 11,
                critRate = 8f,
                critDamage = 160f,
                basicHitsMin = 1,
                basicHitsMax = 1,
                basicPattern = TargetPattern.SingleEnemy,
                basicDescription = "Void Spike - Summons a spike of dark energy to pierce a single enemy's soul.",
                ultimateCost = 3,
                ultimateCooldown = 3,
                ultimatePattern = TargetPattern.AllEnemies,
                ultimateDescription = "Call of the Void - Opens a rift to the void, draining the strength from all enemies.",
                passiveName = "Weakening Hex",
                passiveDescription = "Ultimate reduces all enemy ATK by 25% for 2 turns.",
                passive = new PassiveAbility
                {
                    Type = PassiveType.OnUltimate,
                    EffectType = StatusEffectType.ATKDown,
                    Modifier = 0.25f,
                    Duration = 2
                }
            });

            // DPS - Blood Warrior
            templates.Add(new UnitTemplate
            {
                uniqueName = "Ragnar",
                className = "Berserker",
                type = UnitType.Melee,
                role = UnitRole.DPS,
                element = UnitElement.Physical,
                rarity = 3,
                isEnemy = true,
                baseHP = 115,
                baseATK = 19,
                baseDEF = 6,
                baseSPD = 10,
                critRate = 12f,
                critDamage = 200f,
                basicHitsMin = 1,
                basicHitsMax = 2,
                basicPattern = TargetPattern.SingleEnemy,
                basicDescription = "Reckless Assault - Charges forward with wild abandon, striking 1-2 times with brutal force.",
                ultimateCost = 2,
                ultimateCooldown = 2,
                ultimatePattern = TargetPattern.AllEnemies,
                ultimateDescription = "Bloodlust Rampage - Goes berserk and attacks all enemies in a violent frenzy.",
                passiveName = "Rage",
                passiveDescription = "Each turn in combat, gain +10% ATK (stacks up to 5 times).",
                passive = new PassiveAbility
                {
                    Type = PassiveType.OnTurnStart,
                    EffectType = StatusEffectType.ATKUp,
                    Modifier = 0.1f,
                    TargetSelf = true,
                    Duration = 99
                }
            });

            return templates;
        }

        private class UnitTemplate
        {
            public string uniqueName;  // The actual unique character name
            public string className;   // Class type for reference
            public UnitType type;
            public UnitRole role;
            public UnitElement element;
            public int rarity;
            public bool isEnemy;
            public bool isBoss;

            public int baseHP;
            public int baseATK;
            public int baseDEF;
            public int baseSPD;

            public float critRate;
            public float critDamage;

            public int basicHitsMin;
            public int basicHitsMax;
            public TargetPattern basicPattern;
            public string basicDescription;

            public int ultimateCost;
            public int ultimateCooldown;
            public TargetPattern ultimatePattern;
            public string ultimateDescription;

            public string passiveName;
            public string passiveDescription;
            public PassiveAbility passive;
        }
    }
}
#endif
