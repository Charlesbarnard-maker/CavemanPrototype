using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Caveman
{
    /// <summary>
    /// Placement + selection. Number keys pick a building to place (ghost follows
    /// cursor, green = valid). Otherwise, left-click a building to SELECT it; the
    /// HUD shows a panel to add/remove workers or demolish it. Collectors need a
    /// nearby patch; storage/housing place anywhere. New collectors auto-assign one
    /// free worker.
    /// </summary>
    public class BuildController : MonoBehaviour
    {
        public PlayerGatherer gatherer;
        public List<BuildingDefinition> buildables = new();
        public float placeNodeRange = 6f;

        public int BuildingsPlaced { get; private set; }
        public int PendingIndex { get; private set; } = -1;
        public bool PlacementValid { get; private set; }
        public GameObject Selected { get; private set; }

        public static bool IsPlacing { get; private set; }

        private Inventory Carried => gatherer != null ? gatherer.Inventory : null;
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

            for (int i = 0; i < buildables.Count && i < 9; i++)
                if (kb[Key.Digit1 + i].wasPressedThisFrame) { BeginPlacement(i); break; }

            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (PendingIndex >= 0) CancelPlacement();
                else Selected = null;
            }

            if (PendingIndex >= 0)
            {
                UpdatePlacement(mouse);
                return;
            }

            // --- Selection mode ---
            bool overUI = InventoryHud.PointerOverUI;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !overUI)
                Selected = BuildingGOUnderCursor(mouse);

            var collector = SelectedCollector;
            if (collector != null)
            {
                if (kb.rightBracketKey.wasPressedThisFrame) collector.TryAssign();
                if (kb.leftBracketKey.wasPressedThisFrame) collector.Unassign();
            }

            if (kb.xKey.wasPressedThisFrame) DemolishSelected();
        }

        public ProductionBuilding SelectedCollector =>
            Selected != null ? Selected.GetComponent<ProductionBuilding>() : null;

        private void BeginPlacement(int index)
        {
            if (index < 0 || index >= buildables.Count || buildables[index] == null) return;
            PendingIndex = index;
            IsPlacing = true;
            Selected = null;
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
            _ghost.transform.localScale = Vector3.one * (def.kind == BuildingKind.Collector ? 0.9f : 1.1f);

            bool affordable = Economy.CanAfford(def.cost, Carried);
            bool placeOk = def.kind != BuildingKind.Collector
                           || HasMatchingNodeNear(world, def.item, placeNodeRange);
            PlacementValid = affordable && placeOk;

            _ghostSr.color = PlacementValid
                ? new Color(def.color.r, def.color.g, def.color.b, 0.55f)
                : new Color(1f, 0.3f, 0.3f, 0.45f);

            if (mouse.leftButton.wasPressedThisFrame && PlacementValid)
            {
                // Don't spend now — a builder hauls the materials to the site and
                // consumes them on pickup, then constructs the building.
                ConstructionSite.Spawn(def, world);
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

        public bool CanAfford(BuildingDefinition def) => def != null && Economy.CanAfford(def.cost, Carried);

        public void AssignSelected() { var c = SelectedCollector; if (c != null) c.TryAssign(); }
        public void UnassignSelected() { var c = SelectedCollector; if (c != null) c.Unassign(); }
        public void Deselect() => Selected = null;

        public void DemolishSelected()
        {
            if (Selected == null) return;

            // Cancelling a construction site: undelivered materials were never
            // spent, so there's nothing to refund — just remove it.
            if (Selected.GetComponent<ConstructionSite>() != null)
            {
                Destroy(Selected);
                Selected = null;
                return;
            }

            var pb = Selected.GetComponent<ProductionBuilding>();
            var sb = Selected.GetComponent<StorageBuilding>();
            var hb = Selected.GetComponent<HousingBuilding>();
            BuildingDefinition rdef = pb != null ? pb.def : sb != null ? sb.def : hb != null ? hb.def : null;
            if (rdef == null) return;

            if (pb != null) while (pb.AssignedWorkers > 0) pb.Unassign();
            if (Carried != null)
                foreach (var c in rdef.cost)
                    Carried.Add(c.item, Mathf.Max(0, c.amount / 2));

            BuildingsPlaced = Mathf.Max(0, BuildingsPlaced - 1);
            Destroy(Selected);
            Selected = null;
        }

        private GameObject BuildingGOUnderCursor(Mouse mouse)
        {
            if (_cam == null || mouse == null) return null;
            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(world);
            if (hit == null) return null;
            bool isBuilding = hit.GetComponent<ProductionBuilding>() != null
                              || hit.GetComponent<StorageBuilding>() != null
                              || hit.GetComponent<HousingBuilding>() != null
                              || hit.GetComponent<ConstructionSite>() != null;
            return isBuilding ? hit.gameObject : null;
        }

        private static bool HasMatchingNodeNear(Vector3 pos, ItemDefinition item, float range)
        {
            float rsq = range * range;
            foreach (var n in FindObjectsByType<ResourceNode>())
            {
                if (n == null || n.yields != item) continue;
                if (((Vector2)(n.transform.position - pos)).sqrMagnitude <= rsq) return true;
            }
            return false;
        }
    }
}
