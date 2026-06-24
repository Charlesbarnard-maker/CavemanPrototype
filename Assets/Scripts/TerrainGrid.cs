using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    public enum Terrain : byte { Plains, Forest, Hills, Water }

    /// <summary>
    /// The world as a system: a per-cell biome map that constrains where you can build.
    /// Step 1 — water (rivers/lakes) is UNBUILDABLE, so geography forces you to route belts
    /// and expansion around it; a clear plains BASIN around spawn keeps the early game
    /// compact and workable. Forest/Hills/Plains are visible biomes now; their construction/
    /// movement effects + per-biome resource bias come next (see WORLD.md).
    /// Rendered as one point-filtered texture (1 px = 1 cell), like FogOfWar.
    /// </summary>
    public static class TerrainGrid
    {
        public static int Half { get; private set; }
        private static int _size;
        private static Terrain[] _map;
        private static readonly HashSet<Vector2Int> _bridges = new(); // water cells made passable

        public static bool Ready => _map != null;

        public static void Generate(int half = 120, float seed = 0f, float basinRadius = 15f)
        {
            Half = half;
            _size = half * 2 + 1;
            _map = new Terrain[_size * _size];
            _bridges.Clear();

            const float eF = 0.045f, mF = 0.075f, rF = 0.018f;
            float ox = seed, oy = seed * 1.7f + 13f, ox2 = seed * 0.3f + 100f, oy2 = seed * 2.1f + 50f;
            float rox = seed * 0.7f + 200f, roy = seed * 1.3f + 300f;
            float basin2 = basinRadius * basinRadius;

            for (int gy = 0; gy < _size; gy++)
            {
                for (int gx = 0; gx < _size; gx++)
                {
                    int wx = gx - half, wy = gy - half;
                    if (wx * wx + wy * wy <= basin2) { _map[gy * _size + gx] = Terrain.Plains; continue; }

                    // Ocean rim: a coastline of water near the map edge → a clean world boundary.
                    bool ocean = (wx * wx + wy * wy) > (half - 8) * (half - 8);

                    float e = Mathf.PerlinNoise(wx * eF + ox, wy * eF + oy);     // elevation
                    float m = Mathf.PerlinNoise(wx * mF + ox2, wy * mF + oy2);   // moisture
                    // Winding rivers: THIN bands where a low-freq noise crosses 0.5 — these snake
                    // across the map and DIVIDE it into regions you bridge between (routes, not walls).
                    bool river = Mathf.Abs(Mathf.PerlinNoise(wx * rF + rox, wy * rF + roy) - 0.5f) < 0.020f;

                    Terrain t;
                    if (ocean || river || e < 0.20f) t = Terrain.Water; // coastline + thin rivers + rare lakes
                    else if (e > 0.70f) t = Terrain.Hills;              // high ground → hills
                    else if (m > 0.60f) t = Terrain.Forest;            // wet mid ground → forest
                    else t = Terrain.Plains;
                    _map[gy * _size + gx] = t;
                }
            }
        }

        private static void Set(int wx, int wy, Terrain t)
        {
            int gx = wx + Half, gy = wy + Half;
            if (gx < 0 || gy < 0 || gx >= _size || gy >= _size) return;
            _map[gy * _size + gx] = t;
        }

        /// <summary>Force a disc of water (a lake) — used to guarantee a water feature near spawn.</summary>
        public static void CarveWater(Vector3 center, float radius)
        {
            if (_map == null) return;
            int cx = Mathf.RoundToInt(center.x), cy = Mathf.RoundToInt(center.y);
            int r = Mathf.CeilToInt(radius);
            float r2 = radius * radius;
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (dx * dx + dy * dy <= r2) Set(cx + dx, cy + dy, Terrain.Water);
        }

        public static Terrain At(int wx, int wy)
        {
            if (_map == null) return Terrain.Plains;
            int gx = wx + Half, gy = wy + Half;
            if (gx < 0 || gy < 0 || gx >= _size || gy >= _size) return Terrain.Plains;
            return _map[gy * _size + gx];
        }
        public static Terrain At(Vector2Int c) => At(c.x, c.y);

        public static bool IsWater(Vector2Int c) => At(c) == Terrain.Water;

        /// <summary>Normal buildings need solid land — never water (even bridged; bridges carry belts/feet).</summary>
        public static bool Buildable(Vector2Int c) => At(c) != Terrain.Water;
        public static bool Buildable(Vector3 world) =>
            Buildable(new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y)));

        // --- Bridges: make a water cell passable for feet AND belts (not for buildings). ---
        public static void RegisterBridge(Vector2Int c) => _bridges.Add(c);
        public static void RemoveBridge(Vector2Int c) => _bridges.Remove(c);
        public static bool IsBridged(Vector2Int c) => _bridges.Contains(c);

        /// <summary>Can the player walk here? Water blocks unless bridged.</summary>
        public static bool Walkable(Vector2Int c) => At(c) != Terrain.Water || _bridges.Contains(c);
        public static bool Walkable(Vector3 world) =>
            Walkable(new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y)));

        /// <summary>Can a belt sit here? Land, or a bridged water cell.</summary>
        public static bool BeltAllowed(Vector2Int c) => At(c) != Terrain.Water || _bridges.Contains(c);

        /// <summary>Is there a water cell within `range` of this point? (for water collectors)</summary>
        public static bool HasWaterNear(Vector3 world, float range) => NearestWaterCell(world, range, out _);

        public static bool NearestWaterCell(Vector3 world, float range, out Vector2Int cell)
        {
            cell = default;
            if (_map == null) return false;
            int cx = Mathf.RoundToInt(world.x), cy = Mathf.RoundToInt(world.y);
            int r = Mathf.CeilToInt(range);
            float best = range * range; bool found = false;
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    float d2 = dx * dx + dy * dy;
                    if (d2 > best) continue;
                    if (At(cx + dx, cy + dy) != Terrain.Water) continue;
                    best = d2; cell = new Vector2Int(cx + dx, cy + dy); found = true;
                }
            return found;
        }

        /// <summary>Turn water near a point into plains so spawned resources stay reachable.</summary>
        public static void ClearAround(Vector3 world, float radius)
        {
            if (_map == null) return;
            int cx = Mathf.RoundToInt(world.x), cy = Mathf.RoundToInt(world.y);
            int r = Mathf.CeilToInt(radius);
            float r2 = radius * radius;
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dy * dy > r2) continue;
                    int gx = cx + dx + Half, gy = cy + dy + Half;
                    if (gx < 0 || gy < 0 || gx >= _size || gy >= _size) continue;
                    if (_map[gy * _size + gx] == Terrain.Water) _map[gy * _size + gx] = Terrain.Plains;
                }
        }

        /// <summary>Find a random cell of `biome` at least `minClear` from origin (for biome-placed
        /// resources). Returns false if none found within `attempts` tries.</summary>
        public static bool TryRandomCellOfBiome(Terrain biome, float minClear, int attempts, out Vector3 pos)
        {
            pos = default;
            if (_map == null) return false;
            float min2 = minClear * minClear;
            for (int i = 0; i < attempts; i++)
            {
                int wx = UnityEngine.Random.Range(-Half, Half + 1);
                int wy = UnityEngine.Random.Range(-Half, Half + 1);
                if (wx * wx + wy * wy < min2) continue;
                if (At(wx, wy) != biome) continue;
                pos = new Vector3(wx, wy, 0f);
                return true;
            }
            return false;
        }

        public static Color ColorOf(Terrain t) => t switch
        {
            Terrain.Water => new Color(0.20f, 0.39f, 0.62f),
            Terrain.Hills => new Color(0.47f, 0.43f, 0.36f),
            Terrain.Forest => new Color(0.16f, 0.33f, 0.17f),
            _ => new Color(0.31f, 0.41f, 0.25f), // plains
        };

        /// <summary>Bake the current map into a world-space sprite (call AFTER ClearAround edits).</summary>
        public static void SpawnRenderer()
        {
            if (_map == null) return;
            var tex = new Texture2D(_size, _size, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[_map.Length];
            for (int i = 0; i < _map.Length; i++) px[i] = ColorOf(_map[i]);
            tex.SetPixels32(px);
            tex.Apply();

            var go = new GameObject("Terrain");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, _size, _size), new Vector2(0.5f, 0.5f), 1f); // 1px = 1 unit
            sr.sortingOrder = -90; // above the plain ground backdrop (-100), below world objects
            go.transform.position = Vector3.zero;
        }
    }
}
