using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Caveman
{
    /// <summary>
    /// Placement + selection. Number keys / the build menu pick a building; the ghost follows the
    /// cursor (green = valid) and a click places the FINISHED building INSTANTLY (cost paid up front,
    /// no construction units). Left-click a placed building to SELECT it; the HUD panel shows its
    /// status / pause / demolish. Collectors need a nearby resource patch; most things place anywhere.
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
        // Transport is now managed FROM a Station: vehicle tiers live here (not the build menu), and
        // a route is created by selecting a Station, pressing "+ Add route", then clicking another.
        public List<BuildingDefinition> routeTiers = new();
        public Depot LinkFrom { get; private set; } // the Station we're drawing a route FROM (or null)
        private BuildingDefinition _linkTier;

        /// <summary>Best vehicle tier the player can AFFORD right now (newest/biggest first). Falls
        /// back to the cheapest unlocked tier when nothing is affordable, so the panel still shows a
        /// tier + its cost to gather toward (instead of dead-ending on an unaffordable top tier).</summary>
        public BuildingDefinition BestRouteTier()
        {
            int age = Colony.Instance != null ? Colony.Instance.Age : 0;
            BuildingDefinition bestAfford = null, fallback = null;
            foreach (var t in routeTiers)
            {
                if (t == null || t.unlockAge > age) continue;
                if (fallback == null || t.unlockAge < fallback.unlockAge) fallback = t; // earliest = cheapest
                if (Economy.CanAfford(t.cost, Carried)
                    && (bestAfford == null || t.unlockAge > bestAfford.unlockAge
                        || (t.unlockAge == bestAfford.unlockAge && t.capacity > bestAfford.capacity))) bestAfford = t;
            }
            return bestAfford ?? fallback;
        }

        /// <summary>Start drawing a route from this Station; the next Station click completes it.</summary>
        public void BeginStationLink(Depot from)
        {
            if (from == null) return;
            _linkTier = BestRouteTier();
            if (_linkTier == null) { Toast.Show("<color=#f99>No transport vehicle unlocked yet.</color>"); return; }
            CancelPlacement(); // leave any build-placement mode
            LinkFrom = from;
            Toast.Show($"<color=#ffd24d>Click the DESTINATION Station.</color> A {_linkTier.displayName} will auto-shuttle goods between them — no track to lay. (Esc cancels.)");
        }

        public void CancelLink() { LinkFrom = null; _linkTier = null; }

        /// <summary>Upgrade EXISTING routes to the newest unlocked vehicle tier in place — the
        /// "Donkey Track → Train" path persists without rebuilding. Called when the age advances.</summary>
        public void UpgradeAllRoutes()
        {
            var tier = BestRouteTier();
            if (tier == null) return;
            foreach (var rv in RouteVehicle.All)
                if (rv != null) rv.SetTier(Mathf.Max(1, tier.capacity), Mathf.Max(0.5f, tier.vehicleSpeed), tier.color);
        }

        private void CompleteStationLink(Depot dst)
        {
            if (LinkFrom == null || _linkTier == null) { CancelLink(); return; }
            if (Economy.CanAfford(_linkTier.cost, Carried))
            {
                Economy.Spend(_linkTier.cost, Carried);
                RouteVehicle.Spawn(LinkFrom, dst, Mathf.Max(1, _linkTier.capacity),
                    Mathf.Max(0.5f, _linkTier.vehicleSpeed), _linkTier.color);
                Toast.Show($"<color=#9f9>Route created: {_linkTier.displayName}.</color>");
            }
            else Toast.Show($"<color=#f99>Can't afford a {_linkTier.displayName}.</color>");
            CancelLink();
        }
        private GameObject _highlight; // glow ring around the selected building
        public Belt.Dir BuildDir { get; private set; } = Belt.Dir.E; // output side for the building being placed
        // Per-cell I/O markers on the ghost — one per edge cell, so a 2×2 warehouse shows
        // 2 output arrows + 2 input notches (matching the built ports), not a single marker.
        private readonly List<GameObject> _ghostOutPorts = new();
        private readonly List<GameObject> _ghostInPorts = new();
        private readonly Belt.Dir[] _ghostOutSides = new Belt.Dir[1];
        private readonly Belt.Dir[] _ghostInSides = new Belt.Dir[4];

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
                if (LinkFrom != null) CancelLink();
                else if (PendingIndex >= 0) CancelPlacement();
                else Selected = null;
            }

            // Rapid delete (QoL): X or Delete removes the building UNDER THE CURSOR, in any
            // mode (no need to click-select first) — fast correction of placement mistakes.
            if ((kb.xKey.wasPressedThisFrame || kb.deleteKey.wasPressedThisFrame)
                && mouse != null && !InventoryHud.PointerOverUI)
            {
                var under = BuildingGOUnderCursor(mouse);
                if (under != null) { Selected = under; DemolishSelected(); }
                else if (PendingIndex < 0) DemolishSelected(); // nothing hovered: fall back to selection
            }

            if (PendingIndex >= 0)
            {
                var pk = buildables[PendingIndex] != null ? buildables[PendingIndex].kind : BuildingKind.Collector;
                if (pk == BuildingKind.Belt) UpdateBeltPlacement(mouse, kb);
                else if (pk == BuildingKind.Bridge) UpdateBridgePlacement(mouse);
                else if (pk == BuildingKind.Pipe) UpdatePipePlacement(mouse);
                else UpdatePlacement(mouse);
                return;
            }

            // --- Selection mode ---
            bool overUI = InventoryHud.PointerOverUI;

            // Station route linking: after "+ Add route", the next click on another Station creates
            // the route; clicking empty space (or right-click) cancels. Consumes the click.
            if (LinkFrom != null)
            {
                if (mouse != null && mouse.leftButton.wasPressedThisFrame && !overUI)
                {
                    var go = BuildingGOUnderCursor(mouse);
                    var dst = go != null ? go.GetComponent<Depot>() : null;
                    if (dst != null && dst != LinkFrom) CompleteStationLink(dst);
                    else CancelLink();
                }
                if (mouse != null && mouse.rightButton.wasPressedThisFrame) CancelLink();
                return;
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !overUI)
                Selected = BuildingGOUnderCursor(mouse);

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
            var rb = Selected.GetComponent<ResearchBuilding>();
            BuildingDefinition def = pb != null ? pb.def : sb != null ? sb.def : hb != null ? hb.def
                : wb != null ? wb.def : dpo != null ? dpo.def : pp != null ? pp.def : cy != null ? cy.def
                : wp != null ? wp.def : rb != null ? rb.def : null;
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

        public bool IsUnlocked(BuildingDefinition def) =>
            def != null && (Colony.Instance == null || def.unlockAge <= Colony.Instance.Age)
            && Research.IsPurchased(def.requiredTech); // also gated behind a research Tech, if set

        public void BeginPlacement(int index)
        {
            if (index < 0 || index >= buildables.Count || buildables[index] == null) return;
            if (!IsUnlocked(buildables[index])) return; // locked until a later age
            PendingIndex = index;
            IsPlacing = true;
            Selected = null;
            CancelLink(); // can't be route-linking and placing at once
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
                            || def.kind == BuildingKind.Storage || def.kind == BuildingKind.Research;
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

            // Water buildings must sit on LAND in a cell ADJACENT to water (not just "nearby"):
            // a tight range so the player can't plonk them at a distance from the shore.
            const float waterAdj = 1.6f; // reaches an orthogonally/diagonally adjacent water cell
            bool affordable = Economy.CanAfford(def.cost, Carried);
            bool placeOk;
            if (def.kind == BuildingKind.Collector)
                placeOk = HasMatchingNodeNear(world, def.item, placeNodeRange)
                          || (def.fromWaterTerrain && TerrainGrid.HasWaterNear(world, waterAdj));
            else if (def.kind == BuildingKind.Pump)
                placeOk = def.booster || TerrainGrid.HasWaterNear(world, waterAdj); // pump needs adjacent water; booster doesn't
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
                // INSTANT construction: pay the cost and drop the finished building immediately — no
                // builder units, no hauling. (Economy.Spend no-ops in sandbox/FreeBuild, so it stays
                // free there.) The machine starts running by itself.
                Economy.Spend(def.cost, Carried);
                ConstructionSite.SpawnFinished(def, world, BuildDir);
                BuildingsPlaced++;
            }
            if (mouse.rightButton.wasPressedThisFrame) CancelPlacement();
        }

        // Show the ghost's I/O markers PER EDGE CELL (so a 2×2 warehouse previews 2 outputs +
        // 2 inputs, exactly like the built building): green output arrows on BuildDir, cyan
        // input notches on the opposite side. Hidden for kinds with no belt I/O.
        private void UpdateGhostPorts(Vector3 center, BuildingDefinition def)
        {
            bool hasOut = def.kind == BuildingKind.Collector || def.kind == BuildingKind.Workshop
                          || def.kind == BuildingKind.Storage;
            bool hasIn = def.kind == BuildingKind.Workshop || def.kind == BuildingKind.Storage
                         || def.kind == BuildingKind.Research; // Lodge = input only
            bool multiIn = def.kind == BuildingKind.Workshop && def.inputs != null && def.inputs.Count > 1;

            _ghostOutSides[0] = BuildDir;
            PlaceGhostSides(_ghostOutPorts, hasOut, center, def, _ghostOutSides, 1, true);

            // Multi-input workshops show input notches on EVERY non-output side (matches the placed
            // building); everything else shows the single input side opposite the output.
            int n = 0;
            if (multiIn) { for (int i = 0; i < 4; i++) { var d = (Belt.Dir)i; if (d != BuildDir) _ghostInSides[n++] = d; } }
            else { _ghostInSides[0] = Belt.Opposite(BuildDir); n = 1; }
            PlaceGhostSides(_ghostInPorts, hasIn, center, def, _ghostInSides, n, false);
        }

        // One marker per edge cell across `sideCount` sides, positioned just outside each cell's face
        // (mirrors Ports.PlacePorts). Reuses a pooled list, deactivating any surplus markers.
        private void PlaceGhostSides(List<GameObject> pool, bool show, Vector3 center,
            BuildingDefinition def, Belt.Dir[] sides, int sideCount, bool isOutput)
        {
            int used = 0;
            if (show && def != null)
                for (int si = 0; si < sideCount; si++)
                {
                    var side = sides[si];
                    int w = def.FootW, h = def.FootH;
                    var s = Belt.Step(side);
                    float ax = center.x - (w - 1) * 0.5f, ay = center.y - (h - 1) * 0.5f; // bottom-left cell centre
                    for (int i = 0; i < w; i++)
                        for (int j = 0; j < h; j++)
                        {
                            bool edge = side == Belt.Dir.E ? i == w - 1 : side == Belt.Dir.W ? i == 0
                                      : side == Belt.Dir.N ? j == h - 1 : j == 0;
                            if (!edge) continue;
                            var go = GhostMarker(pool, used++, isOutput);
                            go.transform.position = new Vector3(ax + i + s.x * 0.55f, ay + j + s.y * 0.55f, 0f);
                            go.transform.rotation = isOutput ? Quaternion.Euler(0f, 0f, Belt.Angle(side)) : Quaternion.identity;
                            go.transform.localScale = Vector3.one * (isOutput ? 0.5f : 0.4f);
                        }
                }
            for (int k = used; k < pool.Count; k++) if (pool[k].activeSelf) pool[k].SetActive(false);
        }

        private GameObject GhostMarker(List<GameObject> pool, int index, bool isOutput)
        {
            while (pool.Count <= index)
            {
                var go = new GameObject(isOutput ? "GhostArrow" : "GhostNotch");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = isOutput ? PlaceholderArt.Triangle() : PlaceholderArt.Square();
                sr.color = isOutput ? new Color(0.25f, 0.95f, 0.35f, 0.95f) : new Color(0.35f, 0.70f, 1f, 0.95f);
                sr.sortingOrder = 21;
                pool.Add(go);
            }
            if (!pool[index].activeSelf) pool[index].SetActive(true);
            return pool[index];
        }

        // Hide all ghost I/O markers (belt/bridge/pipe/route modes & on cancel).
        private void HideGhostPorts()
        {
            foreach (var g in _ghostOutPorts) if (g != null && g.activeSelf) g.SetActive(false);
            foreach (var g in _ghostInPorts) if (g != null && g.activeSelf) g.SetActive(false);
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

        private bool CellOccupied(Vector3 world) => SolidBuildingAt(world);

        /// <summary>True if a SOLID building (or construction site) overlaps this point — used for
        /// placement blocking, belt-on-building blocking, and player/worker collision. Belts,
        /// bridges, pipes and resource nodes are NOT solid (you build/walk over them).</summary>
        public static bool SolidBuildingAt(Vector3 world)
        {
            var hits = Physics2D.OverlapPointAll(world);
            foreach (var h in hits)
            {
                if (h == null) continue;
                if (h.GetComponent<ProductionBuilding>() || h.GetComponent<StorageBuilding>()
                    || h.GetComponent<HousingBuilding>() || h.GetComponent<WorkshopBuilding>()
                    || h.GetComponent<TransportHub>() || h.GetComponent<Depot>()
                    || h.GetComponent<PowerPlant>() || h.GetComponent<ConstructionYard>()
                    || h.GetComponent<WaterPump>() || h.GetComponent<ResearchBuilding>()
                    || h.GetComponent<ConstructionSite>()) return true;
            }
            return false;
        }

        // Belt mode: lay directional conveyor segments. R rotates, left-click places
        // (stays in mode so you can lay a line), right-click / Esc finishes.
        private void UpdateBeltPlacement(Mouse mouse, Keyboard kb)
        {
            if (_cam == null || mouse == null || _ghost == null) return;
            HideGhostPorts(); // belts have no I/O ports
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
            _ghostSr.sprite = def.splitter || def.merger ? PlaceholderArt.Hexagon() : PlaceholderArt.Triangle(); // splitter/merger distinct

            bool affordable = Economy.CanAfford(def.cost, Carried);
            // Belts can't sit on water (unless bridged) OR on top of a building.
            bool free = Belt.At(cell) == null && TerrainGrid.BeltAllowed(cell)
                        && !SolidBuildingAt(new Vector3(cell.x, cell.y, 0f));
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
            if (SolidBuildingAt(new Vector3(cell.x, cell.y, 0f))) return; // never lay a belt on a building
            if (!Economy.CanAfford(def.cost, Carried)) return;
            Economy.Spend(def.cost, Carried);
            Belt.Spawn(cell, d, def.interval, def.splitter, def.merger);
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
            HideGhostPorts();
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
            HideGhostPorts();
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

        private void CancelPlacement()
        {
            PendingIndex = -1;
            PlacementValid = false;
            IsPlacing = false;
            _dragging = false;
            if (_ghost != null) _ghost.SetActive(false);
            HideGhostPorts();
        }

        public bool CanAfford(BuildingDefinition def) => def != null && Economy.CanAfford(def.cost, Carried);

        public void Deselect() => Selected = null;

        public void DemolishSelected()
        {
            if (Selected == null) return;

            // Cancelling a construction site: its materials were paid at placement, so refund
            // them in full (the real building never formed).
            var siteCancel = Selected.GetComponent<ConstructionSite>();
            if (siteCancel != null)
            {
                if (Carried != null && siteCancel.def != null)
                    foreach (var c in siteCancel.def.cost)
                        if (c.item != null) Carried.Add(c.item, c.amount);
                Destroy(Selected);
                Selected = null;
                return;
            }

            // Belts, bridges, pipes: cheap, removed without refund (unchanged).
            if (Selected.GetComponent<Belt>() != null
                || Selected.GetComponent<Bridge>() != null || Selected.GetComponent<Pipe>() != null)
            {
                Destroy(Selected);
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
            var rsb = Selected.GetComponent<ResearchBuilding>();
            BuildingDefinition rdef = pb != null ? pb.def : sb != null ? sb.def : hb != null ? hb.def
                : wb != null ? wb.def : th != null ? th.def : dpo != null ? dpo.def : pp != null ? pp.def
                : cy != null ? cy.def : wp != null ? wp.def : rsb != null ? rsb.def : null;
            if (rdef == null) return;

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
                              || hit.GetComponent<ResearchBuilding>() != null
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
