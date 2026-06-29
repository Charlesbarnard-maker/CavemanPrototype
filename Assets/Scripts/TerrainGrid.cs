using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    public enum Terrain : byte { Plains, Forest, Hills, Water, Mountain } // Mountain appended LAST — no index shift

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

            // BIG, READABLE regions: low-freq Perlin sampled at DOMAIN-WARPED coords (so biome borders
            // wander organically instead of tracing noise contours), a RIDGED term that carves linear
            // MOUNTAIN spines (real barriers, not blobs), and a radial CONTINENT FALLOFF that fades the
            // land into a soft coastline near the rim instead of a hard ocean ring. Pure function of `seed`.
            const float eF = 0.016f, mF = 0.020f, rF = 0.011f;      // elevation / moisture / rivers — LOW = big readable regions (rivers spread out)
            const float warpAmp = 22f, warpF = 0.011f, ridgeF = 0.013f; // ridgeF low = a FEW long mountain ranges, not a vein web
            float ox = seed, oy = seed * 1.7f + 13f, ox2 = seed * 0.3f + 100f, oy2 = seed * 2.1f + 50f;
            float rox = seed * 0.7f + 200f, roy = seed * 1.3f + 300f;
            float wox = seed * 1.1f + 400f, woy = seed * 0.9f + 450f, wox2 = seed * 1.5f + 500f, woy2 = seed * 0.6f + 550f;
            float rgx = seed * 0.8f + 600f, rgy = seed * 1.9f + 650f, bnox = seed * 0.4f + 700f, bnoy = seed * 1.2f + 750f;
            float cox = seed * 1.4f + 800f, coy = seed * 0.7f + 850f; // coastline wobble
            float core2 = (basinRadius * 0.75f) * (basinRadius * 0.75f);
            float invHalf = half > 0 ? 1f / half : 1f;

            for (int gy = 0; gy < _size; gy++)
            {
                for (int gx = 0; gx < _size; gx++)
                {
                    int wx = gx - half, wy = gy - half;
                    float d2 = wx * wx + wy * wy;

                    // Feathered basin: a GUARANTEED buildable plains core, then a noisy rim that blends out.
                    if (d2 <= core2) { _map[gy * _size + gx] = Terrain.Plains; continue; }
                    float bR = basinRadius * (0.85f + 0.30f * Mathf.PerlinNoise(wx * 0.05f + bnox, wy * 0.05f + bnoy));
                    bool inBasinRim = d2 <= bR * bR;

                    // Domain warp the sample coordinates so borders meander.
                    float qx = wx + warpAmp * (Mathf.PerlinNoise(wx * warpF + wox, wy * warpF + woy) - 0.5f);
                    float qy = wy + warpAmp * (Mathf.PerlinNoise(wx * warpF + wox2, wy * warpF + woy2) - 0.5f);

                    float e = Mathf.PerlinNoise(qx * eF + ox, qy * eF + oy);     // elevation
                    float m = Mathf.PerlinNoise(qx * mF + ox2, qy * mF + oy2);   // moisture
                    float ridge = 1f - Mathf.Abs(2f * Mathf.PerlinNoise(qx * ridgeF + rgx, qy * ridgeF + rgy) - 1f); // [0,1], 1 on a spine
                    // Rivers: a THIN channel along a low-freq iso-contour, but ONLY through low ground (valleys)
                    // and NOT on a ridge — so they read as a few winding rivers in the lowlands, not a web.
                    bool river = Mathf.Abs(Mathf.PerlinNoise(qx * rF + rox, qy * rF + roy) - 0.5f) < 0.010f
                                 && e < 0.52f && ridge < 0.80f;

                    // Continent falloff: the world stays LAND across the playable interior and only fades to
                    // SEA in the outer rim/corners. `fall` is a GLSL-style smoothstep that is 0 until ~0.90R
                    // (wobbled by coast noise for an organic coastline), then ramps 0→1 to the edge.
                    float dist01 = Mathf.Sqrt(d2) * invHalf;
                    float coastN = 0.05f * (Mathf.PerlinNoise(wx * 0.025f + cox, wy * 0.025f + coy) - 0.5f);
                    float fall = Mathf.Clamp01((dist01 - (0.90f + coastN)) / 0.14f);
                    fall = fall * fall * (3f - 2f * fall);
                    float land = e - fall * 1.10f;

                    Terrain t;
                    if (land < 0.16f || river) t = Terrain.Water;                  // interior lakes + thin rivers + rim sea
                    else if (ridge > 0.92f && land > 0.45f) t = Terrain.Mountain;  // impassable spine cores (thin linear ranges)
                    else if (ridge > 0.82f || land > 0.86f) t = Terrain.Hills;     // range flanks + high ground
                    else if (m > 0.50f) t = Terrain.Forest;                        // wet mid ground (a major biome)
                    else t = Terrain.Plains;

                    if (inBasinRim && t != Terrain.Water) t = Terrain.Plains;       // feather the basin into its surroundings
                    _map[gy * _size + gx] = t;
                }
            }

            // Clustering pass: a 3×3 majority filter widens ecotones so biomes read as large contiguous
            // regions. Water AND Mountain are never smoothed (rivers, coasts and spine ridges stay intact).
            SmoothLand(4);
        }

        // Majority filter over LAND cells only: each plains/forest/hills cell becomes the most
        // common land biome in its 3×3 neighbourhood (ties keep the current type). Water cells
        // (rivers/lakes/ocean) are never changed, so rivers and the coastline stay intact.
        private static void SmoothLand(int iterations)
        {
            if (_map == null) return;
            var src = new Terrain[_map.Length];
            for (int it = 0; it < iterations; it++)
            {
                System.Array.Copy(_map, src, _map.Length);
                for (int gy = 1; gy < _size - 1; gy++)
                    for (int gx = 1; gx < _size - 1; gx++)
                    {
                        int idx = gy * _size + gx;
                        if (src[idx] == Terrain.Water || src[idx] == Terrain.Mountain) continue; // never move water or spine ridges

                        int pl = 0, fo = 0, hi = 0;
                        for (int dy = -1; dy <= 1; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                switch (src[(gy + dy) * _size + (gx + dx)])
                                {
                                    case Terrain.Plains: pl++; break;
                                    case Terrain.Forest: fo++; break;
                                    case Terrain.Hills: hi++; break;
                                }
                            }
                        Terrain win = src[idx];
                        int wc = win == Terrain.Plains ? pl : win == Terrain.Forest ? fo : hi;
                        if (pl > wc) { win = Terrain.Plains; wc = pl; }
                        if (fo > wc) { win = Terrain.Forest; wc = fo; }
                        if (hi > wc) { win = Terrain.Hills; wc = hi; }
                        _map[idx] = win;
                    }
            }
        }

        private static void Set(int wx, int wy, Terrain t)
        {
            int gx = wx + Half, gy = wy + Half;
            if (gx < 0 || gy < 0 || gx >= _size || gy >= _size) return;
            _map[gy * _size + gx] = t;
        }

        /// <summary>Base heading of corridor `k` (shared so resource regions can sit at its end). Seeded
        /// per-corridor jitter de-symmetrises the spokes so the layout reads found, not like a diagram.</summary>
        public static float CorridorAngle(int k, int count)
            => (k / (float)Mathf.Max(1, count)) * Mathf.PI * 2f + 0.45f + (Hash01(k) - 0.5f) * 0.6f;

        /// <summary>Deterministic [0,1) hash of an int — per-corridor/zone jitter that doesn't perturb the
        /// global Random stream (so terrain layout stays stable independent of resource RNG draws).</summary>
        public static float Hash01(int k)
        {
            unchecked
            {
                uint x = (uint)(k * 374761393 + 668265263);
                x = (x ^ (x >> 13)) * 1274126177u;
                return ((x ^ (x >> 16)) & 0xFFFFFFu) / (float)0x1000000;
            }
        }

        /// <summary>Clear `count` dry corridors (water→plains) from spawn so the basin always has
        /// expansion paths out — never water-locked. Corridors MEANDER and vary in width so they
        /// read as natural valleys, not straight rays.</summary>
        public static void CarveCorridors(int count, float length, int halfWidth)
        {
            if (_map == null || count <= 0) return;
            for (int k = 0; k < count; k++)
            {
                float baseAng = CorridorAngle(k, count);
                float x = 0f, y = 0f;
                for (float r = 0f; r <= length; r += 0.5f)
                {
                    float a = baseAng + 0.45f * Mathf.Sin(r * 0.06f + k * 2f); // gentle wander
                    x += Mathf.Cos(a) * 0.5f; y += Mathf.Sin(a) * 0.5f;
                    int cx = Mathf.RoundToInt(x), cy = Mathf.RoundToInt(y);
                    int hw = halfWidth + (Mathf.PerlinNoise(r * 0.1f, k * 5f) > 0.6f ? 1 : 0); // varied width
                    for (int ox = -hw; ox <= hw; ox++)
                        for (int oy = -hw; oy <= hw; oy++)
                        {
                            var tt = At(cx + ox, cy + oy);
                            if (tt == Terrain.Water || tt == Terrain.Mountain) Set(cx + ox, cy + oy, Terrain.Plains); // dry, passable path
                        }
                }
            }
        }

        /// <summary>Paint a disc of cells to a biome (used to guarantee a region at a corridor end).</summary>
        public static void Paint(Vector3 center, float radius, Terrain biome)
        {
            if (_map == null) return;
            int cx = Mathf.RoundToInt(center.x), cy = Mathf.RoundToInt(center.y);
            int r = Mathf.CeilToInt(radius); float r2 = radius * radius;
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (dx * dx + dy * dy <= r2) Set(cx + dx, cy + dy, biome);
        }

        /// <summary>Paint an ORGANIC region: a solid core disc plus several jittered overlapping blobs, so a
        /// resource zone reads as a natural landmass instead of a stamped perfect circle (see the boat island).
        /// Uses the global RNG (called post-Generate, where Random is already in play) — not for terrain gen.</summary>
        public static void PaintBlob(Vector3 center, float radius, Terrain biome, int blobs, float jitter)
        {
            if (_map == null) return;
            Paint(center, radius * 0.72f, biome); // solid core
            for (int b = 0; b < blobs; b++)
            {
                Vector2 off = Random.insideUnitCircle * (radius * jitter);
                float rr = radius * Random.Range(0.45f, 0.95f);
                Paint(new Vector3(center.x + off.x, center.y + off.y, 0f), rr, biome);
            }
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

        /// <summary>Normal buildings need flat, solid land — never water (even bridged; bridges carry belts/
        /// feet) and never a Mountain (an impassable barrier you build around).</summary>
        public static bool Buildable(Vector2Int c) { var t = At(c); return t != Terrain.Water && t != Terrain.Mountain; }
        public static bool Buildable(Vector3 world) =>
            Buildable(new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y)));

        // --- Bridges: make a water cell passable for feet AND belts (not for buildings). ---
        public static void RegisterBridge(Vector2Int c) => _bridges.Add(c);
        public static void RemoveBridge(Vector2Int c) => _bridges.Remove(c);
        public static bool IsBridged(Vector2Int c) => _bridges.Contains(c);

        /// <summary>Can the player walk here? Water blocks unless bridged; Mountain always blocks.</summary>
        public static bool Walkable(Vector2Int c) { var t = At(c); return (t != Terrain.Water && t != Terrain.Mountain) || _bridges.Contains(c); }
        public static bool Walkable(Vector3 world) =>
            Walkable(new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y)));

        /// <summary>Can a belt sit here? Land (not Mountain), or a bridged water cell.</summary>
        public static bool BeltAllowed(Vector2Int c) { var t = At(c); return (t != Terrain.Water && t != Terrain.Mountain) || _bridges.Contains(c); }

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
                    var tt = _map[gy * _size + gx];
                    if (tt == Terrain.Water || tt == Terrain.Mountain) _map[gy * _size + gx] = Terrain.Plains; // keep patches reachable/buildable
                }
        }

        /// <summary>Walk OUTWARD from spawn along `angle` and return the centre of the densest patch of
        /// `biome` between minDist and maxDist — so a resource lands in a real, natural region of its home
        /// biome (you explore the corridor and FIND the forest/hills there). Returns false if the biome
        /// doesn't form a substantial patch along that ray (caller then falls back to painting one).</summary>
        public static bool FindBiomeAlong(float angle, Terrain biome, float minDist, float maxDist, out Vector3 pos)
        {
            pos = default;
            if (_map == null) return false;
            float dx = Mathf.Cos(angle), dy = Mathf.Sin(angle);
            float bestScore = -1f; Vector3 best = default;
            for (float r = minDist; r <= maxDist; r += 2f)
            {
                int cx = Mathf.RoundToInt(dx * r), cy = Mathf.RoundToInt(dy * r);
                int count = 0; // biome cells in a 9×9 neighbourhood → a real region, not a stray cell
                for (int oy = -4; oy <= 4; oy++)
                    for (int ox = -4; ox <= 4; ox++)
                        if (At(cx + ox, cy + oy) == biome) count++;
                if (count >= 30 && count > bestScore) { bestScore = count; best = new Vector3(cx, cy, 0f); }
            }
            if (bestScore < 0f) return false;
            pos = best; return true;
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

        // WARM, living palette (the world used to read as flat cold blocks). Sand is a new coastline rim.
        public static readonly Color Sand = new Color(0.78f, 0.71f, 0.49f);
        public static Color ColorOf(Terrain t) => t switch
        {
            Terrain.Water => new Color(0.20f, 0.44f, 0.60f),     // friendlier teal
            Terrain.Mountain => new Color(0.40f, 0.37f, 0.35f),  // warm rock
            Terrain.Hills => new Color(0.54f, 0.47f, 0.34f),     // warm tan
            Terrain.Forest => new Color(0.19f, 0.38f, 0.20f),    // deep but warm green
            _ => new Color(0.39f, 0.48f, 0.25f),                 // plains — living yellow-green
        };

        /// <summary>Deterministic [0,1) per-cell hash (integer mixer, NO Perlin) — cheap enough to texture every
        /// cell of a 1.7M-cell world at bake time. `salt` selects an independent channel (brightness, fleck, …).</summary>
        public static float CellHash(int gx, int gy, int salt)
        {
            unchecked
            {
                uint h = (uint)(gx * 73856093) ^ (uint)(gy * 19349663) ^ (uint)(salt * 83492791 + 2654435761u);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFFFFu) / (float)0x1000000;
            }
        }

        private static Terrain MapAt(int gx, int gy)
            => (gx < 0 || gy < 0 || gx >= _size || gy >= _size) ? Terrain.Plains : _map[gy * _size + gx];

        /// <summary>The baked biome map texture (1px = 1 cell, world -Half..Half). Set by SpawnRenderer; used
        /// by the full-screen map (M) to draw terrain under the fog. Null until the world is generated.</summary>
        public static Texture2D MapTex { get; private set; }

        /// <summary>Bake the current map into a world-space sprite (call AFTER ClearAround edits). The bake is no
        /// longer a flat colour per cell: it adds a sandy COASTLINE rim, per-cell brightness/warmth MOTTLE, sparse
        /// bright FLECKS, and DITHERED biome edges, so each region reads as textured ground instead of a slab.</summary>
        public static void SpawnRenderer()
        {
            if (_map == null) return;
            int ss = _size > 900 ? 2 : 4; // SUPERSAMPLE: bake several texels per cell so fine grain is crisp (POINT-filtered, sharp) rather than a blurry 1px-per-cell smear. Lower for huge worlds.

            // Pass 1 — per-cell base colour: warm biome palette, sandy COASTLINE rim, dithered biome EDGES.
            var cellCol = new Color[_map.Length];
            for (int gy = 0; gy < _size; gy++)
                for (int gx = 0; gx < _size; gx++)
                {
                    int idx = gy * _size + gx;
                    Terrain t = _map[idx];
                    Color c = ColorOf(t);
                    if (t != Terrain.Water && t != Terrain.Mountain)
                    {
                        bool coast = MapAt(gx - 1, gy) == Terrain.Water || MapAt(gx + 1, gy) == Terrain.Water
                                   || MapAt(gx, gy - 1) == Terrain.Water || MapAt(gx, gy + 1) == Terrain.Water;
                        if (coast) c = Color.Lerp(c, Sand, 0.6f);
                        else { Terrain nb = NeighbourLand(gx, gy, t); if (nb != t && CellHash(gx, gy, 2) < 0.30f) c = ColorOf(nb); }
                    }
                    cellCol[idx] = c;
                }

            // Pass 2 — the sharp high-res surface: each texel = its cell's colour + FINE per-texel grain (a few
            // px of brightness wander, sparse bright flecks = grass/pebble highlights, sparse dark specks = dirt/
            // shade). Point-filtered so it reads as detailed natural ground, not a soft wash.
            int tw = _size * ss;
            float inv = 1f / ss;
            var px = new Color[tw * tw];
            for (int ty = 0; ty < tw; ty++)
                for (int tx = 0; tx < tw; tx++)
                {
                    // BILINEAR-blend the cell base colour → soft, organic biome edges (no stair-stepped coast),
                    // then add SHARP per-texel grain on top (kept crisp by the Point filter) for natural detail.
                    float fcx = (tx + 0.5f) * inv - 0.5f, fcy = (ty + 0.5f) * inv - 0.5f;
                    int x0 = Mathf.Clamp(Mathf.FloorToInt(fcx), 0, _size - 1), y0 = Mathf.Clamp(Mathf.FloorToInt(fcy), 0, _size - 1);
                    int x1 = Mathf.Min(x0 + 1, _size - 1), y1 = Mathf.Min(y0 + 1, _size - 1);
                    float ax = Mathf.Clamp01(fcx - x0), ay = Mathf.Clamp01(fcy - y0);
                    Color b = Color.Lerp(Color.Lerp(cellCol[y0 * _size + x0], cellCol[y0 * _size + x1], ax),
                                         Color.Lerp(cellCol[y1 * _size + x0], cellCol[y1 * _size + x1], ax), ay);
                    float g = 0.90f + 0.18f * CellHash(tx, ty, 20);
                    float f = CellHash(tx, ty, 21);
                    if (f > 0.975f) g *= 1.18f;            // bright fleck
                    else if (f < 0.025f) g *= 0.84f;       // dark speck
                    px[ty * tw + tx] = new Color(Mathf.Clamp01(b.r * g), Mathf.Clamp01(b.g * g), Mathf.Clamp01(b.b * g), 1f);
                }
            var tex = new Texture2D(tw, tw, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(px);
            tex.Apply();
            MapTex = tex; // covers _size world units; pixel for world cell c ≈ (c+Half)*ss

            var go = new GameObject("Terrain");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tw, tw), new Vector2(0.5f, 0.5f), ss); // ppu = ss → tw/ss = _size world units
            sr.sortingOrder = -90; // above the plain ground backdrop (-100), below world objects
            go.transform.position = Vector3.zero;
        }

        // First 4-neighbour that is a DIFFERENT land biome (for edge dithering); else `self`.
        private static Terrain NeighbourLand(int gx, int gy, Terrain self)
        {
            Terrain n;
            n = MapAt(gx - 1, gy); if (n != self && n != Terrain.Water && n != Terrain.Mountain) return n;
            n = MapAt(gx + 1, gy); if (n != self && n != Terrain.Water && n != Terrain.Mountain) return n;
            n = MapAt(gx, gy - 1); if (n != self && n != Terrain.Water && n != Terrain.Mountain) return n;
            n = MapAt(gx, gy + 1); if (n != self && n != Terrain.Water && n != Terrain.Mountain) return n;
            return self;
        }
    }
}
