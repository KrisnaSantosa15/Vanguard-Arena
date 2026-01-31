using UnityEngine;

namespace Project.Presentation
{
    public sealed class BillboardToCamera : MonoBehaviour
    {
        private Camera _cam;

        private void Awake() => _cam = Camera.main;

        private void LateUpdate()
        {
            if (_cam == null) return;
            transform.forward = _cam.transform.forward;
        }
    }
}
