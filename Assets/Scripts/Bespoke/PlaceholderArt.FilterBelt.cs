// Bespoke procedural art for the "Filter / Sorter" belt variant. Auto-generated partial of PlaceholderArt.
// Companion to the SplitterSprite / MergerSprite junction sprites (see PlaceholderArt.cs BuildJunction):
// a straight belt body that tiles flush with neighbouring belts, but carrying a central FUNNEL + SIEVE
// glyph (baked dark) so it reads as a sorter/filter — distinct from the splitter's diverging chevrons
// and the merger's converging chevrons.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _filterSprite;
        public static Sprite FilterSprite()
        {
            if (_filterSprite != null) return _filterSprite;

            const int s = 64;
            var px = new Color[s * s];
            var body = new bool[s * s];      // belt-body (white) pixels — outlined into a frame in pass 2
            var solid = new bool[s * s];     // any non-transparent pixel of the belt shape
            var belt = Color.white;
            var dark = new Color(0.38f, 0.38f, 0.40f, 1f);   // same palette as BuildJunction
            var edge = new Color(0.30f, 0.30f, 0.32f, 1f);
            var hub  = new Color(0.92f, 0.93f, 0.96f, 1f);
            var glyph = new Color(0.16f, 0.16f, 0.18f, 1f);  // contrasting funnel/sieve ink
            float cx = (s - 1) * 0.5f;
            const float L = 11f, t = 2.4f;

            // Pass 1 — a STRAIGHT vertical belt (N/S through-line only). The arm width (0.17..0.83) matches a
            // belt's body — identical to BuildJunction's vertical arm — so a belt connects cleanly above & below.
            // Flow chevrons run up the spine (top/bottom). The middle band carries the FILTER glyph: a downward
            // FUNNEL (converging walls + a narrow throat) sitting over a GRATE / SIEVE of vertical bars.
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    bool vArm = fx >= 0.17f && fx <= 0.83f; // N/S through-line — the belt body
                    if (!vArm) { px[y * s + x] = Clear; continue; }
                    solid[y * s + x] = true;

                    Color c = belt;

                    // Flow chevrons near the two ends (mirrors the junction spine chevrons).
                    bool chev = false;
                    if (Mathf.Abs(fx - 0.5f) <= 0.33f)
                        chev = ChevUp(x, y, 10f, cx, L, t) || ChevUp(x, y, 60f, cx, L, t);

                    // Central FILTER glyph occupies the middle band (fy 0.30..0.70).
                    bool inBand = fy >= 0.30f && fy <= 0.70f && Mathf.Abs(fx - 0.5f) <= 0.30f;
                    if (inBand)
                    {
                        // (a) Funnel: two converging walls from a wide mouth (top of band) down to a narrow throat.
                        //     Half-width of the funnel interior shrinks linearly with depth.
                        float depth = Mathf.InverseLerp(0.30f, 0.56f, fy);     // 0 at mouth → 1 at throat
                        float halfW = Mathf.Lerp(0.255f, 0.055f, Mathf.Clamp01(depth)); // funnel inner half-width
                        float dxn = Mathf.Abs(fx - 0.5f);
                        bool funnelWall = fy <= 0.57f && Mathf.Abs(dxn - halfW) <= 0.035f;

                        // (b) Sieve / grate: a row of vertical bars hanging below the throat (fy 0.56..0.70),
                        //     reading as the screen that material is sorted through.
                        bool grate = fy >= 0.555f && fy <= 0.70f
                                     && dxn <= 0.155f
                                     && (Frac((fx - 0.5f) / 0.072f) < 0.5f);

                        // (c) Mouth lip across the top of the funnel so it doesn't read as an open V.
                        bool lip = fy >= 0.30f && fy <= 0.345f && dxn <= 0.255f;

                        if (funnelWall || grate || lip) c = glyph;
                    }
                    else if (chev) c = dark;

                    if (c == belt) body[y * s + x] = true;
                    px[y * s + x] = c;
                }

            // A small bright hub dot is intentionally omitted (straight belt, not a node); keep `hub` only for
            // optional emphasis on the funnel mouth centre so the sorter still has a focal highlight.
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    if (Mathf.Abs(fx - 0.5f) <= 0.03f && fy >= 0.31f && fy <= 0.345f && solid[y * s + x])
                    { px[y * s + x] = hub; body[y * s + x] = false; }
                }

            // Pass 2 — outline the belt with a continuous side rail (dark `edge`) so it reads as belt framing
            // and lines up with a connecting belt's rails — identical finishing to BuildJunction's pass 2.
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    if (!body[y * s + x]) continue;
                    bool border = x == 0 || x == s - 1 || y == 0 || y == s - 1
                                  || !solid[y * s + x - 1] || !solid[y * s + x + 1]
                                  || !solid[(y - 1) * s + x] || !solid[(y + 1) * s + x];
                    if (border) px[y * s + x] = edge;
                }

            var tex = NewTex(s);
            tex.SetPixels(px);
            tex.Apply();
            _filterSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _filterSprite;
        }
    }
}
