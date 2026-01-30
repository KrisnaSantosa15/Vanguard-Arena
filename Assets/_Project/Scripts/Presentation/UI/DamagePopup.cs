using TMPro;
using UnityEngine;
using Project.Domain;

namespace Project.Presentation.UI
{
    /// <summary>
    /// Floating damage number that animates upward and fades out
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text textLabel;

        [Header("Animation")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float lifetime = 1.5f;
        [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color critColor = new Color(1f, 0.5f, 0f); // Orange
        [SerializeField] private Color healColor = Color.green;

        private float _timer;
        private Vector3 _startPosition;
        private bool _isPooled;
        private static System.Action<DamagePopup> _returnToPoolCallback;

        public static void SetReturnToPoolCallback(System.Action<DamagePopup> callback)
        {
            _returnToPoolCallback = callback;
        }

        public void Initialize(int value, bool isCrit, bool isHeal, Vector3 worldPosition, bool isPooled = false)
        {
            if (textLabel == null)
            {
                textLabel = GetComponentInChildren<TMP_Text>();
            }

            if (textLabel == null)
            {
                FileLogger.LogError("No TMP_Text found!", "DamagePopup");
                Destroy(gameObject);
                return;
            }

            // Set text
            string prefix = isHeal ? "+" : "";
            string suffix = isCrit ? " CRIT!" : "";
            textLabel.text = $"{prefix}{value}{suffix}";

            // Set color
            if (isHeal)
                textLabel.color = healColor;
            else if (isCrit)
                textLabel.color = critColor;
            else
                textLabel.color = normalColor;

            // Set scale for crits
            if (isCrit)
                transform.localScale = Vector3.one * 1.5f;
            else
                transform.localScale = Vector3.one;

            // Position
            transform.position = worldPosition;
            _startPosition = worldPosition;
            _timer = 0f;
            _isPooled = isPooled;

            // Make visible
            gameObject.SetActive(true);

            // Auto-destroy/return after lifetime
            if (_isPooled)
            {
                StartCoroutine(ReturnToPoolAfterDelay());
            }
            else
            {
                Destroy(gameObject, lifetime);
            }
        }

        private System.Collections.IEnumerator ReturnToPoolAfterDelay()
        {
            yield return new WaitForSeconds(lifetime);
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            gameObject.SetActive(false);
            // Reset state
            _timer = 0f;
            transform.localScale = Vector3.one;
            if (textLabel != null)
            {
                Color col = textLabel.color;
                col.a = 1f;
                textLabel.color = col;
            }

            // Return to pool via callback
            _returnToPoolCallback?.Invoke(this);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float t = _timer / lifetime;

            // Move upward
            transform.position = _startPosition + Vector3.up * (moveSpeed * _timer);

            // Fade out
            if (textLabel != null)
            {
                Color col = textLabel.color;
                col.a = fadeOutCurve.Evaluate(t);
                textLabel.color = col;
            }
        }
    }
}
