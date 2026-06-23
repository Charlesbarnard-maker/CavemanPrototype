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

        private static readonly Dictionary<Vector2Int, Belt> Grid = new();

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
        public void Receive(ItemDefinition i) { item = i; count++; }

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

        void Update()
        {
            bool connected = HasForwardTarget();

            _timer += Time.deltaTime;
            if (_timer >= interval)
            {
                _timer -= interval;
                PushForward();
                if (connected) PullFromNeighbour(); // don't pull goods onto a belt that leads nowhere
            }

            if (_sr != null)
            {
                if (!connected) _sr.color = new Color(0.55f, 0.25f, 0.25f); // dead end — not connected
                else _sr.color = _baseColor;
            }

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

            float progress = interval > 0f ? Mathf.Clamp01(_timer / interval) : 0f;
            Vector3 d = new Vector3(Step(dir).x, Step(dir).y, 0f);
            _dot.transform.position = transform.position - d * 0.35f + d * (0.7f * progress);
            _dot.color = item.color;
        }

        // Is the cell ahead a belt, an accepting storage, or a workshop that wants this item?
        private bool HasForwardTarget()
        {
            var ahead = _cell + Step(dir);
            if (At(ahead) != null) return true;
            if (WorldGrid.Storages.TryGetValue(ahead, out var s) && s != null
                && (item == null || s.accepts == null || s.accepts == item)) return true;
            if (WorldGrid.Workshops.TryGetValue(ahead, out var w) && w != null
                && (item == null || w.WantsInput(item))) return true;
            return false;
        }

        private void PushForward()
        {
            if (count <= 0 || item == null) return;
            var ahead = _cell + Step(dir);

            var nb = At(ahead);
            if (nb != null)
            {
                if (nb.CanAccept(item)) { nb.Receive(item); count--; if (count <= 0) item = null; }
                return;
            }

            // No belt ahead — drop into a storage in that cell...
            if (WorldGrid.Storages.TryGetValue(ahead, out var s) && s != null && s.accepts == item)
            {
                if (s.def != null && s.Store.Total() >= s.def.capacity) return;
                s.Store.Add(item, 1); count--; if (count <= 0) item = null; return;
            }

            // ...or into a workshop's input buffer, if it uses this item.
            if (WorkshopAt(ahead) is WorkshopBuilding w && w.WantsInput(item))
            {
                if (w.InBuffer.Total() >= w.InBuffer.capacity) return;
                w.InBuffer.Add(item, 1); count--; if (count <= 0) item = null; return;
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

                if (WorldGrid.Collectors.TryGetValue(c, out var p) && p != null && p.produces != null
                    && (item == null || item == p.produces) && p.Buffer.Count(p.produces) > 0)
                {
                    if (p.Buffer.RemoveUpTo(p.produces, 1) > 0) { item = p.produces; count++; return; }
                }

                if (WorldGrid.Workshops.TryGetValue(c, out var w) && w != null && w.output != null
                    && (item == null || item == w.output) && w.Buffer.Count(w.output) > 0)
                {
                    if (w.Buffer.RemoveUpTo(w.output, 1) > 0) { item = w.output; count++; return; }
                }
            }
        }
    }
}
