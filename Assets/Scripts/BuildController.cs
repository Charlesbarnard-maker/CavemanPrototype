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
        private readonly List<GameObject> _depotGhostTiles = new(); // per-cell blueprint tiles for 3×1 depots
        private readonly List<GameObject> _armGhostCells = new();   // crane placement: tinted GRAB/DROP cell highlights
        private bool _dragging;
        private Vector2Int _dragLast;

        // --- Belt BLUEPRINT placement: drag sketches a plan (no cost), then a click builds it. ---
        private readonly Dictionary<Vector2Int, Belt.Dir> _beltPlan = new(); // planned cell → direction
        private readonly Dictionary<Vector2Int, Belt.Dir> _committed = new(); // strokes finished before the current one
        private readonly List<GameObject> _planGhosts = new();               // translucent previews (pooled)
        private bool _strokeActive, _strokeMoved;                            // current drag stroke state
        private Vector2Int _strokeStart, _strokeLast;
        private int _railCommitted;                                          // rail-plan cells from PRIOR strokes (this stroke rebuilds after them)
        private bool _beltDragHinted; // one-time "drag to lay a line" hint (never reset by ClearBeltPlan)
        private bool _beltPlanDirty; // ghosts rebuilt only when the plan changes (not every frame)
        // --- Underground belt GUIDED placement: first click sets the ENTRANCE, second click the EXIT (snapped
        //     ahead along the entrance's facing, within tunnel range). Nothing is built until the exit is
        //     confirmed, so a cancel leaves no dangling end. ---
        private bool _ugAwaitingExit;
        private Vector2Int _ugEntranceCell;
        private Belt.Dir _ugEntranceDir;
        private GameObject _ugEntranceGhost, _ugSpanGhost; // persistent entrance preview + the tunnel-span bar
        private const int UgMaxTunnel = 4;                 // mirrors Belt.MaxTunnel — exit may sit up to 4 cells ahead
        private static readonly List<ItemAmount> _ugPairCostBuf = new(); // reused 2× cost (both ends paid at once)
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
        public bool WireCutMode { get; private set; }   // "cut a wire" mode — click a cable in the world to delete it

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
            Toast.Show($"<color=#ffd24d>Building a {_linkTier.displayName} line.</color> Click each {what} in visit order (you can revisit one to loop back home); <b>right-click to finish</b>. The vehicle loops the stops, passing any it doesn't stop at. (Esc cancels.)");
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

        // A click on `dst` while building a line ADDS it as the next stop. You MAY pass the same station more than
        // once (loops / there-and-back / via the home station) — the only thing disallowed is adding the stop the
        // line is already ON (the last one), which would be a stop-to-itself. Finish the line with RIGHT-CLICK.
        private void OnLineClick(Depot dst)
        {
            if (LinkFrom == null || _linkTier == null) { CancelLink(); return; }
            if (dst == null) return; // clicked empty — keep the in-progress line
            if (_lineStops.Count > 0 && dst == _lineStops[_lineStops.Count - 1])
            {
                Toast.Show("<color=#fc8>That's the stop the line is already on — pick a different station. (Right-click to finish the line.)</color>");
                return;
            }

            // A line is EITHER a Harbour (ship/water) line OR a Station (train/land) line — never mixed, so a
            // ship can't be routed to a Station over land. Within harbour lines, cargo and liquid docks don't mix.
            var first = _lineStops[0];
            bool lineHarbour = first.def != null && first.def.isHarbour;
            bool dstHarbour = dst.def != null && dst.def.isHarbour;
            if (lineHarbour != dstHarbour)
            {
                Toast.Show(lineHarbour
                    ? "<color=#f99>Ships travel by water — a Harbour line can only link other Harbours, not a Station.</color>"
                    : "<color=#f99>A train line can't include a Harbour — start a separate Harbour line for ships.</color>");
                return;
            }
            if (lineHarbour)
            {
                bool lineLiquid = first.def != null && first.def.isLiquidHarbour;
                bool dstLiquid = dst.def != null && dst.def.isLiquidHarbour;
                if (lineLiquid != dstLiquid)
                {
                    Toast.Show("<color=#f99>Cargo and Liquid harbours can't share a line — keep solid-goods docks and fluid docks separate.</color>");
                    return;
                }
                // The ship must REACH the new stop over water — no land crossing, no jump to an isolated sea.
                var prev = _lineStops[_lineStops.Count - 1];
                if (WaterNet.WaterPath(prev.transform.position, dst.transform.position) == null)
                {
                    Toast.Show("<color=#f99>No water route to that Harbour — ships can't cross land or hop between separate seas.</color>");
                    return;
                }
            }

            _lineStops.Add(dst);
            Toast.Show($"<color=#9cf>Stop {_lineStops.Count} added.</color> Click more stops (you can revisit one to loop back), or <b>right-click to finish</b>.");
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

        /// <summary>Enter "cut a wire" mode: click a cable in the world to delete just that wire. Esc / right-click exits.</summary>
        public void BeginWireCut()
        {
            CancelPlacement();
            CancelLink();
            CancelWire();
            WireCutMode = true;
            Toast.Show("<color=#fc8>✂ Click a WIRE to cut it.</color>  <size=11>(Esc / right-click exits)</size>");
        }
        public void CancelWireCut() => WireCutMode = false;

        // The PowerWire whose cable is under the cursor (for cut mode), or null — reuses the shared overlap buffer.
        private PowerWire WireUnderCursor(Mouse mouse)
        {
            if (_cam == null || mouse == null) return null;
            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            int n = Physics2D.OverlapPoint((Vector2)world, _solidFilter, _solidBuf);
            for (int i = 0; i < n; i++)
            {
                var w = _solidBuf[i] != null ? _solidBuf[i].GetComponent<PowerWire>() : null;
                if (w != null) return w;
            }
            return null;
        }

        private void CompleteWire(PowerNode to)
        {
            if (WireFrom == null) { CancelWire(); return; }
            string why = WireFrom.LinkBlockedReason(to);
            if (why == null) { WireFrom.Connect(to); Toast.Show("<color=#9f9>Wire connected.</color>"); }
            else Toast.Show($"<color=#f99>Can't wire: {why}.</color>");
            CancelWire();
        }

        // The Power Pole buildable (cached) — used by the wire-drag-builds-a-pole QoL.
        private BuildingDefinition PoleDef()
        {
            if (_poleDef == null && buildables != null)
                foreach (var d in buildables) if (d != null && d.kind == BuildingKind.Pole) { _poleDef = d; break; }
            return _poleDef;
        }

        // True if `cell` is a clear, buildable spot for a 1×1 pole (land, not occupied by a building / reserved / pipe).
        private bool PoleCellOk(Vector2Int cell)
        {
            var w = new Vector3(cell.x, cell.y, 0f);
            return TerrainGrid.Buildable(cell) && !SolidBuildingAt(w) && !WorldGrid.IsReserved(cell) && PipeNet.At(cell) == null;
        }

        // While wiring, a left-click on empty ground drops a POWER POLE there, wires it to the current source,
        // and continues the chain FROM the new pole — so you can string poles across a gap in a few clicks.
        private bool TryBuildWirePole(Mouse mouse)
        {
            var def = PoleDef();
            if (def == null || WireFrom == null || _cam == null) return false;
            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            var cell = new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
            var p = new Vector3(cell.x, cell.y, 0f);
            if ((WireFrom.Pos - (Vector2)p).sqrMagnitude > WireFrom.Reach * WireFrom.Reach)
            { Toast.Show("<color=#f99>Too far for one wire — click closer; poles chain (a Tall Pylon reaches further).</color>"); return false; }
            if (!PoleCellOk(cell)) { Toast.Show("<color=#f99>Can't place a pole there.</color>"); return false; }
            if (!Economy.CanAfford(def.cost, Carried)) { Toast.Show("<color=#f99>Can't afford a Power Pole.</color>"); return false; }
            Economy.Spend(def.cost, Carried);
            var pole = PowerPole.Spawn(def, p, autoLink: false); // wire the chain explicitly below
            var node = pole != null ? pole.GetComponent<PowerNode>() : null;
            if (node == null) return false;
            WireFrom.Connect(node);   // join the new pole to the chain — the ONLY wire it gets (no auto-link to nearby poles)
            WireFrom = node;          // keep wiring from the new pole
            BuildingsPlaced++;
            return true;
        }

        // Live wiring feedback: a preview cable from the source node to the cursor (cyan = a valid target /
        // pole spot, red = blocked), plus a ghost POWER POLE over buildable terrain in reach.
        private void UpdateWiringVisuals()
        {
            if (WireFrom == null)
            {
                if (_wireGhost != null && _wireGhost.activeSelf) _wireGhost.SetActive(false);
                if (_wirePreview != null && _wirePreview.gameObject.activeSelf) _wirePreview.gameObject.SetActive(false);
                return;
            }
            var mouse = Mouse.current;
            if (_cam == null || mouse == null) return;
            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue()); world.z = 0f;
            var go = BuildingGOUnderCursor(mouse);
            var node = go != null ? go.GetComponent<PowerNode>() : null;
            var cell = new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
            var cellW = new Vector3(cell.x, cell.y, 0f);
            bool inRange = (WireFrom.Pos - (Vector2)cellW).sqrMagnitude <= WireFrom.Reach * WireFrom.Reach;

            if (_wirePreview == null)
            {
                var lo = new GameObject("WirePreview");
                _wirePreview = lo.AddComponent<LineRenderer>();
                _wirePreview.material = PlaceholderArt.LineMaterial(); // shared
                _wirePreview.widthMultiplier = 0.08f; _wirePreview.numCapVertices = 2;
                _wirePreview.useWorldSpace = true; _wirePreview.sortingOrder = 12; _wirePreview.positionCount = 2;
            }
            if (!_wirePreview.gameObject.activeSelf) _wirePreview.gameObject.SetActive(true);

            Vector3 from = new Vector3(WireFrom.Pos.x, WireFrom.Pos.y, 0f);
            Vector3 to; Color col; bool showGhost = false;
            if (node != null && node != WireFrom)
            {
                to = new Vector3(node.Pos.x, node.Pos.y, 0f);
                col = WireFrom.CanLink(node) ? PowerWire.Powered : new Color(1f, 0.4f, 0.4f, 0.9f);
            }
            else if (node == null && PoleDef() != null && inRange && PoleCellOk(cell) && Economy.CanAfford(PoleDef().cost, Carried))
            {
                to = cellW; showGhost = true; col = new Color(0.5f, 0.9f, 1f, 0.85f);
            }
            else { to = world; col = new Color(1f, 0.5f, 0.3f, 0.7f); }
            _wirePreview.SetPosition(0, from);
            _wirePreview.SetPosition(1, to);
            _wirePreview.startColor = _wirePreview.endColor = col;

            if (showGhost)
            {
                if (_wireGhost == null)
                {
                    _wireGhost = new GameObject("WirePoleGhost");
                    _wireGhostSr = _wireGhost.AddComponent<SpriteRenderer>();
                    _wireGhostSr.sprite = PlaceholderArt.Pole();
                    _wireGhostSr.sortingOrder = 9;
                    _wireGhost.transform.localScale = Vector3.one * 0.55f;
                }
                if (!_wireGhost.activeSelf) _wireGhost.SetActive(true);
                _wireGhost.transform.position = cellW;
                _wireGhostSr.color = new Color(1f, 1f, 1f, 0.6f);
            }
            else if (_wireGhost != null && _wireGhost.activeSelf) _wireGhost.SetActive(false);
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
        // Power-wiring QoL: a live preview cable from the source node to the cursor, and a ghost POWER POLE
        // shown over buildable terrain — click empty ground while wiring to drop a pole + wire it into the chain.
        private LineRenderer _wirePreview;
        private GameObject _wireGhost; private SpriteRenderer _wireGhostSr;
        private BuildingDefinition _poleDef;
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
            UpdateWiringVisuals(); // live wire cable + ghost-pole preview while wiring
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
                if (WireCutMode) CancelWireCut();
                else if (WireFrom != null) CancelWire();
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

            // PIPETTE (QoL): Q picks the building under the cursor straight into placement mode —
            // clone part of a setup without hunting through the build menu (Factorio's Q).
            if (kb.qKey.wasPressedThisFrame && mouse != null && !InventoryHud.PointerOverUI && PendingIndex < 0)
            {
                var pdefUnder = DefOf(BuildingGOUnderCursor(mouse));
                int idx = pdefUnder != null ? buildables.IndexOf(pdefUnder) : -1;
                if (idx >= 0) { BeginPlacement(idx); AudioManager.Click(); }
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
                    if (node == WireFrom) CancelWire();        // clicked the source again → stop
                    else if (node != null) CompleteWire(node); // another building → wire to it (done)
                    else TryBuildWirePole(mouse);              // empty ground → drop a pole + keep chaining (toasts if it can't)
                }
                if (mouse != null && mouse.rightButton.wasPressedThisFrame) CancelWire();
                return;
            }

            // Wire-cut mode: click a cable to delete just that wire (frees a pole slot). Right-click / Esc exits.
            if (WireCutMode)
            {
                if (mouse != null && mouse.leftButton.wasPressedThisFrame && !overUI)
                {
                    var w = WireUnderCursor(mouse);
                    if (w != null && w.a != null && w.b != null) { w.a.Disconnect(w.b); Toast.Show("<color=#9f9>✂ Wire cut.</color>"); }
                    else Toast.Show("<color=#f99>No wire there — click directly on a cable.</color>");
                }
                if (mouse != null && mouse.rightButton.wasPressedThisFrame) CancelWireCut();
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
            if (pd != null && pd.kind == BuildingKind.Collector && !pd.drill && _ghost != null && _ghost.activeSelf)
            {
                ClearRangeLit(); // (a drill has no gather radius — it mines its footprint, so no ring)
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
            _ugAwaitingExit = false;  // a pending underground tunnel shouldn't carry into a new tool
            HideUndergroundGuides();
            HideDepotGhost();         // leftover depot blueprint tiles shouldn't carry into a new tool
            HideArmGhost();           // ...nor crane grab/drop cell highlights
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
            if (_ghostSr != null) _ghostSr.enabled = true; // a depot tool disables this; restore for the next tool
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
            bool canRotate = hasPorts || def.kind == BuildingKind.Depot || def.kind == BuildingKind.Power
                             || def.kind == BuildingKind.Arm; // R aims which way the crane swings
            if (canRotate && kb != null && kb.rKey.wasPressedThisFrame) BuildDir = Belt.RotateCW(BuildDir);

            EffFoot(def, BuildDir, out int ew, out int eh); // footprint after rotation (swaps W/H when turned vertical)

            Vector3 raw = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            // Snap to a valid CENTRE for this footprint (half-integer for even sizes).
            Vector3 world = Footprint.SnapCenter(raw, ew, eh);
            world.z = 0f;
            _ghost.transform.position = world;
            _ghost.transform.rotation = Quaternion.identity;
            // A DEPOT (3×1 platform) previews as one deck tile PER cell — matching what actually gets built —
            // instead of a single square sprite stretched 3:1 (which also flipped when rotated vertical). The
            // main square ghost is hidden for depots; the per-cell tiles are laid + tinted below, once validity
            // is known. Everything else keeps the single scaled ghost.
            bool isDepot = def.kind == BuildingKind.Depot;
            _ghostSr.enabled = !isDepot;
            if (!isDepot)
            {
                _ghostSr.sprite = SpriteDatabase.ForBuilding(def); // ghost matches the building's resolved sprite
                float gb = def.kind == BuildingKind.Collector ? 0.9f : 1.0f;
                _ghost.transform.localScale = new Vector3(ew * gb, eh * gb, 1f);
            }
            UpdateGhostPorts(world, def);

            // Water buildings must sit on LAND in a cell ADJACENT to water (not just "nearby"):
            // a tight range so the player can't plonk them at a distance from the shore.
            const float waterAdj = 1.6f; // reaches an orthogonally/diagonally adjacent water cell
            bool affordable = Economy.CanAfford(def.cost, Carried);
            bool placeOk;
            if (def.kind == BuildingKind.Collector && def.drill)
                placeOk = DepositUnderFootprint(world, ew, eh, def.item); // a DRILL must sit ON its deposit
            else if (def.kind == BuildingKind.Collector)
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
                        && (harbourPlace ? FootprintStraddlesShore(world, ew, eh) : FootprintOnLand(world, ew, eh))
                        // Can't build ON TOP of a resource patch (tree/rock/ore/clay/gems/...) — except the Oil
                        // Well, which is MEANT to sit on its oil deposit. Collectors bind from a radius, so they
                        // just sit beside the patch.
                        && (AllowedOnResource(def) || !FootprintOnResourceNode(world, ew, eh));
            PlacementValid = affordable && placeOk && free;

            // Clear green = OK, red = not OK (don't tint by the building's own colour,
            // which can look like the red "invalid" state — that was the confusion).
            Color validTint = PlacementValid
                ? new Color(0.35f, 1f, 0.4f, 0.55f)
                : new Color(1f, 0.3f, 0.3f, 0.5f);
            _ghostSr.color = validTint;
            if (isDepot) UpdateDepotGhost(def, world, ew, eh, BuildDir == Belt.Dir.N || BuildDir == Belt.Dir.S, validTint);
            else HideDepotGhost();
            if (def.kind == BuildingKind.Arm) UpdateArmGhost(def, world);
            else HideArmGhost();

            // Intentional placement: ONE building per deliberate click (no hold-drag spam —
            // that's reserved for belts/mass infrastructure). Stays in placement mode so you
            // can place several with discrete clicks; right-click / Esc finishes. The
            // PointerOverUI guard stops a build-menu click from also dropping a building.
            if (mouse.leftButton.wasPressedThisFrame && PlacementValid && !InventoryHud.PointerOverUI)
            {
                // One-fluid rule: refuse a source (pump/well) that would touch a network of a different fluid.
                if (def.kind == BuildingKind.Pump && !def.booster && PumpFluidClash(world, ew, eh, def.item, out string pmsg))
                {
                    Toast.Show($"<color=#ffb24d>⛔ {pmsg}</color>");
                }
                else
                {
                    // TIMED construction: pay the cost up front, then drop a construction SITE that assembles
                    // itself over a few seconds (size-scaled) before becoming the finished building — no builder
                    // units, no hauling. (Economy.Spend no-ops in sandbox/FreeBuild.) Cancelling refunds in full.
                    Economy.Spend(def.cost, Carried);
                    ConstructionSite.Spawn(def, world, BuildDir);
                    BuildingsPlaced++;
                    AudioManager.Place(); // placement thunk — the click landed
                }
            }
            else if (mouse.leftButton.wasPressedThisFrame && !PlacementValid && !InventoryHud.PointerOverUI)
            {
                AudioManager.Deny(); // clicked an invalid spot — audible "no" to match the red ghost
            }
            if (mouse.rightButton.wasPressedThisFrame) CancelPlacement();
        }

        // Lay the depot BLUEPRINT as one deck tile per footprint cell (rotated to the lane), tinted by validity —
        // a WYSIWYG preview of the tiled platform that gets built, so it no longer stretches/flips like the old
        // single square ghost. Tiles are pooled + reused, and surplus ones are deactivated.
        private void UpdateDepotGhost(BuildingDefinition def, Vector3 center, int w, int h, bool vertical, Color tint)
        {
            var sprite = def.isHarbour ? PlaceholderArt.HarbourDeckTile() : PlaceholderArt.StationDeckTile();
            float rot = vertical ? 90f : 0f; // deck art is authored for a horizontal (E–W) lane
            var cells = Footprint.Cells(center, w, h);
            int i = 0;
            for (; i < cells.Count; i++)
            {
                if (i >= _depotGhostTiles.Count)
                {
                    var g = new GameObject("DepotGhostTile");
                    g.AddComponent<SpriteRenderer>().sortingOrder = 20;
                    _depotGhostTiles.Add(g);
                }
                var go = _depotGhostTiles[i];
                if (!go.activeSelf) go.SetActive(true);
                go.transform.position = new Vector3(cells[i].x, cells[i].y, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, rot);
                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = tint;
            }
            for (; i < _depotGhostTiles.Count; i++) if (_depotGhostTiles[i].activeSelf) _depotGhostTiles[i].SetActive(false);
        }

        private void HideDepotGhost()
        {
            for (int i = 0; i < _depotGhostTiles.Count; i++)
                if (_depotGhostTiles[i] != null && _depotGhostTiles[i].activeSelf) _depotGhostTiles[i].SetActive(false);
        }

        // While placing a CRANE ARM: tint the actual GRAB cell(s) cyan and DROP cell(s) green (scaled by
        // its reach), so which way it works — and how far — is unmissable before you commit. R re-aims.
        private void UpdateArmGhost(BuildingDefinition def, Vector3 center)
        {
            int reach = Mathf.Max(1, Mathf.RoundToInt(def.searchRadius));
            var cell = Belt.CellOf(center);
            int used = 0;
            for (int side = 0; side < 2; side++) // 0 = grab (behind, cyan), 1 = drop (ahead, green)
            {
                var step = Belt.Step(side == 0 ? Belt.Opposite(BuildDir) : BuildDir);
                for (int d = 1; d <= reach; d++)
                {
                    if (used >= _armGhostCells.Count)
                    {
                        var g = new GameObject("ArmGhostCell");
                        var gsr = g.AddComponent<SpriteRenderer>();
                        gsr.sprite = PlaceholderArt.Square();
                        gsr.sortingOrder = 19; // under the ghost + port markers
                        _armGhostCells.Add(g);
                    }
                    var go = _armGhostCells[used++];
                    if (!go.activeSelf) go.SetActive(true);
                    go.transform.position = new Vector3(cell.x + step.x * d, cell.y + step.y * d, 0f);
                    go.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
                    float fade = 1f - 0.35f * (d - 1); // farther reach cells draw fainter
                    go.GetComponent<SpriteRenderer>().color = side == 0
                        ? new Color(0.35f, 0.70f, 1f, 0.28f * fade)   // cyan — grabs from here
                        : new Color(0.25f, 0.95f, 0.35f, 0.28f * fade); // green — drops here
                }
            }
            for (int k = used; k < _armGhostCells.Count; k++)
                if (_armGhostCells[k].activeSelf) _armGhostCells[k].SetActive(false);
        }

        private void HideArmGhost()
        {
            for (int i = 0; i < _armGhostCells.Count; i++)
                if (_armGhostCells[i] != null && _armGhostCells[i].activeSelf) _armGhostCells[i].SetActive(false);
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
                          || def.kind == BuildingKind.Storage || def.kind == BuildingKind.Arm; // crane: green arrow = DROP side
            bool hasIn = def.kind == BuildingKind.Workshop || def.kind == BuildingKind.Storage
                         || def.kind == BuildingKind.Research  // Lodge = input only
                         || def.kind == BuildingKind.Arm;      // crane: cyan notch = GRAB side
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
                    // Pipes and belts aren't "solid", but a building dropped on one would overlap it and render
                    // ON TOP (looking like the belt runs under the building) — so a pipe OR belt cell blocks
                    // placement too (clear it first). Keeps liquids / conveyors / buildings disjoint.
                    if (CellOccupied(new Vector3(c.x, c.y, 0f)) || WorldGrid.IsReserved(c)
                        || PipeNet.At(c) != null || Belt.At(c) != null) return true;
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

        // True if ANY cell this footprint covers sits on a resource node (tree/rock/ore/clay/gems/oil/…).
        // Resource nodes aren't "solid" (you walk/belt over them), so they don't show up in FootprintBlocked;
        // this is the dedicated "don't bury a resource under a building" test. No per-cell node map exists, so
        // scan ResourceNode.All by rounded cell — same idiom as HasMatchingNodeNear, and only runs while a
        // placement ghost is active.
        private static bool FootprintOnResourceNode(Vector3 center, int w, int h)
        {
            var a = Footprint.Anchor(center, w, h);
            foreach (var nd in ResourceNode.All)
            {
                if (nd == null) continue;
                int nx = Mathf.RoundToInt(nd.transform.position.x);
                int ny = Mathf.RoundToInt(nd.transform.position.y);
                if (nx >= a.x && nx < a.x + w && ny >= a.y && ny < a.y + h) return true;
            }
            return false;
        }

        // Buildings allowed to sit ON their resource: the Oil Well (a non-booster liquid Pump on its
        // deposit) and DRILLS (geological extractors that mine exactly what their footprint covers).
        // Everything else keeps clear of resource patches.
        private static bool AllowedOnResource(BuildingDefinition def) =>
            def.drill
            || (def.kind == BuildingKind.Pump && !def.booster && def.item != null && def.item.isLiquid && !def.fromWaterTerrain);

        // A matching pocket under the footprint + a 1-cell ring (mirrors ProductionBuilding.BindCoverage,
        // so the placement test and what the drill will actually mine can never disagree).
        private static bool DepositUnderFootprint(Vector3 center, int w, int h, ItemDefinition item)
        {
            if (item == null) return false;
            var a = Footprint.Anchor(center, w, h);
            foreach (var nd in ResourceNode.All)
            {
                if (nd == null || nd.yields != item || nd.transform.parent != null) continue;
                int nx = Mathf.RoundToInt(nd.transform.position.x);
                int ny = Mathf.RoundToInt(nd.transform.position.y);
                if (nx >= a.x - 1 && nx < a.x + w + 1 && ny >= a.y - 1 && ny < a.y + h + 1) return true;
            }
            return false;
        }

        // True if the footprint covers BOTH water and BUILDABLE land — a harbour must straddle the shore so the
        // boat can dock on the water half while belts connect on the land half. A dry cell that's NOT buildable
        // (a Mountain) fails the whole placement: a harbour dropped against a cliff would land its "land" half on
        // impassable rock that no belt can reach — a dead building. So we reject any Mountain cell outright.
        private bool FootprintStraddlesShore(Vector3 center, int w, int h)
        {
            var a = Footprint.Anchor(center, w, h);
            bool land = false, water = false;
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                {
                    var cell = new Vector2Int(a.x + i, a.y + j);
                    if (TerrainGrid.IsWater(cell)) water = true;
                    else if (TerrainGrid.Buildable(cell)) land = true; // dry AND buildable (excludes Mountain)
                    else return false;                                 // dry but unbuildable (Mountain) — no harbour on rock
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
        // includePoles: poles count as solid for PLACEMENT (so you can't build on a pole), but NOT for player
        // movement — you walk through a thin post (the player passes includePoles:false).
        public static bool SolidBuildingAt(Vector3 world, bool includePoles = true)
        {
            int n = Physics2D.OverlapPoint((Vector2)world, _solidFilter, _solidBuf);
            for (int i = 0; i < n; i++)
            {
                var h = _solidBuf[i];
                if (h == null) continue;
                if (h.GetComponent<ProductionBuilding>() || h.GetComponent<StorageBuilding>()
                    || h.GetComponent<WorkshopBuilding>() || h.GetComponent<Depot>()
                    || h.GetComponent<PowerPlant>() || h.GetComponent<WaterPump>() || h.GetComponent<Battery>()
                    || (includePoles && h.GetComponent<PowerPole>() != null) // poles block placement, not movement
                    || h.GetComponent<ResearchBuilding>() || h.GetComponent<Garage>()
                    || h.GetComponent<CraneArm>() || h.GetComponent<ConstructionSite>()) return true;
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

            // Underground belts use a GUIDED two-click flow (entrance, then a snapped exit) — handled apart
            // from the plain-belt blueprint and the splitter/filter single-click paths. (R already rotated
            // BeltDir above; the entrance phase reads it, the exit phase locks to the entrance's facing.)
            if (def.underground) { UpdateUndergroundPlacement(mouse, def, cell); return; }

            // Cursor-cell direction: along the active sketch stroke, else auto-oriented toward a sink.
            // A SPLITTER/MERGER over an existing belt previews that belt's FLOW (matching the convert-in-place
            // outcome), so the junction visibly aligns to your line instead of the last manual rotation.
            var beltHere = isJunction ? Belt.At(cell) : null;
            Belt.Dir dir = singleClick ? (beltHere != null ? beltHere.dir : BeltDir)
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

            // --- Plain belts: drag to SKETCH a straight line (live preview), RELEASE to build it; a single
            //     tap places one belt. Placement is IMMEDIATE — no lingering blueprint or separate confirm
            //     click (the preview exists only while you hold the drag). Right-click aborts / leaves mode. ---
            if (mouse.leftButton.wasPressedThisFrame && !overUI)
            {
                _strokeActive = true; _strokeMoved = false; _strokeStart = cell; _strokeLast = cell;
                _committed.Clear(); _beltPlan.Clear(); _beltPlanDirty = true;
                if (!_beltDragHinted) { _beltDragHinted = true; Toast.Show("<color=#9cf>Belt:</color> drag to lay a line · <color=#9f9>release to build</color> · right-click to cancel."); }
            }
            else if (_strokeActive && mouse.leftButton.isPressed)
            {
                // Belts lay as a clean single-corner L that REACHES the cursor cell (long leg + one turn),
                // replanned from the stroke start each move so the preview tracks the cursor.
                if (cell != _strokeLast) { _strokeMoved = true; _strokeLast = cell; ReplanStraight(_strokeStart, cell); }
            }
            else if (_strokeActive && mouse.leftButton.wasReleasedThisFrame)
            {
                _strokeActive = false;
                if (_strokeMoved) BuildBeltPlan(def);                       // dragged → build the sketched line on release
                else if (!overUI) EnsureBelt(cell, AutoBeltDir(cell), def); // tapped → place a single belt right away
                ClearBeltPlan();                                           // the preview is transient — clear it either way
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                if (_strokeActive) { _strokeActive = false; ClearBeltPlan(); } // abort the in-progress sketch
                else CancelPlacement();                                        // nothing pending → leave belt mode
            }

            RebuildPlanGhosts(def);
        }

        // Underground belt: GUIDED two-click placement. First click picks the ENTRANCE (oriented with R);
        // the tool then asks for the EXIT, SNAPPED to a cell up to UgMaxTunnel ahead along the entrance's
        // facing (so the pair always connects). BOTH ends are built — and auto-paired — on the exit click,
        // so right-clicking before then cancels cleanly with nothing left behind.
        private void UpdateUndergroundPlacement(Mouse mouse, BuildingDefinition def, Vector2Int cell)
        {
            bool overUI = InventoryHud.PointerOverUI;
            _ghostSr.sortingOrder = 20; // a prior rail/elevated ghost may have left this lowered

            // ---- Phase 1: place the ENTRANCE. ----
            if (!_ugAwaitingExit)
            {
                HideUndergroundGuides();
                Belt.Dir dir = BeltDir;
                _ghost.transform.position = new Vector3(cell.x, cell.y, 0f);
                _ghost.transform.rotation = Quaternion.Euler(0f, 0f, Belt.Angle(dir));
                _ghost.transform.localScale = Vector3.one;
                _ghostSr.sprite = PlaceholderArt.UndergroundBelt(false);

                bool valid = UndergroundCellFree(cell) && Economy.CanAfford(def.cost, Carried);
                PlacementValid = valid;
                _ghostSr.color = valid ? new Color(0.35f, 1f, 0.4f, 0.6f) : new Color(1f, 0.3f, 0.3f, 0.5f);

                if (mouse.leftButton.wasPressedThisFrame && valid && !overUI)
                {
                    _ugEntranceCell = cell; _ugEntranceDir = dir; _ugAwaitingExit = true;
                    Toast.Show("<color=#9cf>Underground belt:</color> now click the <color=#9f9>EXIT</color> up to 3 tiles ahead (same direction) · right-click cancels.");
                }
                else if (mouse.rightButton.wasPressedThisFrame) CancelPlacement();
                return;
            }

            // ---- Phase 2: place the EXIT, snapped ahead of the entrance along its facing. ----
            var fwd = Belt.Step(_ugEntranceDir);
            int ahead = (cell.x - _ugEntranceCell.x) * fwd.x + (cell.y - _ugEntranceCell.y) * fwd.y;
            ahead = Mathf.Clamp(ahead, 1, UgMaxTunnel);
            var exitCell = _ugEntranceCell + fwd * ahead;

            ShowUndergroundGuides(exitCell);

            _ghost.transform.position = new Vector3(exitCell.x, exitCell.y, 0f);
            _ghost.transform.rotation = Quaternion.Euler(0f, 0f, Belt.Angle(_ugEntranceDir));
            _ghost.transform.localScale = Vector3.one;
            _ghostSr.sprite = PlaceholderArt.UndergroundBelt(true);

            bool ok = UndergroundCellFree(exitCell) && UndergroundCellFree(_ugEntranceCell)
                      && CanAffordUndergroundPair(def.cost);
            PlacementValid = ok;
            _ghostSr.color = ok ? new Color(0.35f, 1f, 0.4f, 0.6f) : new Color(1f, 0.3f, 0.3f, 0.5f);

            if (mouse.leftButton.wasPressedThisFrame && !overUI && ok)
            {
                EnsureBelt(_ugEntranceCell, _ugEntranceDir, def); // entrance (stays unpaired for the moment)
                EnsureBelt(exitCell, _ugEntranceDir, def);         // exit — PairUnderground links it to the entrance behind it
                _ugAwaitingExit = false;
                HideUndergroundGuides();                            // ready for the next pair
            }
            else if (mouse.rightButton.wasPressedThisFrame)
            {
                _ugAwaitingExit = false; // abandon this pair (nothing was built) — back to choosing an entrance
                HideUndergroundGuides();
            }
        }

        // An underground end may only sit on an empty, belt-legal, unreserved, non-solid cell.
        private bool UndergroundCellFree(Vector2Int cell)
            => Belt.At(cell) == null && TerrainGrid.BeltAllowed(cell)
               && !SolidBuildingAt(new Vector3(cell.x, cell.y, 0f)) && !WorldGrid.IsReserved(cell);

        // Can the player afford BOTH tunnel ends? (they're paid together on the exit click)
        private bool CanAffordUndergroundPair(List<ItemAmount> cost)
        {
            _ugPairCostBuf.Clear();
            foreach (var c in cost) if (c != null && c.item != null) _ugPairCostBuf.Add(new ItemAmount(c.item, c.amount * 2));
            return Economy.CanAfford(_ugPairCostBuf, Carried);
        }

        // Persistent previews shown while choosing the exit: a ghost of the chosen entrance + a translucent
        // bar spanning the hidden tunnel between the two ends.
        private void ShowUndergroundGuides(Vector2Int exitCell)
        {
            if (_ugEntranceGhost == null)
            {
                _ugEntranceGhost = new GameObject("UgEntranceGhost");
                _ugEntranceGhost.AddComponent<SpriteRenderer>().sortingOrder = 20;
            }
            _ugEntranceGhost.SetActive(true);
            var eg = _ugEntranceGhost.GetComponent<SpriteRenderer>();
            eg.sprite = PlaceholderArt.UndergroundBelt(false);
            eg.color = new Color(0.5f, 0.85f, 1f, 0.6f);
            _ugEntranceGhost.transform.position = new Vector3(_ugEntranceCell.x, _ugEntranceCell.y, 0f);
            _ugEntranceGhost.transform.rotation = Quaternion.Euler(0f, 0f, Belt.Angle(_ugEntranceDir));
            _ugEntranceGhost.transform.localScale = Vector3.one;

            if (_ugSpanGhost == null)
            {
                _ugSpanGhost = new GameObject("UgSpanGhost");
                var ssr = _ugSpanGhost.AddComponent<SpriteRenderer>();
                ssr.sprite = PlaceholderArt.Square();
                ssr.sortingOrder = 19;
            }
            _ugSpanGhost.SetActive(true);
            var span = _ugSpanGhost.GetComponent<SpriteRenderer>();
            span.color = new Color(0.5f, 0.85f, 1f, 0.26f);
            Vector2 a = _ugEntranceCell, b = exitCell;
            Vector2 mid = (a + b) * 0.5f;
            float len = Vector2.Distance(a, b);
            bool horiz = _ugEntranceDir == Belt.Dir.E || _ugEntranceDir == Belt.Dir.W;
            _ugSpanGhost.transform.position = new Vector3(mid.x, mid.y, 0f);
            _ugSpanGhost.transform.localScale = horiz ? new Vector3(len + 1f, 0.32f, 1f) : new Vector3(0.32f, len + 1f, 1f);
        }

        private void HideUndergroundGuides()
        {
            if (_ugEntranceGhost != null) _ugEntranceGhost.SetActive(false);
            if (_ugSpanGhost != null) _ugSpanGhost.SetActive(false);
        }

        // ---- Belt blueprint helpers (plan, preview, build) ------------------------------------
        // Re-plan the run from `start` to `end` as a clean single-corner L: the LONG leg runs along the dominant
        // axis, then the belt turns ONCE and the short leg finishes on `end`, so the run always REACHES the cursor
        // cell (each cell FLOWS toward the next, and the corner cell turns) instead of snapping to one axis and
        // stopping short. When start and end share a row/column the minor leg is empty → a clean straight belt.
        private void ReplanStraight(Vector2Int start, Vector2Int end)
        {
            _beltPlan.Clear();
            foreach (var kv in _committed) _beltPlan[kv.Key] = kv.Value; // keep earlier strokes
            _beltPlanDirty = true;
            int dx = end.x - start.x, dy = end.y - start.y;
            int sx = dx >= 0 ? 1 : -1, sy = dy >= 0 ? 1 : -1;
            Belt.Dir dirX = dx >= 0 ? Belt.Dir.E : Belt.Dir.W;
            Belt.Dir dirY = dy >= 0 ? Belt.Dir.N : Belt.Dir.S;
            var c = start;
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            {
                bool turn = Mathf.Abs(dy) > 0; // the last long-leg cell turns toward the minor leg
                PlanCell(c, dirX);
                for (int i = 0; i < Mathf.Abs(dx); i++) { c.x += sx; PlanCell(c, (turn && i == Mathf.Abs(dx) - 1) ? dirY : dirX); }
                for (int i = 0; i < Mathf.Abs(dy); i++) { c.y += sy; PlanCell(c, dirY); }
            }
            else
            {
                bool turn = Mathf.Abs(dx) > 0;
                PlanCell(c, dirY);
                for (int i = 0; i < Mathf.Abs(dy); i++) { c.y += sy; PlanCell(c, (turn && i == Mathf.Abs(dy) - 1) ? dirX : dirY); }
                for (int i = 0; i < Mathf.Abs(dx); i++) { c.x += sx; PlanCell(c, dirX); }
            }
        }

        private void PlanCell(Vector2Int cell, Belt.Dir dir)
        {
            _beltPlan[cell] = dir;   // a previewed cell of the in-progress drag (built on release)
            _beltPlanDirty = true;
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
            _beltPlanDirty = false;
            DragCostLabel = "";
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
            int budget = AffordableCellCount(def.cost); // how many cells you can actually pay for
            int paid = 0, i = 0;
            foreach (var kv in _beltPlan)
            {
                var go = _planGhosts[i++];
                if (!go.activeSelf) go.SetActive(true);
                go.transform.position = new Vector3(kv.Key.x, kv.Key.y, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, Belt.Angle(kv.Value));
                go.transform.localScale = Vector3.one; // match the placed belt (full cell)
                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = SpriteDatabase.ForBelt(def.displayName, def.splitter, def.merger);
                bool terrainOk = Belt.At(kv.Key) == null && TerrainGrid.BeltAllowed(kv.Key)
                          && !SolidBuildingAt(new Vector3(kv.Key.x, kv.Key.y, 0f)) && !WorldGrid.IsReserved(kv.Key);
                if (!terrainOk) sr.color = new Color(1f, 0.35f, 0.35f, 0.5f);                     // blocked — red
                else if (paid < budget) { sr.color = new Color(0.4f, 0.9f, 1f, 0.45f); paid++; } // affordable — cyan
                else sr.color = new Color(1f, 0.55f, 0.12f, 0.55f);                               // can't afford — orange
            }
            for (int k = n; k < _planGhosts.Count; k++) if (_planGhosts[k].activeSelf) _planGhosts[k].SetActive(false);
            DragCostLabel = BuildStretchCostLabel(def, n, budget);
        }

        public string DragCostLabel { get; private set; } = "";

        // How many cells of a per-cell build (belt/rail) the player can currently afford.
        private int AffordableCellCount(List<ItemAmount> cost)
        {
            if (Economy.FreeBuild || cost == null || cost.Count == 0) return int.MaxValue;
            int min = int.MaxValue;
            foreach (var c in cost)
            {
                if (c == null || c.item == null || c.amount <= 0) continue;
                int can = Economy.Available(c.item, Carried) / c.amount;
                if (can < min) min = can;
            }
            return min == int.MaxValue ? int.MaxValue : Mathf.Max(0, min);
        }

        // "Conveyor ×12 — 24 Planks, 12 Metal  (enough for 8)" — total stretch cost + how many you can pay for.
        private string BuildStretchCostLabel(BuildingDefinition def, int n, int budget)
        {
            if (n <= 0) return "";
            var sb = new System.Text.StringBuilder();
            sb.Append($"{def.displayName} ×{n}");
            if (def.cost != null && def.cost.Count > 0)
            {
                sb.Append(" — "); bool first = true;
                foreach (var c in def.cost) { if (c == null || c.item == null) continue; if (!first) sb.Append(", "); sb.Append($"{c.amount * n} {c.item.displayName}"); first = false; }
            }
            if (budget != int.MaxValue && budget < n) sb.Append($"  (enough for {budget})");
            return sb.ToString();
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
                // Junctions (splitter/merger) and special belts (filter/gate/underground) keep the orientation
                // they were PLACED with — dragging a belt into their side must never spin them around.
                if (!existing.isSplitter && !existing.isMerger && !existing.isFilter && !existing.isGate && !existing.underground)
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
            AudioManager.Place(); // one thunk per laid segment
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

        // Pipe mode: drag to lay liquid-network segments on land (or bridged water). No direction; placed
        // instantly. A Water Pump pushes water through them into storage. ONE-FLUID rule: a pipe can't join a
        // line/source carrying a different fluid — the ghost goes amber + a toast explains when you try.
        private static readonly Belt.Dir[] _pdirs = { Belt.Dir.N, Belt.Dir.E, Belt.Dir.S, Belt.Dir.W };
        private Vector2Int _pipeCheckCell = new Vector2Int(int.MinValue, int.MinValue);
        private bool _pipeClash; private string _pipeClashMsg;
        private float _pipeClashToastT = -9f;
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

            // The fluid-clash flood is O(network), so recompute it only when the hovered cell changes.
            if (cell != _pipeCheckCell) { _pipeCheckCell = cell; _pipeClash = PipeFluidClash(cell, out _pipeClashMsg); }

            bool ok = PipeNet.At(cell) == null && TerrainGrid.BeltAllowed(cell)
                      && !SolidBuildingAt(new Vector3(cell.x, cell.y, 0f)) // never lay a pipe on/under a building
                      && !_pipeClash                                       // can't merge two different fluids
                      && Economy.CanAfford(def.cost, Carried);
            PlacementValid = ok;
            _ghostSr.color = ok ? new Color(0.35f, 1f, 0.4f, 0.55f)
                           : _pipeClash ? new Color(1f, 0.55f, 0.15f, 0.65f)  // amber = fluid clash (distinct from plain invalid)
                           : new Color(1f, 0.3f, 0.3f, 0.5f);

            if (!mouse.leftButton.isPressed) _dragging = false;
            if (mouse.leftButton.isPressed && !InventoryHud.PointerOverUI)
            {
                // Surface the clash reason when the player actually tries to connect (rate-limited so a drag won't spam).
                if (_pipeClash && Time.unscaledTime - _pipeClashToastT > 1.5f)
                { Toast.Show($"<color=#ffb24d>⛔ {_pipeClashMsg}</color>"); _pipeClashToastT = Time.unscaledTime; }

                // Drag paves a CONTINUOUS pipe: drop one cell on the first press, then fill the orthogonal path
                // between the last laid cell and the cursor each move — so a fast drag can't skip cells and leave
                // a broken line (belts/rails already plan a continuous run; this brings pipes in line).
                if (!_dragging) { TryLayPipe(cell, def); _dragging = true; _dragLast = cell; }
                else if (cell != _dragLast) { PavePipeLine(_dragLast, cell, def); _dragLast = cell; }
            }
            if (mouse.rightButton.wasPressedThisFrame) CancelPlacement();
        }

        // Fill the orthogonal L from `from` (exclusive) to `to` (inclusive), laying a pipe on every cell that's
        // free + affordable — so a fast drag between two cells leaves a connected line, not a gapped one.
        private void PavePipeLine(Vector2Int from, Vector2Int to, BuildingDefinition def)
        {
            int dx = to.x - from.x, dy = to.y - from.y;
            int sx = dx >= 0 ? 1 : -1, sy = dy >= 0 ? 1 : -1;
            var c = from;
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            {
                for (int i = 0; i < Mathf.Abs(dx); i++) { c.x += sx; TryLayPipe(c, def); }
                for (int i = 0; i < Mathf.Abs(dy); i++) { c.y += sy; TryLayPipe(c, def); }
            }
            else
            {
                for (int i = 0; i < Mathf.Abs(dy); i++) { c.y += sy; TryLayPipe(c, def); }
                for (int i = 0; i < Mathf.Abs(dx); i++) { c.x += sx; TryLayPipe(c, def); }
            }
        }

        // Lay one pipe if the cell is clear, buildable, not on a building, affordable, and wouldn't merge two
        // different fluids. No-op otherwise (same gates as the single-cell placement above).
        private void TryLayPipe(Vector2Int cell, BuildingDefinition def)
        {
            if (PipeNet.At(cell) != null || !TerrainGrid.BeltAllowed(cell)
                || SolidBuildingAt(new Vector3(cell.x, cell.y, 0f))
                || !Economy.CanAfford(def.cost, Carried) || PipeFluidClash(cell, out _)) return;
            Economy.Spend(def.cost, Carried);
            Pipe.Spawn(def, new Vector3(cell.x, cell.y, 0f));
            BuildingsPlaced++;
        }

        // True if placing a pipe at `cell` would JOIN two different fluids — an existing line of one fluid meeting
        // another fluid's line or an adjacent source of a different fluid. Sets `msg` with a player-facing reason.
        private bool PipeFluidClash(Vector2Int cell, out string msg)
        {
            msg = null;
            ItemDefinition f0 = null;
            foreach (var d in _pdirs) // adjacent existing pipe networks
            {
                var nb = cell + Belt.Step(d);
                if (PipeNet.At(nb) == null) continue;
                var nf = PipeNet.NetworkFluid(nb, out _);
                if (nf == null) continue;
                if (f0 == null) f0 = nf; else if (f0 != nf) { msg = ClashMsg(f0, nf); return true; }
            }
            foreach (var p in WaterPump.All) // adjacent fluid SOURCES (a pump/well imposes its fluid)
            {
                if (p == null || p.isBooster || p.water == null) continue;
                var pc = new Vector2Int(Mathf.RoundToInt(p.transform.position.x), Mathf.RoundToInt(p.transform.position.y));
                bool adj = false; foreach (var d in _pdirs) if (pc + Belt.Step(d) == cell) { adj = true; break; }
                if (!adj) continue;
                if (f0 == null) f0 = p.water; else if (f0 != p.water) { msg = ClashMsg(f0, p.water); return true; }
            }
            return false;
        }

        // True if a fluid SOURCE placed over footprint (w×h at `world`) would touch a pipe network already
        // carrying a different fluid (e.g. an Oil Well dropped onto a water line). Sets `msg` with the reason.
        private bool PumpFluidClash(Vector3 world, int w, int h, ItemDefinition fluid, out string msg)
        {
            msg = null;
            if (fluid == null) return false;
            var a = Footprint.Anchor(world, w, h);
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                {
                    var c = new Vector2Int(a.x + i, a.y + j);
                    foreach (var d in _pdirs)
                    {
                        var nf = PipeNet.At(c + Belt.Step(d)) != null ? PipeNet.NetworkFluid(c + Belt.Step(d), out _) : null;
                        if (nf != null && nf != fluid) { msg = ClashMsg(fluid, nf); return true; }
                    }
                }
            return false;
        }

        private static string ClashMsg(ItemDefinition a, ItemDefinition b)
            => $"Can't connect {a.displayName} and {b.displayName} pipes — keep each fluid on its own line.";

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
                _strokeActive = true; _strokeMoved = false; _strokeStart = cell; _strokeLast = cell;
                _railCommitted = _railPlan.Count; // prior straight strokes stay; THIS stroke rebuilds after them
            }
            else if (_strokeActive && mouse.leftButton.isPressed)
            {
                if (cell != _strokeLast)
                {
                    _strokeMoved = true; _strokeLast = cell;
                    // Rebuild this stroke as ONE straight segment from the press point → a clean line, no staircase.
                    if (_railPlan.Count > _railCommitted) _railPlan.RemoveRange(_railCommitted, _railPlan.Count - _railCommitted);
                    PlanRailPath(_strokeStart, cell);
                }
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
        // A clean single-corner L from `from` to `to`: the LONG leg runs along the dominant axis, then the run
        // turns ONCE and the short leg finishes on the minor axis so it lands EXACTLY on `to`. Landing on the
        // cursor cell (rather than snapping to one axis and dropping the other) is what lets a drag up to an
        // existing track actually MEET it — the old dominant-axis-only snap stopped short and left a gap. When
        // `from` and `to` share a row or column the minor leg is empty, so it degenerates to a clean straight run.
        private void PlanRailPath(Vector2Int from, Vector2Int to)
        {
            int dx = to.x - from.x, dy = to.y - from.y;
            int sx = dx > 0 ? 1 : -1, sy = dy > 0 ? 1 : -1;
            var c = from; PlanRailCell(c);
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            {
                for (int i = 0; i < Mathf.Abs(dx); i++) { c.x += sx; PlanRailCell(c); }
                for (int i = 0; i < Mathf.Abs(dy); i++) { c.y += sy; PlanRailCell(c); }
            }
            else
            {
                for (int i = 0; i < Mathf.Abs(dy); i++) { c.y += sy; PlanRailCell(c); }
                for (int i = 0; i < Mathf.Abs(dx); i++) { c.x += sx; PlanRailCell(c); }
            }
        }

        private void PlanRailCell(Vector2Int cell)
        {
            if (!_railPlan.Contains(cell)) _railPlan.Add(cell);
            _railPlanDirty = true;
            if (!_railPlanHinted)
            {
                _railPlanHinted = true;
                Toast.Show("<color=#9cf>Track blueprint:</color> drag to run track (turns ONE corner to reach the cursor) · drag again to add another turn · <color=#9f9>click to build</color> · right-click to cancel.");
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
            DragCostLabel = "";
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
            var rdef = (PendingIndex >= 0 && PendingIndex < buildables.Count) ? buildables[PendingIndex] : null;
            int budget = rdef != null ? AffordableCellCount(rdef.cost) : int.MaxValue;
            int paid = 0, buildable = 0;
            for (int i = 0; i < n; i++)
            {
                var go = _railPlanGhosts[i];
                if (!go.activeSelf) go.SetActive(true);
                var c = _railPlan[i];
                go.transform.position = new Vector3(c.x, c.y, 0f);
                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = PlaceholderArt.RailMask(GhostMaskAt(i, n)); // EXACT preview: only what will actually connect
                bool already = RailTile.At(c) != null; // an existing tile we'll just LINK to — not a conflict, and free
                if (already) sr.color = new Color(0.45f, 0.95f, 0.55f, 0.45f);                     // already here — will connect (green)
                else if (!RailCellFree(c, elev)) sr.color = new Color(1f, 0.35f, 0.35f, 0.55f);    // blocked — red
                else { buildable++; if (paid < budget) { sr.color = new Color(0.4f, 0.9f, 1f, 0.5f); paid++; } else sr.color = new Color(1f, 0.55f, 0.12f, 0.55f); }
            }
            for (int k = n; k < _railPlanGhosts.Count; k++) if (_railPlanGhosts[k].activeSelf) _railPlanGhosts[k].SetActive(false);
            DragCostLabel = rdef != null ? BuildStretchCostLabel(rdef, buildable, budget) : "";
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
            // Face an adjacent MERGER (it pulls from belts that point INTO it) — but NOT on its output side — so
            // "connect a belt to a merger from the side" just works instead of laying a dead-end belt that reads red.
            for (int i = 0; i < 4; i++)
            {
                var d = (Belt.Dir)i; var nb = Belt.At(cell + Belt.Step(d));
                if (nb != null && nb.isMerger && nb.dir != Belt.Opposite(d)) return d; // d faces the merger; its output side is excluded
            }
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
            _ugAwaitingExit = false;
            HideUndergroundGuides();
            ClearBeltPlan();
            ClearRailPlan();
            ClearGhostJunctionPorts();
            if (_ghost != null) _ghost.SetActive(false);
            HideGhostPorts();
            HideDepotGhost();
            HideArmGhost();
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
            var armSel = Selected.GetComponent<CraneArm>();
            BuildingDefinition rdef = pb != null ? pb.def : sb != null ? sb.def
                : wb != null ? wb.def : dpo != null ? dpo.def : pp != null ? pp.def
                : pole != null ? pole.def : bat != null ? bat.def
                : wp != null ? wp.def : rsb != null ? rsb.def : armSel != null ? armSel.def : null;
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

        // The menu definition a placed thing was built from (for the Q pipette). Belts/pipes/signals
        // don't carry a def reference, so they resolve by matching a buildable's flags/name.
        private BuildingDefinition DefOf(GameObject go)
        {
            if (go == null) return null;
            var pb = go.GetComponent<ProductionBuilding>(); if (pb != null) return pb.def;
            var wb = go.GetComponent<WorkshopBuilding>(); if (wb != null) return wb.def;
            var sb = go.GetComponent<StorageBuilding>(); if (sb != null) return sb.def;
            var dp = go.GetComponent<Depot>(); if (dp != null) return dp.def;
            var pp = go.GetComponent<PowerPlant>(); if (pp != null) return pp.def;
            var pl = go.GetComponent<PowerPole>(); if (pl != null) return pl.def;
            var bt = go.GetComponent<Battery>(); if (bt != null) return bt.def;
            var gr = go.GetComponent<Garage>(); if (gr != null) return gr.def;
            var wp = go.GetComponent<WaterPump>(); if (wp != null) return wp.def;
            var rb = go.GetComponent<ResearchBuilding>(); if (rb != null) return rb.def;
            var br = go.GetComponent<Bridge>(); if (br != null) return br.def;
            var rt = go.GetComponent<RailTile>(); if (rt != null) return rt.def;
            var cs = go.GetComponent<ConstructionSite>(); if (cs != null) return cs.def;
            var arm = go.GetComponent<CraneArm>(); if (arm != null) return arm.def;
            var pi = go.GetComponent<Pipe>();
            if (pi != null) { foreach (var d in buildables) if (d != null && d.kind == BuildingKind.Pipe && d.splitter == pi.isSplitter && d.merger == pi.isMerger) return d; }
            var sg = go.GetComponent<Signal>();
            if (sg != null) { foreach (var d in buildables) if (d != null && d.kind == BuildingKind.Signal && d.bothWaySignal == sg.bothWays) return d; }
            var bl = go.GetComponent<Belt>();
            if (bl != null) { foreach (var d in buildables) if (d != null && d.displayName == bl.DisplayName) return d; }
            return null;
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
                              || hit.GetComponent<CraneArm>() != null
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
