using UnityEngine;
using UnityEngine.UI;
using Project.Domain;

namespace Project.Presentation.UI
{
    public sealed class UnitOverheadHud : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Image hpFill = null!;
        [SerializeField] private GameObject ultReady = null!;
        [SerializeField] private Image bossIcon = null!;

        [Header("Ult Ready Rules")]
        [Tooltip("If true: only show UltReady when Auto mode is ON (OPM-ish). If false: always show when ready.")]
        [SerializeField] private bool showUltReadyOnlyInAuto = false;

        private UnitRuntimeState _state = null!;
        private System.Func<int> _getTeamEnergy = null!;
        private System.Func<bool> _isAuto = null!;

        public void Bind(UnitRuntimeState state, System.Func<int> getTeamEnergy, System.Func<bool> isAuto)
        {
            _state = state;
            _getTeamEnergy = getTeamEnergy;
            _isAuto = isAuto;
            Refresh();
        }

        private void Update()
        {
            // Continuously refresh to keep HP and Ult Ready indicator in sync
            Refresh();
        }

        public void Refresh()
        {
            if (_state == null) return;

            // HP
            if (hpFill != null)
            {
                float hp01 = (_state.MaxHP <= 0) ? 0f : (_state.CurrentHP / (float)_state.MaxHP);
                hpFill.fillAmount = Mathf.Clamp01(hp01);
            }

            // Boss icon
            if (bossIcon != null)
            {
                bool showBoss = _state.IsBoss;
                bossIcon.gameObject.SetActive(showBoss);

                if (showBoss && _state.BossTypeIcon != null)
                    bossIcon.sprite = _state.BossTypeIcon;
            }

            // Ult ready: TEAM shared energy + unit ult cost + ult cooldown ready
            int teamEnergy = (_getTeamEnergy != null) ? _getTeamEnergy() : 0;
            bool energyReady = teamEnergy >= _state.UltimateEnergyCost;
            bool cooldownReady = _state.CanUseUltimate;

            bool ready = energyReady && cooldownReady;

            if (ultReady != null)
            {
                bool auto = (_isAuto != null) && _isAuto();
                bool visible = ready && (!showUltReadyOnlyInAuto || auto);
                ultReady.SetActive(visible);
            }
        }
    }
}
