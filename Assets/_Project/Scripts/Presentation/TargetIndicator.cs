using UnityEngine;
using Project.Domain;

namespace Project.Presentation
{
    /// <summary>
    /// Simple visual indicator for target selection (MVP).
    /// Attach to UnitView prefab or instantiate dynamically.
    /// </summary>
    public sealed class TargetIndicator : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private GameObject indicatorVisual;
        [SerializeField] private Color validEnemyColor = new Color(0.2f, 1f, 0.2f, 0.9f); // Green for enemies
        [SerializeField] private Color validAllyColor = new Color(0.2f, 0.6f, 1f, 0.9f); // Blue for allies
        [SerializeField] private Color currentActorColor = new Color(1f, 1f, 0f, 0.9f); // Yellow
        [SerializeField] private Color invalidColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gray
        [SerializeField] private float ringRadius = 0.6f;
        [SerializeField] private float ringHeight = 0.05f;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;

        private void Awake()
        {
            // If no visual assigned, create a simple cylinder ring
            if (indicatorVisual == null)
            {
                CreateDefaultVisual();
            }

            if (indicatorVisual != null)
            {
                _renderer = indicatorVisual.GetComponent<Renderer>();
            }

            _propBlock = new MaterialPropertyBlock();
            Hide();
        }

        private void CreateDefaultVisual()
        {
            indicatorVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicatorVisual.name = "TargetIndicator_Ring";
            indicatorVisual.transform.SetParent(transform);
            indicatorVisual.transform.localPosition = Vector3.zero;
            indicatorVisual.transform.localRotation = Quaternion.identity;
            indicatorVisual.transform.localScale = new Vector3(ringRadius * 2f, ringHeight, ringRadius * 2f);

            // Remove collider to avoid raycast interference
            var collider = indicatorVisual.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            // Get renderer but don't set material color - let MaterialPropertyBlock handle it
            _renderer = indicatorVisual.GetComponent<Renderer>();

            FileLogger.Log($"Created default visual for {gameObject.name}", "INDICATOR");
        }

        public void ShowValidEnemy()
        {
            if (indicatorVisual != null)
            {
                indicatorVisual.SetActive(true);
                indicatorVisual.transform.localScale = new Vector3(ringRadius * 2f, ringHeight, ringRadius * 2f);
            }

            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", validEnemyColor);
                _propBlock.SetColor("_Color", validEnemyColor);
                _renderer.SetPropertyBlock(_propBlock);
            }
        }

        public void ShowValidAlly()
        {
            if (indicatorVisual != null)
            {
                indicatorVisual.SetActive(true);
                indicatorVisual.transform.localScale = new Vector3(ringRadius * 2f, ringHeight, ringRadius * 2f);
            }

            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", validAllyColor);
                _propBlock.SetColor("_Color", validAllyColor);
                _renderer.SetPropertyBlock(_propBlock);
            }
        }

        // Legacy method - defaults to enemy (green)
        public void ShowValid()
        {
            ShowValidEnemy();
        }

        public void ShowCurrentActor()
        {
            if (indicatorVisual != null)
            {
                indicatorVisual.SetActive(true);
                // Make current actor ring BIGGER for distinction
                indicatorVisual.transform.localScale = new Vector3(ringRadius * 2.5f, ringHeight * 2f, ringRadius * 2.5f);
            }

            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", currentActorColor);
                _propBlock.SetColor("_Color", currentActorColor); // Fallback for built-in
                _renderer.SetPropertyBlock(_propBlock);
                FileLogger.Log($"Showing YELLOW ring for {gameObject.name}", "INDICATOR");
            }
        }

        public void ShowInvalid()
        {
            if (indicatorVisual != null)
                indicatorVisual.SetActive(true);

            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", invalidColor);
                _propBlock.SetColor("_Color", invalidColor); // Fallback for built-in
                _renderer.SetPropertyBlock(_propBlock);
                FileLogger.Log($"Showing GRAY ring for {gameObject.name}", "INDICATOR");
            }
        }

        public void Hide()
        {
            if (indicatorVisual != null)
                indicatorVisual.SetActive(false);
        }
    }
}
