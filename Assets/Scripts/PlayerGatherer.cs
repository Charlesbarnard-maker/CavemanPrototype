using UnityEngine;
using UnityEngine.InputSystem;

namespace Caveman
{
    /// <summary>
    /// Click-to-gather. On left-click, finds the ResourceNode under the cursor and,
    /// if the player is within reach, harvests it into the player's Inventory.
    /// Owns the player's Inventory for the MVP.
    /// </summary>
    public class PlayerGatherer : MonoBehaviour
    {
        [Tooltip("How close the player must be to a node to harvest it (world units).")]
        public float reach = 4f;

        public Inventory Inventory { get; } = new Inventory();

        private Camera _cam;

        void Awake() => _cam = Camera.main;

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            Vector2 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(world);
            if (hit == null) return;

            var node = hit.GetComponent<ResourceNode>();
            if (node == null) return;

            if (Vector2.Distance(transform.position, node.transform.position) > reach) return;

            node.Harvest(Inventory);
        }
    }
}
