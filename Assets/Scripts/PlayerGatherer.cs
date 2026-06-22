using UnityEngine;
using UnityEngine.InputSystem;

namespace Caveman
{
    /// <summary>
    /// Click-to-gather for the EARLY (manual) game only. Highlights the node under
    /// the cursor when it's in reach and harvests it on left-click. Owns the
    /// player's Inventory. Manual gathering is meant to be the bootstrap that funds
    /// your first automation — it is deliberately NOT upgraded to be faster.
    /// </summary>
    public class PlayerGatherer : MonoBehaviour
    {
        [Tooltip("How close the player must be to a node to harvest it (world units).")]
        public float reach = 4f;

        public Inventory Inventory { get; } = new Inventory();

        private Camera _cam;
        private ResourceNode _highlighted;

        void Awake() => _cam = Camera.main;

        void Update()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            // Don't gather while placing a building or clicking on the HUD.
            if (BuildController.IsPlacing || InventoryHud.PointerOverUI)
            {
                if (_highlighted != null) { _highlighted.SetHighlighted(false); _highlighted = null; }
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(world);
            ResourceNode node = hit != null ? hit.GetComponent<ResourceNode>() : null;

            bool inReach = node != null &&
                           Vector2.Distance(transform.position, node.transform.position) <= reach;

            // Keep exactly one node highlighted at a time.
            ResourceNode target = inReach ? node : null;
            if (target != _highlighted)
            {
                if (_highlighted != null) _highlighted.SetHighlighted(false);
                _highlighted = target;
                if (_highlighted != null) _highlighted.SetHighlighted(true);
            }

            if (inReach && mouse.leftButton.wasPressedThisFrame)
                node.Harvest(Inventory);
        }
    }
}
