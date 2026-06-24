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
        private Depot _routeA; // first depot picked when linking a route
        private GameObject _highlight; // glow ring around the selected building
        public Belt.Dir BuildDir { get; private set; } = Belt.Dir.E; // output side for the building being placed
        private GameObject _ghostArrow; // output-side marker on the ghost
        private SpriteRenderer _ghostArrowSr;
        private GameObject _ghostNotch; // input-side marker on the ghost
        private SpriteRenderer _ghostNotchSr;

        void Awake() => _cam = Camera.main;

        void Update()
        {
            UpdateHighlight();
            // While placing a collector, make its target resource patches glow so it's
            // obvious where it must go. Cleared whenever we're not placing a collector.
            var pd = PendingDef;
            SetResourceHighlight(pd != null && pd.kind == BuildingKind.Collector ? pd.item : null);

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
                var pk = buildables[PendingIndex] != null ? buildables[PendingIndex].kind : BuildingKind.Collector;
                if (pk == BuildingKind.Belt) UpdateBeltPlacement(mouse, kb);
                else if (pk == BuildingKind.Route) UpdateRoutePlacement(mouse);
                else if (pk == BuildingKind.Bridge) UpdateBridgePlacement(mouse);
                else if (pk == BuildingKind.Pipe) UpdatePipePlacement(mouse);
                else UpdatePlacement(mouse);
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
            if (kb.cKey.wasPressedThisFrame && Selected != null) CopySelected();
        }

        /// <summary>Pick the selected building's type as the active placement (quick repeat).</summary>
        private void CopySelected()
        {
            if (Selected == null) return;
            var pb = Selected.GetComponent<ProductionBuilding>();
            var sb = Selected.GetComponent<StorageBuilding>();
            var hb = Selected.GetComponent<HousingBuilding>();
            var wb = Selected.GetComponent<WorkshopBuilding>();
            var dpo = Selected.GetComponent<Depot>();
            var pp = Selected.GetComponent<PowerPlant>();
            var cy = Selected.GetComponent<ConstructionYard>();
            var wp = Selected.GetComponent<WaterPump>();
            BuildingDefinition def = pb != null ? pb.def : sb != null ? sb.def : hb != null ? hb.def
                : wb != null ? wb.def : dpo != null ? dpo.def : pp != null ? pp.def : cy != null ? cy.def
                : wp != null ? wp.def : null;
            if (def == null) return;
            int idx = buildables.IndexOf(def);
            if (idx >= 0 && IsUnlocked(def)) BeginPlacement(idx);
        }

        // Glow all resource patches of a given type (the collector's target); pass null to clear.
        private ItemDefinition _highlightItem;
        private void SetResourceHighlight(ItemDefinition item)
        {
            if (item == _highlightItem) return;
            if (_highlightItem != null)
                foreach (var n in ResourceNode.All)
                    if (n != null && n.yields == _highlightItem) n.SetHighlighted(false);
            _highlightItem = item;
            if (_highlightItem != null)
                foreach (var n in ResourceNode.All)
                    if (n != null && n.yields == _highlightItem) n.SetHighlighted(true);
        }

        // A soft glowing square behind the selected building so it's obvious what's selected.
        private void UpdateHighlight()
        {
            if (_highlight == null)
            {
                _highlight = new GameObject("SelectionHighlight");
                var sr = _highlight.AddComponent<SpriteRenderer>();
                sr.sprite = PlaceholderArt.Square();
                sr.color = new Color(1f, 0.95f, 0.4f, 0.5f);
                sr.sortingOrder = 4; // just under buildings (5) so it reads as a border
            }
            if (Selected != null)
            {
                if (!_highlight.activeSelf) _highlight.SetActive(true);
                var p = Selected.transform.position;
                _highlight.transform.position = new Vector3(p.x, p.y, 0f);
                _highlight.transform.localScale = Vector3.one * (1.25f + 0.08f * Mathf.Sin(Time.unscaledTime * 5f));
            }
            else if (_highlight.activeSelf) _highlight.SetActive(false);
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
            var kb = Keyboard.current;

            // Port buildings have a rotatable facing — R cycles it. Output (green arrow) is
            // on BuildDir; input (cyan notch) is opposite. Belts pull output only from the
            // arrow side and deliver inputs only on the notch side.
            bool hasPorts = def.kind == BuildingKind.Collector || def.kind == BuildingKind.Workshop
                            || def.kind == BuildingKind.Storage;
            if (hasPorts && kb != null && kb.rKey.wasPressedThisFrame) BuildDir = Belt.RotateCW(BuildDir);

            Vector3 raw = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            // Snap to a valid CENTRE for this footprint (half-integer for even sizes).
            Vector3 world = Footprint.SnapCenter(raw, def.FootW, def.FootH);
            world.z = 0f;
            _ghost.transform.position = world;
            _ghost.transform.rotation = Quaternion.identity;
            _ghostSr.sprite = PlaceholderArt.Square();
            float gb = def.kind == BuildingKind.Collector ? 0.9f : 1.0f;
            _ghost.transform.localScale = new Vector3(def.FootW * gb, def.FootH * gb, 1f);
            UpdateGhostPorts(world, def);

            bool affordable = Economy.CanAfford(def.cost, Carried);
            bool placeOk;
            if (def.kind == BuildingKind.Collector)
                placeOk = HasMatchingNodeNear(world, def.item, placeNodeRange)
                          || (def.fromWaterTerrain && TerrainGrid.HasWaterNear(world, placeNodeRange));
            else if (def.kind == BuildingKind.Pump)
                placeOk = TerrainGrid.HasWaterNear(world, placeNodeRange); // pump must reach water
            else placeOk = true;
            bool free = !FootprintBlocked(world, def) && FootprintOnLand(world, def);
            PlacementValid = affordable && placeOk && free;

            // Clear green = OK, red = not OK (don't tint by the building's own colour,
            // which can look like the red "invalid" state — that was the confusion).
            _ghostSr.color = PlacementValid
                ? new Color(0.35f, 1f, 0.4f, 0.55f)
                : new Color(1f, 0.3f, 0.3f, 0.5f);

            // Intentional placement: ONE building per deliberate click (no hold-drag spam —
            // that's reserved for belts/mass infrastructure). Stays in placement mode so you
            // can place several with discrete clicks; right-click / Esc finishes. The
            // PointerOverUI guard stops a build-menu click from also dropping a building.
            if (mouse.leftButton.wasPressedThisFrame && PlacementValid && !InventoryHud.PointerOverUI)
            {
                if (Economy.FreeBuild) ConstructionSite.SpawnFinished(def, world, BuildDir); // sandbox: instant
                else ConstructionSite.Spawn(def, world, BuildDir); // builders haul materials, then construct
                BuildingsPlaced++;
            }
            if (mouse.rightButton.wasPressedThisFrame) CancelPlacement();
        }

        // Show the ghost's I/O markers: green output arrow (collectors/workshops) on BuildDir,
        // cyan input notch (workshops/storages) on the opposite side. Hidden otherwise.
        private void UpdateGhostPorts(Vector3 center, BuildingDefinition def)
        {
            bool hasOut = def.kind == BuildingKind.Collector || def.kind == BuildingKind.Workshop;
            bool hasIn = def.kind == BuildingKind.Workshop || def.kind == BuildingKind.Storage;
            PlaceGhostMarker(ref _ghostArrow, ref _ghostArrowSr, hasOut, center, def, BuildDir, true);
            PlaceGhostMarker(ref _ghostNotch, ref _ghostNotchSr, hasIn, center, def, Belt.Opposite(BuildDir), false);
        }

        private void PlaceGhostMarker(ref GameObject go, ref SpriteRenderer sr, bool show,
            Vector3 center, BuildingDefinition def, Belt.Dir side, bool isOutput)
        {
            if (go == null)
            {
                go = new GameObject(isOutput ? "GhostArrow" : "GhostNotch");
                sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = isOutput ? PlaceholderArt.Triangle() : PlaceholderArt.Square();
                sr.color = isOutput ? new Color(0.25f, 0.95f, 0.35f, 0.95f) : new Color(0.35f, 0.70f, 1f, 0.95f);
                sr.sortingOrder = 21;
            }
            if (!show) { if (go.activeSelf) go.SetActive(false); return; }
            if (!go.activeSelf) go.SetActive(true);
            var s = Belt.Step(side);
            float ext = (s.x != 0 ? def.FootW : def.FootH) * 0.5f + 0.25f;
            go.transform.position = center + new Vector3(s.x, s.y, 0f) * ext;
            go.transform.rotation = isOutput ? Quaternion.Euler(0f, 0f, Belt.Angle(side)) : Quaternion.identity;
            go.transform.localScale = Vector3.one * (isOutput ? 0.5f : 0.4f);
        }

        // True if ANY cell this footprint would cover is already taken.
        private bool FootprintBlocked(Vector3 center, BuildingDefinition def)
        {
            foreach (var c in Footprint.Cells(center, def.FootW, def.FootH))
                if (CellOccupied(new Vector3(c.x, c.y, 0f))) return true;
            return false;
        }

        // True only if EVERY footprint cell is on buildable terrain (not water).
        private bool FootprintOnLand(Vector3 center, BuildingDefinition def)
        {
            foreach (var c in Footprint.Cells(center, def.FootW, def.FootH))
                if (!TerrainGrid.Buildable(c)) return false;
            return true;
        }

        private bool CellOccupied(Vector3 world)
        {
            var hits = Physics2D.OverlapPointAll(world);
            foreach (var h in hits)
            {
                if (h == null) continue;
                if (h.GetComponent<ProductionBuilding>() || h.GetComponent<StorageBuilding>()
                    || h.GetComponent<HousingBuilding>() || h.GetComponent<WorkshopBuilding>()
                    || h.GetComponent<TransportHub>() || h.GetComponent<Depot>()
                    || h.GetComponent<PowerPlant>() || h.GetComponent<ConstructionYard>()
                    || h.GetComponent<WaterPump>() || h.GetComponent<ConstructionSite>()) return true;
            }
            return false;
        }

        // Belt mode: lay directional conveyor segments. R rotates, left-click places
        // (stays in mode so you can lay a line), right-click / Esc finishes.
        private void UpdateBeltPlacement(Mouse mouse, Keyboard kb)
        {
            if (_cam == null || mouse == null || _ghost == null) return;
            if (_ghostArrow != null) _ghostArrow.SetActive(false); // belts have no I/O ports
            if (_ghostNotch != null) _ghostNotch.SetActive(false);
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
            bool free = Belt.At(cell) == null && TerrainGrid.BeltAllowed(cell); // water only if bridged
            PlacementValid = affordable && free;
            _ghostSr.color = PlacementValid
                ? new Color(0.35f, 1f, 0.4f, 0.6f)
                : new Color(1f, 0.3f, 0.3f, 0.5f);

            if (!mouse.leftButton.isPressed) _dragging = false;

            if (mouse.leftButton.isPressed)
            {
                if (!_dragging)
                {
                    EnsureBelt(cell, dir, def); // first cell (auto-oriented)
                    _dragging = true;
                    _dragLast = cell;
                    BeltDir = dir;
                }
                else if (cell != _dragLast)
                {
                    // Fill an L-shaped path from the last cell to here, orienting each
                    // belt toward the next — snapped corners + full 90° lines in one drag.
                    DragBeltPath(_dragLast, cell, def);
                    _dragLast = cell;
                    var here = Belt.At(cell);
                    if (here != null) BeltDir = here.dir;
                }
            }
            else if (mouse.rightButton.wasPressedThisFrame)
            {
                CancelPlacement();
            }
        }

        // Place a belt at `cell` pointing `d`, or re-orient the one already there.
        private void EnsureBelt(Vector2Int cell, Belt.Dir d, BuildingDefinition def)
        {
            var existing = Belt.At(cell);
            if (existing != null) { existing.SetDir(d); return; }
            if (!TerrainGrid.BeltAllowed(cell)) return; // water only if bridged
            if (!Economy.CanAfford(def.cost, Carried)) return;
            Economy.Spend(def.cost, Carried);
            Belt.Spawn(cell, d, def.interval);
            BuildingsPlaced++;
        }

        // Lay an L-shaped run from `from` to `to` (longer axis first), placing/orienting a
        // belt in every cell so each flows into the next — corners snap automatically.
        private void DragBeltPath(Vector2Int from, Vector2Int to, BuildingDefinition def)
        {
            int dx = to.x - from.x, dy = to.y - from.y;
            int sx = dx > 0 ? 1 : dx < 0 ? -1 : 0;
            int sy = dy > 0 ? 1 : dy < 0 ? -1 : 0;

            var path = new List<Vector2Int> { from };
            var c = from;
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            {
                for (int i = 0; i < Mathf.Abs(dx); i++) { c.x += sx; path.Add(c); }
                for (int i = 0; i < Mathf.Abs(dy); i++) { c.y += sy; path.Add(c); }
            }
            else
            {
                for (int i = 0; i < Mathf.Abs(dy); i++) { c.y += sy; path.Add(c); }
                for (int i = 0; i < Mathf.Abs(dx); i++) { c.x += sx; path.Add(c); }
            }

            for (int i = 0; i < path.Count - 1; i++)
                EnsureBelt(path[i], Belt.FromTo(path[i], path[i + 1]), def);
            // The final cell continues in the last segment's direction.
            if (path.Count >= 2)
                EnsureBelt(path[path.Count - 1], Belt.FromTo(path[path.Count - 2], path[path.Count - 1]), def);
        }

        // Bridge mode: lay plank tiles on WATER cells (hold-drag across a river). Bridges
        // make water passable for feet + belts; placed instantly (builders can't reach water).
        private void UpdateBridgePlacement(Mouse mouse)
        {
            if (_cam == null || mouse == null || _ghost == null) return;
            if (_ghostArrow != null) _ghostArrow.SetActive(false);
            if (_ghostNotch != null) _ghostNotch.SetActive(false);
            var def = buildables[PendingIndex];

            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            var cell = new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
            _ghost.transform.position = new Vector3(cell.x, cell.y, 0f);
            _ghost.transform.rotation = Quaternion.identity;
            _ghostSr.sprite = PlaceholderArt.Square();
            _ghost.transform.localScale = Vector3.one;

            bool ok = TerrainGrid.IsWater(cell) && !TerrainGrid.IsBridged(cell) && Economy.CanAfford(def.cost, Carried);
            PlacementValid = ok;
            _ghostSr.color = ok ? new Color(0.35f, 1f, 0.4f, 0.55f) : new Color(1f, 0.3f, 0.3f, 0.5f);

            if (!mouse.leftButton.isPressed) _dragging = false;
            if (mouse.leftButton.isPressed && ok && !InventoryHud.PointerOverUI)
            {
                if (!_dragging || cell != _dragLast)
                {
                    Economy.Spend(def.cost, Carried);
                    Bridge.Spawn(def, new Vector3(cell.x, cell.y, 0f));
                    BuildingsPlaced++;
                    _dragging = true;
                    _dragLast = cell;
                }
            }
            if (mouse.rightButton.wasPressedThisFrame) CancelPlacement();
        }

        // Pipe mode: drag to lay liquid-network segments on land (or bridged water). No
        // direction; placed instantly. A Water Pump pushes water through them into storage.
        private void UpdatePipePlacement(Mouse mouse)
        {
            if (_cam == null || mouse == null || _ghost == null) return;
            if (_ghostArrow != null) _ghostArrow.SetActive(false);
            if (_ghostNotch != null) _ghostNotch.SetActive(false);
            var def = buildables[PendingIndex];

            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            var cell = new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
            _ghost.transform.position = new Vector3(cell.x, cell.y, 0f);
            _ghost.transform.rotation = Quaternion.identity;
            _ghostSr.sprite = PlaceholderArt.Square();
            _ghost.transform.localScale = Vector3.one * 0.6f;

            bool ok = PipeNet.At(cell) == null && TerrainGrid.BeltAllowed(cell) && Economy.CanAfford(def.cost, Carried);
            PlacementValid = ok;
            _ghostSr.color = ok ? new Color(0.35f, 1f, 0.4f, 0.55f) : new Color(1f, 0.3f, 0.3f, 0.5f);

            if (!mouse.leftButton.isPressed) _dragging = false;
            if (mouse.leftButton.isPressed && ok && !InventoryHud.PointerOverUI)
            {
                if (!_dragging || cell != _dragLast)
                {
                    Economy.Spend(def.cost, Carried);
                    Pipe.Spawn(def, new Vector3(cell.x, cell.y, 0f));
                    BuildingsPlaced++;
                    _dragging = true;
                    _dragLast = cell;
                }
            }
            if (mouse.rightButton.wasPressedThisFrame) CancelPlacement();
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

        // Route mode: click depot A, then depot B, to run a caravan between them.
        private void UpdateRoutePlacement(Mouse mouse)
        {
            if (_ghost != null) _ghost.SetActive(false);
            if (_ghostArrow != null) _ghostArrow.SetActive(false);
            if (_ghostNotch != null) _ghostNotch.SetActive(false);
            if (_cam == null || mouse == null) return;

            if (mouse.rightButton.wasPressedThisFrame) { _routeA = null; CancelPlacement(); return; }
            if (!mouse.leftButton.wasPressedThisFrame || InventoryHud.PointerOverUI) return;

            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            var hit = Physics2D.OverlapPoint(world);
            var depot = hit != null ? hit.GetComponent<Depot>() : null;
            if (depot == null) return;

            if (_routeA == null) { _routeA = depot; return; } // first endpoint
            if (depot == _routeA) return;

            var def = buildables[PendingIndex];
            if (Economy.CanAfford(def.cost, Carried))
            {
                Economy.Spend(def.cost, Carried);
                RouteVehicle.Spawn(_routeA, depot, Mathf.Max(1, def.capacity), Mathf.Max(0.5f, def.vehicleSpeed), def.color);
            }
            _routeA = null; // ready for the next route; stay in mode
        }

        public bool RoutePickingFirst => PendingDef != null && PendingDef.kind == BuildingKind.Route && _routeA == null;

        private void CancelPlacement()
        {
            PendingIndex = -1;
            PlacementValid = false;
            IsPlacing = false;
            _dragging = false;
            _routeA = null;
            if (_ghost != null) _ghost.SetActive(false);
            if (_ghostArrow != null) _ghostArrow.SetActive(false);
            if (_ghostNotch != null) _ghostNotch.SetActive(false);
        }

        public bool CanAfford(BuildingDefinition def) => def != null && Economy.CanAfford(def.cost, Carried);

        public void AssignSelected() { var s = SelectedStaffable; if (s != null) s.TryAssign(); }
        public void UnassignSelected() { var s = SelectedStaffable; if (s != null) s.Unassign(); }
        public void Deselect() => Selected = null;

        public void DemolishSelected()
        {
            if (Selected == null) return;

            // The Town Hall (HQ) manages builders and sets the starting pop cap —
            // demolishing it would break both, so it's protected.
            var hqCheck = Selected.GetComponent<HousingBuilding>();
            if (hqCheck != null && hqCheck.isHQ)
            {
                Toast.Show("<color=#f99>The Town Hall can't be demolished.</color>");
                return;
            }

            // Cancelling a construction site: undelivered materials were never
            // spent, so there's nothing to refund — just remove it.
            if (Selected.GetComponent<ConstructionSite>() != null || Selected.GetComponent<Belt>() != null
                || Selected.GetComponent<Bridge>() != null || Selected.GetComponent<Pipe>() != null)
            {
                Destroy(Selected); // belts, bridges, pipes & unbuilt sites: nothing to refund
                Selected = null;
                return;
            }

            var pb = Selected.GetComponent<ProductionBuilding>();
            var sb = Selected.GetComponent<StorageBuilding>();
            var hb = Selected.GetComponent<HousingBuilding>();
            var wb = Selected.GetComponent<WorkshopBuilding>();
            var th = Selected.GetComponent<TransportHub>();
            var dpo = Selected.GetComponent<Depot>();
            var pp = Selected.GetComponent<PowerPlant>();
            var cy = Selected.GetComponent<ConstructionYard>();
            var wp = Selected.GetComponent<WaterPump>();
            BuildingDefinition rdef = pb != null ? pb.def : sb != null ? sb.def : hb != null ? hb.def
                : wb != null ? wb.def : th != null ? th.def : dpo != null ? dpo.def : pp != null ? pp.def
                : cy != null ? cy.def : wp != null ? wp.def : null;
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
                              || hit.GetComponent<Depot>() != null
                              || hit.GetComponent<PowerPlant>() != null
                              || hit.GetComponent<ConstructionYard>() != null
                              || hit.GetComponent<WaterPump>() != null
                              || hit.GetComponent<Bridge>() != null
                              || hit.GetComponent<Pipe>() != null
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
