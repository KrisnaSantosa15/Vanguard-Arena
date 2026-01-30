namespace Project.Domain
{
    public enum TargetPattern
    {
        // Single target
        SingleEnemy,
        SingleAlly,
        Self,

        // Row-based (for 3v3/6v6)
        FrontRowSingle,     // Choose 1 from front row
        BackRowSingle,      // Choose 1 from back row
        FrontRowAll,        // Hit all front row
        BackRowAll,         // Hit all back row

        // Area
        AllEnemies,
        AllAllies,

        // Column (for 6v6 later)
        ColumnSingle,       // Choose column (hits front+back)
        ColumnAll,          // All in that column

        // Smart targeting
        LowestHpEnemy,
        HighestThreatEnemy
    }

    public enum PlayerActionType
    {
        None,
        Basic,
        Ultimate
    }

    public enum BattlePhase
    {
        AutoResolve,
        WaitingForPlayerAction,
        WaitingForTargetSelection,
        ExecutingAction
    }

    /// <summary>
    /// Per-unit action preference for auto mode.
    /// </summary>
    public enum ActionPreference
    {
        SmartAI,    // AI decides based on conditions (default)
        Basic,      // Always use Basic attack
        Ultimate    // Always use Ultimate (if available)
    }

    /// <summary>
    /// Helper class for TargetPattern utilities.
    /// </summary>
    public static class TargetPatternHelper
    {
        /// <summary>
        /// Returns true if the pattern targets allies or self (should heal/buff instead of damage).
        /// </summary>
        public static bool IsAllyTargeting(TargetPattern pattern)
        {
            switch (pattern)
            {
                case TargetPattern.Self:
                case TargetPattern.SingleAlly:
                case TargetPattern.AllAllies:
                    return true;
                default:
                    return false;
            }
        }
    }
}
