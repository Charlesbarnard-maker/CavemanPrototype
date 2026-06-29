// Procedural art for WORLD DECORATION — the little props (grass, bushes, ferns, flowers, mushrooms, rocks,
// pebbles, reeds) scattered by DecorScatter to make the world feel filled-in and warm, plus the alpha-only
// cosmetics (cloud shadow, water glint, contact shadow, vignette). All 64×64, fy=0 BOTTOM, centre pivot.
// Each PROP is drawn into a body buffer then run through FinishDecor: a dark silhouette outline + a soft
// contact-shadow ellipse composited UNDERNEATH (so the outline never rings the shadow). Cached per sprite.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _grass, _fern, _mushroom, _pebbles, _decorRock, _reed, _cloudShadow, _waterGlint, _contactShadow;
        private static Sprite[] _bush, _flower;

        // Outline the opaque BODY, then composite a soft dark contact-shadow ellipse only where still empty
        // (centre shx/shy, radii shrx/shry in [0,1], peak alpha sha). No outline ever lands on the shadow.
        private static Sprite FinishDecor(Color[] px, int s, float shx, float shy, float shrx, float shry, float sha)
        {
            var dark = new Color(0.10f, 0.08f, 0.07f, 1f);
            var outPx = new Color[s * s];
            System.Array.Copy(px, outPx, px.Length);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    if (px[y * s + x].a > 0.01f) continue;
                    bool adj = false;
                    for (int oy = -1; oy <= 1 && !adj; oy++)
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            if (ox == 0 && oy == 0) continue;
                            int nx = x + ox, ny = y + oy;
                            if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                            if (px[ny * s + nx].a > 0.01f) { adj = true; break; }
                        }
                    if (adj) outPx[y * s + x] = dark;
                }
            if (sha > 0f)
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        if (outPx[y * s + x].a > 0.01f) continue;
                        float dx = (x / (float)(s - 1) - shx) / shrx, dy = (y / (float)(s - 1) - shy) / shry;
                        float d = dx * dx + dy * dy;
                        if (d <= 1f) outPx[y * s + x] = new Color(0f, 0f, 0f, sha * (1f - d));
                    }
            var tex = NewTex(s);
            tex.SetPixels(outPx);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        // ---- Soft, swaying GRASS TUFT --------------------------------------------------------------
        public static Sprite GrassTuft()
        {
            if (_grass != null) return _grass;
            const int s = 64; var px = new Color[s * s];
            var gL = new Color(0.57f, 0.74f, 0.34f); var gM = new Color(0.43f, 0.61f, 0.25f); var gD = new Color(0.31f, 0.47f, 0.19f);
            (float bx, float tx, float ty, Color c)[] blades =
            { (0f, 0f, 0.80f, gM), (-0.12f, -0.26f, 0.58f, gD), (0.12f, 0.26f, 0.58f, gD), (-0.06f, -0.14f, 0.68f, gL), (0.07f, 0.15f, 0.68f, gL) };
            const float baseY = 0.12f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1) - 0.5f, fy = y / (float)(s - 1);
                    foreach (var b in blades)
                    {
                        if (fy < baseY || fy > b.ty) continue;
                        float t = (fy - baseY) / (b.ty - baseY);
                        if (Mathf.Abs(fx - Mathf.Lerp(b.bx, b.tx, t)) <= Mathf.Lerp(0.045f, 0.008f, t)) { px[y * s + x] = b.c; break; }
                    }
                }
            _grass = FinishDecor(px, s, 0.5f, baseY, 0.22f, 0.055f, 0.28f);
            return _grass;
        }

        // ---- BUSH (variant 0 = leafy green, 1 = dry hill shrub) -------------------------------------
        public static Sprite Bush(int variant)
        {
            variant = Mathf.Clamp(variant, 0, 1);
            if (_bush == null) _bush = new Sprite[2];
            if (_bush[variant] != null) return _bush[variant];
            const int s = 64; var px = new Color[s * s];
            Color leafM = variant == 0 ? new Color(0.27f, 0.46f, 0.22f) : new Color(0.47f, 0.45f, 0.27f);
            Color leafL = variant == 0 ? new Color(0.37f, 0.57f, 0.29f) : new Color(0.57f, 0.55f, 0.33f);
            Color leafD = variant == 0 ? new Color(0.18f, 0.34f, 0.16f) : new Color(0.37f, 0.35f, 0.21f);
            var berry = new Color(0.80f, 0.30f, 0.32f);
            (float cx, float cy, float r)[] lobes = { (0.5f, 0.34f, 0.22f), (0.36f, 0.30f, 0.16f), (0.64f, 0.30f, 0.16f), (0.5f, 0.46f, 0.15f) };
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    bool inLeaf = false; float topMost = 0f;
                    foreach (var l in lobes)
                    {
                        float dx = fx - l.cx, dy = fy - l.cy;
                        if (dx * dx + dy * dy <= l.r * l.r) { inLeaf = true; if (fy + dx * 0.3f > topMost) topMost = fy + dx * 0.3f; }
                    }
                    if (!inLeaf) continue;
                    Color c = leafM;
                    if (fy > 0.40f) c = leafL;          // sun on top
                    else if (fy < 0.26f) c = leafD;     // shade at base
                    if (variant == 0 && TerrainGrid.CellHash(x, y, 9) > 0.93f) c = berry;
                    px[y * s + x] = c;
                }
            _bush[variant] = FinishDecor(px, s, 0.5f, 0.15f, 0.26f, 0.06f, 0.30f);
            return _bush[variant];
        }

        // ---- FERN — arched stem + chevron leaflets -------------------------------------------------
        public static Sprite Fern()
        {
            if (_fern != null) return _fern;
            const int s = 64; var px = new Color[s * s];
            var fM = new Color(0.31f, 0.51f, 0.27f); var fD = new Color(0.22f, 0.40f, 0.20f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1) - 0.5f, fy = y / (float)(s - 1);
                    if (fy < 0.12f || fy > 0.78f) continue;
                    float t = (fy - 0.12f) / 0.66f;
                    float spineX = 0.16f * Mathf.Sin(t * 2.2f);      // arched central stem
                    if (Mathf.Abs(fx - spineX) <= 0.020f) { px[y * s + x] = fD; continue; }
                    float leafW = Mathf.Lerp(0.26f, 0.03f, t);        // leaflets taper to the tip
                    float along = Frac(fy * 9f);
                    if (along < 0.5f && Mathf.Abs(fx - spineX) <= leafW && Mathf.Abs(Mathf.Abs(fx - spineX) - along * 0.5f) < 0.05f)
                        px[y * s + x] = fM;
                }
            _fern = FinishDecor(px, s, 0.5f, 0.13f, 0.20f, 0.05f, 0.24f);
            return _fern;
        }

        // ---- FLOWER (3 colour variants) ------------------------------------------------------------
        public static Sprite Flower(int variant)
        {
            variant = ((variant % 3) + 3) % 3;
            if (_flower == null) _flower = new Sprite[3];
            if (_flower[variant] != null) return _flower[variant];
            const int s = 64; var px = new Color[s * s];
            Color petal = variant == 0 ? new Color(0.96f, 0.83f, 0.30f) : variant == 1 ? new Color(0.93f, 0.52f, 0.68f) : new Color(0.57f, 0.64f, 0.93f);
            var core = new Color(0.55f, 0.40f, 0.16f); var stem = new Color(0.40f, 0.56f, 0.24f);
            float hx = 0.5f, hy = 0.62f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    if (Mathf.Abs(fx - 0.5f) <= 0.018f && fy >= 0.12f && fy <= hy) { px[y * s + x] = stem; continue; }
                    float dx = fx - hx, dy = fy - hy, d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d <= 0.06f) { px[y * s + x] = core; continue; }
                    if (d <= 0.17f)
                    {
                        float ang = Mathf.Atan2(dy, dx);
                        if (Frac(ang / (Mathf.PI * 2f) * 5f) < 0.62f) px[y * s + x] = petal; // 5 petals
                    }
                }
            _flower[variant] = FinishDecor(px, s, 0.5f, 0.12f, 0.10f, 0.035f, 0.18f);
            return _flower[variant];
        }

        // ---- MUSHROOM (forest) ---------------------------------------------------------------------
        public static Sprite Mushroom()
        {
            if (_mushroom != null) return _mushroom;
            const int s = 64; var px = new Color[s * s];
            var stem = new Color(0.92f, 0.88f, 0.78f); var cap = new Color(0.83f, 0.27f, 0.23f); var spot = new Color(0.97f, 0.94f, 0.88f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    if (Mathf.Abs(fx - 0.5f) <= 0.07f && fy >= 0.14f && fy <= 0.42f) { px[y * s + x] = stem; continue; }
                    float dx = (fx - 0.5f) / 0.24f, dy = (fy - 0.42f) / 0.22f;
                    if (fy >= 0.42f && dx * dx + dy * dy <= 1f)
                        px[y * s + x] = (TerrainGrid.CellHash(x, y, 4) > 0.84f) ? spot : cap;
                }
            _mushroom = FinishDecor(px, s, 0.5f, 0.14f, 0.14f, 0.05f, 0.24f);
            return _mushroom;
        }

        // ---- PEBBLES — a few flat stones -----------------------------------------------------------
        public static Sprite Pebbles()
        {
            if (_pebbles != null) return _pebbles;
            const int s = 64; var px = new Color[s * s];
            var pL = new Color(0.67f, 0.64f, 0.58f); var pD = new Color(0.47f, 0.44f, 0.40f);
            (float cx, float cy, float rx, float ry)[] stones = { (0.42f, 0.34f, 0.12f, 0.08f), (0.60f, 0.40f, 0.10f, 0.07f), (0.52f, 0.26f, 0.08f, 0.055f) };
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    foreach (var st in stones)
                    {
                        float dx = (fx - st.cx) / st.rx, dy = (fy - st.cy) / st.ry;
                        if (dx * dx + dy * dy <= 1f) { px[y * s + x] = (dx + dy < -0.3f) ? pL : pD; break; }
                    }
                }
            _pebbles = FinishDecor(px, s, 0f, 0f, 1f, 1f, 0f); // flat — no shadow
            return _pebbles;
        }

        // ---- ROCK / small boulder ------------------------------------------------------------------
        public static Sprite DecorRock()
        {
            if (_decorRock != null) return _decorRock;
            const int s = 64; var px = new Color[s * s];
            var rL = new Color(0.63f, 0.60f, 0.55f); var rM = new Color(0.51f, 0.48f, 0.44f); var rD = new Color(0.37f, 0.35f, 0.32f); var moss = new Color(0.35f, 0.47f, 0.25f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    float dx = (fx - 0.5f) / 0.30f, dy = (fy - 0.36f) / 0.24f;
                    if (dx * dx + dy * dy > 1f) continue;
                    Color c = rM;
                    if (dx + dy < -0.5f) c = rL;                 // lit upper-left
                    else if (dx + dy > 0.4f) c = rD;             // shadowed lower-right
                    if (Mathf.Abs(fx - 0.46f) < 0.02f && fy > 0.30f && fy < 0.46f) c = rD; // crack
                    if (fy > 0.50f && TerrainGrid.CellHash(x, y, 7) > 0.7f) c = moss;                  // moss cap
                    px[y * s + x] = c;
                }
            _decorRock = FinishDecor(px, s, 0.5f, 0.14f, 0.27f, 0.06f, 0.32f);
            return _decorRock;
        }

        // ---- REED — water-edge blades + a cattail --------------------------------------------------
        public static Sprite Reed()
        {
            if (_reed != null) return _reed;
            const int s = 64; var px = new Color[s * s];
            var rM = new Color(0.41f, 0.57f, 0.31f); var rD = new Color(0.31f, 0.45f, 0.23f); var tail = new Color(0.52f, 0.35f, 0.20f);
            (float bx, float tx, float ty, bool cat)[] blades = { (0f, 0.06f, 0.88f, true), (-0.10f, -0.16f, 0.70f, false), (0.10f, 0.18f, 0.66f, false), (-0.04f, -0.02f, 0.78f, false) };
            const float baseY = 0.10f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1) - 0.5f, fy = y / (float)(s - 1);
                    foreach (var b in blades)
                    {
                        if (fy < baseY || fy > b.ty) continue;
                        float t = (fy - baseY) / (b.ty - baseY);
                        float bx = Mathf.Lerp(b.bx, b.tx, t);
                        if (b.cat && fy > b.ty - 0.18f && Mathf.Abs(fx - bx) <= 0.045f) { px[y * s + x] = tail; break; } // cattail head
                        if (Mathf.Abs(fx - bx) <= Mathf.Lerp(0.030f, 0.010f, t)) { px[y * s + x] = (b.bx < 0 ? rD : rM); break; }
                    }
                }
            _reed = FinishDecor(px, s, 0f, 0f, 1f, 1f, 0f); // standing in shallows — no ground shadow
            return _reed;
        }

        // ---- Alpha-only cosmetics (tinted via SpriteRenderer.color) --------------------------------
        // A soft round CLOUD SHADOW blob (white + radial alpha, broken rim). The component tints it cool-dark.
        public static Sprite CloudShadow()
        {
            if (_cloudShadow != null) return _cloudShadow;
            const int s = 64; var px = new Color[s * s]; float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - c) / c, dy = (y - c) / c, d = Mathf.Sqrt(dx * dx + dy * dy);
                    float rim = 0.78f + 0.18f * (TerrainGrid.CellHash(x / 5, y / 5, 8) - 0.5f); // lumpy edge
                    float a = Mathf.Clamp01((rim - d) / rim);
                    px[y * s + x] = new Color(1f, 1f, 1f, a * a * 0.9f);
                }
            var tex = NewTex(s); tex.SetPixels(px); tex.Apply();
            _cloudShadow = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _cloudShadow;
        }

        // A narrow horizontal WATER GLINT lens (white + alpha). The component tints it pale cyan + twinkles alpha.
        public static Sprite WaterGlint()
        {
            if (_waterGlint != null) return _waterGlint;
            const int s = 64; var px = new Color[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1) - 0.5f, fy = y / (float)(s - 1) - 0.5f;
                    float a = Mathf.Clamp01(1f - (fx * fx) / (0.42f * 0.42f) - (fy * fy) / (0.10f * 0.10f));
                    if (a > 0f) px[y * s + x] = new Color(1f, 1f, 1f, a * a);
                }
            var tex = NewTex(s); tex.SetPixels(px); tex.Apply();
            _waterGlint = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _waterGlint;
        }

        // A flat dark CONTACT SHADOW ellipse (for grounding nodes/buildings if wanted).
        public static Sprite ContactShadow()
        {
            if (_contactShadow != null) return _contactShadow;
            const int s = 64; var px = new Color[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x / (float)(s - 1) - 0.5f) / 0.45f, dy = (y / (float)(s - 1) - 0.4f) / 0.16f;
                    float d = dx * dx + dy * dy;
                    if (d <= 1f) px[y * s + x] = new Color(0f, 0f, 0f, 0.36f * (1f - d));
                }
            var tex = NewTex(s); tex.SetPixels(px); tex.Apply();
            _contactShadow = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _contactShadow;
        }
    }
}
