using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Caveman
{
    /// <summary>
    /// Placement-mode building. Press a number key to pick a building; a ghost
    /// follows the cursor and turns green only when placement is valid (affordable
    /// AND next to a matching resource patch). Left-click places, right-click/Esc
    /// cancels. Press X over a building to demolish it for a partial refund.
    /// </summary>
    public class BuildController : MonoBehaviour
    {
        public PlayerGatherer gatherer;
        public List<BuildingDefinition> buildables = new();
        [Tooltip("How close a matching resource patch must be to place a building.")]
        public float placeNodeRange = 2.5f;

        public int BuildingsPlaced { get; private set; }
        public int PendingIndex { get; private set; } = -1;
        public bool PlacementValid { get; private set; }

        /// <summary>True while a building is being positioned (suppresses manual gather clicks).</summary>
        public static bool IsPlacing { get; private set; }

        private Inventory Inv => gatherer != null ? gatherer.Inventory : null;
        private Camera _cam;
        private GameObject _ghost;
        private SpriteRenderer _ghostSr;

        void Awake() => _cam = Camera.main;

        void Update()
        {
            if (_cam == null) _cam = Camera.main;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) Select(0);
            else if (kb.digit2Key.wasPressedThisFrame) Select(1);
            else if (kb.digit3Key.wasPressedThisFrame) Select(2);

            if (kb.escapeKey.wasPressedThisFrame) CancelPlacement();

            if (PendingIndex >= 0)
                UpdatePlacement(mouse);
            else if (mouse != null && kb.xKey.wasPressedThisFrame)
                TryRemoveUnderCursor(mouse);
        }

        private void Select(int index)
        {
            if (index < 0 || index >= buildables.Count || buildables[index] == null) return;
            PendingIndex = index;
            IsPlacing = true;
            EnsureGhost();
        }

        private void EnsureGhost()
        {
            if (_ghost == null)
            {
                _ghost = new GameObject("BuildGhost");
                _ghostSr = _ghost.AddComponent<SpriteRenderer>();
                _ghostSr.sprite = PlaceholderArt.Square();
                _ghostSr.sortingOrder = 20;
                _ghost.transform.localScale = Vector3.one * 0.9f;
            }
            _ghost.SetActive(true);
        }

        private void UpdatePlacement(Mouse mouse)
        {
            if (_cam == null || mouse == null || _ghost == null) return;

            var def = buildables[PendingIndex];
            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            world.z = 0f;
            _ghost.transform.position = world;

            bool affordable = CanAfford(def);
            bool nearNode = HasMatchingNodeNear(world, def.produces, placeNodeRange);
            PlacementValid = affordable && nearNode;

            _ghostSr.color = PlacementValid
                ? new Color(def.color.r, def.color.g, def.color.b, 0.55f)
                : new Color(1f, 0.3f, 0.3f, 0.45f);

            if (mouse.leftButton.wasPressedThisFrame && PlacementValid)
            {
                foreach (var c in def.cost) Inv.TryRemove(c.item, c.amount);
                ProductionBuilding.Spawn(def, world, Inv);
                BuildingsPlaced++;
                CancelPlacement();
            }
            else if (mouse.rightButton.wasPressedThisFrame)
            {
                CancelPlacement();
            }
        }

        private void CancelPlacement()
        {
            PendingIndex = -1;
            PlacementValid = false;
            IsPlacing = false;
            if (_ghost != null) _ghost.SetActive(false);
        }

        public bool CanAfford(BuildingDefinition def)
        {
            if (def == null || Inv == null) return false;
            foreach (var c in def.cost)
                if (c.item == null || Inv.Count(c.item) < c.amount) return false;
            return true;
        }

        private static bool HasMatchingNodeNear(Vector3 pos, ItemDefinition produces, float range)
        {
            float rsq = range * range;
            foreach (var n in FindObjectsByType<ResourceNode>())
            {
                if (n == null || n.yields != produces) continue;
                if (((Vector2)(n.transform.position - pos)).sqrMagnitude <= rsq) return true;
            }
            return false;
        }

        private void TryRemoveUnderCursor(Mouse mouse)
        {
            if (_cam == null) return;
            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(world);
            if (hit == null) return;

            var pb = hit.GetComponent<ProductionBuilding>();
            if (pb == null || pb.def == null) return;

            // Partial refund (half, rounded down).
            if (Inv != null)
                foreach (var c in pb.def.cost)
                    Inv.Add(c.item, Mathf.Max(0, c.amount / 2));

            BuildingsPlaced = Mathf.Max(0, BuildingsPlaced - 1);
            Destroy(pb.gameObject);
        }
    }
}
