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

        public static Belt Spawn(Vector2Int cell, Dir dir)
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
            b._sr = sr;
            sr.color = b._baseColor;
            return b;
        }

        void OnEnable() { _cell = CellOf(transform.position); Grid[_cell] = this; }
        void OnDisable() { if (Grid.TryGetValue(_cell, out var b) && b == this) Grid.Remove(_cell); }

        public bool CanAccept(ItemDefinition i) => count < capacity && (item == null || item == i);
        public void Receive(ItemDefinition i) { item = i; count++; }

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= interval)
            {
                _timer -= interval;
                PushForward();
                PullFromNeighbour();
            }

            if (_sr != null)
                _sr.color = (count > 0 && item != null) ? Color.Lerp(_baseColor, item.color, 0.6f) : _baseColor;
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

            // No belt ahead — try to drop into a storage sitting in that cell.
            foreach (var s in StorageBuilding.All)
            {
                if (s == null || s.accepts != item) continue;
                if (CellOf(s.transform.position) != ahead) continue;
                if (s.def != null && s.Store.Total() >= s.def.capacity) return;
                s.Store.Add(item, 1); count--; if (count <= 0) item = null; return;
            }
        }

        private void PullFromNeighbour()
        {
            if (count >= capacity) return;
            for (int di = 0; di < 4; di++)
            {
                var c = _cell + Step((Dir)di);

                foreach (var p in ProductionBuilding.All)
                {
                    if (p == null || p.produces == null) continue;
                    if (CellOf(p.transform.position) != c) continue;
                    if (item != null && item != p.produces) continue;
                    if (p.Buffer.Count(p.produces) <= 0) continue;
                    if (p.Buffer.RemoveUpTo(p.produces, 1) > 0) { item = p.produces; count++; return; }
                }

                foreach (var w in WorkshopBuilding.All)
                {
                    if (w == null || w.output == null) continue;
                    if (CellOf(w.transform.position) != c) continue;
                    if (item != null && item != w.output) continue;
                    if (w.Buffer.Count(w.output) <= 0) continue;
                    if (w.Buffer.RemoveUpTo(w.output, 1) > 0) { item = w.output; count++; return; }
                }
            }
        }
    }
}
