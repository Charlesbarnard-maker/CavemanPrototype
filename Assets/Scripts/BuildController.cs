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

        // --- Belt BLUEPRINT placement: drag sketches a plan (no cost), then a click builds it. ---
        private readonly Dictionary<Vector2Int, Belt.Dir> _beltPlan = new(); // planned cell → direction
        private readonly Dictionary<Vector2Int, Belt.Dir> _committed = new(); // strokes finished before the current one
        private readonly List<GameObject> _planGhosts = new();               // translucent previews (pooled)
        private bool _strokeActive, _strokeMoved;                            // current drag stroke state
        private Vector2Int _strokeStart, _strokeLast;
        private bool _beltPlanHinted;
        private bool _beltPlanDirty; // ghosts rebuilt only when the plan changes (not every frame)
        // --- Rail BLUEPRINT placement (same drag-plan-then-click as belts; 90° L-paths only). ---
        private readonly List<Vector2Int> _railPlan = new();
        private readonly List<GameObject> _railPlanGhosts = new();
        private bool _railPlanHinted;
        private bool _railPlanDirty;
        // Ghost in/out port markers for a splitter/merger being placed (so its sides are visible).
        private readonly List<GameObject> _ghostJunctionPorts = new();
        private bool _ghostJunctionBuilt, _ghostJunctionSplitter, _ghostJunctionMerger;
        // Transport is now managed FROM a Station: vehicle tiers live here (not the build menu), and
        // a route is created by selecting a Station, pressing "+ Add route", then clicking another.
        public List<BuildingDefinition> routeTiers = new();
        public List<BuildingDefinition> shipTiers = new(); // cargo-ship tiers for harbour lines (over water)
        public Depot LinkFrom { get; private set; } // the Station we're drawing a route FROM (or null)
        private BuildingDefinition _linkTier;
        public PowerNode WireFrom { get; private set; } // the power node we're drawing a WIRE from (or null)

        /// <summary>Best vehicle tier the player can AFFORD right now (newest/biggest first). Falls
        /// back to the cheapest unlocked tier when nothing is affordable, so the panel still shows a
        /// tier + its cost to gather toward (instead of dead-ending on an unaffordable top tier).</summary>
        public BuildingDefinition BestRouteTier() => BestTier(routeTiers);
        public BuildingDefinition BestShipTier() => BestTier(shipTiers);
        private BuildingDefinition BestTier(List<BuildingDefinition> tiers)
        {
            int age = Colony.Instance != null ? Colony.Instance.Age : 0;
            BuildingDefinition bestAfford = null, fallback = null;
            if (tiers == null) return null;
            foreach (var t in tiers)
            {
                if (t == null || t.unlockAge > age) continue;
                if (fallback == null || t.unlockAge < fallback.unlockAge) fallback = t; // earliest = cheapest
                if (Economy.CanAfford(t.cost, Carried)
                    && (bestAfford == null || t.unlockAge > bestAfford.unlockAge
                        || (t.unlockAge == bestAfford.unlockAge && t.capacity > bestAfford.capacity))) bestAfford = t;
            }
            return bestAfford ?? fallback;
        }

        private readonly List<Depot> _lineStops = new(); // stops being collected for a new line
        public int LineStopCount => _lineStops.Count;
        public bool LineContains(Depot d) => _lineStops.Contains(d);

        /// <summary>Start building a LINE from this Station — click more stations to add stops, then the
        /// first station (or right-click) to finish. The vehicle visits the stops in a loop.</summary>
        public void BeginStationLink(Depot from)
        {
            if (from == null) return;
            bool harbour = from.def != null && from.def.isHarbour;
            _linkTier = harbour ? BestShipTier() : BestRouteTier();
            if (_linkTier == null) { Toast.Show("<color=#f99>No transport vehicle unlocked yet.</color>"); return; }
            CancelPlacement(); // leave any build-placement mode
            CancelWire();
            _lineStops.Clear();
            _lineStops.Add(from);
            LinkFrom = from; // the anchor (first stop)
            string what = harbour ? "Harbour" : "Station";
            Toast.Show($"<color=#ffd24d>Building a {_linkTier.displayName} line.</color> Click the next {what} to add a stop; click the FIRST {what} (or right-click) to finish. The vehicle loops the stops, passing any it doesn't stop at. (Esc cancels.)");
        }

        public void CancelLink() { LinkFrom = null; _linkTier = null; _lineStops.Clear(); }

        // Amber preview through the stops collected so far, out to the cursor — so you see the line forming.
        private void UpdateLinkPreview()
        {
            if (LinkFrom == null || _lineStops.Count == 0)
            {
                if (_linkPreview != null && _linkPreview.gameObject.activeSelf) _linkPreview.gameObject.SetActive(false);
                return;
            }
            if (_linkPreview == null)
            {
                var go = new GameObject("LinePreview");
                _linkPreview = go.AddComponent<LineRenderer>();
                _linkPreview.material = PlaceholderArt.LineMaterial(); // shared
                _linkPreview.widthMultiplier = 0.18f;
                _linkPreview.useWorldSpace = true;
                _linkPreview.numCapVertices = 2;
                _linkPreview.sortingOrder = 12;
                _linkPreview.startColor = _linkPreview.endColor = new Color(1f, 0.85f, 0.3f, 0.8f);
            }
            if (!_linkPreview.gameObject.activeSelf) _linkPreview.gameObject.SetActive(true);
            Vector3 cursor = (_cam != null && Mouse.current != null)
                ? _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue())
                : _lineStops[_lineStops.Count - 1].transform.position;
            cursor.z = 0f;
            int n = _lineStops.Count;
            _linkPreview.positionCount = n + 1;
            for (int i = 0; i < n; i++)
                _linkPreview.SetPosition(i, _lineStops[i] != null ? _lineStops[i].transform.position : cursor);
            _linkPreview.SetPosition(n, cursor);
        }

        // A click on `dst` while building a line: finish if it's the first stop (loop closed), else add it.
        private void OnLineClick(Depot dst)
        {
            if (LinkFrom == null || _linkTier == null) { CancelLink(); return; }
            if (dst == null) return; // clicked empty — keep the in-progress line
            if (dst == _lineStops[0] && _lineStops.Count >= 2) { FinishLine(); return; }
            if (!_lineStops.Contains(dst))
            {
                _lineStops.Add(dst);
                Toast.Show($"<color=#9cf>Stop {_lineStops.Count} added.</color> Click more, or the first station / right-click to finish.");
            }
        }

        private void FinishLine()
        {
            if (LinkFrom == null || _linkTier == null || _lineStops.Count < 2) { CancelLink(); return; }
            if (Economy.CanAfford(_linkTier.cost, Carried))
            {
                Economy.Spend(_linkTier.cost, Carried);
                RouteVehicle.Spawn(new List<Depot>(_lineStops), Mathf.Max(1, _linkTier.capacity),
                    Mathf.Max(0.5f, _linkTier.vehicleSpeed), _linkTier.color);
                Toast.Show($"<color=#9f9>Line created: {_linkTier.displayName} · {_lineStops.Count} stops.</color>");
            }
            else Toast.Show($"<color=#f99>Can't afford a {_linkTier.displayName}.</color>");
            CancelLink();
        }

        // --- Power wiring: draw a cable from one power node to another (mirrors the Station route flow). ---
        /// <summary>Start drawing a wire from this power node; the next click on another power building
        /// completes it (if allowed). Esc / right-click cancels.</summary>
        public void BeginWire(PowerNode from)
        {
            if (from == null) return;
            CancelPlacement();
            CancelLink();
            WireFrom = from;
            Toast.Show("<color=#9cf>Click another power building (Generator, Pole, Battery, or machine) to wire to it.</color>  <size=11>(Esc cancels)</size>");
        }

        public void CancelWire() => WireFrom = null;

        private void CompleteWire(PowerNode to)
        {
            if (WireFrom == null) { CancelWire(); return; }
            string why = WireFrom.LinkBlockedReason(to);
            if (why == null) { WireFrom.Connect(to); Toast.Show("<color=#9f9>Wire connected.</color>"); }
            else Toast.Show($"<color=#f99>Can't wire: {why}.</color>");
            CancelWire();
        }

        /// <summary>Upgrade EXISTING routes to the newest unlocked vehicle tier in place — the
        /// "Donkey Track → Train" path persists without rebuilding. Called when the age advances.</summary>
        public void UpgradeAllRoutes()
        {
            var land = BestRouteTier(); var ship = BestShipTier();
            foreach (var rv in RouteVehicle.All)
            {
                if (rv == null) continue;
                bool isShip = rv.a != null && rv.a.def != null && rv.a.def.isHarbour;
                var tier = isShip ? ship : land;
                if (tier != null) rv.SetTier(Mathf.Max(1, tier.capacity), Mathf.Max(0.5f, tier.vehicleSpeed), tier.color);
            }
        }

        private GameObject _highlight; // glow ring around the selected building
        private LineRenderer _linkPreview; // amber line through the stops being collected (+ cursor)
        public Belt.Dir BuildDir { get; private set; } = Belt.Dir.E; // output side for the building being placed
        // Per-cell I/O markers on the ghost — one per edge cell, so a 2×2 warehouse shows
        // 2 output arrows + 2 input notches (matching the built ports), not a single marker.
        private readonly List<GameObject> _ghostOutPorts = new();
        private readonly List<GameObject> _ghostInPorts = new();
        private readonly Belt.Dir[] _ghostOutSides = new Belt.Dir[1];
        private readonly Belt.Dir[] _ghostInSides = new Belt.Dir[4];

        // Collector range overlay (#1): a ring around the selected collector + a glow on the
        // resource nodes within its harvest reach, so "what this collector can reach" is visible.
        private GameObject _ringGo;
        private LineRenderer _ring;
        private const int RingSegments = 48;
        private readonly List<ResourceNode> _rangeLit = new();
        private ProductionBuilding _rangeFor;

        void Awake() => _cam = Camera.main;

        void Update()
        {
            UpdateHighlight();
            UpdateLinkPreview();
            // Building "reach" indicator: a harvest-radius RING for a collector (selected OR being
            // placed) + a box outline of the input-adjacency cells for any other selected building.
            // Runs BEFORE the placement highlight so that starting a placement cleanly re-lights nodes.
            UpdateReachIndicator();
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
                if (WireFrom != null) CancelWire();
                else if (LinkFrom != null) CancelLink();
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
                else if (pk == BuildingKind.Rail) UpdateRailPlacement(mouse);
                else if (pk == BuildingKind.Signal) UpdateSignalPlacement(mouse, kb);
                else UpdatePlacement(mouse);
                return;
            }

            // --- Selection mode ---
            bool overUI = InventoryHud.PointerOverUI;

            // Power wiring: after "Connect wire", the next click on another power building draws the
            // cable; clicking empty space (or right-click) cancels. Consumes the click.
            if (WireFrom != null)
            {
                if (mouse != null && mouse.leftButton.wasPressedThisFrame && !overUI)
                {
                    var go = BuildingGOUnderCursor(mouse);
                    var node = go != null ? go.GetComponent<PowerNode>() : null;
                    if (node != null && node != WireFrom) CompleteWire(node);
                    else CancelWire();
                }
                if (mouse != null && mouse.rightButton.wasPressedThisFrame) CancelWire();
                return;
            }

            // Station LINE building: after "+ Add line", each click on a Station adds a stop; clicking the
            // FIRST station (or right-click with ≥2 stops) finishes; right-click with <2 cancels. Consumes click.
            if (LinkFrom != null)
            {
                if (mouse != null && mouse.leftButton.wasPressedThisFrame && !overUI)
                {
                    var go = BuildingGOUnderCursor(mouse);
                    OnLineClick(go != null ? go.GetComponent<Depot>() : null);
                }
                if (mouse != null && mouse.rightButton.wasPressedThisFrame)
                {
                    if (_lineStops.Count >= 2) FinishLine(); else CancelLink();
                }
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
            var wb = Selected.GetComponent<WorkshopBuilding>();
            var dpo = Selected.GetComponent<Depot>();
            var pp = Selected.GetComponent<PowerPlant>();
            var pole = Selected.GetComponent<PowerPole>();
            var bat = Selected.GetComponent<Battery>();
            var wp = Selected.GetComponent<WaterPump>();
            var rb = Selected.GetComponent<ResearchBuilding>();
            BuildingDefinition def = pb != null ? pb.def : sb != null ? sb.def
                : wb != null ? wb.def : dpo != null ? dpo.def : pp != null ? pp.def
                : pole != null ? pole.def : bat != null ? bat.def
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

        // Shows what the relevant building REACHES: a collector's harvest RADIUS (a ring) — for the
        // building being PLACED and for a selected collector, plus a glow on the in-range nodes — and,
        // for any OTHER selected building, a box outline of its input-adjacency cells (workshops/
        // storages pull inputs from the cells around them + belts; they have no harvest radius).
        private void UpdateReachIndicator()
        {
            // Placing a collector → show its harvest reach at the ghost, so you can tell the range
            // BEFORE committing (the node glow during placement is handled by SetResourceHighlight).
            var pd = PendingDef;
            if (pd != null && pd.kind == BuildingKind.Collector && _ghost != null && _ghost.activeSelf)
            {
                ClearRangeLit();
                DrawRing(_ghost.transform.position, placeNodeRange);
                return;
            }

            // A collector is selected → harvest-radius ring + glow the in-range nodes (recomputed only
            // when the selection changes).
            var pb = Selected != null ? Selected.GetComponent<ProductionBuilding>() : null;
            if (_rangeFor != pb)
            {
                ClearRangeLit();
                _rangeFor = pb;
                if (pb != null && pb.produces != null)
                {
                    float r = pb.sourceRange;
                    foreach (var n in ResourceNode.All)
                        if (n != null && n.yields == pb.produces && n.HasResource
                            && ((Vector2)(n.transform.position - pb.transform.position)).sqrMagnitude <= r * r)
                        { n.SetHighlighted(true); _rangeLit.Add(n); }
                }
            }
            if (pb != null) { DrawRing(pb.transform.position, pb.sourceRange); return; }

            // Any other selected building (Sawmill, smelter, storage, lodge, …): no harvest radius —
            // it pulls inputs from ADJACENT cells + belts. Show that reach as a box around footprint+1.
            var def = SelectedDef();
            if (def != null) DrawBox(Selected.transform.position, def.FootW * 0.5f + 1f, def.FootH * 0.5f + 1f);
            else HideRing();
        }

        private void ClearRangeLit()
        {
            foreach (var n in _rangeLit) if (n != null) n.SetHighlighted(false);
            _rangeLit.Clear();
            _rangeFor = null;
        }

        private void HideRing() { if (_ringGo != null && _ringGo.activeSelf) _ringGo.SetActive(false); }

        // The shared LineRenderer used for both the circle (collector) and box (other) reach outlines.
        private LineRenderer EnsureRing()
        {
            if (_ringGo == null)
            {
                _ringGo = new GameObject("ReachIndicator");
                _ring = _ringGo.AddComponent<LineRenderer>();
                _ring.material = PlaceholderArt.LineMaterial(); // shared
                _ring.widthMultiplier = 0.14f;
                _ring.loop = true;
                _ring.useWorldSpace = true;
                _ring.numCornerVertices = 0;
                _ring.sortingOrder = 6; // above buildings (5) so the outline is clearly visible
            }
            if (!_ringGo.activeSelf) _ringGo.SetActive(true);
            return _ring;
        }

        // Amber circle — a collector's harvest radius.
        private void DrawRing(Vector3 center, float radius)
        {
            var lr = EnsureRing();
            lr.startColor = lr.endColor = new Color(1f, 0.92f, 0.4f, 0.65f); // amber
            lr.positionCount = RingSegments;
            for (int i = 0; i < RingSegments; i++)
            {
                float a = (i / (float)RingSegments) * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, 0f));
            }
        }

        // Cyan box — the input-adjacency reach of a workshop/storage/etc. (matches the cyan input notches).
        private void DrawBox(Vector3 center, float halfX, float halfY)
        {
            var lr = EnsureRing();
            lr.startColor = lr.endColor = new Color(0.4f, 0.78f, 1f, 0.65f); // cyan = inputs
            lr.positionCount = 4;
            lr.SetPosition(0, new Vector3(center.x - halfX, center.y - halfY, 0f));
            lr.SetPosition(1, new Vector3(center.x + halfX, center.y - halfY, 0f));
            lr.SetPosition(2, new Vector3(center.x + halfX, center.y + halfY, 0f));
            lr.SetPosition(3, new Vector3(center.x - halfX, center.y + halfY, 0f));
        }

        // The BuildingDefinition of the selected building (for its footprint), or null.
        private BuildingDefinition SelectedDef()
        {
            if (Selected == null) return null;
            var wb = Selected.GetComponent<WorkshopBuilding>(); if (wb != null) return wb.def;
            var sb = Selected.GetComponent<StorageBuilding>(); if (sb != null) return sb.def;
            var rb = Selected.GetComponent<ResearchBuilding>(); if (rb != null) return rb.def;
            var dp = Selected.GetComponent<Depot>(); if (dp != null) return dp.def;
            var pp = Selected.GetComponent<PowerPlant>(); if (pp != null) return pp.def;
            var pole = Selected.GetComponent<PowerPole>(); if (pole != null) return pole.def;
            var bat = Selected.GetComponent<Battery>(); if (bat != null) return bat.def;
            return null;
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
            CancelWire();
            ClearBeltPlan();          // a leftover plan from a previous belt type shouldn't carry over
            ClearRailPlan();
            ClearGhostJunctionPorts();
            _strokeActive = false;
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

            // Rotate placement with R. Port buildings re-aim their output (green arrow on BuildDir, input
            // notch opposite); RECTANGULAR buildings (the Station) ALSO turn their footprint between a
            // horizontal and a vertical orientation, so a Station can straddle a north–south track lane.
            bool hasPorts = def.kind == BuildingKind.Collector || def.kind == BuildingKind.Workshop
                            || def.kind == BuildingKind.Storage || def.kind == BuildingKind.Research;
            bool canRotate = hasPorts || def.kind == BuildingKind.Depot || def.kind == BuildingKind.Power;
            if (canRotate && kb != null && kb.rKey.wasPressedThisFrame) BuildDir = Belt.RotateCW(BuildDir);

            EffFoot(def, BuildDir, out int ew, out int eh); // footprint after rotation (swaps W/H when turned vertical)

            Vector3 raw = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            // Snap to a valid CENTRE for this footprint (half-integer for even sizes).
            Vector3 world = Footprint.SnapCenter(raw, ew, eh);
            world.z = 0f;
            _ghost.transform.position = world;
            _ghost.transform.rotation = Quaternion.identity;
            _ghostSr.sprite = SpriteDatabase.ForBuilding(def); // ghost matches the building's resolved sprite
            float gb = def.kind == BuildingKind.Collector ? 0.9f : 1.0f;
            _ghost.transform.localScale = new Vector3(ew * gb, eh * gb, 1f);
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
                placeOk = def.booster || TerrainGrid.HasWaterNear(world, waterAdj)            // Water Pump: water terrain
                          || HasMatchingNodeNear(world, def.item, placeNodeRange);            // Oil Well: an oil deposit
            else if (def.kind == BuildingKind.Depot && def.isHarbour)
                placeOk = TerrainGrid.HasWaterNear(world, Mathf.Max(def.FootW, def.FootH) * 0.5f + 1.4f); // a harbour must touch water
            else placeOk = true;
            // A HARBOUR straddles the shore (half on water, half on land) so the ship can dock; everything
            // else must sit fully on land.
            bool harbourPlace = def.kind == BuildingKind.Depot && def.isHarbour;
            bool free = !FootprintBlocked(world, ew, eh)
                        && (harbourPlace ? FootprintStraddlesShore(world, ew, eh) : FootprintOnLand(world, ew, eh));
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
                // TIMED construction: pay the cost up front, then drop a construction SITE that assembles
                // itself over a few seconds (size-scaled) before becoming the finished building — no builder
                // units, no hauling. (Economy.Spend no-ops in sandbox/FreeBuild.) Cancelling refunds in full.
                Economy.Spend(def.cost, Carried);
                ConstructionSite.Spawn(def, world, BuildDir);
                BuildingsPlaced++;
            }
            if (mouse.rightButton.wasPressedThisFrame) CancelPlacement();
        }

        // Show the ghost's I/O markers PER EDGE CELL (so a 2×2 warehouse previews 2 outputs +
        // 2 inputs, exactly like the built building): green output arrows on BuildDir, cyan
        // input notches on the opposite side. Hidden for kinds with no belt I/O.
        private void UpdateGhostPorts(Vector3 center, BuildingDefinition def)
        {
            // Generator: a single FUEL input notch on the BuildDir edge (R aims it); it has no belt output.
            if (def.kind == BuildingKind.Power)
            {
                _ghostInSides[0] = BuildDir;
                PlaceGhostSides(_ghostInPorts, def.inputs != null && def.inputs.Count > 0, center, def, _ghostInSides, 1, false);
                PlaceGhostSides(_ghostOutPorts, false, center, def, _ghostOutSides, 1, true);
                return;
            }
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
                    int cm = (w - 1) / 2, cn = (h - 1) / 2; // centre edge-cell (single port per side, matching the placed building)
                    for (int i = 0; i < w; i++)
                        for (int j = 0; j < h; j++)
                        {
                            bool edge = side == Belt.Dir.E ? i == w - 1 : side == Belt.Dir.W ? i == 0
                                      : side == Belt.Dir.N ? j == h - 1 : j == 0;
                            if (!edge) continue;
                            if ((side == Belt.Dir.E || side == Belt.Dir.W) ? j != cn : i != cm) continue; // one marker per side
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

        // Effective footprint for the building being placed, after R-rotation. Only RECTANGULAR buildings
        // (today: the Station) swap W/H when turned to a vertical facing (N/S); square footprints are
        // unaffected, so R just re-aims their output port. The placed building must honour the same swap —
        // see Depot.Spawn (the only rectangular kind). Add spawn-side support before adding new rectangular kinds.
        private static void EffFoot(BuildingDefinition def, Belt.Dir face, out int w, out int h)
        {
            w = def.FootW; h = def.FootH;
            bool vertical = face == Belt.Dir.N || face == Belt.Dir.S;
            if (w != h && vertical) { int t = w; w = h; h = t; }
        }

        // True if ANY cell this footprint would cover is already taken (a building, or a reserved
        // road/rail tile — so you can't drop a building on track). Inlined over the anchor (these run
        // EVERY frame a ghost is active) so there's no per-frame List alloc from Footprint.Cells.
        private bool FootprintBlocked(Vector3 center, int w, int h)
        {
            var a = Footprint.Anchor(center, w, h);
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                {
                    var c = new Vector2Int(a.x + i, a.y + j);
                    if (CellOccupied(new Vector3(c.x, c.y, 0f)) || WorldGrid.IsReserved(c)) return true;
                }
            return false;
        }

        // True only if EVERY footprint cell is on buildable terrain (not water).
        private bool FootprintOnLand(Vector3 center, int w, int h)
        {
            var a = Footprint.Anchor(center, w, h);
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    if (!TerrainGrid.Buildable(new Vector2Int(a.x + i, a.y + j))) return false;
            return true;
        }

        // True if the footprint covers BOTH land and water cells — a harbour must straddle the shore so the
        // boat can dock on the water half while belts connect on the land half.
        private bool FootprintStraddlesShore(Vector3 center, int w, int h)
        {
            var a = Footprint.Anchor(center, w, h);
            bool land = false, water = false;
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                {
                    if (TerrainGrid.IsWater(new Vector2Int(a.x + i, a.y + j))) water = true; else land = true;
                }
            return land && water;
        }

        private bool CellOccupied(Vector3 world) => SolidBuildingAt(world);

        /// <summary>True if a SOLID building (or construction site) overlaps this point — used for
        /// placement blocking, belt-on-building blocking, and player/worker collision. Belts,
        /// bridges, pipes and resource nodes are NOT solid (you build/walk over them).</summary>
        // Reused query buffer + filter so this hot test (run per footprint cell, every frame a ghost is active)
        // doesn't allocate a Collider2D[] each call.
        private static readonly Collider2D[] _solidBuf = new Collider2D[24];
        private static readonly ContactFilter2D _solidFilter = new ContactFilter2D { useTriggers = true, useLayerMask = false };
        public static bool SolidBuildingAt(Vector3 world)
        {
            int n = Physics2D.OverlapPoint((Vector2)world, _solidFilter, _solidBuf);
            for (int i = 0; i < n; i++)
            {
                var h = _solidBuf[i];
                if (h == null) continue;
                if (h.GetComponent<ProductionBuilding>() || h.GetComponent<StorageBuilding>()
                    || h.GetComponent<WorkshopBuilding>() || h.GetComponent<Depot>()
                    || h.GetComponent<PowerPlant>() || h.GetComponent<WaterPump>() || h.GetComponent<Battery>()
                    || h.GetComponent<ResearchBuilding>() || h.GetComponent<Garage>() || h.GetComponent<ConstructionSite>()) return true;
            }
            return false;
        }

        // Belt mode. PLAIN belts are PLANNED: drag to sketch a blueprint (no cost), then click to build
        // it (so you can lay out a run and see it before committing). Splitters/Mergers stay one-per-
        // click and now show their in/out ports on the ghost so you can orient them.
        private void UpdateBeltPlacement(Mouse mouse, Keyboard kb)
        {
            if (_cam == null || mouse == null || _ghost == null) return;
            HideGhostPorts(); // building-style I/O ports off
            var def = buildables[PendingIndex];
            bool isJunction = def.splitter || def.merger;
            // Discrete one-click placements (NOT the belt drag-line): junctions, underground ends, filter/gate.
            bool singleClick = isJunction || def.underground || def.filter || def.gate;
            bool overUI = InventoryHud.PointerOverUI;

            if (kb.rKey.wasPressedThisFrame) BeltDir = Belt.RotateCW(BeltDir);

            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            Vector2Int cell = Belt.CellOf(world);

            // Cursor-cell direction: along the active sketch stroke, else auto-oriented toward a sink.
            Belt.Dir dir = singleClick ? BeltDir
                         : (_strokeActive && Adjacent(cell, _strokeLast)) ? Belt.FromTo(_strokeLast, cell)
                         : AutoBeltDir(cell);

            _ghost.transform.position = new Vector3(cell.x, cell.y, 0f);
            _ghost.transform.rotation = Quaternion.Euler(0f, 0f, Belt.Angle(dir));
            _ghost.transform.localScale = Vector3.one; // match the placed belt (full cell)
            _ghostSr.sprite = def.underground ? PlaceholderArt.UndergroundBelt(false)
                            : SpriteDatabase.ForBelt(def.displayName, def.splitter, def.merger);

            // Point 1: a splitter/merger ghost shows its in (cyan) / out (green) sides.
            if (isJunction) ShowGhostJunctionPorts(def.splitter, def.merger);
            else ClearGhostJunctionPorts();

            var existing = Belt.At(cell);
            bool affordable = Economy.CanAfford(def.cost, Carried);
            bool emptyOk = existing == null && TerrainGrid.BeltAllowed(cell)
                           && !SolidBuildingAt(new Vector3(cell.x, cell.y, 0f)) && !WorldGrid.IsReserved(cell);
            // A variant (splitter/merger/filter/gate) can overlay-CONVERT a plain belt in place; an underground
            // end needs an empty cell (it's a discrete tunnel mouth, not an overlay).
            bool onPlainBelt = existing != null && !existing.isSplitter && !existing.isMerger && !existing.isFilter && !existing.isGate && !existing.underground;
            PlacementValid = affordable && (def.underground ? emptyOk : (emptyOk || onPlainBelt));
            _ghostSr.color = PlacementValid ? new Color(0.35f, 1f, 0.4f, 0.6f) : new Color(1f, 0.3f, 0.3f, 0.5f);

            // --- Junctions / underground / filter / gate: one deliberate click each. ---
            if (singleClick)
            {
                if (mouse.leftButton.wasPressedThisFrame && PlacementValid && !overUI) EnsureBelt(cell, dir, def);
                else if (mouse.rightButton.wasPressedThisFrame) CancelPlacement();
                return;
            }

            // --- Plain belts: blueprint flow (drag = plan a STRAIGHT line, click = build, right-click = cancel). ---
            if (mouse.leftButton.wasPressedThisFrame && !overUI)
            {
                _strokeActive = true; _strokeMoved = false; _strokeStart = cell; _strokeLast = cell;
                // Snapshot earlier strokes so this drag ADDS a straight segment (compose an L from two drags).
                _committed.Clear();
                foreach (var kv in _beltPlan) _committed[kv.Key] = kv.Value;
            }
            else if (_strokeActive && mouse.leftButton.isPressed)
            {
                // Belts lay as a STRAIGHT run along the dominant drag axis (no auto-corner) — easier to pull
                // a clean line; turn by doing a second drag. Replanned from the stroke start each move.
                if (cell != _strokeLast) { _strokeMoved = true; _strokeLast = cell; ReplanStraight(_strokeStart, cell); }
            }
            else if (_strokeActive && mouse.leftButton.wasReleasedThisFrame)
            {
                _strokeActive = false;
                if (!_strokeMoved)
                {
                    if (_beltPlan.Count > 0) BuildBeltPlan(def);          // tap with a plan pending → build it
                    else if (!overUI) PlanCell(cell, AutoBeltDir(cell));   // tap on empty → start a 1-tile plan
                }
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                if (_beltPlan.Count > 0) ClearBeltPlan(); // discard the pending plan
                else CancelPlacement();                    // nothing planned → leave belt mode
            }

            RebuildPlanGhosts(def);
        }

        // ---- Belt blueprint helpers (plan, preview, build) ------------------------------------
        // Re-plan the run as a single STRAIGHT line from `start` to `end` along the DOMINANT axis (so a
        // drag always gives a clean straight belt; to turn, do a second drag). Replaces the plan each move.
        private void ReplanStraight(Vector2Int start, Vector2Int end)
        {
            _beltPlan.Clear();
            foreach (var kv in _committed) _beltPlan[kv.Key] = kv.Value; // keep earlier strokes
            _beltPlanDirty = true;
            int dx = end.x - start.x, dy = end.y - start.y;
            var c = start;
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            {
                Belt.Dir d = dx >= 0 ? Belt.Dir.E : Belt.Dir.W; int sx = dx >= 0 ? 1 : -1;
                PlanCell(c, d);
                for (int i = 0; i < Mathf.Abs(dx); i++) { c.x += sx; PlanCell(c, d); }
            }
            else
            {
                Belt.Dir d = dy >= 0 ? Belt.Dir.N : Belt.Dir.S; int sy = dy >= 0 ? 1 : -1;
                PlanCell(c, d);
                for (int i = 0; i < Mathf.Abs(dy); i++) { c.y += sy; PlanCell(c, d); }
            }
        }

        private void PlanCell(Vector2Int cell, Belt.Dir dir)
        {
            _beltPlan[cell] = dir;
            _beltPlanDirty = true;
            if (!_beltPlanHinted)
            {
                _beltPlanHinted = true;
                Toast.Show("<color=#9cf>Belt blueprint:</color> drag to extend · <color=#9f9>click to build</color> · right-click to cancel.");
            }
        }

        private void BuildBeltPlan(BuildingDefinition def)
        {
            foreach (var kv in _beltPlan) EnsureBelt(kv.Key, kv.Value, def); // pays per cell; skips blocked/unaffordable
            ClearBeltPlan();
        }

        private void ClearBeltPlan()
        {
            _beltPlan.Clear();
            _committed.Clear();
            foreach (var g in _planGhosts) if (g != null) Destroy(g);
            _planGhosts.Clear();
            _beltPlanHinted = false;
            _beltPlanDirty = false;
        }

        // Sync the translucent blueprint previews to the planned cells (cyan = ok, red = blocked).
        // Only when the plan changed — otherwise this ran a physics overlap per cell every frame.
        private void RebuildPlanGhosts(BuildingDefinition def)
        {
            if (!_beltPlanDirty) return;
            _beltPlanDirty = false;
            int n = _beltPlan.Count;
            while (_planGhosts.Count < n)
            {
                var go = new GameObject("BeltPlanGhost");
                go.AddComponent<SpriteRenderer>().sortingOrder = 19;
                _planGhosts.Add(go);
            }
            int i = 0;
            foreach (var kv in _beltPlan)
            {
                var go = _planGhosts[i++];
                if (!go.activeSelf) go.SetActive(true);
                go.transform.position = new Vector3(kv.Key.x, kv.Key.y, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, Belt.Angle(kv.Value));
                go.transform.localScale = Vector3.one; // match the placed belt (full cell)
                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = SpriteDatabase.ForBelt(def.displayName, def.splitter, def.merger);
                bool ok = Belt.At(kv.Key) == null && TerrainGrid.BeltAllowed(kv.Key)
                          && !SolidBuildingAt(new Vector3(kv.Key.x, kv.Key.y, 0f)) && !WorldGrid.IsReserved(kv.Key);
                sr.color = ok ? new Color(0.4f, 0.9f, 1f, 0.45f) : new Color(1f, 0.35f, 0.35f, 0.5f);
            }
            for (int k = n; k < _planGhosts.Count; k++) if (_planGhosts[k].activeSelf) _planGhosts[k].SetActive(false);
        }

        // Splitter/merger in/out ports on the ghost (parented to the rotated ghost, LOCAL dirs — so they
        // sit on the right world edges and follow R rotation, exactly like the placed junction's ports).
        private void ShowGhostJunctionPorts(bool splitter, bool merger)
        {
            if (_ghostJunctionBuilt && _ghostJunctionSplitter == splitter && _ghostJunctionMerger == merger
                && _ghostJunctionPorts.Count > 0 && _ghostJunctionPorts[0] != null) return;
            ClearGhostJunctionPorts();
            var t = _ghost.transform;
            if (splitter)
            {
                _ghostJunctionPorts.Add(Ports.MakeOutputArrow(t, Belt.Dir.N).gameObject);
                _ghostJunctionPorts.Add(Ports.MakeOutputArrow(t, Belt.Dir.E).gameObject);
                _ghostJunctionPorts.Add(Ports.MakeOutputArrow(t, Belt.Dir.W).gameObject);
                _ghostJunctionPorts.Add(Ports.MakeInputNotch(t, Belt.Dir.S).gameObject);
            }
            else if (merger)
            {
                _ghostJunctionPorts.Add(Ports.MakeOutputArrow(t, Belt.Dir.N).gameObject);
                _ghostJunctionPorts.Add(Ports.MakeInputNotch(t, Belt.Dir.S).gameObject);
                _ghostJunctionPorts.Add(Ports.MakeInputNotch(t, Belt.Dir.E).gameObject);
                _ghostJunctionPorts.Add(Ports.MakeInputNotch(t, Belt.Dir.W).gameObject);
            }
            _ghostJunctionBuilt = true; _ghostJunctionSplitter = splitter; _ghostJunctionMerger = merger;
        }

        private void ClearGhostJunctionPorts()
        {
            foreach (var g in _ghostJunctionPorts) if (g != null) Destroy(g);
            _ghostJunctionPorts.Clear();
            _ghostJunctionBuilt = false;
        }

        // Place a belt at `cell` pointing `d`, or re-orient the one already there.
        private void EnsureBelt(Vector2Int cell, Belt.Dir d, BuildingDefinition def)
        {
            var existing = Belt.At(cell);
            if (def.underground && existing != null) return; // an underground end only goes on an empty cell
            if (existing != null)
            {
                // QoL: dropping a Splitter/Merger/Filter/Gate onto an existing plain belt CONVERTS it in place
                // (keeps its direction + any carried items) — no need to delete the belt first. A plain belt
                // dropped on a belt keeps the old behaviour (just re-orient it).
                bool isVariant = def.splitter || def.merger || def.filter || def.gate;
                bool plain = !existing.isSplitter && !existing.isMerger && !existing.isFilter && !existing.isGate && !existing.underground;
                bool convert = isVariant && plain;
                if (convert)
                {
                    if (Economy.CanAfford(def.cost, Carried))
                    {
                        Economy.Spend(def.cost, Carried);
                        existing.ConvertTo(def.splitter, def.merger, def.priority, def.filter, def.gate, def.displayName);
                        existing.SetBaseColor(def.color);
                    }
                    return; // don't re-orient on conversion — preserve the line's flow
                }
                // OVERLAY-UPGRADE: dropping a FASTER plain-belt tier onto an existing plain belt
                // upgrades it in place (overlay, no delete). Only strictly-faster (smaller interval)
                // tiers upgrade; same/slower just re-orients. Charges the new tier's cost per segment
                // (re-touching an already-upgraded cell is same-tier → no re-charge).
                bool upgrade = !def.splitter && !def.merger && !existing.isSplitter && !existing.isMerger
                               && def.interval < existing.interval - 0.001f;
                if (upgrade)
                {
                    if (Economy.CanAfford(def.cost, Carried))
                    {
                        Economy.Spend(def.cost, Carried);
                        existing.SetTier(def.interval, def.color, def.displayName);
                    }
                    return; // upgraded in place — keep direction + carried items
                }
                existing.SetDir(d);
                return;
            }
            if (!TerrainGrid.BeltAllowed(cell)) return; // water only if bridged
            if (SolidBuildingAt(new Vector3(cell.x, cell.y, 0f))) return; // never lay a belt on a building
            if (WorldGrid.IsReserved(cell)) return; // never lay a belt on a road/rail tile
            if (!Economy.CanAfford(def.cost, Carried)) return;
            Economy.Spend(def.cost, Carried);
            Belt.Spawn(cell, d, def.interval, def.splitter, def.merger, def.color, def.displayName,
                       def.underground, def.filter, def.priority, def.gate);
            BuildingsPlaced++;
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

        // Rail mode: BLUEPRINT placement, like belts — drag to plan a continuous 90° run (cyan ghosts, no
        // cost), click to build it, right-click to cancel. Only orthogonal L-paths (no diagonals) so track
        // stays tidy. Trains then path along the laid track.
        private void UpdateRailPlacement(Mouse mouse)
        {
            if (_cam == null || mouse == null || _ghost == null) return;
            HideGhostPorts();
            ClearGhostJunctionPorts();
            var def = buildables[PendingIndex];
            bool overUI = InventoryHud.PointerOverUI;

            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            var cell = new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
            _ghost.transform.position = new Vector3(cell.x, cell.y, 0f);
            _ghost.transform.rotation = Quaternion.identity;
            _ghostSr.sprite = PlaceholderArt.RailMask(GhostMask(cell)); // orient to neighbours / drag direction
            _ghost.transform.localScale = Vector3.one;

            bool ok = RailCellFree(cell, def.elevated) && Economy.CanAfford(def.cost, Carried);
            PlacementValid = ok;
            if (_ghostSr != null) _ghostSr.sortingOrder = def.elevated ? 9 : 1; // show the elevated ghost above belts
            _ghostSr.color = ok ? (def.elevated ? new Color(0.7f, 0.8f, 1f, 0.6f) : new Color(0.4f, 0.9f, 1f, 0.6f))
                                : new Color(1f, 0.35f, 0.35f, 0.5f);

            if (mouse.leftButton.wasPressedThisFrame && !overUI)
            {
                _strokeActive = true; _strokeMoved = false; _strokeLast = cell;
            }
            else if (_strokeActive && mouse.leftButton.isPressed)
            {
                if (cell != _strokeLast) { _strokeMoved = true; PlanRailPath(_strokeLast, cell); _strokeLast = cell; }
            }
            else if (_strokeActive && mouse.leftButton.wasReleasedThisFrame)
            {
                _strokeActive = false;
                if (!_strokeMoved)
                {
                    if (_railPlan.Count > 0) BuildRailPlan(def);   // tap with a plan pending → build it
                    else if (!overUI) PlanRailCell(cell);           // tap on empty → start a 1-tile plan
                }
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                if (_railPlan.Count > 0) ClearRailPlan(); else CancelPlacement();
            }

            RebuildRailPlanGhosts();
        }

        // A rail cell is free if there's no rail there, the terrain is buildable, and no SOLID building sits on
        // it. ELEVATED track additionally MAY sit over a belt (it crosses above), so it drops the no-belt rule.
        private bool RailCellFree(Vector2Int cell, bool elevated = false)
            => RailTile.At(cell) == null && TerrainGrid.Buildable(cell)
               && !SolidBuildingAt(new Vector3(cell.x, cell.y, 0f)) && (elevated || Belt.At(cell) == null);

        // Place one rail tile at `cell` (paid) if the cell is clear; no-op otherwise.
        private void LayRail(Vector2Int cell, BuildingDefinition def)
        {
            if (!RailCellFree(cell, def.elevated) || !Economy.CanAfford(def.cost, Carried)) return;
            Economy.Spend(def.cost, Carried);
            RailTile.Spawn(def, new Vector3(cell.x, cell.y, 0f), def.elevated);
            BuildingsPlaced++;
        }

        // ---- Rail blueprint helpers (90° L-paths only) -----------------------------------------
        private void PlanRailPath(Vector2Int from, Vector2Int to)
        {
            int dx = to.x - from.x, dy = to.y - from.y;
            int sx = dx > 0 ? 1 : dx < 0 ? -1 : 0, sy = dy > 0 ? 1 : dy < 0 ? -1 : 0;
            var c = from; PlanRailCell(c);
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            { for (int i = 0; i < Mathf.Abs(dx); i++) { c.x += sx; PlanRailCell(c); } for (int i = 0; i < Mathf.Abs(dy); i++) { c.y += sy; PlanRailCell(c); } }
            else
            { for (int i = 0; i < Mathf.Abs(dy); i++) { c.y += sy; PlanRailCell(c); } for (int i = 0; i < Mathf.Abs(dx); i++) { c.x += sx; PlanRailCell(c); } }
        }

        private void PlanRailCell(Vector2Int cell)
        {
            if (!_railPlan.Contains(cell)) _railPlan.Add(cell);
            _railPlanDirty = true;
            if (!_railPlanHinted)
            {
                _railPlanHinted = true;
                Toast.Show("<color=#9cf>Track blueprint:</color> drag to extend (90° only) · <color=#9f9>click to build</color> · right-click to cancel.");
            }
        }

        private void BuildRailPlan(BuildingDefinition def)
        {
            foreach (var c in _railPlan) LayRail(c, def);
            ApplyPathLinks(); // wire EXPLICIT connections from the laid path, so parallels stay separate + merges join
            ClearRailPlan();
        }

        // Set each tile's links from the drag PATH: consecutive cells link mutually, and each END joins an
        // existing line only in its OWN line direction (so a connector merges two parallels, but running beside
        // one never fuses to it). A lone tile joins whatever rail it's already touching.
        private void ApplyPathLinks()
        {
            int n = _railPlan.Count;
            if (n == 0) return;
            for (int i = 0; i + 1 < n; i++)
                if (Adjacent(_railPlan[i], _railPlan[i + 1])) RailTile.Link(_railPlan[i], _railPlan[i + 1]);
            if (n == 1)
            {
                foreach (var d in RailTile.Four) JoinIfRail(_railPlan[0], d);
            }
            else
            {
                JoinIfRail(_railPlan[0], Belt.Opposite(Belt.FromTo(_railPlan[0], _railPlan[1])));     // extend the start backward
                JoinIfRail(_railPlan[n - 1], Belt.FromTo(_railPlan[n - 2], _railPlan[n - 1]));        // extend the end forward
            }
        }
        private static void JoinIfRail(Vector2Int c, Belt.Dir d)
        {
            var nb = c + Belt.Step(d);
            if (RailNet.IsRail(nb)) RailTile.Link(c, nb);
        }

        private void ClearRailPlan()
        {
            _railPlan.Clear();
            foreach (var g in _railPlanGhosts) if (g != null) Destroy(g);
            _railPlanGhosts.Clear();
            _railPlanHinted = false;
            _railPlanDirty = false;
        }

        private void RebuildRailPlanGhosts()
        {
            if (!_railPlanDirty) return;
            _railPlanDirty = false;
            int n = _railPlan.Count;
            bool elev = PendingIndex >= 0 && PendingIndex < buildables.Count && buildables[PendingIndex].elevated;
            while (_railPlanGhosts.Count < n)
            {
                var go = new GameObject("RailPlanGhost");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PlaceholderArt.Rail();
                sr.sortingOrder = 19;
                _railPlanGhosts.Add(go);
            }
            for (int i = 0; i < n; i++)
            {
                var go = _railPlanGhosts[i];
                if (!go.activeSelf) go.SetActive(true);
                var c = _railPlan[i];
                go.transform.position = new Vector3(c.x, c.y, 0f);
                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = PlaceholderArt.RailMask(GhostMaskAt(i, n)); // EXACT preview: only what will actually connect
                sr.color = RailCellFree(c, elev) // elevated may cross belts, so a belt cell is still valid for it
                    ? new Color(0.4f, 0.9f, 1f, 0.5f) : new Color(1f, 0.35f, 0.35f, 0.55f);
            }
            for (int k = n; k < _railPlanGhosts.Count; k++) if (_railPlanGhosts[k].activeSelf) _railPlanGhosts[k].SetActive(false);
        }

        // EXACT ghost mask for plan cell i — mirrors ApplyPathLinks, so the blueprint shows precisely what will
        // connect: links along the path + each END joining an existing line only in its own line direction.
        private int GhostMaskAt(int i, int n)
        {
            var c = _railPlan[i];
            int m = 0;
            if (i > 0 && Adjacent(c, _railPlan[i - 1])) m |= RailTile.DirBit(Belt.FromTo(c, _railPlan[i - 1]));
            if (i < n - 1 && Adjacent(c, _railPlan[i + 1])) m |= RailTile.DirBit(Belt.FromTo(c, _railPlan[i + 1]));
            if (n == 1)
                foreach (var d in RailTile.Four) { if (RailNet.IsRail(c + Belt.Step(d))) m |= RailTile.DirBit(d); }
            else if (i == 0)
            { var b = Belt.Opposite(Belt.FromTo(c, _railPlan[1])); if (RailNet.IsRail(c + Belt.Step(b))) m |= RailTile.DirBit(b); }
            else if (i == n - 1)
            { var f = Belt.FromTo(_railPlan[n - 2], c); if (RailNet.IsRail(c + Belt.Step(f))) m |= RailTile.DirBit(f); }
            return m;
        }

        // Hover ghost (single cell, no plan yet): show which adjacent EXISTING rail it would join if placed.
        private int GhostMask(Vector2Int c)
        {
            int m = 0;
            foreach (var d in RailTile.Four) if (RailNet.IsRail(c + Belt.Step(d))) m |= RailTile.DirBit(d);
            return m;
        }

        // Signal mode: click a rail cell to drop/aim a signal (R rotates its allowed travel direction).
        private void UpdateSignalPlacement(Mouse mouse, Keyboard kb)
        {
            if (_cam == null || mouse == null || _ghost == null) return;
            HideGhostPorts();
            ClearGhostJunctionPorts();
            var def = buildables[PendingIndex];

            if (kb != null && kb.rKey.wasPressedThisFrame) BeltDir = Belt.RotateCW(BeltDir);

            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            var cell = new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
            _ghost.transform.position = new Vector3(cell.x, cell.y, 0f);
            _ghost.transform.rotation = Quaternion.Euler(0f, 0f, Belt.Angle(BeltDir));
            _ghostSr.sprite = PlaceholderArt.Triangle();
            _ghost.transform.localScale = Vector3.one * 0.5f;

            bool ok = RailTile.At(cell) != null; // signals only sit on track
            PlacementValid = ok;
            _ghostSr.color = ok ? new Color(0.35f, 1f, 0.4f, 0.7f) : new Color(1f, 0.3f, 0.3f, 0.5f);

            if (mouse.leftButton.wasPressedThisFrame && ok && !InventoryHud.PointerOverUI)
                Signal.Place(cell, BeltDir, def.bothWaySignal); // place or re-aim (one-way or two-way per the def)
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
            _strokeActive = false;
            ClearBeltPlan();
            ClearRailPlan();
            ClearGhostJunctionPorts();
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

            // Belts, bridges, pipes, rails, signals: cheap, removed without refund (unchanged).
            if (Selected.GetComponent<Belt>() != null
                || Selected.GetComponent<Bridge>() != null || Selected.GetComponent<Pipe>() != null
                || Selected.GetComponent<RailTile>() != null || Selected.GetComponent<Signal>() != null)
            {
                Destroy(Selected);
                Selected = null;
                return;
            }

            var pb = Selected.GetComponent<ProductionBuilding>();
            var sb = Selected.GetComponent<StorageBuilding>();
            var wb = Selected.GetComponent<WorkshopBuilding>();
            var dpo = Selected.GetComponent<Depot>();
            var pp = Selected.GetComponent<PowerPlant>();
            var pole = Selected.GetComponent<PowerPole>();
            var bat = Selected.GetComponent<Battery>();
            var wp = Selected.GetComponent<WaterPump>();
            var rsb = Selected.GetComponent<ResearchBuilding>();
            BuildingDefinition rdef = pb != null ? pb.def : sb != null ? sb.def
                : wb != null ? wb.def : dpo != null ? dpo.def : pp != null ? pp.def
                : pole != null ? pole.def : bat != null ? bat.def
                : wp != null ? wp.def : rsb != null ? rsb.def : null;
            if (rdef == null) return;

            // Return any stored/buffered goods to the player's hands so demolishing never SILENTLY
            // destroys resources (carried is unlimited). Then refund half the build cost.
            DumpToCarried(pb != null ? pb.Buffer : null);
            DumpToCarried(wb != null ? wb.Buffer : null);
            DumpToCarried(wb != null ? wb.InBuffer : null);
            DumpToCarried(sb != null ? sb.Store : null);
            DumpToCarried(dpo != null ? dpo.store : null);
            DumpToCarried(rsb != null ? rsb.InBuffer : null);

            if (Carried != null)
                foreach (var c in rdef.cost)
                    Carried.Add(c.item, Mathf.Max(0, c.amount / 2));

            BuildingsPlaced = Mathf.Max(0, BuildingsPlaced - 1);
            Destroy(Selected);
            Selected = null;
        }

        // Move every item out of a building's inventory into the player's hands (used on demolish so
        // goods are never silently destroyed). Carried is unlimited, so nothing is lost.
        private void DumpToCarried(Inventory inv)
        {
            if (inv == null || Carried == null) return;
            foreach (var kv in new List<KeyValuePair<ItemDefinition, int>>(inv.Items))
            {
                int moved = Carried.Add(kv.Key, kv.Value);
                if (moved > 0) inv.RemoveUpTo(kv.Key, moved);
            }
        }

        private GameObject BuildingGOUnderCursor(Mouse mouse)
        {
            if (_cam == null || mouse == null) return null;
            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            // A signal sits ON a rail tile — prefer it so you can select/remove the signal, not the track.
            var sig = Signal.At(new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y)));
            if (sig != null) return sig.gameObject;
            Collider2D hit = Physics2D.OverlapPoint(world);
            if (hit == null) return null;
            bool isBuilding = hit.GetComponent<ProductionBuilding>() != null
                              || hit.GetComponent<StorageBuilding>() != null
                              || hit.GetComponent<WorkshopBuilding>() != null
                              || hit.GetComponent<Depot>() != null
                              || hit.GetComponent<PowerPlant>() != null
                              || hit.GetComponent<PowerPole>() != null
                              || hit.GetComponent<Battery>() != null
                              || hit.GetComponent<Garage>() != null
                              || hit.GetComponent<WaterPump>() != null
                              || hit.GetComponent<ResearchBuilding>() != null
                              || hit.GetComponent<Bridge>() != null
                              || hit.GetComponent<Pipe>() != null
                              || hit.GetComponent<Belt>() != null
                              || hit.GetComponent<RailTile>() != null
                              || hit.GetComponent<Signal>() != null
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
