using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Project.Domain;

namespace Project.Presentation.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class BattleHudController : MonoBehaviour
    {
        [Header("Timeline")]
        [SerializeField, Range(3, 12)] private int timelineSlotsCount = 6;

        // --- UI refs (UI Toolkit) ---
        [Header("Combat Log (Optional)")]
        [SerializeField] private CombatLogPanel combatLogPanel;

        private UIDocument _doc;
        private VisualElement _root;

        private Button _btnPause;
        private Button _btnSpeed;
        private Button _btnAuto;

        private Label _lblTurn;

        private Label _bossName;
        private ProgressBar _bossHpBar;

        private Label _lblEnergy;
        private ProgressBar _energyBar;

        private VisualElement _timelineStrip;

        private VisualElement _pauseOverlay;
        private Button _btnResume;

        // --- Passive Info Modal ---
        private VisualElement _passiveOverlay;
        private Label _lblPassiveTitle;
        private Label _lblPassiveDesc;
        private Button _btnClosePassive;

        // --- Skill Info Modal ---
        private VisualElement _skillOverlay;
        private Label _lblSkillTitle;
        private Label _lblSkillDesc;
        private Button _btnCloseSkill;

        // --- Manual action UI (optional; add to UXML later) ---
        private VisualElement _actionPanel;
        private Button _btnBasic;
        private Button _btnUlt;
        private Button _btnPassive;
        private Label _lblActionHint; // optional

        // --- Auto mode squad avatars ---
        private VisualElement _squadAvatarPanel;
        private readonly List<VisualElement> _squadAvatarItems = new();

        // --- Current actor for passive info ---
        private UnitRuntimeState _currentActor;

        // --- Data providers from BattleController ---
        private int _maxEnergy;

        private Func<int> _getRound;
        private Func<int> _getRoundMax;

        private Func<bool> _getAuto;
        private Action<bool> _setAuto;

        private Func<TeamEnergyState> _getPlayerEnergy;
        private Func<TeamEnergyState> _getEnemyEnergy;

        private Func<UnitRuntimeState> _getBoss;

        private Func<List<UnitRuntimeState>> _getRollingTimeline;
        private Func<UnitRuntimeState> _getCurrentActor;
        private Func<string, ActionPreference> _getUnitPreference;
        private Func<List<UnitRuntimeState>> _getPlayerSquad;

        // --- Timeline runtime items ---
        private readonly List<BattleHudTimelineItem> _timelineItems = new();

        // --- Events to BattleController ---
        public event Action OnBasicClicked;
        public event Action OnUltClicked;
        public event Action<string> OnSquadAvatarClicked; // For auto mode preference toggle

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;
            FileLogger.Log($"HUD root children={_root.childCount} names: " +
          string.Join(", ", _root.Children().Select(c => c.name)));


            // Query by names from your BattleHUD.uxml
            _btnPause = _root.Q<Button>("btnPause");
            _btnSpeed = _root.Q<Button>("btnSpeed");
            _btnAuto = _root.Q<Button>("btnAuto");

            _lblTurn = _root.Q<Label>("lblTurn");

            _bossName = _root.Q<Label>("bossName");
            _bossHpBar = _root.Q<ProgressBar>("bossHpBar");

            _lblEnergy = _root.Q<Label>("lblEnergy");
            _energyBar = _root.Q<ProgressBar>("energyBar");

            _timelineStrip = _root.Q<VisualElement>("timelineStrip");

            _pauseOverlay = _root.Q<VisualElement>("pauseOverlay");
            _btnResume = _root.Q<Button>("btnResume");

            // Passive modal
            _passiveOverlay = _root.Q<VisualElement>("passiveOverlay");
            _lblPassiveTitle = _root.Q<Label>("lblPassiveTitle");
            _lblPassiveDesc = _root.Q<Label>("lblPassiveDesc");
            _btnClosePassive = _root.Q<Button>("btnClosePassive");

            // Skill info modal
            _skillOverlay = _root.Q<VisualElement>("skillOverlay");
            _lblSkillTitle = _root.Q<Label>("lblSkillTitle");
            _lblSkillDesc = _root.Q<Label>("lblSkillDesc");
            _btnCloseSkill = _root.Q<Button>("btnCloseSkill");

            // Create skill overlay dynamically if not in UXML
            if (_skillOverlay == null)
            {
                CreateSkillOverlay();
            }

            // Optional manual controls (you will add these later in UXML)
            _actionPanel = _root.Q<VisualElement>("actionPanel");
            _btnBasic = _root.Q<Button>("btnBasic");
            _btnUlt = _root.Q<Button>("btnUlt");
            _btnPassive = _root.Q<Button>("btnPassive");
            _lblActionHint = _root.Q<Label>("lblActionHint");

            // Auto mode squad avatars
            _squadAvatarPanel = _root.Q<VisualElement>("squadAvatarPanel");
            if (_squadAvatarPanel == null)
            {
                // Create it dynamically if not in UXML
                _squadAvatarPanel = new VisualElement();
                _squadAvatarPanel.name = "squadAvatarPanel";
                _squadAvatarPanel.style.position = Position.Absolute;
                _squadAvatarPanel.style.bottom = 20;
                _squadAvatarPanel.style.right = 20;
                _squadAvatarPanel.style.flexDirection = FlexDirection.Row;
                _squadAvatarPanel.style.display = DisplayStyle.None; // Hidden by default
                _root.Add(_squadAvatarPanel);
            }

            FileLogger.Log($"HUD found btnBasic={_btnBasic != null}, btnUlt={_btnUlt != null}, btnPassive={_btnPassive != null}, actionPanel={_actionPanel != null}", "HUD");
            FileLogger.Log($"HUD doc VTA = {_doc.visualTreeAsset?.name}", "HUD");

            WireUiEvents();
            BuildTimelineStrip();
            BuildSquadAvatars();

            HideManualActionPanel(); // start hidden
        }

        private void WireUiEvents()
        {
            if (_btnPause != null)
            {
                _btnPause.clicked += () =>
                {
                    if (_pauseOverlay == null) return;
                    _pauseOverlay.RemoveFromClassList("hidden");
                    Time.timeScale = 0f;
                };
            }

            if (_btnResume != null)
            {
                _btnResume.clicked += () =>
                {
                    if (_pauseOverlay == null) return;
                    _pauseOverlay.AddToClassList("hidden");
                    Time.timeScale = 1f;
                };
            }

            if (_btnSpeed != null)
            {
                _btnSpeed.clicked += () =>
                {
                    float t = Time.timeScale;
                    if (Mathf.Approximately(t, 0f)) return;

                    if (t < 1.5f) t = 2f;
                    else if (t < 2.5f) t = 3f;
                    else t = 1f;

                    Time.timeScale = t;
                    _btnSpeed.text = $"x{(int)t}";
                };
            }

            if (_btnAuto != null)
            {
                _btnAuto.clicked += () =>
                {
                    if (_getAuto == null || _setAuto == null) return;
                    bool next = !_getAuto.Invoke();
                    _setAuto.Invoke(next);
                    _btnAuto.text = next ? "Auto" : "Manual";
                };
            }

            // Manual action buttons (optional)
            if (_btnBasic != null)
            {
                _btnBasic.clicked += () => OnBasicClicked?.Invoke();
            }

            if (_btnUlt != null)
            {
                _btnUlt.clicked += () => OnUltClicked?.Invoke();
            }

            if (_btnPassive != null)
            {
                _btnPassive.clicked += () =>
                {
                    if (_currentActor != null)
                    {
                        ShowPassiveInfo(_currentActor);
                    }
                    else
                    {
                        FileLogger.Log("No current actor for passive info", "HUD");
                    }
                };
            }

            if (_btnClosePassive != null)
            {
                _btnClosePassive.clicked += () =>
                {
                    if (_passiveOverlay != null)
                        _passiveOverlay.AddToClassList("hidden");
                };
            }

            if (_btnCloseSkill != null)
            {
                _btnCloseSkill.clicked += () =>
                {
                    if (_skillOverlay != null)
                        _skillOverlay.AddToClassList("hidden");
                };
            }

            // Right-click to show skill descriptions
            if (_btnBasic != null)
            {
                _btnBasic.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 1) // Right click
                    {
                        evt.StopPropagation();
                        ShowBasicSkillInfo();
                    }
                });
            }

            if (_btnUlt != null)
            {
                _btnUlt.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 1) // Right click
                    {
                        evt.StopPropagation();
                        ShowUltimateSkillInfo();
                    }
                });
            }
        }

        private void BuildTimelineStrip()
        {
            if (_timelineStrip == null) return;

            _timelineStrip.Clear();
            _timelineItems.Clear();

            for (int i = 0; i < timelineSlotsCount; i++)
            {
                var item = new BattleHudTimelineItem();
                _timelineItems.Add(item);
                _timelineStrip.Add(item.Root);
            }
        }

        private void BuildSquadAvatars()
        {
            if (_squadAvatarPanel == null) return;

            _squadAvatarPanel.Clear();
            _squadAvatarItems.Clear();

            // Create 6 avatar slots for player squad (full team)
            for (int i = 0; i < 6; i++)
            {
                var avatar = new VisualElement();
                avatar.name = $"squadAvatar{i}";
                avatar.style.width = 80;
                avatar.style.height = 80;
                avatar.style.marginLeft = 10;
                avatar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                avatar.style.borderTopLeftRadius = 8;
                avatar.style.borderTopRightRadius = 8;
                avatar.style.borderBottomLeftRadius = 8;
                avatar.style.borderBottomRightRadius = 8;
                avatar.style.alignItems = Align.Center;
                avatar.style.justifyContent = Justify.Center;

                // Unit name label
                var nameLabel = new Label("");
                nameLabel.name = "nameLabel";
                nameLabel.style.fontSize = 11;
                nameLabel.style.color = Color.white;
                nameLabel.style.position = Position.Absolute;
                nameLabel.style.top = 5;
                avatar.Add(nameLabel);

                // Preference label (B/U/A or Ultimate/Basic/SmartAI)
                var prefLabel = new Label("");
                prefLabel.name = "prefLabel";
                prefLabel.style.fontSize = 14;
                prefLabel.style.color = new Color(1f, 1f, 0f, 1f); // Yellow
                prefLabel.style.position = Position.Absolute;
                prefLabel.style.bottom = 5;
                prefLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                avatar.Add(prefLabel);

                // Store unit ID as user data
                avatar.userData = "";

                // Click handler
                avatar.RegisterCallback<ClickEvent>(evt =>
                {
                    var unitId = (string)avatar.userData;
                    if (!string.IsNullOrEmpty(unitId))
                    {
                        OnSquadAvatarClicked?.Invoke(unitId);
                    }
                });

                _squadAvatarItems.Add(avatar);
                _squadAvatarPanel.Add(avatar);
            }
        }

        public void ShowAutoSquadPanel()
        {
            if (_squadAvatarPanel != null)
            {
                _squadAvatarPanel.style.display = DisplayStyle.Flex;
                RefreshSquadAvatars();
            }
        }

        public void HideAutoSquadPanel()
        {
            if (_squadAvatarPanel != null)
                _squadAvatarPanel.style.display = DisplayStyle.None;
        }

        private void RefreshSquadAvatars()
        {
            if (_getPlayerSquad == null || _getUnitPreference == null) return;

            var squad = _getPlayerSquad();
            for (int i = 0; i < _squadAvatarItems.Count; i++)
            {
                var avatar = _squadAvatarItems[i];
                var nameLabel = avatar.Q<Label>("nameLabel");
                var prefLabel = avatar.Q<Label>("prefLabel");

                if (i < squad.Count && squad[i] != null && squad[i].IsAlive)
                {
                    var unit = squad[i];
                    avatar.userData = unit.UnitId;
                    avatar.style.display = DisplayStyle.Flex;

                    if (nameLabel != null)
                        nameLabel.text = unit.DisplayName.Length <= 8 ? unit.DisplayName : unit.DisplayName.Substring(0, 8);

                    if (prefLabel != null)
                    {
                        var pref = _getUnitPreference(unit.UnitId);
                        prefLabel.text = pref switch
                        {
                            ActionPreference.Basic => "Basic",
                            ActionPreference.Ultimate => "Ultimate",
                            ActionPreference.SmartAI => "SmartAI",
                            _ => "SmartAI"
                        };
                    }
                }
                else
                {
                    avatar.style.display = DisplayStyle.None;
                    avatar.userData = "";
                }
            }
        }

        public void Bind(
            int maxEnergy,
            Func<int> getRound,
            Func<int> getRoundMax,
            Func<bool> getAuto,
            Action<bool> setAuto,
            Func<TeamEnergyState> getPlayerEnergy,
            Func<TeamEnergyState> getEnemyEnergy,
            Func<UnitRuntimeState> getBoss,
            Func<List<UnitRuntimeState>> getRollingTimeline,
            Func<UnitRuntimeState> getCurrentActor,
            Func<string, ActionPreference> getUnitPreference,
            Func<List<UnitRuntimeState>> getPlayerSquad
        )
        {
            _maxEnergy = maxEnergy;

            _getRound = getRound;
            _getRoundMax = getRoundMax;

            _getAuto = getAuto;
            _setAuto = setAuto;

            _getPlayerEnergy = getPlayerEnergy;
            _getEnemyEnergy = getEnemyEnergy;

            _getBoss = getBoss;

            _getRollingTimeline = getRollingTimeline;
            _getCurrentActor = getCurrentActor;
            _getUnitPreference = getUnitPreference;
            _getPlayerSquad = getPlayerSquad;

            if (_btnAuto != null) _btnAuto.text = _getAuto.Invoke() ? "Auto" : "Manual";
        }

        private void Update()
        {
            if (_getRound == null || _getPlayerEnergy == null || _getBoss == null) return;

            RefreshTurn();
            RefreshEnergy();
            RefreshBoss();
            RefreshTimeline();
        }

        private void RefreshTurn()
        {
            if (_lblTurn == null || _getRound == null || _getRoundMax == null) return;
            _lblTurn.text = $"Turn {_getRound.Invoke()}/{_getRoundMax.Invoke()}";
        }

        private void RefreshEnergy()
        {
            if (_lblEnergy == null || _energyBar == null || _getPlayerEnergy == null) return;

            TeamEnergyState e = _getPlayerEnergy.Invoke();
            _lblEnergy.text = $"Energy {e.Energy}/{_maxEnergy}";

            _energyBar.lowValue = 0f;
            _energyBar.highValue = 100f;
            _energyBar.value = e.EnergyBarPct;
        }

        private void RefreshBoss()
        {
            if (_bossName == null || _bossHpBar == null || _getBoss == null) return;

            var boss = _getBoss.Invoke();
            if (boss == null)
            {
                _bossName.text = "—";
                _bossHpBar.lowValue = 0;
                _bossHpBar.highValue = 100;
                _bossHpBar.value = 0;
                return;
            }

            // Show boss name even when dead
            _bossName.text = boss.DisplayName;

            // Show 0 HP when dead
            float hp01 = boss.MaxHP <= 0 ? 0f : (boss.CurrentHP / (float)boss.MaxHP);
            _bossHpBar.lowValue = 0f;
            _bossHpBar.highValue = 100f;
            _bossHpBar.value = hp01 * 100f;
        }

        private void RefreshTimeline()
        {
            if (_getRollingTimeline == null || _getCurrentActor == null) return;
            if (_timelineItems.Count == 0) return;

            var list = _getRollingTimeline.Invoke() ?? new List<UnitRuntimeState>();
            var current = _getCurrentActor.Invoke();
            bool isAuto = _getAuto != null && _getAuto.Invoke();

            // MVP: show alive only, up to slots
            var aliveList = list.Where(u => u != null && u.IsAlive).ToList();

            for (int i = 0; i < _timelineItems.Count; i++)
            {
                if (i < aliveList.Count && aliveList[i] != null)
                {
                    var unit = aliveList[i];
                    _timelineItems[i].Root.style.display = DisplayStyle.Flex;

                    // Don't show preference in timeline - only show in squad avatars
                    _timelineItems[i].Set(unit, isCurrent: (current == unit), preference: null);

                    // Timeline items are not clickable
                }
                else
                {
                    _timelineItems[i].Root.style.display = DisplayStyle.None;
                }
            }

            // Refresh squad avatars if in auto mode
            if (isAuto)
            {
                RefreshSquadAvatars();
            }
        }

        // -------------------------
        // Manual action panel API
        // -------------------------

        public void ShowManualActionPanel(UnitRuntimeState actor, bool canUlt)
        {
            if (_actionPanel == null) return;

            _currentActor = actor;
            _actionPanel.RemoveFromClassList("hidden");

            if (_lblActionHint != null)
                _lblActionHint.text = $"Acting: {actor.DisplayName}";

            if (_btnUlt != null)
                _btnUlt.SetEnabled(canUlt);
        }

        public void HideManualActionPanel()
        {
            if (_actionPanel == null) return;
            _actionPanel.AddToClassList("hidden");
            _currentActor = null;
        }


        public void SetActionHint(string text)
        {
            if (_lblActionHint != null)
                _lblActionHint.text = text;
        }

        public void ShowPassiveInfo(UnitRuntimeState unit)
        {
            if (_passiveOverlay == null) return;

            if (_lblPassiveTitle != null)
                _lblPassiveTitle.text = unit.PassiveName ?? "Passive Ability";

            if (_lblPassiveDesc != null)
                _lblPassiveDesc.text = unit.PassiveDescription ?? "No description available.";

            _passiveOverlay.RemoveFromClassList("hidden");
        }

        // --- Combat Log Integration ---
        public void AddCombatLog(string message)
        {
            if (combatLogPanel != null)
            {
                combatLogPanel.AddLog(message);
            }
        }

        public void ShowCombatLog()
        {
            if (combatLogPanel != null)
                combatLogPanel.Show();
        }

        public void HideCombatLog()
        {
            if (combatLogPanel != null)
                combatLogPanel.Hide();
        }

        public void ToggleCombatLog()
        {
            if (combatLogPanel != null)
                combatLogPanel.ToggleVisibility();
        }

        // --- Skill Info Modal Methods ---

        private void CreateSkillOverlay()
        {
            // Create a modal overlay similar to passiveOverlay
            _skillOverlay = new VisualElement();
            _skillOverlay.name = "skillOverlay";
            _skillOverlay.AddToClassList("hidden");
            _skillOverlay.style.position = Position.Absolute;
            _skillOverlay.style.left = 0;
            _skillOverlay.style.top = 0;
            _skillOverlay.style.right = 0;
            _skillOverlay.style.bottom = 0;
            _skillOverlay.style.backgroundColor = new Color(0, 0, 0, 0.8f);
            _skillOverlay.style.alignItems = Align.Center;
            _skillOverlay.style.justifyContent = Justify.Center;

            // Create modal content box
            var modalBox = new VisualElement();
            modalBox.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            modalBox.style.borderTopLeftRadius = 10;
            modalBox.style.borderTopRightRadius = 10;
            modalBox.style.borderBottomLeftRadius = 10;
            modalBox.style.borderBottomRightRadius = 10;
            modalBox.style.paddingTop = 20;
            modalBox.style.paddingBottom = 20;
            modalBox.style.paddingLeft = 30;
            modalBox.style.paddingRight = 30;
            modalBox.style.minWidth = 300;
            modalBox.style.maxWidth = 500;

            // Title
            _lblSkillTitle = new Label("Skill Name");
            _lblSkillTitle.name = "lblSkillTitle";
            _lblSkillTitle.style.fontSize = 24;
            _lblSkillTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            _lblSkillTitle.style.color = Color.white;
            _lblSkillTitle.style.marginBottom = 10;
            modalBox.Add(_lblSkillTitle);

            // Description
            _lblSkillDesc = new Label("Skill description goes here.");
            _lblSkillDesc.name = "lblSkillDesc";
            _lblSkillDesc.style.fontSize = 16;
            _lblSkillDesc.style.color = new Color(0.9f, 0.9f, 0.9f);
            _lblSkillDesc.style.whiteSpace = WhiteSpace.Normal;
            _lblSkillDesc.style.marginBottom = 20;
            modalBox.Add(_lblSkillDesc);

            // Close button
            _btnCloseSkill = new Button(() => _skillOverlay.AddToClassList("hidden"));
            _btnCloseSkill.text = "Close";
            _btnCloseSkill.name = "btnCloseSkill";
            _btnCloseSkill.style.paddingTop = 10;
            _btnCloseSkill.style.paddingBottom = 10;
            _btnCloseSkill.style.paddingLeft = 20;
            _btnCloseSkill.style.paddingRight = 20;
            modalBox.Add(_btnCloseSkill);

            _skillOverlay.Add(modalBox);
            _root.Add(_skillOverlay);
        }

        public void ShowBasicSkillInfo()
        {
            if (_skillOverlay == null || _currentActor == null) return;

            if (_lblSkillTitle != null)
                _lblSkillTitle.text = "Basic Attack";

            if (_lblSkillDesc != null)
            {
                string desc = _currentActor.BasicSkillDescription ?? "Standard attack.";
                int minHits = _currentActor.BasicHitsMin;
                int maxHits = _currentActor.BasicHitsMax;
                string hitsText = minHits == maxHits ? $"{minHits} hit(s)" : $"{minHits}-{maxHits} hits";

                _lblSkillDesc.text = $"{desc}\n\n<b>Hits:</b> {hitsText}\n<b>Target:</b> {_currentActor.BasicTargetPattern}";
            }

            _skillOverlay.RemoveFromClassList("hidden");
        }

        public void ShowUltimateSkillInfo()
        {
            if (_skillOverlay == null || _currentActor == null) return;

            if (_lblSkillTitle != null)
                _lblSkillTitle.text = "Ultimate";

            if (_lblSkillDesc != null)
            {
                string desc = _currentActor.UltimateSkillDescription ?? "Powerful ultimate ability.";
                int cost = _currentActor.UltimateEnergyCost;
                int cooldown = _currentActor.UltimateCooldownTurns;

                _lblSkillDesc.text = $"{desc}\n\n<b>Energy Cost:</b> {cost}\n<b>Cooldown:</b> {cooldown} turn(s)\n<b>Target:</b> {_currentActor.UltimateTargetPattern}";
            }

            _skillOverlay.RemoveFromClassList("hidden");
        }
    }
}
