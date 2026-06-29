using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Water-only pathfinding for cargo SHIPS — the aquatic counterpart to RailNet. Ships must keep to WATER:
    /// they never use the rail graph and never fly over land. A* over 4-connected water cells (TerrainGrid.IsWater)
    /// finds the shortest sea route between two harbours' dock cells; returns null when no water route connects
    /// them (so line-creation can refuse an impossible harbour-to-harbour line, and a station can't be a ship stop).
    /// </summary>
    public static class WaterNet
    {
        private static readonly Vector2Int[] _dirs4 =
            { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };

        /// <summary>The nearest WATER cell to a world position (a harbour's dock side), searched in rings out
        /// to <paramref name="radius"/>. Null if there's no water that close (shouldn't happen for a harbour,
        /// which straddles the shore).</summary>
        public static Vector2Int? WaterCellNear(Vector3 world, int radius = 5)
        {
            var c0 = new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
            if (TerrainGrid.IsWater(c0)) return c0;
            for (int r = 1; r <= radius; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue; // current ring only
                        var c = new Vector2Int(c0.x + dx, c0.y + dy);
                        if (TerrainGrid.IsWater(c)) return c;
                    }
            return null;
        }

        /// <summary>A water-only route (list of cells, start→goal) between two world positions, or null if the
        /// two aren't connected by water. Used both to drive ship travel and to validate a harbour line.</summary>
        public static List<Vector2Int> WaterPath(Vector3 from, Vector3 to)
        {
            var s = WaterCellNear(from);
            var g = WaterCellNear(to);
            if (s == null || g == null) return null;
            if (s.Value == g.Value) return new List<Vector2Int> { s.Value };
            return AStar(s.Value, g.Value);
        }

        // A* over water cells with a Manhattan heuristic. SortedSet acts as the priority queue (a monotonic
        // tie-breaker keeps entries unique); a closed set + per-node best-g pruning keeps it efficient. The
        // guard caps exploration so a huge unreachable sea can't stall the game.
        private static List<Vector2Int> AStar(Vector2Int start, Vector2Int goal)
        {
            var came = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, int> { [start] = 0 };
            var closed = new HashSet<Vector2Int>();
            var open = new SortedSet<(int f, int tie, int x, int y)>();
            int tie = 0;
            int H(Vector2Int c) => Mathf.Abs(c.x - goal.x) + Mathf.Abs(c.y - goal.y);
            open.Add((H(start), tie++, start.x, start.y));

            int guard = 0;
            while (open.Count > 0 && guard++ < 60000)
            {
                var top = open.Min; open.Remove(top);
                var cur = new Vector2Int(top.x, top.y);
                if (cur == goal) return Reconstruct(came, cur);
                if (!closed.Add(cur)) continue; // a stale (already-finalised) queue entry
                int gc = gScore[cur];
                foreach (var d in _dirs4)
                {
                    var nb = cur + d;
                    if (closed.Contains(nb) || !TerrainGrid.IsWater(nb)) continue;
                    int ng = gc + 1;
                    if (gScore.TryGetValue(nb, out var old) && old <= ng) continue;
                    gScore[nb] = ng; came[nb] = cur;
                    open.Add((ng + H(nb), tie++, nb.x, nb.y));
                }
            }
            return null;
        }

        private static List<Vector2Int> Reconstruct(Dictionary<Vector2Int, Vector2Int> came, Vector2Int cur)
        {
            var path = new List<Vector2Int> { cur };
            while (came.TryGetValue(cur, out var prev)) { cur = prev; path.Add(cur); }
            path.Reverse();
            return path;
        }
    }
}
