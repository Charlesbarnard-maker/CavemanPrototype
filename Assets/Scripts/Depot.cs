using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A transfer station for long-distance logistics. Holds one resource type
    /// (set like a warehouse). Belts deliver into it and pull from it locally; a
    /// caravan Route carries goods between two depots across the map. Connect a
    /// belt → depot → (route) → depot → belt to move resources home from afar.
    /// </summary>
    public class Depot : MonoBehaviour
    {
        public BuildingDefinition def;
        public ItemDefinition item;       // what this depot handles (configurable)
        public Inventory store;
        private int _minX, _maxX, _minY, _maxY; // footprint bounds (the belt-I/O edge rows/columns)
        private Belt.Dir _inSide = Belt.Dir.S, _outSide = Belt.Dir.N; // which platform edge takes IN / gives OUT

        // Strict platform I/O: belts deliver IN across the input edge (moving INTO it) and take OUT across the
        // output edge (a belt just beyond it, scanning toward it). The edges flip with the station's rotation:
        // a horizontal station takes IN on the SOUTH edge / OUT on the NORTH; a vertical one IN on the EAST /
        // OUT on the WEST. Matches the green/cyan port markers.
        public bool IsInputDeposit(Vector2Int depotCell, Belt.Dir beltMoveDir)
            => beltMoveDir == Belt.Opposite(_inSide) && OnEdge(depotCell, _inSide);
        public bool IsOutputPull(Vector2Int depotCell, Belt.Dir beltScanDir)
            => beltScanDir == Belt.Opposite(_outSide) && OnEdge(depotCell, _outSide);

        private bool OnEdge(Vector2Int c, Belt.Dir side) => side switch
        {
            Belt.Dir.N => c.y == _maxY,
            Belt.Dir.S => c.y == _minY,
            Belt.Dir.E => c.x == _maxX,
            _ => c.x == _minX,
        };

        // Which way the WATER lies relative to a harbour's footprint (so I/O can face the LAND side).
        private static Belt.Dir HarbourWaterDir(System.Collections.Generic.List<Vector2Int> cells, Vector3 center)
        {
            Vector2 ctr = new Vector2(center.x, center.y), waterSum = Vector2.zero; int n = 0;
            foreach (var c in cells) if (TerrainGrid.IsWater(c)) { waterSum += new Vector2(c.x, c.y); n++; }
            if (n == 0) return Belt.Dir.N;
            Vector2 d = (waterSum / n) - ctr;
            return Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? (d.x >= 0 ? Belt.Dir.E : Belt.Dir.W) : (d.y >= 0 ? Belt.Dir.N : Belt.Dir.S);
        }

        // Pipe-fed liquid logistics: a station handling a LIQUID is filled by pumps through pipes (WaterPump
        // Sink 3) and — at a destination — pushes its delivered liquid ONWARD through pipes into consumers.
        // We disambiguate the two roles by RECENCY: a station drains only when it was train-fed more recently
        // than pump-fed, so a pump-fed SOURCE never leaks the cargo a tanker is meant to collect.
        public float pumpFedAt = -1f, trainFedAt = -1f;
        private float _liquidT;
        private static readonly Queue<Vector2Int> _lq = new();
        private static readonly Dictionary<Vector2Int, int> _ldist = new();
        private const int LiquidDrainRange = 24;

        public static readonly List<Depot> All = new();
        private List<Vector2Int> _cells; // every grid cell this building occupies
        private readonly List<GameObject> _decor = new(); // track sprites laid over the platform lane
        private readonly List<SpriteRenderer> _deckSrs = new(); // per-cell deck tiles (fill-tinted together)
        private Color _baseColor;

        public static Depot Spawn(BuildingDefinition def, Vector3 pos, Belt.Dir face = Belt.Dir.E)
        {
            // The station rotates with R: a horizontal facing (E/W) keeps the authored 3×1 platform with an
            // east–west lane; a vertical facing (N/S) turns it 1×3 with a north–south lane. The belt I/O edges
            // turn with it (OUT/IN on N/S when horizontal, on W/E when vertical).
            bool vertical = face == Belt.Dir.N || face == Belt.Dir.S;
            int w = vertical ? def.FootH : def.FootW;
            int h = vertical ? def.FootW : def.FootH;
            Belt.Dir outSide = vertical ? Belt.Dir.W : Belt.Dir.N;
            Belt.Dir inSide = vertical ? Belt.Dir.E : Belt.Dir.S;

            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            // Root stays UNIFORM (scale 1) so the per-cell deck tiles, port markers and track sprites all render
            // at world scale and never distort. The deck is laid as ONE seamless tile PER footprint cell (rotated
            // to the lane axis) instead of a single square sprite stretched 3:1 — so a 3×1 platform reads cleanly
            // and turns WITH the lane rather than squishing when it's placed vertically. Collider sized explicitly.
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w, h);

            var d = go.AddComponent<Depot>();
            d.def = def;
            d.item = def.item; // usually null (configurable)
            d.store = new Inventory { capacity = Mathf.Max(1, def.capacity) };
            d._baseColor = def.color;
            d._outSide = outSide; d._inSide = inSide;
            bool harbour = def != null && def.isHarbour;
            d.BuildDeck(w, h, vertical, harbour); // per-cell tiled deck + a centre-cell landmark accent
            d._cells = Footprint.Cells(go.transform.position, w, h);
            d._minX = int.MaxValue; d._maxX = int.MinValue; d._minY = int.MaxValue; d._maxY = int.MinValue;
            // A Station's footprint IS a TRACK LANE (registered as rail so trains route through it). A HARBOUR
            // is a dock — NOT part of the rail net — so cargo ships (which travel straight over water) run
            // between harbours instead. Both deliver belt goods IN on the input edge, OUT on the output edge.
            foreach (var c in d._cells)
            {
                WorldGrid.Depots[c] = d;
                if (!harbour) RailNet.StationLane.Add(c);
                if (c.x < d._minX) d._minX = c.x; if (c.x > d._maxX) d._maxX = c.x;
                if (c.y < d._minY) d._minY = c.y; if (c.y > d._maxY) d._maxY = c.y;
            }
            // With EXPLICIT rail links, a station placed beside ALREADY-laid track wouldn't connect (that track
            // has no link toward this new lane). So point each adjacent existing rail tile INTO the lane.
            if (!harbour)
                foreach (var c in d._cells)
                    foreach (var dir in RailTile.Four)
                    {
                        var t = RailTile.At(c + Belt.Step(dir));
                        if (t != null) t.links |= RailTile.DirBit(Belt.Opposite(dir));
                    }
            if (harbour)
            {
                // The ship docks on the WATER side; goods connect on the LAND side. A CARGO harbour takes belts
                // there (1 in + 1 out); a LIQUID harbour connects by PIPE instead, so it gets no belt ports.
                Belt.Dir waterDir = HarbourWaterDir(d._cells, go.transform.position);
                Belt.Dir landDir = Belt.Opposite(waterDir);
                d._outSide = landDir; d._inSide = landDir;
                if (def.isLiquidHarbour) { /* pipe-connected — run a pipe up to its land side; no belt ports */ }
                else
                {
                    Ports.PlacePorts(go.transform, w, h, landDir, false, true);               // OUT arrows on the land edge
                    Ports.PlacePorts(go.transform, w, h, Belt.Opposite(landDir), true, false); // IN notches on the land edge
                }
            }
            else
            {
                Ports.PlacePorts(go.transform, w, h, outSide, true, true); // OUT on outSide, IN opposite
                d.AddTrackLane(vertical);                                  // rail Station only
            }
            return d;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable()
        {
            All.Remove(this);
            bool harbour = def != null && def.isHarbour;
            if (_cells != null)
                foreach (var c in _cells)
                {
                    WorldGrid.Remove(WorldGrid.Depots, c, this); RailNet.StationLane.Remove(c); RailGraph.Clear(c);
                    // Drop the link bits this station stamped INTO adjacent rail tiles (Spawn pointed each existing
                    // neighbour into the lane) so demolishing it leaves no dangling half-connection to the empty cell.
                    if (!harbour)
                        foreach (var dir in RailTile.Four)
                        {
                            var t = RailTile.At(c + Belt.Step(dir));
                            if (t != null) t.links &= ~RailTile.DirBit(Belt.Opposite(dir));
                        }
                }
            foreach (var g in _decor) if (g != null) Destroy(g);
            _decor.Clear();
        }

        // Lay the platform DECK as one seamless tile per footprint cell (each rotated to the lane axis), plus a
        // single landmark ACCENT (station shelter / harbour crane) on the centre cell. All children of the
        // UNIFORM root, so nothing is stretched — this is what replaced the single square sprite that squashed
        // 3:1 and flipped on rotate. Deck tiles are collected so Update can fill-tint them together.
        private void BuildDeck(int w, int h, bool vertical, bool harbour)
        {
            Vector3 center = transform.position;
            float deckRot = vertical ? 90f : 0f; // deck art is authored for a horizontal (E–W) lane
            var deckSprite = harbour ? PlaceholderArt.HarbourDeckTile() : PlaceholderArt.StationDeckTile();
            foreach (var c in Footprint.Cells(center, w, h))
            {
                var t = new GameObject("deck");
                t.transform.SetParent(transform);
                t.transform.localPosition = new Vector3(c.x - center.x, c.y - center.y, 0f);
                t.transform.localRotation = Quaternion.Euler(0f, 0f, deckRot);
                var sr = t.AddComponent<SpriteRenderer>();
                sr.sprite = deckSprite;
                sr.color = _baseColor;      // the body's identity tint (fill-lerped in Update)
                sr.sortingOrder = 4;
                _deckSrs.Add(sr);
            }
            // Landmark on the centre cell (a 3×1 footprint centres on a real cell). Kept UPRIGHT so it always
            // reads, in its authored colours (white tint), drawn above the deck (4) and any track lane (6).
            var accent = new GameObject(harbour ? "crane" : "shelter");
            accent.transform.SetParent(transform);
            accent.transform.localPosition = Vector3.zero;
            var asr = accent.AddComponent<SpriteRenderer>();
            asr.sprite = harbour ? PlaceholderArt.HarbourCrane() : PlaceholderArt.StationShelter();
            asr.color = Color.white;
            asr.sortingOrder = 7;
        }

        // Lay a visible TRACK across every lane cell, over the platform deck, so it's obvious the rail runs
        // through the station. Unparented (kept out of the root so lifecycle is explicit). The lane
        // runs along the platform's long axis — east–west when horizontal, north–south when rotated.
        private void AddTrackLane(bool vertical)
        {
            foreach (var c in _cells)
            {
                var go = new GameObject("stationtrack");
                go.transform.position = new Vector3(c.x, c.y, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, vertical ? 0f : 90f); // along the lane axis
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PlaceholderArt.Rail();
                sr.color = Color.white;
                sr.sortingOrder = 6; // above the platform deck (4)
                _decor.Add(go);
            }
        }

        // --- Route role + validation (read by the HUD; no behaviour change to the sim) ---

        /// <summary>Is this station a stop on any line (a vehicle visits it)?</summary>
        public bool OnRoute { get { foreach (var rv in RouteVehicle.All) if (rv != null && rv.Serves(this)) return true; return false; } }
        // A line stop both sends and receives, so the old source/sink flags both reflect "on a line".
        public bool HasOutgoing => OnRoute;
        public bool HasIncoming => OnRoute;

        /// <summary>Plain-language role for the panel.</summary>
        public string Role => OnRoute ? "On a line" : "Idle — no line";

        public float FillFraction => def != null && def.capacity > 0 ? (float)store.Total() / def.capacity : 0f;

        /// <summary>Subtle validation note ("" = fine): nothing to send, a full stop, or no resource set —
        /// the lightweight "is this stop working?" check. Shown small, never flashed.</summary>
        public string Issue()
        {
            if (item == null) return "no resource set — belt one in, or set it below";
            if (OnRoute && store.Total() == 0) return "nothing to send — belt goods in";
            if (OnRoute && def != null && store.Total() >= def.capacity) return "full — incoming goods will stall";
            return "";
        }

        /// <summary>Choose which resource this depot handles (only while empty).</summary>
        public void CycleItem()
        {
            if (store.Total() > 0) return;
            var items = new List<ItemDefinition>();
            void Add(ItemDefinition i) { if (i != null && !items.Contains(i)) items.Add(i); }
            foreach (var p in ProductionBuilding.All) Add(p.produces);
            foreach (var w in WorkshopBuilding.All) Add(w.output);
            if (items.Count == 0) return;
            int idx = items.IndexOf(item);
            item = items[(idx + 1) % items.Count];
        }

        void Update()
        {
            if (def == null) return;
            float f = def.capacity > 0 ? (float)store.Total() / def.capacity : 0f;
            Color empty = Color.Lerp(_baseColor, Color.black, 0.5f);
            Color lit = Color.Lerp(empty, _baseColor, Mathf.Clamp01(0.2f + 0.8f * f)); // darker when empty, lit when full
            for (int i = 0; i < _deckSrs.Count; i++) if (_deckSrs[i] != null) _deckSrs[i].color = lit;

            // A train-fed liquid DESTINATION pushes its cargo onward through the pipe network (throttled).
            if (item != null && item.isLiquid && trainFedAt > pumpFedAt)
            {
                _liquidT += Time.deltaTime;
                if (_liquidT >= 0.4f) { _liquidT = 0f; DrainLiquid(); }
            }
        }

        // Flood the connected pipe network from this station's footprint and push the held liquid into liquid
        // storages (barrels) + liquid-using workshops — the same sinks a WaterPump feeds, so a tanker's delivery
        // actually reaches what consumes it. Range-bounded + per-tick capped so a big network won't churn.
        private void DrainLiquid()
        {
            int avail = store.Count(item);
            if (avail <= 0) return;
            int budget = Mathf.Min(avail, 8);

            _lq.Clear(); _ldist.Clear();
            if (_cells != null)
                foreach (var cell in _cells)
                    foreach (var dir in RailTile.Four)
                    {
                        var s = cell + Belt.Step(dir);
                        var sp = PipeNet.At(s);
                        if (sp != null && !_ldist.ContainsKey(s)) { _ldist[s] = 1; _lq.Enqueue(s); sp.MarkFlow(item, dir); }
                    }
            if (_lq.Count == 0) return;

            int delivered = 0, guard = 0;
            while (_lq.Count > 0 && guard++ < 2048)
            {
                var c = _lq.Dequeue();
                int dc = _ldist[c];
                if (dc > LiquidDrainRange) continue;

                if (budget > 0)
                    foreach (var dir in RailTile.Four)
                    {
                        var nb = c + Belt.Step(dir);
                        if (budget <= 0) break;
                        if (WorldGrid.Storages.TryGetValue(nb, out var st) && st != null && st.accepts == item && st.def != null)
                        {
                            int room = st.def.capacity - st.Store.Total();
                            if (room > 0) { int add = Mathf.Min(budget, room); st.Store.Add(item, add); budget -= add; delivered += add; }
                        }
                        if (budget > 0 && WorldGrid.Workshops.TryGetValue(nb, out var wk) && wk != null)
                        {
                            int room = wk.LiquidInputRoom(item); // shared reserve-floor fair-share (matches the pump + belt gate)
                            if (room > 0) { int add = Mathf.Min(budget, room); wk.InBuffer.Add(item, add); budget -= add; delivered += add; }
                        }
                    }

                int baseD = PipeNet.BoostCells.Contains(c) ? 0 : dc;
                foreach (var dir in RailTile.Four)
                {
                    var nb = c + Belt.Step(dir);
                    var np = PipeNet.At(nb);
                    if (np == null) continue;
                    int nd = baseD + 1;
                    if (!_ldist.TryGetValue(nb, out var old) || nd < old) { _ldist[nb] = nd; _lq.Enqueue(nb); np.MarkFlow(item, dir); }
                }
            }
            if (delivered > 0) store.RemoveUpTo(item, delivered);
        }
    }
}
