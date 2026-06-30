using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>One item riding a belt: its type, its continuous progress p∈[0,1] along the
    /// CURRENT cell's lane (0 = entry edge, 1 = exit edge), and the edge it entered from (so a
    /// corner cell animates its arc correctly). Items are plain data moved between cells by BeltSim.</summary>
    public class BeltItem
    {
        public ItemDefinition def;
        public float p;
        public Belt.Dir entryEdge;
        public BeltItem(ItemDefinition def, float p, Belt.Dir entryEdge) { this.def = def; this.p = p; this.entryEdge = entryEdge; }
    }

    /// <summary>
    /// A directional conveyor segment on a 1-unit grid. Each cell carries a list of items at
    /// CONTINUOUS sub-tile positions; the central <see cref="BeltSim"/> advances them at
    /// speed = 1/interval cells/sec, holding a deterministic minimum gap so items never overlap
    /// and never leave gaps when compressed. Items flow smoothly across cell boundaries (a cell's
    /// exit edge is the next cell's entry edge in world space, so a hand-off is visually seamless).
    /// All building I/O (deposit into storage/workshop/depot/research, pull from a feeder) lives
    /// here, so no other building changes.
    /// </summary>
    public class Belt : MonoBehaviour
    {
        public enum Dir { N, E, S, W }
        public Dir dir;
        public string DisplayName = "Belt"; // tier name (Wooden Belt / Conveyor / …) for hover + selection
        public float interval = 0.5f;

        // --- Spacing / density knob -------------------------------------------------------------
        // MinGap is the minimum spacing between successive items, measured in CELLS along the lane.
        // It is the SINGLE control over belt density AND throughput, because for a saturated belt:
        //     sustained throughput (items/sec) = speed / MinGap = (1/interval) / MinGap
        // MinGap = 1.0 → exactly one item per cell → throughput = 1/interval, i.e. the historic
        // 30/60/120/240-per-min caps and the current visual speed are PRESERVED (the conservative
        // default). Lowering MinGap packs items denser (Factorio look) but multiplies every belt's
        // throughput by 1/MinGap (e.g. 0.26 ≈ 4×) and would need an economy re-tune. One constant.
        public const float MinGap = 1.0f;
        // Hard safety cap on items per cell (spacing is the real limiter; this just bounds the list).
        public static int CellCapacity => Mathf.Max(2, Mathf.FloorToInt(1f / MinGap) + 1);

        // Splitter: a 1→3 belt that sends items EVENLY to its three non-input outputs — forward
        // (`dir`), right (RotateCW(dir)) and left (RotateCCW(dir)); the input is the back edge.
        // `_splitNext` round-robins the preferred output each item; a full/blocked output falls
        // through to the next available one so the splitter never stalls.
        public bool isSplitter;
        private int _splitNext;

        // Merger: an N→1 belt that PULLS from its three input sides round-robin (so every feeder line
        // gets a fair turn into the single output). Normal belts reject a 2nd feeder (so you can't
        // silently merge by pointing a belt onto a line — you must place a Merger).
        public bool isMerger;
        private int _mergeNext; // round-robin input preference (0=back, 1=right, 2=left)

        // Underground belt: an ENTRANCE/EXIT pair. The entrance pulls + carries normally, then — instead of
        // handing off forward — TELEPORTS its matured lead to its paired exit a few tiles ahead, so surface
        // belts/track can cross the GAP between them. The exit is fed only by its entrance (it ignores surface
        // feeders) and conveys forward like a normal belt. Auto-paired on placement (see PairUnderground).
        public bool underground;
        public bool undergroundExit;     // false = entrance (sends down), true = exit (comes up)
        public Belt undergroundPair;     // the linked other end (null = unpaired → inert / shows red)
        private const int MaxTunnel = 4; // an exit may sit up to 4 cells ahead → up to 3 hidden cells bridged

        // Filter belt: conveys ONLY this item type (null = unset, accepts the first item to arrive, like a
        // configurable warehouse). A non-matching item is refused so it backs up / takes another route.
        public bool isFilter;
        public ItemDefinition filterItem;

        // Priority splitter: fills the forward output first, sending to the sides only as OVERFLOW.
        public bool isPriority;

        // Conditional gate: only passes while the line it feeds has room (nearest downstream storage below
        // GateOpenBelow full). Closed → it holds items + stops pulling (acts backed-up), opening when space frees.
        public bool isGate;
        public const float GateOpenBelow = 0.9f; // open while the target storage is < 90% full
        private bool _gateOpen = true;

        private SpriteRenderer _sr;
        private readonly List<BeltItem> _items = new();           // lead-first: _items[0] has the highest p
        private readonly List<SpriteRenderer> _dots = new();      // one sprite per carried item
        private readonly List<GameObject> _portMarkers = new();   // in/out arrows on a splitter/merger
        private Color _baseColor = new Color(0.58f, 0.50f, 0.34f); // tier tint (overlaid by flow-state colour)
        private Vector2Int _cell;

        private bool _connected;    // snapshot: goods on this belt can ultimately reach a real sink
        private bool _blocked;      // the lead reached the exit edge but could not hand off

        private static readonly Dictionary<Vector2Int, Belt> Grid = new();

        public static int Count => Grid.Count;
        public Vector2Int Cell => _cell;
        public int ItemCount => _items.Count;
        // Compatibility shims for the old (item, count) bucket model — read-only views over the list.
        public ItemDefinition item => _items.Count > 0 ? _items[0].def : null; // lead item TYPE
        public int count => _items.Count;

        public static Vector2Int CellOf(Vector3 p) => new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
        public static Belt At(Vector2Int c) => Grid.TryGetValue(c, out var b) ? b : null;
        public static Dir RotateCW(Dir d) => (Dir)(((int)d + 1) % 4);
        public static Dir RotateCCW(Dir d) => (Dir)(((int)d + 3) % 4);
        // The splitter's three output directions, indexed for round-robin: 0=forward, 1=right, 2=left.
        private Dir SplitDir(int i) => i == 1 ? RotateCW(dir) : i == 2 ? RotateCCW(dir) : dir;

        public static Vector2Int Step(Dir d) => d switch
        {
            Dir.N => new Vector2Int(0, 1),
            Dir.E => new Vector2Int(1, 0),
            Dir.S => new Vector2Int(0, -1),
            _ => new Vector2Int(-1, 0),
        };

        public static float Angle(Dir d) => d switch { Dir.N => 0f, Dir.E => -90f, Dir.S => 180f, _ => 90f };

        public static Belt Spawn(Vector2Int cell, Dir dir, float interval = 0.5f, bool isSplitter = false, bool isMerger = false, Color? baseColor = null, string displayName = "Belt",
                                 bool underground = false, bool isFilter = false, bool isPriority = false, bool isGate = false)
        {
            var go = new GameObject(isSplitter ? "Splitter" : isMerger ? "Merger" : displayName);
            go.transform.position = new Vector3(cell.x, cell.y, 0f);
            go.transform.localScale = Vector3.one; // fill the whole cell so a run reads as ONE continuous conveyor (sprites are full-bleed)
            go.transform.rotation = Quaternion.Euler(0f, 0f, Angle(dir));

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = underground ? PlaceholderArt.UndergroundBelt(false)
                      : SpriteDatabase.ForBelt(isSplitter ? "Splitter" : isMerger ? "Merger" : displayName, isSplitter, isMerger); // routed via SpriteDatabase (fallback: conveyor / hex junction)
            sr.sortingOrder = 1;

            go.AddComponent<BoxCollider2D>(); // clickable to demolish

            var b = go.AddComponent<Belt>();
            b.dir = dir;
            b.isSplitter = isSplitter;
            b.isMerger = isMerger;
            b.underground = underground;
            b.isFilter = isFilter;
            b.isPriority = isPriority;
            b.isGate = isGate;
            b.DisplayName = isSplitter ? "Splitter" : isMerger ? "Merger" : displayName;
            b._tier = BeltTier(b.DisplayName);
            b.interval = Mathf.Max(0.05f, interval);
            b._sr = sr;
            if (baseColor.HasValue) b._baseColor = baseColor.Value; // per-tier tint (else the default brown)
            sr.color = b._baseColor;
            b.AddPortMarkers(); // in/out arrows if this is a splitter/merger (no-op for a plain belt)
            if (underground) b.PairUnderground(); // link with an aligned entrance/exit within range
            return b;
        }

        void OnEnable() { _cell = CellOf(transform.position); Grid[_cell] = this; BeltSim.Register(this); }
        void OnDisable()
        {
            BeltSim.Unregister(this);
            if (Grid.TryGetValue(_cell, out var b) && b == this) Grid.Remove(_cell);
            if (undergroundPair != null && undergroundPair.undergroundPair == this) undergroundPair.undergroundPair = null; // don't leave a dangling tunnel
            foreach (var d in _dots) if (d != null) Destroy(d.gameObject);
            _dots.Clear();
            // _portMarkers are children of this GameObject → destroyed with it automatically.
        }

        // Link this freshly-placed underground end with an aligned one within range: prefer an unpaired
        // ENTRANCE behind me (I become its EXIT), else an unpaired EXIT ahead of me (I become its ENTRANCE).
        // Same direction + within MaxTunnel, so the two ends form one straight tunnel. Else: a lone entrance.
        public void PairUnderground()
        {
            var fwd = Step(dir);
            for (int i = 1; i <= MaxTunnel; i++)
            {
                var e = At(_cell - fwd * i);
                if (e != null && e.underground && !e.undergroundExit && e.undergroundPair == null && e.dir == dir)
                { undergroundExit = true; undergroundPair = e; e.undergroundPair = this; return; }
            }
            for (int i = 1; i <= MaxTunnel; i++)
            {
                var x = At(_cell + fwd * i);
                if (x != null && x.underground && x.undergroundExit && x.undergroundPair == null && x.dir == dir)
                { undergroundExit = false; undergroundPair = x; x.undergroundPair = this; return; }
            }
            undergroundExit = false; undergroundPair = null;
        }

        // ---- Compatibility surface (used by belt-to-belt hand-off + building pulls) -------------
        private float TailP => _items.Count > 0 ? _items[_items.Count - 1].p : 1f;
        private bool TailRoom() => _items.Count < CellCapacity && (_items.Count == 0 || _items[_items.Count - 1].p >= MinGap);
        public bool CanAccept(ItemDefinition i) => TailRoom() && (item == null || item == i) && FilterAccepts(i);

        // A filter belt conveys ONLY its chosen item (an unset one adopts the first to arrive). Everything
        // else is refused so it backs up / routes elsewhere. A plain belt accepts anything (no filter).
        private bool FilterAccepts(ItemDefinition d) => !isFilter || filterItem == null || filterItem == d;

        // Take in one item at the tail (entry edge). startP is the desired entry progress (0 for a
        // fresh pull; a small carry-over for a belt→belt hand-off). Clamped so it never overlaps the
        // current tail. Returns false if there is no room. The list stays lead-first (descending p).
        public bool ReceiveItem(ItemDefinition def, Dir fromEdge, float startP)
        {
            if (!TailRoom() || (item != null && item != def)) return false;
            if (isFilter && filterItem != null && def != filterItem) return false; // filter turns away other items
            if (isFilter && filterItem == null) filterItem = def;                  // an unset filter adopts the first item
            float p = startP;
            if (_items.Count > 0) p = Mathf.Min(p, _items[_items.Count - 1].p - MinGap);
            p = Mathf.Clamp01(p);
            _items.Add(new BeltItem(def, p, fromEdge));
            return true;
        }
        // Legacy entry point (no external callers today; kept for safety).
        public void Receive(ItemDefinition i, Dir fromEdge) => ReceiveItem(i, fromEdge, 0f);

        // Whether this belt accepts a hand-off pushed by belt `from`. A MERGER accepts any feeder.
        // A normal belt accepts only ONE upstream: the belt directly behind it (a straight run), or
        // — if nothing is directly behind — the single first side-feeder (one corner). Any 2nd feeder
        // is rejected (it backs up), so you can't silently merge two lines: place a Merger instead.
        public bool AcceptsHandoffFrom(Belt from)
        {
            if (undergroundExit) return false; // an exit is fed ONLY by its paired entrance (direct teleport)
            if (isMerger) return true;
            var behind = _cell + Step(Opposite(dir));
            if (from._cell == behind) return true;   // inline / straight feeder — always allowed
            if (At(behind) != null) return false;     // a straight feeder exists → side feeders blocked
            // No straight feeder: allow the FIRST side-belt that points into me (deterministic), reject others.
            for (int di = 0; di < 4; di++)
            {
                var nc = _cell + Step((Dir)di);
                if (nc == behind) continue;
                var nb = At(nc);
                if (nb != null && nb._cell + Step(nb.dir) == _cell) return nb == from; // it points into me
            }
            return false;
        }

        public static Dir Opposite(Dir d) => (Dir)(((int)d + 2) % 4);
        public static Dir FromTo(Vector2Int a, Vector2Int b)
        {
            var d = b - a;
            if (d.x > 0) return Dir.E;
            if (d.x < 0) return Dir.W;
            if (d.y > 0) return Dir.N;
            return Dir.S;
        }

        public void SetDir(Dir d)
        {
            dir = d;
            transform.rotation = Quaternion.Euler(0f, 0f, Angle(d));
        }

        /// <summary>Upgrade this belt to a faster tier in place (new interval + colour) without
        /// rebuilding — keeps its direction and any carried items.</summary>
        public void SetTier(float newInterval, Color newColor, string displayName = null)
        {
            interval = Mathf.Max(0.05f, newInterval);
            _baseColor = newColor; // the per-frame flow-state colouring in Update uses this as the base
            if (displayName != null && !isSplitter && !isMerger)
            {
                DisplayName = displayName; gameObject.name = displayName;
                _tier = BeltTier(displayName); _lastBeltKey = -1; // re-pick the per-tier sprite next Update
            }
        }

        /// <summary>Overlay-convert a plain belt into a splitter or merger in place (keeps its
        /// direction + any carried items).</summary>
        public void ConvertTo(bool splitter, bool merger, bool priority = false, bool filter = false, bool gate = false, string displayName = null)
        {
            isSplitter = splitter;
            isMerger = merger;
            isPriority = priority;
            isFilter = filter;
            isGate = gate;
            DisplayName = displayName ?? (splitter ? "Splitter" : merger ? "Merger" : DisplayName);
            gameObject.name = DisplayName;
            if (_sr != null) _sr.sprite = SpriteDatabase.ForBelt(DisplayName, splitter, merger);
            AddPortMarkers();
        }

        /// <summary>Set the belt's base (flow-OK) tint — used when converting a plain belt to a variant.</summary>
        public void SetBaseColor(Color c) { _baseColor = c; }

        // Build the in/out port arrows for a splitter/merger so which side is IN vs OUT is obvious.
        // Markers are parented to this (rotated) belt and use LOCAL dirs — N = forward (= world dir),
        // E = right (= RotateCW(dir)), S = back, W = left — so they land on the correct world edges
        // AND stay correct if the belt is later re-oriented (SetDir rotates the parent + its markers).
        private void AddPortMarkers()
        {
            foreach (var m in _portMarkers) if (m != null) Destroy(m);
            _portMarkers.Clear();
            if (isSplitter)
            {
                _portMarkers.Add(Ports.MakeOutputArrow(transform, Dir.N).gameObject); // forward out
                _portMarkers.Add(Ports.MakeOutputArrow(transform, Dir.E).gameObject); // right out
                _portMarkers.Add(Ports.MakeOutputArrow(transform, Dir.W).gameObject); // left out
                _portMarkers.Add(Ports.MakeInputNotch(transform, Dir.S).gameObject);  // back in
            }
            else if (isMerger)
            {
                _portMarkers.Add(Ports.MakeOutputArrow(transform, Dir.N).gameObject); // forward out
                _portMarkers.Add(Ports.MakeInputNotch(transform, Dir.S).gameObject);  // back in
                _portMarkers.Add(Ports.MakeInputNotch(transform, Dir.E).gameObject);  // right in
                _portMarkers.Add(Ports.MakeInputNotch(transform, Dir.W).gameObject);  // left in
            }
        }

        public static bool IsStorageCell(Vector2Int c) => WorldGrid.Storages.ContainsKey(c);
        public static bool IsSourceCell(Vector2Int c) =>
            WorldGrid.Collectors.ContainsKey(c) || WorldGrid.Workshops.ContainsKey(c);

        // =======================================================================================
        //  CENTRAL SIMULATION — called by BeltSim each fixed step, in a stable cell-sorted order.
        // =======================================================================================

        // PASS 1: refresh sink-reachability (gates pulls + drives the colour state). Items are then
        // advanced (PASS 2) reading the LIVE downstream tail; BeltSim processes belts DOWNSTREAM-FIRST
        // (nearest-to-sink first, see DistToSink), so an upstream cell reads its downstream's already-
        // advanced position this step → exact throughput. No-overlap holds in ANY order, because the
        // lead is capped at the downstream tail (which only moves forward), so ordering affects only
        // throughput, never safety.
        public void SimSnapshot()
        {
            _connected = HasForwardTarget();
            if (isGate) _gateOpen = GateConditionOpen();
        }

        // A conditional gate is OPEN while the line it feeds still has room: it walks forward to the nearest
        // STORAGE and opens only while that store is below GateOpenBelow full. No storage ahead (it feeds a
        // workshop/depot, which self-regulate via their own buffers) → always open.
        private bool GateConditionOpen()
        {
            var cell = _cell; var d = dir;
            for (int i = 0; i < 256; i++)
            {
                var ahead = cell + Step(d);
                if (WorldGrid.Storages.TryGetValue(ahead, out var s) && s != null && s.def != null)
                    return s.def.capacity <= 0 || (float)s.Store.Total() / s.def.capacity < GateOpenBelow;
                if (WorldGrid.Workshops.ContainsKey(ahead) || WorldGrid.Depots.ContainsKey(ahead) || WorldGrid.Research.ContainsKey(ahead)) return true;
                var nb = At(ahead);
                if (nb == null) return true;
                cell = ahead; d = nb.dir;
            }
            return true;
        }

        // PASS 2: advance every item along this cell's lane. The lead is capped at the exit edge
        // (p = 1) AND held a MinGap behind the next cell's tail (cross-tile spacing); followers are
        // held a MinGap behind the item ahead. Pure intra-cell writes + neighbour SNAPSHOT reads.
        public void SimAdvance(float h)
        {
            if (_items.Count == 0) return;
            float adv = (1f / Mathf.Max(0.05f, interval)) * h; // speed (cells/sec) × dt
            var lead = _items[0];
            float limit = Mathf.Min(1f, LeadHeadLimit());
            float np = lead.p + adv;
            lead.p = np < limit ? np : (limit > lead.p ? limit : lead.p); // never moves backward
            for (int i = 1; i < _items.Count; i++)
            {
                float maxP = _items[i - 1].p - MinGap;
                float cand = _items[i].p + adv;
                _items[i].p = cand < maxP ? cand : (maxP > _items[i].p ? maxP : _items[i].p);
                if (_items[i].p < 0f) _items[i].p = 0f;
            }
        }

        // The furthest p (in THIS cell's coordinates) the lead may reach this step. A downstream
        // belt that can take our item lets the lead reach 1+tail−MinGap (so it trails the next
        // cell's tail by MinGap across the shared boundary); a sink/dead-end lets it reach the exit
        // edge (1) and either deposit or pin there. A splitter takes the best of its three outputs.
        private float LeadHeadLimit()
        {
            if (underground && !undergroundExit) return 1f; // roll to the tunnel mouth; SimHandoff teleports to the exit
            if (isGate && !_gateOpen) return 1f;            // gate shut → pin at the exit edge so the line backs up
            if (isSplitter) return Mathf.Max(HeadLimitDir(dir), Mathf.Max(HeadLimitDir(RotateCW(dir)), HeadLimitDir(RotateCCW(dir))));
            return HeadLimitDir(dir);
        }
        private float HeadLimitDir(Dir d)
        {
            var ahead = _cell + Step(d);
            var nb = At(ahead);
            if (nb != null)
            {
                bool typeOk = item == null || nb.item == null || nb.item == item;
                if (typeOk && nb.AcceptsHandoffFrom(this))
                    // Empty downstream → roll to the exit edge. Occupied → trail its LIVE tail by
                    // MinGap across the shared boundary (1 + tail − MinGap), so no two items ever
                    // share a world point. Live read + downstream-first order ⇒ exact + overlap-free.
                    return nb.ItemCount == 0 ? 1f : 1f + nb.TailP - MinGap;
                return 1f; // can't hand off here → roll to the exit edge and pin (blocked)
            }
            return 1f; // building sink or dead-end → roll to the exit edge
        }

        // PASS 3: a matured lead (at the exit edge) is pushed into the next belt cell or a building.
        public void SimHandoff()
        {
            _blocked = false;
            if (_items.Count == 0) return;
            var lead = _items[0];
            if (lead.p < 1f - 1e-4f) return; // not at the exit edge yet

            if (isGate && !_gateOpen) { _blocked = true; return; } // shut → hold the matured lead (backs up)

            bool moved = false;
            if (underground && !undergroundExit)
            {
                // Underground entrance: TELEPORT the matured lead to the paired exit (no surface hand-off);
                // an unpaired entrance (pair == null) just pins here (shows red/backed-up).
                moved = undergroundPair != null && undergroundPair.ReceiveItem(lead.def, Opposite(undergroundPair.dir), 0f);
            }
            else if (isSplitter)
            {
                // 1→3 split: a PRIORITY splitter always fills forward→right→left (sides take overflow only);
                // a plain splitter round-robins the preferred output for an even share. Either way a full/
                // blocked lane falls through to the next so the splitter never stalls.
                for (int k = 0; k < 3 && !moved; k++)
                {
                    int idx = isPriority ? k : (_splitNext + k) % 3;
                    if (TryDepositTo(SplitDir(idx), lead.def)) { moved = true; if (!isPriority) _splitNext = (idx + 1) % 3; }
                }
            }
            else
            {
                moved = TryDepositTo(dir, lead.def);
            }

            if (moved) _items.RemoveAt(0);
            else
            {
                // Not truly "blocked" if a Merger ahead is going to PULL from us this step (it just
                // hasn't yet) — only show backed-up when the forward target genuinely has no room.
                var fwd = At(_cell + Step(dir));
                _blocked = !(fwd != null && fwd.isMerger && fwd.CanAccept(lead.def));
            }
        }

        // PASS 4: pull a new item from an adjacent feeder building into a cleared entry. Gated by
        // connectivity (don't pull onto a belt that leads nowhere) and tail room (deterministic
        // spacing at the entry). One item per step. A MERGER instead round-robins its three input
        // sides, taking from a belt OR a building on each side (so neither feed starves the other).
        public void SimPull()
        {
            if (undergroundExit) return;      // an exit is fed only by its paired entrance's teleport
            if (isGate && !_gateOpen) return; // shut → don't draw new items onto the line
            if (!_connected || !TailRoom()) return;
            if (isMerger) { MergerPull(); return; }
            for (int di = 0; di < 4; di++) if (PullBuildingFrom((Dir)di)) return; // a plain belt/splitter pulls from a feeder building on any side
        }

        // Pull ONE matured output unit from a feeder BUILDING on `side` (its output must FACE us, i.e. its
        // OutputSide == Opposite(side)). Liquids never ride belts. Shared by plain belts (scan all 4 sides)
        // and mergers (their 3 input sides). Returns true if one unit was taken.
        private bool PullBuildingFrom(Dir side)
        {
            var c = _cell + Step(side);
            Dir need = Opposite(side);
            if (WorldGrid.Collectors.TryGetValue(c, out var p) && p != null && p.produces != null && !p.produces.isLiquid
                && p.OutputSide == need && FilterAccepts(p.produces) && (item == null || item == p.produces) && p.Buffer.Count(p.produces) > 0)
            { if (p.Buffer.RemoveUpTo(p.produces, 1) > 0) { ReceiveItem(p.produces, side, 0f); return true; } }

            if (WorldGrid.Workshops.TryGetValue(c, out var w) && w != null && w.output != null && !w.output.isLiquid
                && w.OutputSide == need && FilterAccepts(w.output) && (item == null || item == w.output) && w.Buffer.Count(w.output) > 0)
            { if (w.Buffer.RemoveUpTo(w.output, 1) > 0) { ReceiveItem(w.output, side, 0f); return true; } }

            // Storages have an OUTPUT side too — belt FROM a warehouse to a workshop (e.g. warehouse → sawmill).
            if (WorldGrid.Storages.TryGetValue(c, out var st) && st != null && st.accepts != null && !st.accepts.isLiquid
                && st.OutputSide == need && FilterAccepts(st.accepts) && (item == null || item == st.accepts) && st.Store.Count(st.accepts) > 0)
            { if (st.Store.RemoveUpTo(st.accepts, 1) > 0) { ReceiveItem(st.accepts, side, 0f); return true; } }

            if (WorldGrid.Depots.TryGetValue(c, out var dp) && dp != null && dp.item != null && !dp.item.isLiquid
                && dp.IsOutputPull(c, side) && FilterAccepts(dp.item) && (item == null || item == dp.item) && dp.store.Count(dp.item) > 0)
            { if (dp.store.RemoveUpTo(dp.item, 1) > 0) { ReceiveItem(dp.item, side, 0f); return true; } }

            return false;
        }

        // MERGER input: round-robin across the three non-output sides — back (Opposite dir), right
        // (RotateCW), left (RotateCCW). On each side it takes from whichever feeder is ready THIS step:
        // a belt that POINTS INTO us, or a building whose output faces us. So a merger fairly combines
        // belt lines AND draws directly from an adjacent collector/workshop/storage/depot — a belt feed
        // never starves the building feed (the bug where a merger on a collector stalled the line).
        private Dir MergeInputDir(int idx) => idx == 1 ? RotateCW(dir) : idx == 2 ? RotateCCW(dir) : Opposite(dir);
        private bool MergerPull()
        {
            for (int k = 0; k < 3; k++)
            {
                int idx = (_mergeNext + k) % 3;
                Dir side = MergeInputDir(idx);
                var f = At(_cell + Step(side));
                if (f != null && f != this && f._cell + Step(f.dir) == _cell && f._items.Count > 0)
                {
                    var lead = f._items[0];
                    if (lead.p >= 1f - 1e-3f && (item == null || item == lead.def) && ReceiveItem(lead.def, side, 0f))
                    { f._items.RemoveAt(0); _mergeNext = (idx + 1) % 3; return true; }
                }
                if (PullBuildingFrom(side)) { _mergeNext = (idx + 1) % 3; return true; }
            }
            return false;
        }

        // Move ONE item out in direction d — into a belt (carry-over hand-off), or a building sink
        // on its input side. Returns true if it left. Shared by belts AND splitters.
        private bool TryDepositTo(Dir d, ItemDefinition def)
        {
            var ahead = _cell + Step(d);
            var nb = At(ahead);
            if (nb != null)
            {
                // A Merger PULLS its inputs round-robin (see MergerPullFromBelts) so every feeder line
                // gets a fair turn — never push into one here. (The old push let whichever feeder came
                // first in cell order win the merger's single slot every step, starving the others —
                // the "items get stuck and don't go in the sides" bug.)
                if (nb.isMerger) return false;
                // The item enters the next belt from the edge facing us (Opposite our dir). p=1 on
                // this cell == p=0 on the next in world space, so entry at ~0 is seamless. A normal
                // belt only accepts ONE feeder (straight or a single corner).
                if (nb.CanAccept(def) && nb.AcceptsHandoffFrom(this)) return nb.ReceiveItem(def, Opposite(d), 0f);
                return false;
            }
            return TryDepositToBuilding(d, def);
        }

        // Deposit the item into a storage / workshop / depot / research sink directly ahead, on a
        // valid input side. Returns true if accepted. (Lifted verbatim from the old TryDepositTo so
        // every sink/side/auto-register rule is preserved.)
        private bool TryDepositToBuilding(Dir d, ItemDefinition def)
        {
            var ahead = _cell + Step(d);

            // Storage, on its INPUT side (d == OutputSide). A configurable warehouse with no type yet
            // AUTO-REGISTERS the first item belted in (so goods aren't lost into an "unset" store).
            if (WorldGrid.Storages.TryGetValue(ahead, out var s) && s != null && d == s.OutputSide
                && (s.accepts == def || (s.accepts == null && s.configurable && s.CanAdopt(def)))) // Warehouse won't adopt raws; the raw Stockpile will
            {
                if (s.def != null && s.Store.Total() >= s.def.capacity) return false;
                if (s.accepts == null) s.accepts = def; // adopt the first delivered item's type
                s.Store.Add(def, 1); return true;
            }

            // Workshop input buffer, on a valid input side, if it can take this item
            // (fair-share per input → no mixed-buffer deadlock; see CanAcceptBeltInput).
            if (WorkshopAt(ahead) is WorkshopBuilding w && AcceptsInputSide(w, d) && w.CanAcceptBeltInput(def))
            {
                w.InBuffer.Add(def, 1); return true;
            }

            // Station, on its INPUT (south) edge, if it handles this item.
            if (WorldGrid.Depots.TryGetValue(ahead, out var dp) && dp != null && dp.IsInputDeposit(ahead, d) && dp.item == def)
            {
                if (dp.def != null && dp.store.Total() >= dp.def.capacity) return false;
                dp.store.Add(def, 1); return true;
            }

            // Research Lodge, on its INPUT side, if it's a research item.
            if (WorldGrid.Research.TryGetValue(ahead, out var rb) && rb != null && d == rb.OutputSide && rb.Accepts(def))
            {
                if (rb.InBuffer.Total() >= rb.InBuffer.capacity) return false;
                rb.InBuffer.Add(def, 1); return true;
            }

            // Power generator FUEL input, on its input edge — belt-feed charcoal/wood so a generator runs
            // without hand-feeding from the carried pile.
            if (WorldGrid.Generators.TryGetValue(ahead, out var gen) && gen != null && gen.fuel == def
                && d == Opposite(gen.InputSide))
            {
                if (gen.Buffer == null || gen.Buffer.Total() >= gen.Buffer.capacity) return false;
                gen.Buffer.Add(def, 1); return true;
            }
            return false;
        }

        private static WorkshopBuilding WorkshopAt(Vector2Int c)
            => WorldGrid.Workshops.TryGetValue(c, out var w) ? w : null;

        // A belt at this cell moving `d` sits on the workshop's `Opposite(d)` side. Multi-input
        // workshops accept inputs on ANY non-output side; single-input workshops keep the one input
        // side (opposite the output).
        private static bool AcceptsInputSide(WorkshopBuilding w, Dir d)
            => (w.inputs != null && w.inputs.Count > 1) ? Opposite(d) != w.OutputSide : d == w.OutputSide;

        // ---- Connectivity (gates pulls + drives the colour state). Unchanged from before; the
        //      event-driven replacement of this per-step walk is a later phase. ----

        // Connected only if goods can actually LEAVE: a sink directly ahead, or a belt chain ahead
        // that ultimately reaches a sink. Stops belts conveying into nothing.
        private bool HasForwardTarget()
        {
            // An underground entrance is "connected" only through its paired exit (its surface-forward cell is
            // the covered gap, not its real next hop).
            if (underground && !undergroundExit) return undergroundPair != null && undergroundPair.HasForwardTarget();
            if (isSplitter) return OutputConnected(dir) || OutputConnected(RotateCW(dir)) || OutputConnected(RotateCCW(dir));
            return OutputConnected(dir);
        }

        private bool OutputConnected(Dir d)
        {
            var ahead = _cell + Step(d);
            if (WorldGrid.Storages.TryGetValue(ahead, out var s) && s != null && d == s.OutputSide
                && (item == null || s.accepts == item || (s.accepts == null && s.configurable))) return true;
            if (WorldGrid.Workshops.TryGetValue(ahead, out var w) && w != null && AcceptsInputSide(w, d)
                && (item == null || w.WantsInput(item))) return true;
            if (WorldGrid.Depots.TryGetValue(ahead, out var dp) && dp != null && dp.IsInputDeposit(ahead, d)
                && (item == null || dp.item == null || dp.item == item)) return true; // deliver on the station's south edge
            if (WorldGrid.Research.TryGetValue(ahead, out var rb) && rb != null && d == rb.OutputSide
                && (item == null || rb.Accepts(item))) return true;
            if (WorldGrid.Generators.TryGetValue(ahead, out var gen) && gen != null && d == Opposite(gen.InputSide)
                && (item == null || gen.fuel == item)) return true; // fuel feeds a generator
            if (At(ahead) != null) return LeadsToSink(_cell, d);
            return false;
        }

        // Steps forward to the nearest sink, for BeltSim's downstream-first ordering (lower = nearer
        // a sink = processed earlier, so an upstream cell reads its downstream's fresh position this
        // step → exact throughput). Dead-ends/loops return a large value (processed last; harmless —
        // ordering never affects correctness, only throughput). A splitter follows its primary output.
        public int DistToSink()
        {
            var cell = _cell; var d = dir;
            // An underground entrance continues from its paired exit (the gap between isn't its lane).
            if (underground && !undergroundExit && undergroundPair != null) { cell = undergroundPair._cell; d = undergroundPair.dir; }
            for (int i = 0; i < 300; i++)
            {
                var ahead = cell + Step(d);
                if (WorldGrid.Storages.ContainsKey(ahead) || WorldGrid.Workshops.ContainsKey(ahead)
                    || WorldGrid.Depots.ContainsKey(ahead) || WorldGrid.Research.ContainsKey(ahead)) return i;
                var nb = At(ahead);
                if (nb == null) return 1_000_000;
                cell = ahead; d = nb.dir;
            }
            return 1_000_000;
        }

        // Walk the belt chain forward; true if it reaches any sink. SPLITTER-AWARE: at a splitter it branches into
        // all THREE outputs (forward + both sides), so a line that reaches a sink via a splitter's SIDE output is
        // correctly seen as connected — fixing splitter→splitter (and splitter→side→sink) reading RED while it
        // actually flows. A visited-splitter set keeps junction cycles loop-safe. Also counts a Generator's fuel
        // intake as a sink, so a belt CHAIN feeding a generator no longer reads red.
        private static readonly System.Collections.Generic.HashSet<Vector2Int> _sinkVisited = new();
        private static bool LeadsToSink(Vector2Int cell, Dir dir)
        {
            _sinkVisited.Clear();
            return LeadsToSinkRec(cell, dir);
        }
        private static bool LeadsToSinkRec(Vector2Int cell, Dir dir)
        {
            for (int i = 0; i < 256; i++)
            {
                var ahead = cell + Step(dir);
                if (WorldGrid.Storages.ContainsKey(ahead) || WorldGrid.Workshops.ContainsKey(ahead)
                    || WorldGrid.Depots.ContainsKey(ahead) || WorldGrid.Research.ContainsKey(ahead)
                    || WorldGrid.Generators.ContainsKey(ahead)) return true;
                var nb = At(ahead);
                if (nb == null) return false;
                if (nb.isSplitter)
                {
                    if (!_sinkVisited.Add(ahead)) return false; // this splitter is already on the current walk → don't loop
                    return LeadsToSinkRec(ahead, nb.dir)
                        || LeadsToSinkRec(ahead, RotateCW(nb.dir))
                        || LeadsToSinkRec(ahead, RotateCCW(nb.dir));
                }
                cell = ahead; dir = nb.dir;
            }
            return false;
        }

        // =======================================================================================
        //  RENDERING — runs every frame for smoothness (the sim is fixed-step; the view is not).
        // =======================================================================================
        void Update()
        {
            // Belt colour reads the bottleneck at a glance:
            //   red    = dead end (no sink ahead), yellow = backed up (lead can't move on),
            //   base   = flowing or empty (an empty connected belt = an upstream/supply issue).
            if (_sr != null)
                _sr.color = !_connected ? new Color(0.62f, 0.24f, 0.24f)      // red — dead end
                          : _blocked   ? new Color(0.85f, 0.66f, 0.18f)        // yellow — backed up downstream
                          : _baseColor;                                         // brown — ok

            // Underground ends use a fixed entrance/exit sprite (role decided at pairing); plain belts pick
            // their sprite from TIER + SHAPE so a run reads per-age and corners visibly connect; splitters/
            // mergers keep their junction sprite.
            if (underground)
            {
                int key = undergroundExit ? 901 : 900;
                if (key != _lastBeltKey && _sr != null) { _lastBeltKey = key; _sr.sprite = PlaceholderArt.UndergroundBelt(undergroundExit); }
            }
            else if (!isSplitter && !isMerger) UpdateBeltShape();
            UpdateDots();
        }

        // ---- Per-tier + corner sprite selection ------------------------------------------------
        private int _tier = 1;        // 0 Wooden(rollers) 1 Conveyor(chevron) 2 Geared 3 Steel
        private int _lastBeltKey = -1; // tier*3+shape the current sprite is drawn for
        private static int BeltTier(string name)
        {
            if (string.IsNullOrEmpty(name)) return 1;
            if (name.StartsWith("Wood")) return 0;
            if (name.StartsWith("Geared")) return 2;
            if (name.StartsWith("Steel")) return 3;
            return 1; // Conveyor / default
        }

        private void UpdateBeltShape()
        {
            if (_sr == null) return;
            int shape = BeltShape();
            // The surface SCROLLS so a belt's travel direction + flow read at a glance even when empty; faster
            // tiers scroll faster. Only the per-frame sprite swap happens when the animation frame actually ticks.
            int frame = (int)(Time.time * (5f + _tier * 3f)) & (PlaceholderArt.BeltFrames - 1);
            int key = (_tier * 3 + shape) * PlaceholderArt.BeltFrames + frame;
            if (key != _lastBeltKey) { _lastBeltKey = key; _sr.sprite = PlaceholderArt.BeltSprite(_tier, shape, frame); }
        }

        // 0 = straight (fed from behind / by a building), 1 = corner from the RIGHT side, 2 = from the LEFT.
        private int BeltShape()
        {
            if (FeedsInto(Opposite(dir))) return 0;       // straight feeder behind takes priority
            if (FeedsInto(RotateCW(dir))) return 1;        // a belt curves IN from our right
            if (FeedsInto(RotateCCW(dir))) return 2;       // …or our left
            return 0;
        }

        // True if the belt on `side` has its output pointing INTO this cell (so it feeds us from there).
        private bool FeedsInto(Dir side)
        {
            var nb = At(_cell + Step(side));
            return nb != null && nb._cell + Step(nb.dir) == _cell;
        }

        // Draw one sprite per carried item at its own continuous position along the lane. The path
        // is a quadratic Bézier with its control point at the cell centre → a straight line on a
        // straight belt and a smooth ARC through a corner (entryEdge is per-item, so each item arcs
        // correctly). 0.5 offsets make one belt's exit == the next's entry (seamless hand-off).
        // Render each LOGICAL item as a short 2-dot CLUSTER (chunkier sprites) so a saturated belt reads as
        // PACKED and bottlenecks pop. This is PURELY VISUAL: it never touches _items, MinGap, the handoff or
        // throughput — the sim is byte-identical, so nothing can get stuck and the economy balance is unchanged.
        // (MinGap stays 1.0; LOWERING it would pack real items but multiply every belt's throughput by 1/MinGap.)
        private const int DotsPerItem = 2;
        private const float DotScale = 0.40f;   // chunkier than the old single 0.26 dot
        private const float ClusterGap = 0.34f; // sub-dot spacing along the lane
        private void UpdateDots()
        {
            int n = _items.Count;
            int need = n * DotsPerItem;
            while (_dots.Count < need)
            {
                var go = new GameObject("BeltItem");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 2;
                go.transform.localScale = Vector3.one * DotScale;
                _dots.Add(sr);
            }
            for (int i = 0; i < _dots.Count; i++)
            {
                var d = _dots[i];
                if (d == null) continue;
                int idx = i / DotsPerItem, sub = i % DotsPerItem;
                bool on = idx < n;
                d.enabled = on;
                if (!on) continue;
                var it = _items[idx];
                d.sprite = SpriteDatabase.ForItem(it.def); // routed via SpriteDatabase (fallback Circle)
                d.color = it.def.color;
                Vector3 inE = new Vector3(Step(it.entryEdge).x, Step(it.entryEdge).y, 0f) * 0.5f;
                Vector3 outE = new Vector3(Step(dir).x, Step(dir).y, 0f) * 0.5f;
                float sp = Mathf.Clamp(it.p - sub * ClusterGap, 0f, 1f); // the item + a trailing companion, kept on the lane
                d.transform.position = transform.position + PathPoint(sp, inE, outE);
            }
        }

        // Quadratic Bézier with the control point at the cell centre (the origin): straight when
        // inE == -outE (a straight belt), a smooth quarter-arc when inE ⟂ outE (a corner).
        private static Vector3 PathPoint(float t, Vector3 inE, Vector3 outE)
            => (1f - t) * (1f - t) * inE + t * t * outE;
    }
}
