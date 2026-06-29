using UnityEngine;
using UnityEngine.InputSystem;

namespace Caveman
{
    /// <summary>
    /// Follows the player smoothly (SmoothDamp — less jitter than a raw lerp) and
    /// handles mouse-wheel zoom (in/out). Zoom is instant (works while paused).
    /// The full world MAP is a separate pan/zoom HUD overlay (M), owned by InventoryHud.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = 0.12f;

        public float minZoom = 4f;
        public float maxZoom = 190f;       // zoom out far enough to read the bigger ~560-wide world
        public float zoomStep = 2.5f;

        private Camera _cam;
        private Vector3 _vel;

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

            var mouse = Mouse.current;
            if (mouse != null && !InventoryHud.PointerOverUI) // don't zoom while scrolling a panel
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    float step = scroll > 0f ? -zoomStep : zoomStep; // wheel up = zoom in
                    _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize + step, minZoom, maxZoom);
                }
            }
        }
    }
}
