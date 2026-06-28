// Bespoke procedural art for "Harbour" (Age 0). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _harbourDock;
        public static Sprite HarbourDock()
        {
            if (_harbourDock != null) return _harbourDock;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            var waterD    = new Color(0.12f, 0.26f, 0.40f, 1f);
            var waterM    = new Color(0.18f, 0.36f, 0.54f, 1f);
            var waterL    = new Color(0.34f, 0.54f, 0.72f, 1f);
            var woodD     = new Color(0.27f, 0.17f, 0.09f, 1f);
            var woodM     = new Color(0.45f, 0.31f, 0.17f, 1f);
            var woodL     = new Color(0.60f, 0.44f, 0.26f, 1f);
            var ropeD     = new Color(0.46f, 0.38f, 0.22f, 1f);
            var ropeM     = new Color(0.66f, 0.56f, 0.34f, 1f);
            var ropeL     = new Color(0.80f, 0.70f, 0.46f, 1f);
            var bodyHi    = new Color(1f, 1f, 1f, 1f);            // structural mass, tinted at runtime
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ---- WATER band along the bottom (baked blue) with ripple lines ----
                if (fy < 0.22f) {
                    c = waterM;
                    if (fy < 0.07f) c = waterD; else if (fy > 0.16f) c = waterL;
                    // deterministic ripple streaks
                    float wave1 = 0.135f + 0.022f * Mathf.Sin(fx * 11f);
                    if (fy >= wave1 && fy <= wave1 + 0.016f) c = waterL;
                    float wave2 = 0.055f + 0.018f * Mathf.Sin(fx * 9f + 1.7f);
                    if (fy >= wave2 && fy <= wave2 + 0.014f) c = waterD;
                }

                // ---- MOORING PILES: round posts rising from the water up to the deck ----
                // four evenly spaced across the width, span fy 0.04 .. 0.34 (deck underside)
                for (int p = 0; p < 4; p++) {
                    float pcx = 0.155f + p * 0.23f;
                    float pdx = fx - pcx;
                    if (Mathf.Abs(pdx) <= 0.042f && fy >= 0.04f && fy <= 0.34f) {
                        c = woodM;
                        if (pdx < -0.016f) c = woodL; else if (pdx > 0.016f) c = woodD;
                        // darker wet base where the pile meets the water
                        if (fy <= 0.12f) c = woodD;
                    }
                }

                // ---- JETTY DECK: white-body planks spanning the width (fy 0.30 .. 0.50) ----
                if (fx >= 0.04f && fx <= 0.96f && fy >= 0.30f && fy <= 0.50f) {
                    c = bodyHi;
                    float t = (fy - 0.30f) / 0.20f;             // 0 underside .. 1 top
                    if (t < 0.20f) c = bodyShade;               // shaded underside
                    // baked plank seams running across the deck (kept dark)
                    if (Frac(fx * 9f) < 0.085f) c = woodD;
                    // baked front-edge fascia board along the bottom of the deck
                    if (fy <= 0.345f) { c = woodM; if (Frac(fx * 9f) < 0.085f) c = woodD; }
                }

                // ---- BOLLARD posts standing on the deck (white body, two of them) ----
                float b1 = fx - 0.30f, b2 = fx - 0.70f;
                bool boll1 = Mathf.Abs(b1) <= 0.05f && fy >= 0.50f && fy <= 0.70f;
                bool boll2 = Mathf.Abs(b2) <= 0.05f && fy >= 0.50f && fy <= 0.64f;
                if (boll1 || boll2) {
                    float bdx = boll1 ? b1 : b2;
                    c = bodyHi;
                    if (bdx > 0.018f) c = bodyShade;
                    // rounded cap on top (slightly wider, lit on the upper-left)
                    float capY = boll1 ? 0.665f : 0.605f;
                    if (fy >= capY && Mathf.Abs(bdx) <= 0.062f) {
                        c = (bdx > 0.018f) ? bodyShade : bodyHi;
                    }
                }

                // ---- ROPE COIL wrapped around the first bollard (baked tan) ----
                for (int r = 0; r < 3; r++) {
                    float ry = 0.520f + r * 0.034f;
                    if (Mathf.Abs(fx - 0.30f) <= 0.060f && fy >= ry && fy <= ry + 0.018f) {
                        c = ropeM;
                        if (fx < 0.285f) c = ropeL; else if (fx > 0.315f) c = ropeD;
                    }
                }
                // slung rope line sagging from bollard to bollard (dips DOWN at centre)
                if (fx >= 0.30f && fx <= 0.70f) {
                    float t2 = (fx - 0.30f) / 0.40f;            // 0..1 across the span
                    float sag = 0.620f - 0.075f * Mathf.Sin(t2 * 3.14159f);
                    if (fy >= sag && fy <= sag + 0.016f) {
                        c = ropeM; if (fx < 0.50f) c = ropeL; else c = ropeD;
                    }
                }

                // ---- HOIST / CRANE at the right end (baked timber mast + boom + line) ----
                // vertical mast
                if (Mathf.Abs(fx - 0.86f) <= 0.032f && fy >= 0.50f && fy <= 0.90f) {
                    c = woodM; if (fx < 0.846f) c = woodL; else if (fx > 0.874f) c = woodD;
                }
                // diagonal boom from the top of the mast reaching out to the left over the water
                float boomY = 0.885f - (0.86f - fx) * 0.55f;
                if (fx >= 0.58f && fx <= 0.88f && fy <= boomY && fy >= boomY - 0.05f) {
                    c = woodM; if (fy >= boomY - 0.02f) c = woodL; else c = woodD;
                }
                // hanging hoist line + hook block below the boom tip
                if (Mathf.Abs(fx - 0.60f) <= 0.008f && fy >= 0.50f && fy <= 0.665f) c = woodD;
                if (Mathf.Abs(fx - 0.60f) <= 0.026f && fy >= 0.475f && fy <= 0.515f) {
                    c = woodM; if (fx < 0.60f) c = woodL; else c = woodD;
                }

                px[y * s + x] = c;
            }

            // ---- DARK OUTLINE pass: only fully-opaque pixels count as silhouette ----
            var outPx = new Color[s * s];
            for (int i = 0; i < px.Length; i++) outPx[i] = px[i];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                if (px[y * s + x].a > 0.05f) continue;
                bool near = false;
                for (int dy = -1; dy <= 1 && !near; dy++) for (int dx = -1; dx <= 1; dx++) {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                    if (px[ny * s + nx].a > 0.9f) { near = true; break; }
                }
                if (near) outPx[y * s + x] = outline;
            }
            px = outPx;

            tex.SetPixels(px); tex.Apply();
            _harbourDock = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _harbourDock;
        }
    }
}
