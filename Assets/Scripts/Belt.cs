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

        // Merger: an N→1 belt that ACCEPTS hand-offs from any feeder (used to deliberately combine
        // two belt lines). Normal belts reject a 2nd feeder (so you can't silently merge by pointing
        // a belt onto a line — you must place a Merger; see AcceptsHandoffFrom).
        public bool isMerger;

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

        public static Belt Spawn(Vector2Int cell, Dir dir, float interval = 0.5f, bool isSplitter = false, bool isMerger = false, Color? baseColor = null, string displayName = "Belt")
        {
            var go = new GameObject(isSplitter ? "Splitter" : isMerger ? "Merger" : displayName);
            go.transform.position = new Vector3(cell.x, cell.y, 0f);
            go.transform.localScale = Vector3.one * 0.8f;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Angle(dir));

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = isSplitter || isMerger ? PlaceholderArt.Hexagon() : PlaceholderArt.Conveyor(); // splitter/merger = hex; belt = conveyor tile
            sr.sortingOrder = 1;

            go.AddComponent<BoxCollider2D>(); // clickable to demolish

            var b = go.AddComponent<Belt>();
            b.dir = dir;
            b.isSplitter = isSplitter;
            b.isMerger = isMerger;
            b.DisplayName = isSplitter ? "Splitter" : isMerger ? "Merger" : displayName;
            b.interval = Mathf.Max(0.05f, interval);
            b._sr = sr;
            if (baseColor.HasValue) b._baseColor = baseColor.Value; // per-tier tint (else the default brown)
            sr.color = b._baseColor;
            b.AddPortMarkers(); // in/out arrows if this is a splitter/merger (no-op for a plain belt)
            return b;
        }

        void OnEnable() { _cell = CellOf(transform.position); Grid[_cell] = this; BeltSim.Register(this); }
        void OnDisable()
        {
            BeltSim.Unregister(this);
            if (Grid.TryGetValue(_cell, out var b) && b == this) Grid.Remove(_cell);
            foreach (var d in _dots) if (d != null) Destroy(d.gameObject);
            _dots.Clear();
            // _portMarkers are children of this GameObject → destroyed with it automatically.
        }

        // ---- Compatibility surface (used by belt-to-belt hand-off + building pulls) -------------
        private float TailP => _items.Count > 0 ? _items[_items.Count - 1].p : 1f;
        private bool TailRoom() => _items.Count < CellCapacity && (_items.Count == 0 || _items[_items.Count - 1].p >= MinGap);
        public bool CanAccept(ItemDefinition i) => TailRoom() && (item == null || item == i);

        // Take in one item at the tail (entry edge). startP is the desired entry progress (0 for a
        // fresh pull; a small carry-over for a belt→belt hand-off). Clamped so it never overlaps the
        // current tail. Returns false if there is no room. The list stays lead-first (descending p).
        public bool ReceiveItem(ItemDefinition def, Dir fromEdge, float startP)
        {
            if (!TailRoom() || (item != null && item != def)) return false;
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
            if (displayName != null && !isSplitter && !isMerger) { DisplayName = displayName; gameObject.name = displayName; }
        }

        /// <summary>Overlay-convert a plain belt into a splitter or merger in place (keeps its
        /// direction + any carried items).</summary>
        public void ConvertTo(bool splitter, bool merger)
        {
            isSplitter = splitter;
            isMerger = merger;
            if (_sr != null) _sr.sprite = (splitter || merger) ? PlaceholderArt.Hexagon() : PlaceholderArt.Conveyor();
            DisplayName = splitter ? "Splitter" : merger ? "Merger" : DisplayName;
            gameObject.name = DisplayName;
            AddPortMarkers();
        }

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

            bool moved = false;
            if (isSplitter)
            {
                // 1→3 even split: try the round-robin PREFERRED output first, then fall through to
                // the next outputs in order so a full/blocked lane never stalls the splitter. On a
                // successful send, advance the preference to the NEXT output for even distribution.
                for (int k = 0; k < 3 && !moved; k++)
                {
                    int idx = (_splitNext + k) % 3;
                    if (TryDepositTo(SplitDir(idx), lead.def)) { moved = true; _splitNext = (idx + 1) % 3; }
                }
            }
            else
            {
                moved = TryDepositTo(dir, lead.def);
            }

            if (moved) _items.RemoveAt(0);
            else _blocked = true; // downstream full / dead-end → lead holds at the exit edge
        }

        // PASS 4: pull a new item from an adjacent feeder building into a cleared entry. Gated by
        // connectivity (don't pull onto a belt that leads nowhere) and tail room (deterministic
        // spacing at the entry). One item per step, mirroring the old PullFromNeighbour scan/rules.
        public void SimPull()
        {
            if (!_connected || !TailRoom()) return;
            for (int di = 0; di < 4; di++)
            {
                var c = _cell + Step((Dir)di);

                // Output ports: a belt only pulls a building's output from its OUTPUT side (the
                // building's output must face this belt — Opposite of our scan dir). Liquids never
                // ride belts — they move via pipes / carrying.
                if (WorldGrid.Collectors.TryGetValue(c, out var p) && p != null && p.produces != null && !p.produces.isLiquid
                    && p.OutputSide == Opposite((Dir)di)
                    && (item == null || item == p.produces) && p.Buffer.Count(p.produces) > 0)
                {
                    if (p.Buffer.RemoveUpTo(p.produces, 1) > 0) { ReceiveItem(p.produces, (Dir)di, 0f); return; }
                }

                if (WorldGrid.Workshops.TryGetValue(c, out var w) && w != null && w.output != null && !w.output.isLiquid
                    && w.OutputSide == Opposite((Dir)di)
                    && (item == null || item == w.output) && w.Buffer.Count(w.output) > 0)
                {
                    if (w.Buffer.RemoveUpTo(w.output, 1) > 0) { ReceiveItem(w.output, (Dir)di, 0f); return; }
                }

                // Storages have an OUTPUT side too — a belt on it pulls the stored item, so you can
                // belt FROM a warehouse to a workshop (e.g. warehouse → sawmill).
                if (WorldGrid.Storages.TryGetValue(c, out var st) && st != null && st.accepts != null && !st.accepts.isLiquid
                    && st.OutputSide == Opposite((Dir)di)
                    && (item == null || item == st.accepts) && st.Store.Count(st.accepts) > 0)
                {
                    if (st.Store.RemoveUpTo(st.accepts, 1) > 0) { ReceiveItem(st.accepts, (Dir)di, 0f); return; }
                }

                if (WorldGrid.Depots.TryGetValue(c, out var dp) && dp != null && dp.item != null && !dp.item.isLiquid
                    && (item == null || item == dp.item) && dp.store.Count(dp.item) > 0)
                {
                    if (dp.store.RemoveUpTo(dp.item, 1) > 0) { ReceiveItem(dp.item, (Dir)di, 0f); return; }
                }
            }
        }

        // Move ONE item out in direction d — into a belt (carry-over hand-off), or a building sink
        // on its input side. Returns true if it left. Shared by belts AND splitters.
        private bool TryDepositTo(Dir d, ItemDefinition def)
        {
            var ahead = _cell + Step(d);
            var nb = At(ahead);
            if (nb != null)
            {
                // The item enters the next belt from the edge facing us (Opposite our dir). p=1 on
                // this cell == p=0 on the next in world space, so entry at ~0 is seamless. A normal
                // belt only accepts ONE feeder (straight or a single corner); a Merger accepts any.
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
                && (s.accepts == def || (s.accepts == null && s.configurable)))
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

            // Depot that handles this item (route endpoint).
            if (WorldGrid.Depots.TryGetValue(ahead, out var dp) && dp != null && dp.item == def)
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
            if (WorldGrid.Depots.TryGetValue(ahead, out var dp) && dp != null
                && (item == null || dp.item == null || dp.item == item)) return true; // depots omnidirectional
            if (WorldGrid.Research.TryGetValue(ahead, out var rb) && rb != null && d == rb.OutputSide
                && (item == null || rb.Accepts(item))) return true;
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

        // Walk the belt chain forward; true if it reaches any storage/workshop/depot/research.
        // Bounded (no visited set) so a closed loop simply returns false after the cap.
        private static bool LeadsToSink(Vector2Int cell, Dir dir)
        {
            for (int i = 0; i < 256; i++)
            {
                var ahead = cell + Step(dir);
                if (WorldGrid.Storages.ContainsKey(ahead) || WorldGrid.Workshops.ContainsKey(ahead)
                    || WorldGrid.Depots.ContainsKey(ahead) || WorldGrid.Research.ContainsKey(ahead)) return true;
                var nb = At(ahead);
                if (nb == null) return false;
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

            UpdateDots();
        }

        // Draw one sprite per carried item at its own continuous position along the lane. The path
        // is a quadratic Bézier with its control point at the cell centre → a straight line on a
        // straight belt and a smooth ARC through a corner (entryEdge is per-item, so each item arcs
        // correctly). 0.5 offsets make one belt's exit == the next's entry (seamless hand-off).
        private void UpdateDots()
        {
            int n = _items.Count;
            while (_dots.Count < n)
            {
                var go = new GameObject("BeltItem");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 2;
                go.transform.localScale = Vector3.one * 0.26f;
                _dots.Add(sr);
            }
            for (int i = 0; i < _dots.Count; i++)
            {
                var d = _dots[i];
                if (d == null) continue;
                bool on = i < n;
                d.enabled = on;
                if (!on) continue;
                var it = _items[i];
                d.sprite = it.def.icon != null ? it.def.icon : PlaceholderArt.Circle();
                d.color = it.def.color;
                Vector3 inE = new Vector3(Step(it.entryEdge).x, Step(it.entryEdge).y, 0f) * 0.5f;
                Vector3 outE = new Vector3(Step(dir).x, Step(dir).y, 0f) * 0.5f;
                d.transform.position = transform.position + PathPoint(it.p, inE, outE);
            }
        }

        // Quadratic Bézier with the control point at the cell centre (the origin): straight when
        // inE == -outE (a straight belt), a smooth quarter-arc when inE ⟂ outE (a corner).
        private static Vector3 PathPoint(float t, Vector3 inE, Vector3 outE)
            => (1f - t) * (1f - t) * inE + t * t * outE;
    }
}
