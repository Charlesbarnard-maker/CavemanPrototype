using UnityEngine;
using UnityEngine.InputSystem;

namespace Caveman
{
    /// <summary>
    /// Follows the player smoothly (SmoothDamp — less jitter than a raw lerp) and
    /// handles zoom: mouse wheel to zoom in/out, and M to toggle a far "map"
    /// overview. Zoom is instant (works while paused).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = 0.12f;

        public float minZoom = 4f;
        public float maxZoom = 140f;
        public float zoomStep = 2.5f;
        public float overviewZoom = 130f; // M = full map overview (big world)

        private Camera _cam;
        private Vector3 _vel;
        private float _savedZoom;
        private bool _overview;

        void Awake() => _cam = GetComponent<Camera>();

        void LateUpdate()
        {
            HandleZoom();

            if (target != null)
            {
                Vector3 goal = new Vector3(target.position.x, target.position.y, transform.position.z);
                transform.position = Vector3.SmoothDamp(transform.position, goal, ref _vel, smoothTime);
            }
        }

        private void HandleZoom()
        {
            if (_cam == null) return;

            var kb = Keyboard.current;
            if (kb != null && kb.mKey.wasPressedThisFrame)
            {
                _overview = !_overview;
                if (_overview) { _savedZoom = _cam.orthographicSize; _cam.orthographicSize = overviewZoom; }
                else _cam.orthographicSize = _savedZoom;
            }

            var mouse = Mouse.current;
            if (mouse != null && !InventoryHud.PointerOverUI) // don't zoom while scrolling a panel
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    float step = scroll > 0f ? -zoomStep : zoomStep; // wheel up = zoom in
                    _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize + step, minZoom, maxZoom);
                    _overview = false;
                }
            }
        }
    }
}
