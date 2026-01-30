using UnityEngine.UIElements;
using Project.Domain;

namespace Project.Presentation.UI
{
    public sealed class BattleHudTimelineItem
    {
        public VisualElement Root { get; }
        private readonly Label _label;
        private readonly Label _prefLabel; // Shows B/U/A for Basic/Ultimate/Auto
        private string _currentUnitId;

        public BattleHudTimelineItem()
        {
            Root = new VisualElement();
            Root.AddToClassList("timelineItem");

            _label = new Label("");
            _label.AddToClassList("timelineLabel");
            Root.Add(_label);

            _prefLabel = new Label("");
            _prefLabel.style.fontSize = 9;
            _prefLabel.style.position = Position.Absolute;
            _prefLabel.style.top = 2;
            _prefLabel.style.right = 2;
            _prefLabel.style.color = new UnityEngine.Color(1f, 1f, 1f, 0.8f);
            Root.Add(_prefLabel);

            SetEmpty();
        }

        public void Set(UnitRuntimeState unit, bool isCurrent, ActionPreference? preference = null)
        {
            _currentUnitId = unit?.UnitId;
            var name = string.IsNullOrWhiteSpace(unit.DisplayName) ? "Unit" : unit.DisplayName;
            _label.text = name.Length <= 6 ? name : name.Substring(0, 6);

            if (isCurrent) Root.AddToClassList("timelineItem--current");
            else Root.RemoveFromClassList("timelineItem--current");

            Root.style.opacity = unit.IsEnemy ? 0.85f : 1f;

            // Team colors: Blue for player, Red for enemy
            if (unit.IsEnemy)
            {
                Root.style.backgroundColor = new UnityEngine.Color(0.8f, 0.2f, 0.2f, 0.7f); // Red
                _prefLabel.style.display = DisplayStyle.None; // No preference for enemies
            }
            else
            {
                Root.style.backgroundColor = new UnityEngine.Color(0.2f, 0.4f, 0.8f, 0.7f); // Blue

                // Show preference indicator for player units
                if (preference.HasValue)
                {
                    _prefLabel.style.display = DisplayStyle.Flex;
                    _prefLabel.text = preference.Value switch
                    {
                        ActionPreference.Basic => "B",
                        ActionPreference.Ultimate => "U",
                        ActionPreference.SmartAI => "A",
                        _ => "A"
                    };
                }
                else
                {
                    _prefLabel.style.display = DisplayStyle.None;
                }
            }
        }

        public string GetUnitId() => _currentUnitId;

        public void SetEmpty()
        {
            _label.text = "";
            _currentUnitId = null;
            Root.RemoveFromClassList("timelineItem--current");
            Root.style.opacity = 0.35f;
            _prefLabel.style.display = DisplayStyle.None;
        }
    }
}
