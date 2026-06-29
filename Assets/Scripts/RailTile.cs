using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A single track segment on the grid. Rails are drag-laid like pipes (instant), reserve their cell
    /// (no belts/buildings on top), and form a network that route vehicles (trains) PATH ALONG instead
    /// of flying straight A→B. A tile auto-orients (horizontal vs vertical) to its rail neighbours so a
    /// run reads as continuous track. Pathing + station hook-up live in <see cref="RailNet"/>.
    /// (Signals — the planned next step — would sit on these tiles as block boundaries.)
    /// </summary>
    public class RailTile : MonoBehaviour
    {
        public BuildingDefinition def;
        public static readonly Dictionary<Vector2Int, RailTile> Grid = new();
        public static readonly List<RailTile> All = new();
        private Vector2Int _cell;
        private SpriteRenderer _sr;
        private int _lastMask = -1; // neighbour config the current sprite is drawn for
        public Vector2Int Cell => _cell;

        public static RailTile At(Vector2Int c) => Grid.TryGetValue(c, out var r) ? r : null;

        public static RailTile Spawn(BuildingDefinition def, Vector3 pos)
        {
            var cell = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
            var prev = At(cell);
            if (prev != null) return prev; // one rail per cell

            var go = new GameObject("Rail");
            go.transform.position = new Vector3(cell.x, cell.y, 0f);
            go.transform.localScale = Vector3.one;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Rail(); // proper track (rails + sleepers), not a grey block
            sr.color = Color.white;            // baked rail/sleeper colours
            sr.sortingOrder = 0;               // ground infrastructure: under belts (1) and buildings (5)

            go.AddComponent<BoxCollider2D>(); // clickable to demolish

            var r = go.AddComponent<RailTile>();
            r.def = def; r._sr = sr; r._cell = cell;
            return r;
        }

        void OnEnable()
        {
            _cell = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
            Grid[_cell] = this;
            WorldGrid.Rails[_cell] = this;
            All.Add(this);
        }

        void OnDisable()
        {
            if (Grid.TryGetValue(_cell, out var r) && r == this) Grid.Remove(_cell);
            WorldGrid.Remove(WorldGrid.Rails, _cell, this);
            RailGraph.Clear(_cell); // a train can't be "on" a demolished tile
            All.Remove(this);
        }

        void Update()
        {
            if (_sr == null) return;
            // Pick the track sprite from this tile's NEIGHBOUR config so corners curve and junctions meet.
            // Station lanes count as neighbours (RailNet.IsRail) so track joins a platform cleanly.
            int mask = RenderMask(_cell);
            if (mask != _lastMask) { _lastMask = mask; _sr.sprite = PlaceholderArt.RailMask(mask); }
        }

        // Visual connection mask (N=1,E=2,S=4,W=8) shaped so two PARALLEL runs laid side by side don't fuse
        // into a ladder of tee-junctions: a lateral link is dropped when it would only weld two parallel
        // runs together. Real corners / tees / crossings keep their links. (Pathfinding in RailNet still
        // treats every 4-neighbour as connected — this only changes how the tile is DRAWN.)
        private static int RenderMask(Vector2Int c)
        {
            int m = 0;
            if (Connects(c, Belt.Dir.N)) m |= 1;
            if (Connects(c, Belt.Dir.E)) m |= 2;
            if (Connects(c, Belt.Dir.S)) m |= 4;
            if (Connects(c, Belt.Dir.W)) m |= 8;
            return m;
        }

        /// <summary>True when this rail cell genuinely links to its neighbour in direction d — i.e. they're
        /// both rail AND it isn't just two parallel runs sitting side by side. Shared by the render mask AND
        /// by <see cref="RailNet.FindPath"/>, so a train never hops between parallel lines that don't visually
        /// connect (only real corners / tees / crossings link).</summary>
        internal static bool Connects(Vector2Int c, Belt.Dir d)
        {
            var nb = c + Belt.Step(d);
            if (!RailNet.IsRail(nb)) return false;
            return !ParallelMerge(c, nb, d);
        }

        // True when linking `a` to its neighbour `b` (= a+step(d)) would only weld two PARALLEL runs side by
        // side: both cells carry on across the perpendicular axis, and NEITHER continues along d past the
        // other — so it isn't part of a real corner / tee / crossing (those have a cell continuing along d).
        private static bool ParallelMerge(Vector2Int a, Vector2Int b, Belt.Dir d)
        {
            bool aAxisOther = RailNet.IsRail(a - Belt.Step(d)); // rail on a's far side (away from b)
            bool bAxisOther = RailNet.IsRail(b + Belt.Step(d)); // rail continuing past b
            return HasPerp(a, d) && HasPerp(b, d) && !aAxisOther && !bAxisOther;
        }

        // Does `cell` have a rail neighbour on the axis PERPENDICULAR to d?
        private static bool HasPerp(Vector2Int cell, Belt.Dir d)
        {
            bool horizontal = d == Belt.Dir.E || d == Belt.Dir.W;
            return horizontal
                ? RailNet.IsRail(cell + Belt.Step(Belt.Dir.N)) || RailNet.IsRail(cell + Belt.Step(Belt.Dir.S))
                : RailNet.IsRail(cell + Belt.Step(Belt.Dir.E)) || RailNet.IsRail(cell + Belt.Step(Belt.Dir.W));
        }
    }

    /// <summary>Pathfinding over the rail network: a BFS that returns the cell path between two tiles,
    /// and a helper to find the rail tile a station hooks onto.</summary>
    public static class RailNet
    {
        private static readonly Vector2Int[] _dirs =
            { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };

        // Station through-track cells: a station has a rail lane running across it (see Depot) that acts
        // as track so trains route straight through (or stop). Registered/cleared by the Depot.
        public static readonly HashSet<Vector2Int> StationLane = new();

        /// <summary>Is this cell part of the rail network — a laid tile OR a station's through-lane?</summary>
        public static bool IsRail(Vector2Int c) => RailTile.At(c) != null || StationLane.Contains(c);

        /// <summary>Shortest rail path (inclusive cell list) from start to goal that OBEYS one-way signals
        /// (you may only enter a signal cell travelling its allowed direction), or null if unreachable.
        /// So on a one-way loop the path runs the legal way round instead of doubling back.</summary>
        public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            if (!IsRail(start) || !IsRail(goal)) return null;
            if (start == goal) return new List<Vector2Int> { start };
            var came = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
            var q = new Queue<Vector2Int>();
            q.Enqueue(start);
            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur == goal) return Reconstruct(came, start, goal);
                for (int i = 0; i < _dirs.Length; i++)
                {
                    var nx = cur + _dirs[i];
                    if (came.ContainsKey(nx) || !IsRail(nx)) continue;
                    if (!RailTile.Connects(cur, Belt.FromTo(cur, nx))) continue; // don't hop between parallel runs that don't actually link
                    var sig = Signal.At(nx);
                    if (sig != null && !sig.Allows(Belt.FromTo(cur, nx))) continue; // one-way: wrong way blocked (two-way allows its axis)
                    came[nx] = cur;
                    q.Enqueue(nx);
                }
            }
            return null;
        }

        private static List<Vector2Int> Reconstruct(Dictionary<Vector2Int, Vector2Int> came, Vector2Int s, Vector2Int g)
        {
            var path = new List<Vector2Int>();
            var c = g;
            int guard = 0;
            while (c != s && guard++ < 100000) { path.Add(c); c = came[c]; }
            path.Add(s);
            path.Reverse();
            return path;
        }

        /// <summary>The rail cell nearest a position (within radius) — a laid tile or a station lane cell.</summary>
        public static Vector2Int? RailNear(Vector2 worldPos, float radius = 3.6f)
        {
            bool found = false;
            Vector2Int best = default;
            float bestSq = radius * radius;
            for (int i = 0; i < RailTile.All.Count; i++)
            {
                var r = RailTile.All[i];
                if (r == null) continue;
                float sq = ((Vector2)r.transform.position - worldPos).sqrMagnitude;
                if (sq <= bestSq) { bestSq = sq; best = r.Cell; found = true; }
            }
            foreach (var c in StationLane)
            {
                float dx = c.x - worldPos.x, dy = c.y - worldPos.y, sq = dx * dx + dy * dy;
                if (sq <= bestSq) { bestSq = sq; best = c; found = true; }
            }
            return found ? best : (Vector2Int?)null;
        }
    }

    /// <summary>Runtime rail OCCUPANCY: which train is on each track cell. Trains claim the cell they're
    /// entering and release the one behind, so two trains never share a cell (no crossing-over) and a
    /// signal can see whether the block ahead of it is occupied. Pure bookkeeping — see RouteVehicle.</summary>
    public static class RailGraph
    {
        public static readonly Dictionary<Vector2Int, RouteVehicle> Occ = new();

        public static bool OccupiedByOther(Vector2Int c, RouteVehicle self)
            => Occ.TryGetValue(c, out var t) && t != null && t != self;

        public static bool AnyTrainAt(Vector2Int c) => Occ.TryGetValue(c, out var t) && t != null;

        public static void Claim(Vector2Int c, RouteVehicle t) => Occ[c] = t;

        public static void Release(Vector2Int c, RouteVehicle t)
        {
            if (Occ.TryGetValue(c, out var cur) && cur == t) Occ.Remove(c);
        }

        /// <summary>Drop any occupancy on a cell unconditionally (e.g. its track was demolished).</summary>
        public static void Clear(Vector2Int c) => Occ.Remove(c);

        /// <summary>Wipe all rail occupancy — called on a fresh game so stale entries can't persist when
        /// the Editor keeps statics alive between Play sessions (domain-reload-off).</summary>
        public static void Reset() => Occ.Clear();
    }
}
