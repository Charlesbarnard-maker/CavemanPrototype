using UnityEngine;

namespace Caveman
{
    /// <summary>Scatters cosmetic nature PROPS across the world (biome-weighted) so it reads as a lived-in
    /// landscape instead of flat colour. Static SpriteRenderers under one "Decor" parent, sorted BELOW
    /// buildings (-80) and naturally HIDDEN by fog (order 50) until explored. No colliders — purely visual.
    /// Density is front-loaded near spawn and the total is hard-capped, so cost is bounded on any map size.</summary>
    public static class DecorScatter
    {
        private const int Step = 4;        // one candidate cell per Step×Step block
        private const int MaxSprites = 14000;
        private const int SortOrder = -80;

        public static void Populate()
        {
            int half = TerrainGrid.Half;
            var root = new GameObject("Decor").transform;
            int placed = 0;

            for (int by = -half; by <= half && placed < MaxSprites; by += Step)
                for (int bx = -half; bx <= half && placed < MaxSprites; bx += Step)
                {
                    int x = bx + (int)(TerrainGrid.CellHash(bx, by, 11) * Step); // jittered candidate in the block
                    int y = by + (int)(TerrainGrid.CellHash(bx, by, 12) * Step);
                    var biome = TerrainGrid.At(x, y);

                    float dens = Density(biome);
                    if (dens <= 0f) continue;
                    // lush near spawn (where the player lives), thinning toward the rim (fog hides it anyway)
                    float dist = Mathf.Sqrt(x * (float)x + y * (float)y);
                    float boost = Mathf.Lerp(1.8f, 0.55f, Mathf.Clamp01(dist / 210f));
                    if (TerrainGrid.CellHash(x, y, 13) > dens * boost) continue;

                    var sprite = Pick(biome, x, y);
                    if (sprite == null) continue;
                    Place(root, sprite, x, y);
                    placed++;
                }
        }

        private static float Density(Terrain t) => t switch
        {
            Terrain.Forest => 0.62f,
            Terrain.Plains => 0.50f,
            Terrain.Hills => 0.34f,
            Terrain.Mountain => 0.12f,
            Terrain.Water => 0.35f, // reeds, but only on shore cells (see Pick)
            _ => 0f,
        };

        private static bool IsLand(int x, int y)
        { var t = TerrainGrid.At(x, y); return t != Terrain.Water && t != Terrain.Mountain; }

        private static Sprite Pick(Terrain t, int x, int y)
        {
            float r = TerrainGrid.CellHash(x, y, 14);
            switch (t)
            {
                case Terrain.Plains:
                    if (r < 0.55f) return PlaceholderArt.GrassTuft();
                    if (r < 0.78f) return PlaceholderArt.Flower((int)(TerrainGrid.CellHash(x, y, 15) * 3f));
                    if (r < 0.90f) return PlaceholderArt.Bush(0);
                    return PlaceholderArt.Pebbles();
                case Terrain.Forest:
                    if (r < 0.40f) return PlaceholderArt.Bush(0);
                    if (r < 0.66f) return PlaceholderArt.Fern();
                    if (r < 0.82f) return PlaceholderArt.GrassTuft();
                    if (r < 0.93f) return PlaceholderArt.Mushroom();
                    return PlaceholderArt.Flower(1);
                case Terrain.Hills:
                    if (r < 0.45f) return PlaceholderArt.DecorRock();
                    if (r < 0.72f) return PlaceholderArt.Pebbles();
                    if (r < 0.90f) return PlaceholderArt.Bush(1);
                    return PlaceholderArt.GrassTuft();
                case Terrain.Mountain:
                    return r < 0.7f ? PlaceholderArt.DecorRock() : PlaceholderArt.Pebbles();
                case Terrain.Water:
                    bool shore = IsLand(x - 1, y) || IsLand(x + 1, y) || IsLand(x, y - 1) || IsLand(x, y + 1);
                    return shore ? PlaceholderArt.Reed() : null;
                default: return null;
            }
        }

        private static bool IsSoft(Sprite sp) =>
            sp == PlaceholderArt.GrassTuft() || sp == PlaceholderArt.Bush(0) || sp == PlaceholderArt.Bush(1)
            || sp == PlaceholderArt.Fern() || sp == PlaceholderArt.Reed();

        private static void Place(Transform root, Sprite sprite, int x, int y)
        {
            var go = new GameObject("d");
            go.transform.SetParent(root, false);
            float ox = (TerrainGrid.CellHash(x, y, 16) - 0.5f) * 0.85f, oy = (TerrainGrid.CellHash(x, y, 17) - 0.5f) * 0.85f;
            go.transform.position = new Vector3(x + ox, y + oy, 0f);
            float sc = 0.55f + 0.40f * TerrainGrid.CellHash(x, y, 18); // size variety
            go.transform.localScale = new Vector3(sc, sc, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = SortOrder;
            if (SwayAnimator.Instance != null && IsSoft(sprite))
                SwayAnimator.Instance.Register(go.transform, 3.5f + 3f * TerrainGrid.CellHash(x, y, 19));
        }
    }
}
