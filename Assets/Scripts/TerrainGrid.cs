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

        public static bool Ready => _map != null;

        public static void Generate(int half = 120, float seed = 0f, float basinRadius = 15f)
        {
            Half = half;
            _size = half * 2 + 1;
            _map = new Terrain[_size * _size];

            const float eF = 0.045f, mF = 0.075f;
            float ox = seed, oy = seed * 1.7f + 13f, ox2 = seed * 0.3f + 100f, oy2 = seed * 2.1f + 50f;
            float basin2 = basinRadius * basinRadius;

            for (int gy = 0; gy < _size; gy++)
            {
                for (int gx = 0; gx < _size; gx++)
                {
                    int wx = gx - half, wy = gy - half;
                    if (wx * wx + wy * wy <= basin2) { _map[gy * _size + gx] = Terrain.Plains; continue; }

                    float e = Mathf.PerlinNoise(wx * eF + ox, wy * eF + oy);     // elevation
                    float m = Mathf.PerlinNoise(wx * mF + ox2, wy * mF + oy2);   // moisture
                    Terrain t;
                    if (e < 0.34f) t = Terrain.Water;        // low ground floods → rivers/lakes
                    else if (e > 0.70f) t = Terrain.Hills;   // high ground → hills
                    else if (m > 0.62f) t = Terrain.Forest;  // wet mid ground → forest
                    else t = Terrain.Plains;
                    _map[gy * _size + gx] = t;
                }
            }
        }

        public static Terrain At(int wx, int wy)
        {
            if (_map == null) return Terrain.Plains;
            int gx = wx + Half, gy = wy + Half;
            if (gx < 0 || gy < 0 || gx >= _size || gy >= _size) return Terrain.Plains;
            return _map[gy * _size + gx];
        }
        public static Terrain At(Vector2Int c) => At(c.x, c.y);

        /// <summary>Can a building/belt sit on this cell? (water needs a bridge — Step 2)</summary>
        public static bool Buildable(Vector2Int c) => At(c) != Terrain.Water;
        public static bool Buildable(Vector3 world) =>
            Buildable(new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y)));

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
