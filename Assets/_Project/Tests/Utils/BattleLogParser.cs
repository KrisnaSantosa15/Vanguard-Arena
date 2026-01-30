using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace VanguardArena.Tests.Utils
{
    /// <summary>
    /// Represents a parsed battle log event.
    /// </summary>
    public class BattleLogEvent
    {
        public string Timestamp { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public Dictionary<string, string> Data { get; set; } = new();

        public override string ToString()
        {
            return $"[{Timestamp}] [{Category}] {Message}";
        }
    }

    /// <summary>
    /// Parses battle_debug.log into structured events for test verification.
    /// </summary>
    public class BattleLogParser
    {
        private static readonly Regex LogLineRegex = new Regex(
            @"^\[(?<timestamp>[\d:\.]+)\] \[(?<category>[A-Z\-]+)\] (?<message>.+)$",
            RegexOptions.Compiled
        );

        /// <summary>
        /// Parses a battle log file and returns structured events.
        /// </summary>
        public static List<BattleLogEvent> ParseFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Battle log not found: {filePath}");
            }

            var events = new List<BattleLogEvent>();
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                var match = LogLineRegex.Match(line);
                if (match.Success)
                {
                    var evt = new BattleLogEvent
                    {
                        Timestamp = match.Groups["timestamp"].Value,
                        Category = match.Groups["category"].Value,
                        Message = match.Groups["message"].Value
                    };

                    // Extract additional data based on category
                    ParseEventData(evt);
                    events.Add(evt);
                }
            }

            return events;
        }

        /// <summary>
        /// Parses additional structured data from event messages.
        /// </summary>
        private static void ParseEventData(BattleLogEvent evt)
        {
            switch (evt.Category)
            {
                case "DAMAGE":
                    ParseDamageEvent(evt);
                    break;
                case "HEAL":
                    ParseHealEvent(evt);
                    break;
                case "SHIELD":
                    ParseShieldEvent(evt);
                    break;
                case "ENERGY":
                    ParseEnergyEvent(evt);
                    break;
                case "PASSIVE":
                    ParsePassiveEvent(evt);
                    break;
                case "TURN":
                    ParseTurnEvent(evt);
                    break;
            }
        }

        private static void ParseDamageEvent(BattleLogEvent evt)
        {
            // Example: "Computing damage for Lumina (HP: 95/95)"
            var targetMatch = Regex.Match(evt.Message, @"for (?<target>[A-Za-z]+) \(HP: (?<current>\d+)/(?<max>\d+)\)");
            if (targetMatch.Success)
            {
                evt.Data["Target"] = targetMatch.Groups["target"].Value;
                evt.Data["CurrentHP"] = targetMatch.Groups["current"].Value;
                evt.Data["MaxHP"] = targetMatch.Groups["max"].Value;
            }

            // Example: "Calculated damage: 33 (Crit: False)"
            var damageMatch = Regex.Match(evt.Message, @"damage: (?<damage>\d+) \(Crit: (?<crit>True|False)\)");
            if (damageMatch.Success)
            {
                evt.Data["Damage"] = damageMatch.Groups["damage"].Value;
                evt.Data["Crit"] = damageMatch.Groups["crit"].Value;
            }

            // Example: "HP change: 95 -> 62"
            var hpChangeMatch = Regex.Match(evt.Message, @"HP change: (?<before>\d+) -> (?<after>\d+)");
            if (hpChangeMatch.Success)
            {
                evt.Data["HPBefore"] = hpChangeMatch.Groups["before"].Value;
                evt.Data["HPAfter"] = hpChangeMatch.Groups["after"].Value;
            }
        }

        private static void ParseHealEvent(BattleLogEvent evt)
        {
            // Example: "Healing Aurelius for 25 HP (80/100 -> 100/100)"
            var healMatch = Regex.Match(evt.Message, @"Healing (?<target>[A-Za-z]+) for (?<amount>\d+) HP");
            if (healMatch.Success)
            {
                evt.Data["Target"] = healMatch.Groups["target"].Value;
                evt.Data["Amount"] = healMatch.Groups["amount"].Value;
            }
        }

        private static void ParseShieldEvent(BattleLogEvent evt)
        {
            // Example: "Shield absorbed ALL damage: 30 -> 9 (blocked 21 dmg)"
            var absorbMatch = Regex.Match(evt.Message, @"(?<before>\d+) -> (?<after>\d+) \(blocked (?<blocked>\d+) dmg\)");
            if (absorbMatch.Success)
            {
                evt.Data["ShieldBefore"] = absorbMatch.Groups["before"].Value;
                evt.Data["ShieldAfter"] = absorbMatch.Groups["after"].Value;
                evt.Data["DamageBlocked"] = absorbMatch.Groups["blocked"].Value;
            }
        }

        private static void ParseEnergyEvent(BattleLogEvent evt)
        {
            // Example: "Energy gained: 2 -> 3 (+1)"
            // Example: "Energy spent: 2 -> 0 (-2)"
            var energyMatch = Regex.Match(evt.Message, @"(?<before>\d+) -> (?<after>\d+) \((?<change>[+-]\d+)\)");
            if (energyMatch.Success)
            {
                evt.Data["EnergyBefore"] = energyMatch.Groups["before"].Value;
                evt.Data["EnergyAfter"] = energyMatch.Groups["after"].Value;
                evt.Data["EnergyChange"] = energyMatch.Groups["change"].Value;
            }
        }

        private static void ParsePassiveEvent(BattleLogEvent evt)
        {
            // Example: "🌟 Aegis triggers 'Protective Shield' (OnBattleStart)"
            var triggerMatch = Regex.Match(evt.Message, @"(?<unit>[A-Za-z]+) triggers '(?<passive>[^']+)' \((?<type>[^)]+)\)");
            if (triggerMatch.Success)
            {
                evt.Data["Unit"] = triggerMatch.Groups["unit"].Value;
                evt.Data["Passive"] = triggerMatch.Groups["passive"].Value;
                evt.Data["Type"] = triggerMatch.Groups["type"].Value;
            }

            // Example: "Targeting 6 allies: Aurelius, Elara, Malzahar"
            var targetingMatch = Regex.Match(evt.Message, @"Targeting (?<count>\d+) (?<type>[a-z]+): (?<targets>.+)");
            if (targetingMatch.Success)
            {
                evt.Data["TargetCount"] = targetingMatch.Groups["count"].Value;
                evt.Data["TargetType"] = targetingMatch.Groups["type"].Value;
                evt.Data["Targets"] = targetingMatch.Groups["targets"].Value;
            }
        }

        private static void ParseTurnEvent(BattleLogEvent evt)
        {
            // Example: "Unit: Kaito, IsEnemy: True, HP: 90/90, Energy: 0"
            var unitMatch = Regex.Match(evt.Message, @"Unit: (?<unit>[A-Za-z]+), IsEnemy: (?<enemy>True|False), HP: (?<hp>\d+/\d+), Energy: (?<energy>\d+)");
            if (unitMatch.Success)
            {
                evt.Data["Unit"] = unitMatch.Groups["unit"].Value;
                evt.Data["IsEnemy"] = unitMatch.Groups["enemy"].Value;
                evt.Data["HP"] = unitMatch.Groups["hp"].Value;
                evt.Data["Energy"] = unitMatch.Groups["energy"].Value;
            }
        }

        #region Query Helpers

        /// <summary>
        /// Checks if the log contains a specific event type with matching criteria.
        /// </summary>
        public static bool ContainsEvent(List<BattleLogEvent> events, string category, string messagePattern = null)
        {
            return events.Any(e => e.Category == category && 
                (messagePattern == null || e.Message.Contains(messagePattern)));
        }

        /// <summary>
        /// Counts how many times a passive targeted units.
        /// </summary>
        public static int CountPassiveTargets(List<BattleLogEvent> events, string passiveName)
        {
            var passiveEvents = events.Where(e => 
                e.Category == "PASSIVE" && 
                e.Data.ContainsKey("Passive") && 
                e.Data["Passive"] == passiveName &&
                e.Data.ContainsKey("TargetCount")
            ).ToList();

            if (passiveEvents.Any())
            {
                return int.Parse(passiveEvents.First().Data["TargetCount"]);
            }

            return 0;
        }

        /// <summary>
        /// Gets all events of a specific category.
        /// </summary>
        public static List<BattleLogEvent> GetEvents(List<BattleLogEvent> events, string category)
        {
            return events.Where(e => e.Category == category).ToList();
        }

        /// <summary>
        /// Gets the turn order from the log.
        /// </summary>
        public static List<string> GetTurnOrder(List<BattleLogEvent> events)
        {
            var timelineEvent = events.FirstOrDefault(e => e.Category == "TURN" && e.Message.Contains("Timeline:"));
            if (timelineEvent != null)
            {
                var match = Regex.Match(timelineEvent.Message, @"Timeline: (.+)");
                if (match.Success)
                {
                    return match.Groups[1].Value.Split(" -> ").Select(s => s.Trim()).ToList();
                }
            }

            return new List<string>();
        }

        #endregion
    }
}
