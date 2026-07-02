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

        /// <summary>Lifetime units gathered BY HAND this game — drives the first tutorial quest (the carried
        /// pile can't: the starter kit pre-fills it, which made "gather 12 wood" self-complete at spawn).
        /// Static (statics persist with domain-reload off) — reset by GameBootstrap on a new game.</summary>
        public static int HandGathered;

        private Camera _cam;
        private ResourceNode _highlighted;
        private bool _minedHintShown;   // one-time "click to mine" nudge
        private float _farWarnT = -99f;  // rate-limits the out-of-reach warning

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

            // Highlight the node under the cursor — bright when in reach (click to mine), blue when out of reach
            // (a resource is here, walk closer). Highlighting either way makes it clear resources are clickable.
            if (node != _highlighted)
            {
                if (_highlighted != null) _highlighted.SetHighlighted(false);
                _highlighted = node;
            }
            if (_highlighted != null) _highlighted.SetHighlighted(true, inReach);

            // One-time nudge the first time a resource is actually within reach, so mining-by-click is obvious.
            if (inReach && !_minedHintShown)
            {
                _minedHintShown = true;
                Toast.Show("<color=#cfe>⛏ Left-click a glowing resource to mine it by hand.</color>");
            }

            if (mouse.leftButton.wasPressedThisFrame && node != null)
            {
                if (inReach)
                {
                    if (node.Harvest(Inventory))
                    {
                        HandGathered++; // tutorial progress: units mined by hand (not the pre-filled kit)
                        node.Nudge(); // chop recoil wobble — makes the manual hit feel responsive
                        if (node.yields != null)
                            GatherPopup.Show(node.transform.position, $"+1 {node.yields.displayName}", node.yields.color);
                    }
                }
                else if (Time.unscaledTime - _farWarnT > 1.2f) // clicked a resource that's out of reach
                {
                    _farWarnT = Time.unscaledTime;
                    GatherPopup.Show(node.transform.position, "Too far — move closer", new Color(1f, 0.55f, 0.4f));
                }
            }
        }
    }
}
