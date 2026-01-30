using System.Collections.Generic;
using System.Linq;

namespace Project.Domain
{
    /// <summary>
    /// Manages the turn order, timeline rolling, and filtering of units.
    /// </summary>
    public class TurnManager
    {
        private readonly List<UnitRuntimeState> _rollingTimeline = new();
        private int _turnCounter = 1;

        public int CurrentTurn => _turnCounter;
        public List<UnitRuntimeState> Timeline => _rollingTimeline;

        public UnitRuntimeState CurrentActor => _rollingTimeline.Count > 0 ? _rollingTimeline[0] : null;

        public void IncrementTurn()
        {
            _turnCounter++;
        }

        public void RebuildTimeline(List<UnitRuntimeState> allUnits)
        {
            _rollingTimeline.Clear();

            var alive = allUnits
                .Where(u => u != null && u.IsAlive)
                .OrderByDescending(u => u.SPD)
                .ToList();

            _rollingTimeline.AddRange(alive);
        }

        public void RollAfterAction(UnitRuntimeState actor)
        {
            if (_rollingTimeline.Count > 0 && _rollingTimeline[0] == actor)
                _rollingTimeline.RemoveAt(0);
            else
                _rollingTimeline.Remove(actor);

            if (actor != null && actor.IsAlive)
                _rollingTimeline.Add(actor);
        }

        public void RemoveDeadUnits()
        {
            for (int i = _rollingTimeline.Count - 1; i >= 0; i--)
            {
                var u = _rollingTimeline[i];
                if (u == null || !u.IsAlive)
                    _rollingTimeline.RemoveAt(i);
            }
        }
    }
}
