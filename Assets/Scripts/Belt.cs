using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A directional conveyor segment on a 1-unit grid. Each tick it pushes its
    /// carried item one cell forward (to the next belt, or into a storage in front)
    /// and pulls a new item from an adjacent collector/workshop's buffer. You lay
    /// belts out and orient them (R to rotate) to physically route goods — the
    /// spatial logistics puzzle. All I/O lives here, so no other building changes.
    /// </summary>
    public class Belt : MonoBehaviour
    {
        public enum Dir { N, E, S, W }
        public Dir dir;
        public int capacity = 4;
        public float interval = 0.5f;

        public ItemDefinition item;
        public int count;

        // Splitter: a 1→2 belt that sends items EVENLY to two outputs — its facing `dir` (forward)
        // and the cell to its right (RotateCW(dir)). `_splitToggle` flips the preferred output each
        // item; a full output falls back to the other so it never stalls.
        public bool isSplitter;
        private bool _splitToggle;

        private float _timer;
        private SpriteRenderer _sr;
        private SpriteRenderer _dot; // visible item sliding along the belt
        private readonly Color _baseColor = new Color(0.58f, 0.50f, 0.34f);
        private Vector2Int _cell;
        private Dir _inDir; // edge the current item arrived from (so corners animate right)

        private static readonly Dictionary<Vector2Int, Belt> Grid = new();

        public static int Count => Grid.Count;
        public static Vector2Int CellOf(Vector3 p) => new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
        public static Belt At(Vector2Int c) => Grid.TryGetValue(c, out var b) ? b : null;
        public static Dir RotateCW(Dir d) => (Dir)(((int)d + 1) % 4);

        public static Vector2Int Step(Dir d) => d switch
        {
            Dir.N => new Vector2Int(0, 1),
            Dir.E => new Vector2Int(1, 0),
            Dir.S => new Vector2Int(0, -1),
            _ => new Vector2Int(-1, 0),
        };

        public static float Angle(Dir d) => d switch { Dir.N => 0f, Dir.E => -90f, Dir.S => 180f, _ => 90f };

        public static Belt Spawn(Vector2Int cell, Dir dir, float interval = 0.5f, bool isSplitter = false)
        {
            var go = new GameObject(isSplitter ? "Splitter" : "Belt");
            go.transform.position = new Vector3(cell.x, cell.y, 0f);
            go.transform.localScale = Vector3.one * 0.8f;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Angle(dir));

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = isSplitter ? PlaceholderArt.Hexagon() : PlaceholderArt.Triangle(); // splitter looks distinct
            sr.sortingOrder = 1;

            go.AddComponent<BoxCollider2D>(); // clickable to demolish

            var b = go.AddComponent<Belt>();
            b.dir = dir;
            b.isSplitter = isSplitter;
            b._inDir = Opposite(dir); // default: item enters from directly behind
            b.interval = Mathf.Max(0.05f, interval);
            b._sr = sr;
            sr.color = b._baseColor;
            return b;
        }

        void OnEnable() { _cell = CellOf(transform.position); Grid[_cell] = this; }
        void OnDisable()
        {
            if (Grid.TryGetValue(_cell, out var b) && b == this) Grid.Remove(_cell);
            if (_dot != null) Destroy(_dot.gameObject);
        }

        public bool CanAccept(ItemDefinition i) => count < capacity && (item == null || item == i);
        // Reset the move timer on receive so the item dwells a full interval here and the
        // dot animates from the entry edge — no popping to a free-running clock's position.
        public void Receive(ItemDefinition i, Dir fromEdge) { item = i; count++; _inDir = fromEdge; _timer = 0f; }

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

        public static bool IsStorageCell(Vector2Int c) => WorldGrid.Storages.ContainsKey(c);
        public static bool IsSourceCell(Vector2Int c) =>
            WorldGrid.Collectors.ContainsKey(c) || WorldGrid.Workshops.ContainsKey(c);

        private bool _connected;
        private bool _blocked; // had an item but couldn't move it forward (dead end / backed up)

        void Update()
        {
            // Connectivity (a chain-walk up to 256 cells) is only needed when we actually move
            // an item — recompute it on the interval tick and cache, not every frame.
            _timer += Time.deltaTime;
            if (_timer >= interval)
            {
                _timer -= interval;
                _connected = HasForwardTarget();
                int before = count;
                PushForward();
                _blocked = count > 0 && count == before; // item present and it didn't leave
                if (_connected) PullFromNeighbour(); // don't pull goods onto a belt that leads nowhere
            }

            // Belt colour reads the bottleneck at a glance:
            //   red    = dead end (goes nowhere — no sink ahead),
            //   yellow = backed up (connected but item can't move → DOWNSTREAM is full/blocked),
            //   base   = flowing or empty (an empty connected belt = an UPSTREAM/supply issue).
            if (_sr != null)
                _sr.color = !_connected ? new Color(0.62f, 0.24f, 0.24f)      // red — dead end
                          : _blocked   ? new Color(0.85f, 0.66f, 0.18f)        // yellow — backed up downstream
                          : _baseColor;                                         // brown — ok (empty = supply issue)

            UpdateDot();
        }

        // A small sprite that slides from the back of the cell to the front each tick,
        // so you can see goods physically moving along the belt.
        private void UpdateDot()
        {
            if (_dot == null)
            {
                var go = new GameObject("BeltItem");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 2;
                go.transform.localScale = Vector3.one * 0.26f;
                _dot = sr;
            }

            bool show = count > 0 && item != null;
            _dot.enabled = show;
            if (!show) return;

            // Slide from the edge the item arrived at, through the centre, to the exit edge —
            // so at a corner it tracks the bend (0.5 offsets make one belt's exit == the next's
            // entry, seamless). When BLOCKED (dead end / backed up) the item HOLDS at the front
            // and stops animating, so a broken belt reads as "stuck", not phantom-travelling.
            float progress = _blocked ? 1f : (interval > 0f ? Mathf.Clamp01(_timer / interval) : 0f);
            Vector3 inE = new Vector3(Step(_inDir).x, Step(_inDir).y, 0f) * 0.5f;
            Vector3 outE = new Vector3(Step(dir).x, Step(dir).y, 0f) * 0.5f;
            _dot.transform.position = transform.position + Vector3.Lerp(inE, outE, progress);
            _dot.sprite = item.icon != null ? item.icon : PlaceholderArt.Circle(); // per-item look
            _dot.color = item.color;
        }

        // Connected only if goods can actually LEAVE: a sink directly ahead, or a belt
        // chain ahead that ultimately reaches a sink. A belt ahead is NOT enough on its
        // own — otherwise we'd pump goods into a chain that dead-ends in empty space and
        // lose them from circulation. This is what stops belts "conveying into nothing".
        private bool HasForwardTarget()
        {
            // A splitter is connected if EITHER of its two outputs can take goods.
            if (isSplitter) return OutputConnected(dir) || OutputConnected(RotateCW(dir));
            return OutputConnected(dir);
        }

        // Can goods LEAVE via direction d? A sink directly there, or a belt chain that reaches one.
        private bool OutputConnected(Dir d)
        {
            var ahead = _cell + Step(d);
            // Input ports: a belt may only DELIVER into a building's INPUT side (opposite its
            // output). Geometrically that means the belt must travel in the building's output
            // direction (d == OutputSide) to enter the input face.
            if (WorldGrid.Storages.TryGetValue(ahead, out var s) && s != null && d == s.OutputSide
                && (item == null || s.accepts == item || (s.accepts == null && s.configurable))) return true;
            if (WorldGrid.Workshops.TryGetValue(ahead, out var w) && w != null && AcceptsInputSide(w, d)
                && (item == null || w.WantsInput(item))) return true;
            if (WorldGrid.Depots.TryGetValue(ahead, out var dp) && dp != null
                && (item == null || dp.item == null || dp.item == item)) return true; // depots omnidirectional
            if (WorldGrid.Research.TryGetValue(ahead, out var rb) && rb != null
                && (item == null || rb.Accepts(item))) return true; // research sink (omnidirectional)
            if (At(ahead) != null) return LeadsToSink(_cell, d); // follow the chain
            return false;
        }

        // Walk the belt chain forward; true if it reaches any storage/workshop/depot.
        // Bounded (no visited set) so a closed loop simply returns false after the cap.
        private static bool LeadsToSink(Vector2Int cell, Dir dir)
        {
            for (int i = 0; i < 256; i++)
            {
                var ahead = cell + Step(dir);
                if (WorldGrid.Storages.ContainsKey(ahead) || WorldGrid.Workshops.ContainsKey(ahead)
                    || WorldGrid.Depots.ContainsKey(ahead) || WorldGrid.Research.ContainsKey(ahead)) return true;
                var nb = At(ahead);
                if (nb == null) return false; // chain ends in empty space → dead end
                cell = ahead; dir = nb.dir;
            }
            return false;
        }

        private void PushForward()
        {
            if (count <= 0 || item == null) return;

            if (isSplitter)
            {
                // 1→2 even split: alternate the PREFERRED output each successful item; if that side
                // is blocked, fall back to the other so a full lane never stalls the splitter.
                Dir a = _splitToggle ? RotateCW(dir) : dir;
                Dir b = _splitToggle ? dir : RotateCW(dir);
                if (TryDepositTo(a)) { _splitToggle = !_splitToggle; return; }
                TryDepositTo(b);
                return;
            }

            TryDepositTo(dir);
        }

        // Move ONE item out in direction d — into a belt, or a building sink (storage/workshop/depot/
        // research) on its input side. Returns true if an item left. Shared by belts AND splitters.
        private bool TryDepositTo(Dir d)
        {
            if (count <= 0 || item == null) return false;
            var ahead = _cell + Step(d);

            var nb = At(ahead);
            if (nb != null)
            {
                // The item enters the next belt from the edge facing us (Opposite our dir).
                if (nb.CanAccept(item)) { nb.Receive(item, Opposite(d)); count--; if (count <= 0) item = null; return true; }
                return false;
            }

            // No belt ahead — drop into a storage, but only on its INPUT side (d == OutputSide).
            // A configurable warehouse with no type yet AUTO-REGISTERS the first item belted in
            // (so goods aren't lost into an "unset" store) — it then only accepts that type.
            if (WorldGrid.Storages.TryGetValue(ahead, out var s) && s != null && d == s.OutputSide
                && (s.accepts == item || (s.accepts == null && s.configurable)))
            {
                if (s.def != null && s.Store.Total() >= s.def.capacity) return false;
                if (s.accepts == null) s.accepts = item; // adopt the first delivered item's type
                s.Store.Add(item, 1); count--; if (count <= 0) item = null; return true;
            }

            // ...or into a workshop's input buffer, on its INPUT side, if it uses this item.
            if (WorkshopAt(ahead) is WorkshopBuilding w && AcceptsInputSide(w, d) && w.WantsInput(item))
            {
                if (w.InBuffer.Total() >= w.InBuffer.capacity) return false;
                w.InBuffer.Add(item, 1); count--; if (count <= 0) item = null; return true;
            }

            // ...or into a depot that handles this item (route endpoint).
            if (WorldGrid.Depots.TryGetValue(ahead, out var dp) && dp != null && dp.item == item)
            {
                if (dp.def != null && dp.store.Total() >= dp.def.capacity) return false;
                dp.store.Add(item, 1); count--; if (count <= 0) item = null; return true;
            }

            // ...or into a Research Lodge, if it's the item currently being researched.
            if (WorldGrid.Research.TryGetValue(ahead, out var rb) && rb != null && rb.Accepts(item))
            {
                if (rb.InBuffer.Total() >= rb.InBuffer.capacity) return false;
                rb.InBuffer.Add(item, 1); count--; if (count <= 0) item = null; return true;
            }
            return false;
        }

        private static WorkshopBuilding WorkshopAt(Vector2Int c)
            => WorldGrid.Workshops.TryGetValue(c, out var w) ? w : null;

        // A belt at this cell moving `d` sits on the workshop's `Opposite(d)` side. Multi-input
        // workshops accept inputs on ANY non-output side (so each of their different items can come
        // on its own belt); single-input workshops keep just the one input side (opposite the output).
        private static bool AcceptsInputSide(WorkshopBuilding w, Dir d)
            => (w.inputs != null && w.inputs.Count > 1) ? Opposite(d) != w.OutputSide : d == w.OutputSide;

        private void PullFromNeighbour()
        {
            if (count >= capacity) return;
            for (int di = 0; di < 4; di++)
            {
                var c = _cell + Step((Dir)di);

                // Output ports: a belt only pulls a building's output from its OUTPUT side
                // (the building's output must face this belt — Opposite of our scan dir).
                // Liquids never ride belts — they move via pipes / carrying.
                if (WorldGrid.Collectors.TryGetValue(c, out var p) && p != null && p.produces != null && !p.produces.isLiquid
                    && p.OutputSide == Opposite((Dir)di)
                    && (item == null || item == p.produces) && p.Buffer.Count(p.produces) > 0)
                {
                    if (p.Buffer.RemoveUpTo(p.produces, 1) > 0) { item = p.produces; count++; _inDir = (Dir)di; _timer = 0f; return; }
                }

                if (WorldGrid.Workshops.TryGetValue(c, out var w) && w != null && w.output != null && !w.output.isLiquid
                    && w.OutputSide == Opposite((Dir)di)
                    && (item == null || item == w.output) && w.Buffer.Count(w.output) > 0)
                {
                    if (w.Buffer.RemoveUpTo(w.output, 1) > 0) { item = w.output; count++; _inDir = (Dir)di; _timer = 0f; return; }
                }

                // Storages have an OUTPUT side too — a belt on it pulls the stored item, so you
                // can belt FROM a warehouse to a workshop (e.g. warehouse → sawmill).
                if (WorldGrid.Storages.TryGetValue(c, out var st) && st != null && st.accepts != null && !st.accepts.isLiquid
                    && st.OutputSide == Opposite((Dir)di)
                    && (item == null || item == st.accepts) && st.Store.Count(st.accepts) > 0)
                {
                    if (st.Store.RemoveUpTo(st.accepts, 1) > 0) { item = st.accepts; count++; _inDir = (Dir)di; _timer = 0f; return; }
                }

                if (WorldGrid.Depots.TryGetValue(c, out var dp) && dp != null && dp.item != null && !dp.item.isLiquid
                    && (item == null || item == dp.item) && dp.store.Count(dp.item) > 0)
                {
                    if (dp.store.RemoveUpTo(dp.item, 1) > 0) { item = dp.item; count++; _inDir = (Dir)di; _timer = 0f; return; }
                }
            }
        }
    }
}
