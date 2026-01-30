using UnityEngine;
using Project.Domain;
using Project.Presentation.UI;

namespace Project.Presentation
{
    public sealed class UnitView : MonoBehaviour
    {
        public UnitRuntimeState State { get; private set; }

        [Header("Visuals")]
        [SerializeField] private Transform modelRoot;

        [Header("Team Colors")]
        [SerializeField] private Color playerColor = new Color(0.2f, 0.4f, 1f, 1f); // Blue
        [SerializeField] private Color enemyColor = new Color(1f, 0.2f, 0.2f, 1f); // Red

        [Header("UI (World Space uGUI)")]
        [SerializeField] private UnitOverheadHud overheadHud;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _propBlock;
        private bool _deathAnimationStarted = false;

        public void Bind(UnitRuntimeState state)
        {
            State = state;

            if (modelRoot == null)
                modelRoot = transform;

            // Get all renderers on this unit
            _renderers = modelRoot.GetComponentsInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();

            if (overheadHud == null)
                overheadHud = GetComponentInChildren<UnitOverheadHud>(true);

            // Bind overhead HUD (optional)
            if (overheadHud != null)
            {
                var controller = FindFirstObjectByType<BattleController>();
                if (controller != null)
                {
                    System.Func<int> teamEnergyGetter =
                        () => state.IsEnemy ? controller.EnemyEnergyValue : controller.PlayerEnergyValue;

                    System.Func<bool> isAutoGetter =
                        () => controller.AutoModeValue;

                    overheadHud.Bind(state, teamEnergyGetter, isAutoGetter);
                }
            }

            // Apply team color
            ApplyTeamColor();

            Refresh();
        }

        public void Refresh()
        {
            if (State == null) return;

            bool alive = State.IsAlive;
            FileLogger.Log($"{State.DisplayName} alive={alive}, _deathAnimationStarted={_deathAnimationStarted}", "VIEW-REFRESH");

            if (!alive && !_deathAnimationStarted)
            {
                _deathAnimationStarted = true;
                var anim = GetComponent<UnitAnimationDriver>();
                FileLogger.Log($"Triggering death animation for {State.DisplayName}, anim={anim != null}", "VIEW-DEATH");
                if (anim != null)
                {
                    anim.PlayDeath();
                    // Delay disabling until death animation completes
                    StartCoroutine(DisableAfterDeath());
                    return; // Don't immediately disable
                }
            }

            if (modelRoot != null && alive)
                modelRoot.gameObject.SetActive(true);

            if (overheadHud != null)
                overheadHud.Refresh();
        }

        private void ApplyTeamColor()
        {
            if (State == null || _renderers == null || _renderers.Length == 0) return;

            Color teamColor = State.IsEnemy ? enemyColor : playerColor;

            foreach (var renderer in _renderers)
            {
                if (renderer == null) continue;

                renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_Color", teamColor);
                _propBlock.SetColor("_BaseColor", teamColor); // URP Lit shader
                renderer.SetPropertyBlock(_propBlock);
            }

            FileLogger.Log($"Applied {(State.IsEnemy ? "RED (Enemy)" : "BLUE (Player)")} to {State.DisplayName}", "COLOR");
        }

        private System.Collections.IEnumerator DisableAfterDeath()
        {
            // Wait for death animation to complete
            yield return new WaitForSeconds(1.0f);

            // Now disable the model
            if (modelRoot != null)
                modelRoot.gameObject.SetActive(false);
        }
    }
}
