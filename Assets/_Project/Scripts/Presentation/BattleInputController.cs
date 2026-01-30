using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Project.Domain;

namespace Project.Presentation
{
    /// <summary>
    /// Handles Raycasting and Input events for the Battle.
    /// Sends events back to the Controller.
    /// </summary>
    public class BattleInputController : MonoBehaviour
    {
        [SerializeField] private LayerMask unitClickMask = ~0;
        [SerializeField] private Camera worldCamera;

        public event Action<UnitRuntimeState> OnUnitClicked;
        public event Action OnCancel;

        private bool _inputEnabled = false;

        private void Start()
        {
            if (worldCamera == null) worldCamera = Camera.main;
        }

        public void EnableInput(bool enable)
        {
            _inputEnabled = enable;
        }

        private void Update()
        {
            if (!_inputEnabled) return;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            // ESC to cancel
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                OnCancel?.Invoke();
                return;
            }

            // Mouse Click
            if (mouse == null) return;
            if (!mouse.leftButton.wasPressedThisFrame) return;

            if (TryRaycastUnit(mouse.position.ReadValue(), out var clicked))
            {
                OnUnitClicked?.Invoke(clicked);
            }
        }

        private bool TryRaycastUnit(Vector2 screenPos, out UnitRuntimeState hitUnit)
        {
            hitUnit = null;

            if (worldCamera == null) return false;

            Ray ray = worldCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, 200f, unitClickMask))
                return false;

            var view = hit.collider.GetComponentInParent<UnitView>();
            if (view == null || view.State == null) return false;

            hitUnit = view.State;
            return true;
        }
    }
}
