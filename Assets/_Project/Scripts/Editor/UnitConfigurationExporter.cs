using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using Project.Domain;

namespace Project.Editor
{
    public static class UnitConfigurationExporter
    {
        [MenuItem("Tools/Export Unit Configurations")]
        public static void ExportUnitConfigs()
        {
            // Find all UnitDefinitionSO assets
            string[] guids = AssetDatabase.FindAssets("t:UnitDefinitionSO");
            var units = guids
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<UnitDefinitionSO>(path))
                .Where(unit => unit != null)
                .OrderBy(unit => unit.Element)
                .ThenBy(unit => unit.DisplayName)
                .ToList();

            if (units.Count == 0)
            {
                Debug.LogWarning("No UnitDefinitionSO assets found!");
                return;
            }

            var output = new StringBuilder();
            output.AppendLine("# Vanguard Arena - Unit Configuration Export");
            output.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            output.AppendLine($"Total Units: {units.Count}");
            output.AppendLine();
            output.AppendLine("---");
            output.AppendLine();

            // Group by element
            var groupedByElement = units.GroupBy(u => u.Element);

            foreach (var group in groupedByElement)
            {
                output.AppendLine($"## {group.Key} Units");
                output.AppendLine();

                foreach (var unit in group)
                {
                    output.AppendLine($"### {unit.DisplayName}");
                    output.AppendLine();

                    // Identity
                    output.AppendLine("**Identity:**");
                    output.AppendLine($"- ID: `{unit.Id}`");
                    output.AppendLine($"- Element: {unit.Element}");
                    output.AppendLine($"- Role: {unit.Role}");
                    output.AppendLine($"- Type: {unit.Type}");
                    output.AppendLine($"- Rarity: {unit.Rarity} ⭐");
                    output.AppendLine($"- Boss: {(unit.IsBoss ? "Yes" : "No")}");
                    output.AppendLine();

                    // Stats
                    output.AppendLine("**Base Stats:**");
                    output.AppendLine($"- HP: {unit.HP}");
                    output.AppendLine($"- ATK: {unit.ATK}");
                    output.AppendLine($"- DEF: {unit.DEF}");
                    output.AppendLine($"- SPD: {unit.SPD}");
                    output.AppendLine($"- Crit Rate: {unit.CritRate}%");
                    output.AppendLine($"- Crit Damage: {unit.CritDamage}%");
                    output.AppendLine();

                    // Basic Attack
                    output.AppendLine("**Basic Attack:**");
                    output.AppendLine($"- Target: {unit.BasicTargetPattern}");
                    output.AppendLine($"- Hits: {unit.BasicHitsMin}-{unit.BasicHitsMax}");
                    output.AppendLine($"- Description: _{unit.BasicSkillDescription}_");
                    output.AppendLine();

                    // Ultimate
                    output.AppendLine("**Ultimate:**");
                    output.AppendLine($"- Target: {unit.UltimateTargetPattern}");
                    output.AppendLine($"- Energy Cost: {unit.UltimateEnergyCost}");
                    output.AppendLine($"- Cooldown: {unit.UltimateCooldownTurns} turns");
                    output.AppendLine($"- Description: _{unit.UltimateSkillDescription}_");
                    output.AppendLine();

                    // Passive
                    output.AppendLine("**Passive:**");
                    output.AppendLine($"- Name: **{unit.PassiveName}**");
                    output.AppendLine($"- Type: {unit.Passive.Type}");
                    if (unit.Passive.Type != PassiveType.None)
                    {
                        output.AppendLine($"- Effect: {unit.Passive.EffectType}");
                        output.AppendLine($"- Value: {unit.Passive.Value}");
                        output.AppendLine($"- Modifier: {unit.Passive.Modifier:P0}");
                        output.AppendLine($"- Duration: {unit.Passive.Duration} turns");
                        output.AppendLine($"- Target Self: {unit.Passive.TargetSelf}");
                        output.AppendLine($"- Target Allies: {unit.Passive.TargetAllies}");
                        
                        if (unit.Passive.TriggerThreshold > 0)
                            output.AppendLine($"- Threshold: {unit.Passive.TriggerThreshold:P0}");
                    }
                    output.AppendLine($"- Description: _{unit.PassiveDescription}_");
                    output.AppendLine();

                    output.AppendLine("---");
                    output.AppendLine();
                }
            }

            // Summary Table
            output.AppendLine("## Summary Table");
            output.AppendLine();
            output.AppendLine("| Unit | Type | Role | Basic Target | Ultimate Target | Passive Type | Status Effect |");
            output.AppendLine("|------|------|------|-------------|-----------------|--------------|---------------|");
            
            foreach (var unit in units)
            {
                output.AppendLine($"| {unit.DisplayName} | {unit.Type} | {unit.Role} | {unit.BasicTargetPattern} | {unit.UltimateTargetPattern} | {unit.Passive.Type} | {unit.Passive.EffectType} |");
            }

            output.AppendLine();
            output.AppendLine("---");
            output.AppendLine();

            // Test Coverage Matrix
            output.AppendLine("## Test Coverage Matrix");
            output.AppendLine();
            output.AppendLine("### Unit Types");
            var meleeCount = units.Count(u => u.Type == UnitType.Melee);
            var rangedCount = units.Count(u => u.Type == UnitType.Ranged);
            output.AppendLine($"- Melee: {meleeCount} units");
            output.AppendLine($"- Ranged: {rangedCount} units");
            output.AppendLine();

            output.AppendLine("### Passive Types Coverage");
            var passiveGroups = units.GroupBy(u => u.Passive.Type).OrderBy(g => g.Key);
            foreach (var pg in passiveGroups)
            {
                var unitNames = string.Join(", ", pg.Select(u => u.DisplayName));
                output.AppendLine($"- **{pg.Key}**: {pg.Count()} units ({unitNames})");
            }
            output.AppendLine();

            output.AppendLine("### Status Effects Coverage");
            var effectGroups = units.Where(u => u.Passive.EffectType != StatusEffectType.None)
                                    .GroupBy(u => u.Passive.EffectType)
                                    .OrderBy(g => g.Key);
            foreach (var eg in effectGroups)
            {
                var unitNames = string.Join(", ", eg.Select(u => u.DisplayName));
                output.AppendLine($"- **{eg.Key}**: {eg.Count()} units ({unitNames})");
            }

            // Write to file
            string path = Path.Combine(Application.dataPath, "..", "UNIT_CONFIGURATIONS.md");
            File.WriteAllText(path, output.ToString());
            
            Debug.Log($"✅ Exported {units.Count} unit configurations to: {path}");
            
            // Ping the file in Project window
            AssetDatabase.Refresh();
        }
    }
}
