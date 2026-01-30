using UnityEngine;
using Project.Domain;

namespace Project.Presentation
{
    /// <summary>
    /// MVP: clicking a unit selects it as a target (only when BattleController is waiting for target).
    /// Requires a Collider on this GameObject (or parent) so OnMouseDown works.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnitClickable : MonoBehaviour
    {
        private UnitView _unitView;

        private void Awake()
        {
            _unitView = GetComponentInParent<UnitView>();
            if (_unitView == null)
                FileLogger.LogWarning($"No UnitView found in parents of {name}", "UnitClickable");
        }

        private void OnMouseDown()
        {
            FileLogger.Log($"clicked unitView={(_unitView != null ? _unitView.name : "null")}", "CLICK");
            if (_unitView == null) return;

            var controller = FindFirstObjectByType<BattleController>();
            if (controller == null) return;

            controller.TrySelectTarget(_unitView.State);
        }
    }
}
