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
        public Belt.Dir BeltDir { get; private set; } = Belt.Dir.E;
        public BuildingDefinition PendingDef =>
            PendingIndex >= 0 && PendingIndex < buildables.Count ? buildables[PendingIndex] : null;

        public static bool IsPlacing { get; private set; }

        private Inventory Carried => gatherer != null ? gatherer.Inventory : null;
        private Camera _cam;
        private GameObject _ghost;
        private SpriteRenderer _ghostSr;
        private bool _dragging;
        private Vector2Int _dragLast;

        void Awake() => _cam = Camera.main;

        void Update()
        {
            if (_cam == null) _cam = Camera.main;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null) return;

            for (int i = 0; i < buildables.Count && i < 9; i++)
                if (kb[Key.Digit1 + i].wasPressedThisFrame) { BeginPlacement(i); break; }
            if (buildables.Count >= 10 && kb.digit0Key.wasPressedThisFrame) BeginPlacement(9);

            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (PendingIndex >= 0) CancelPlacement();
                else Selected = null;
            }

            if (PendingIndex >= 0)
            {
                if (buildables[PendingIndex] != null && buildables[PendingIndex].kind == BuildingKind.Belt)
                    UpdateBeltPlacement(mouse, kb);
                else
                    UpdatePlacement(mouse);
                return;
            }

            // --- Selection mode ---
            bool overUI = InventoryHud.PointerOverUI;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !overUI)
                Selected = BuildingGOUnderCursor(mouse);

            var staff = SelectedStaffable;
            if (staff != null)
            {
                if (kb.rightBracketKey.wasPressedThisFrame) staff.TryAssign();
                if (kb.leftBracketKey.wasPressedThisFrame) staff.Unassign();
            }

            if (kb.xKey.wasPressedThisFrame) DemolishSelected();
        }

        public IStaffable SelectedStaffable =>
            Selected != null ? Selected.GetComponent<IStaffable>() : null;

        public bool IsUnlocked(BuildingDefinition def) =>
            def != null && (Colony.Instance == null || def.unlockAge <= Colony.Instance.Age);

        public void BeginPlacement(int index)
        {
            if (index < 0 || index >= buildables.Count || buildables[index] == null) return;
            if (!IsUnlocked(buildables[index])) return; // locked until a later age
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
            world.x = Mathf.Round(world.x); // snap buildings to the same grid as belts
            world.y = Mathf.Round(world.y);
            world.z = 0f;
            _ghost.transform.position = world;
            _ghost.transform.rotation = Quaternion.identity;
            _ghostSr.sprite = PlaceholderArt.Square();
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
                if (Economy.FreeBuild) ConstructionSite.SpawnFinished(def, world); // sandbox: instant
                else ConstructionSite.Spawn(def, world); // builders haul materials, then construct
                BuildingsPlaced++;
                CancelPlacement();
            }
            else if (mouse.rightButton.wasPressedThisFrame)
            {
                CancelPlacement();
            }
        }

        // Belt mode: lay directional conveyor segments. R rotates, left-click places
        // (stays in mode so you can lay a line), right-click / Esc finishes.
        private void UpdateBeltPlacement(Mouse mouse, Keyboard kb)
        {
            if (_cam == null || mouse == null || _ghost == null) return;
            var def = buildables[PendingIndex];

            if (kb.rKey.wasPressedThisFrame) BeltDir = Belt.RotateCW(BeltDir);

            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            Vector2Int cell = Belt.CellOf(world);

            // Auto-direction: along the drag path, else auto-oriented toward a sink / away from a source.
            Belt.Dir dir = BeltDir;
            if (_dragging && Adjacent(cell, _dragLast)) dir = Belt.FromTo(_dragLast, cell);
            else if (!_dragging) dir = AutoBeltDir(cell);

            _ghost.transform.position = new Vector3(cell.x, cell.y, 0f);
            _ghost.transform.rotation = Quaternion.Euler(0f, 0f, Belt.Angle(dir));
            _ghost.transform.localScale = Vector3.one * 0.8f;
            _ghostSr.sprite = PlaceholderArt.Triangle();

            bool affordable = Economy.CanAfford(def.cost, Carried);
            bool free = Belt.At(cell) == null;
            PlacementValid = affordable && free;
            _ghostSr.color = PlacementValid
                ? new Color(def.color.r, def.color.g, def.color.b, 0.6f)
                : new Color(1f, 0.3f, 0.3f, 0.45f);

            if (!mouse.leftButton.isPressed) _dragging = false;

            if (mouse.leftButton.isPressed && PlacementValid)
            {
                if (_dragging && Adjacent(cell, _dragLast))
                {
                    var prev = Belt.At(_dragLast);
                    if (prev != null) prev.SetDir(Belt.FromTo(_dragLast, cell)); // corner: previous flows into this
                    dir = Belt.FromTo(_dragLast, cell);
                }
                Economy.Spend(def.cost, Carried);
                Belt.Spawn(cell, dir, def.interval);
                BuildingsPlaced++;
                _dragging = true;
                _dragLast = cell;
                BeltDir = dir;
            }
            else if (mouse.rightButton.wasPressedThisFrame)
            {
                CancelPlacement();
            }
        }

        private static bool Adjacent(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }

        private Belt.Dir AutoBeltDir(Vector2Int cell)
        {
            for (int i = 0; i < 4; i++) { var d = (Belt.Dir)i; if (Belt.IsStorageCell(cell + Belt.Step(d))) return d; }
            for (int i = 0; i < 4; i++) { var d = (Belt.Dir)i; if (Belt.IsSourceCell(cell + Belt.Step(d))) return Belt.Opposite(d); }
            return BeltDir;
        }

        private void CancelPlacement()
        {
            PendingIndex = -1;
            PlacementValid = false;
            IsPlacing = false;
            _dragging = false;
            if (_ghost != null) _ghost.SetActive(false);
        }

        public bool CanAfford(BuildingDefinition def) => def != null && Economy.CanAfford(def.cost, Carried);

        public void AssignSelected() { var s = SelectedStaffable; if (s != null) s.TryAssign(); }
        public void UnassignSelected() { var s = SelectedStaffable; if (s != null) s.Unassign(); }
        public void Deselect() => Selected = null;

        public void DemolishSelected()
        {
            if (Selected == null) return;

            // Cancelling a construction site: undelivered materials were never
            // spent, so there's nothing to refund — just remove it.
            if (Selected.GetComponent<ConstructionSite>() != null || Selected.GetComponent<Belt>() != null)
            {
                Destroy(Selected); // belts & unbuilt sites: nothing to refund
                Selected = null;
                return;
            }

            var pb = Selected.GetComponent<ProductionBuilding>();
            var sb = Selected.GetComponent<StorageBuilding>();
            var hb = Selected.GetComponent<HousingBuilding>();
            var wb = Selected.GetComponent<WorkshopBuilding>();
            var th = Selected.GetComponent<TransportHub>();
            BuildingDefinition rdef = pb != null ? pb.def : sb != null ? sb.def : hb != null ? hb.def
                : wb != null ? wb.def : th != null ? th.def : null;
            if (rdef == null) return;

            var staff = Selected.GetComponent<IStaffable>();
            if (staff != null) while (staff.AssignedWorkers > 0) staff.Unassign();
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
                              || hit.GetComponent<WorkshopBuilding>() != null
                              || hit.GetComponent<TransportHub>() != null
                              || hit.GetComponent<Belt>() != null
                              || hit.GetComponent<ConstructionSite>() != null;
            return isBuilding ? hit.gameObject : null;
        }

        private static bool HasMatchingNodeNear(Vector3 pos, ItemDefinition item, float range)
        {
            float rsq = range * range;
            foreach (var n in ResourceNode.All)
            {
                if (n == null || n.yields != item) continue;
                if (((Vector2)(n.transform.position - pos)).sqrMagnitude <= rsq) return true;
            }
            return false;
        }
    }
}
