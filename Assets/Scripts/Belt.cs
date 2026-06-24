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

        public static Belt Spawn(Vector2Int cell, Dir dir, float interval = 0.5f)
        {
            var go = new GameObject("Belt");
            go.transform.position = new Vector3(cell.x, cell.y, 0f);
            go.transform.localScale = Vector3.one * 0.8f;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Angle(dir));

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Triangle(); // arrow shows flow direction
            sr.sortingOrder = 1;

            go.AddComponent<BoxCollider2D>(); // clickable to demolish

            var b = go.AddComponent<Belt>();
            b.dir = dir;
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

        void Update()
        {
            // Connectivity (a chain-walk up to 256 cells) is only needed when we actually move
            // an item — recompute it on the interval tick and cache, not every frame.
            _timer += Time.deltaTime;
            if (_timer >= interval)
            {
                _timer -= interval;
                _connected = HasForwardTarget();
                PushForward();
                if (_connected) PullFromNeighbour(); // don't pull goods onto a belt that leads nowhere
            }

            if (_sr != null)
                _sr.color = _connected ? _baseColor : new Color(0.55f, 0.25f, 0.25f); // red = dead end

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
                sr.sprite = PlaceholderArt.Circle();
                sr.sortingOrder = 2;
                go.transform.localScale = Vector3.one * 0.24f;
                _dot = sr;
            }

            bool show = count > 0 && item != null;
            _dot.enabled = show;
            if (!show) return;

            // Slide from the edge the item arrived at, through the centre, to the exit
            // edge — so at a corner it tracks the bend instead of teleporting across.
            // 0.5 offsets put the exit edge of one belt exactly on the entry edge of the
            // next, so an item handed forward is visually continuous (no gap/jump).
            float progress = interval > 0f ? Mathf.Clamp01(_timer / interval) : 0f;
            Vector3 inE = new Vector3(Step(_inDir).x, Step(_inDir).y, 0f) * 0.5f;
            Vector3 outE = new Vector3(Step(dir).x, Step(dir).y, 0f) * 0.5f;
            _dot.transform.position = transform.position + Vector3.Lerp(inE, outE, progress);
            _dot.color = item.color;
        }

        // Connected only if goods can actually LEAVE: a sink directly ahead, or a belt
        // chain ahead that ultimately reaches a sink. A belt ahead is NOT enough on its
        // own — otherwise we'd pump goods into a chain that dead-ends in empty space and
        // lose them from circulation. This is what stops belts "conveying into nothing".
        private bool HasForwardTarget()
        {
            var ahead = _cell + Step(dir);
            // Input ports: a belt may only DELIVER into a building's INPUT side (opposite its
            // output). Geometrically that means the belt must travel in the building's output
            // direction (dir == OutputSide) to enter the input face.
            if (WorldGrid.Storages.TryGetValue(ahead, out var s) && s != null && dir == s.OutputSide
                && (item == null || s.accepts == null || s.accepts == item)) return true;
            if (WorldGrid.Workshops.TryGetValue(ahead, out var w) && w != null && dir == w.OutputSide
                && (item == null || w.WantsInput(item))) return true;
            if (WorldGrid.Depots.TryGetValue(ahead, out var dp) && dp != null
                && (item == null || dp.item == null || dp.item == item)) return true; // depots omnidirectional
            if (At(ahead) != null) return LeadsToSink(_cell, dir); // follow the chain
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
                    || WorldGrid.Depots.ContainsKey(ahead)) return true;
                var nb = At(ahead);
                if (nb == null) return false; // chain ends in empty space → dead end
                cell = ahead; dir = nb.dir;
            }
            return false;
        }

        private void PushForward()
        {
            if (count <= 0 || item == null) return;
            var ahead = _cell + Step(dir);

            var nb = At(ahead);
            if (nb != null)
            {
                // The item enters the next belt from the edge facing us (Opposite our dir).
                if (nb.CanAccept(item)) { nb.Receive(item, Opposite(dir)); count--; if (count <= 0) item = null; }
                return;
            }

            // No belt ahead — drop into a storage, but only on its INPUT side (dir == OutputSide).
            if (WorldGrid.Storages.TryGetValue(ahead, out var s) && s != null && dir == s.OutputSide && s.accepts == item)
            {
                if (s.def != null && s.Store.Total() >= s.def.capacity) return;
                s.Store.Add(item, 1); count--; if (count <= 0) item = null; return;
            }

            // ...or into a workshop's input buffer, on its INPUT side, if it uses this item.
            if (WorkshopAt(ahead) is WorkshopBuilding w && dir == w.OutputSide && w.WantsInput(item))
            {
                if (w.InBuffer.Total() >= w.InBuffer.capacity) return;
                w.InBuffer.Add(item, 1); count--; if (count <= 0) item = null; return;
            }

            // ...or into a depot that handles this item (route endpoint).
            if (WorldGrid.Depots.TryGetValue(ahead, out var dp) && dp != null && dp.item == item)
            {
                if (dp.def != null && dp.store.Total() >= dp.def.capacity) return;
                dp.store.Add(item, 1); count--; if (count <= 0) item = null; return;
            }
        }

        private static WorkshopBuilding WorkshopAt(Vector2Int c)
            => WorldGrid.Workshops.TryGetValue(c, out var w) ? w : null;

        private void PullFromNeighbour()
        {
            if (count >= capacity) return;
            for (int di = 0; di < 4; di++)
            {
                var c = _cell + Step((Dir)di);

                // Output ports: a belt only pulls a building's output from its OUTPUT side
                // (the building's output must face this belt — Opposite of our scan dir).
                if (WorldGrid.Collectors.TryGetValue(c, out var p) && p != null && p.produces != null
                    && p.OutputSide == Opposite((Dir)di)
                    && (item == null || item == p.produces) && p.Buffer.Count(p.produces) > 0)
                {
                    if (p.Buffer.RemoveUpTo(p.produces, 1) > 0) { item = p.produces; count++; _inDir = (Dir)di; _timer = 0f; return; }
                }

                if (WorldGrid.Workshops.TryGetValue(c, out var w) && w != null && w.output != null
                    && w.OutputSide == Opposite((Dir)di)
                    && (item == null || item == w.output) && w.Buffer.Count(w.output) > 0)
                {
                    if (w.Buffer.RemoveUpTo(w.output, 1) > 0) { item = w.output; count++; _inDir = (Dir)di; _timer = 0f; return; }
                }

                if (WorldGrid.Depots.TryGetValue(c, out var dp) && dp != null && dp.item != null
                    && (item == null || item == dp.item) && dp.store.Count(dp.item) > 0)
                {
                    if (dp.store.RemoveUpTo(dp.item, 1) > 0) { item = dp.item; count++; _inDir = (Dir)di; _timer = 0f; return; }
                }
            }
        }
    }
}
