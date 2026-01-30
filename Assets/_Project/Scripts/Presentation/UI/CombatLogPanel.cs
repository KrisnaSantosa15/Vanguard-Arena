using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Project.Domain;

namespace Project.Presentation.UI
{
    /// <summary>
    /// Combat log panel showing battle events with timestamps
    /// </summary>
    public class CombatLogPanel : MonoBehaviour
    {
        [Header("UI Toolkit")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string panelName = "combatLogPanel";
        [SerializeField] private string scrollViewName = "logScrollView";
        [SerializeField] private string toggleButtonName = "btnToggleLog";

        [Header("Settings")]
        [SerializeField] private int maxLogEntries = 100;
        [SerializeField] private bool startVisible = false;

        private VisualElement _panel;
        private ScrollView _scrollView;
        private Button _toggleButton;
        private bool _isVisible;

        private readonly List<string> _logEntries = new();

        private void Start()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogWarning("[CombatLogPanel] No UIDocument found.");
                return;
            }

            var root = uiDocument.rootVisualElement;

            _panel = root.Q<VisualElement>(panelName);
            _scrollView = root.Q<ScrollView>(scrollViewName);
            _toggleButton = root.Q<Button>(toggleButtonName);

            if (_panel == null || _scrollView == null)
            {
                FileLogger.LogWarning($"Panel '{panelName}' or ScrollView '{scrollViewName}' not found in UI.", "CombatLogPanel");
                return;
            }

            // Setup toggle button
            if (_toggleButton != null)
            {
                _toggleButton.clicked += ToggleVisibility;
            }

            // Set initial visibility
            _isVisible = startVisible;
            _panel.style.display = _isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void AddLog(string message)
        {
            if (_scrollView == null) return;

            // Add timestamp
            string timestamp = $"[{Time.time:F1}s]";
            string entry = $"{timestamp} {message}";

            _logEntries.Add(entry);

            // Trim old entries
            if (_logEntries.Count > maxLogEntries)
            {
                _logEntries.RemoveAt(0);
            }

            // Create label
            var label = new Label(entry);
            label.AddToClassList("log-entry");

            // Add color classes based on content
            if (message.Contains("CRIT"))
                label.AddToClassList("log-crit");
            else if (message.Contains("ULTIMATE") || message.Contains("💥"))
                label.AddToClassList("log-ultimate");
            else if (message.Contains("DEFEATED") || message.Contains("☠️"))
                label.AddToClassList("log-death");
            else if (message.Contains("STUN") || message.Contains("BURN"))
                label.AddToClassList("log-status");

            _scrollView.Add(label);

            // Auto-scroll to bottom
            _scrollView.scrollOffset = new Vector2(0, _scrollView.contentContainer.layout.height);
        }

        public void ToggleVisibility()
        {
            _isVisible = !_isVisible;
            if (_panel != null)
            {
                _panel.style.display = _isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void Show()
        {
            _isVisible = true;
            if (_panel != null)
                _panel.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _isVisible = false;
            if (_panel != null)
                _panel.style.display = DisplayStyle.None;
        }

        public void ClearLog()
        {
            _logEntries.Clear();
            if (_scrollView != null)
                _scrollView.Clear();
        }
    }
}
