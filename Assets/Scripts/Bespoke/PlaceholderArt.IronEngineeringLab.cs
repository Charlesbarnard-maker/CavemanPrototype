// Bespoke procedural art for "Engineering Lab" (Age 3). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _ironEngineeringLab;
        public static Sprite IronEngineeringLab()
        {
            if (_ironEngineeringLab != null) return _ironEngineeringLab;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            var body      = Color.white;                                  // structural workbench, tinted at runtime
            var bodyHi    = new Color(1f, 1f, 1f, 1f);
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);

            // blueprint sheet (baked blue, light ruling lines)
            var blueD = new Color(0.16f, 0.28f, 0.50f, 1f);
            var blueM = new Color(0.20f, 0.34f, 0.58f, 1f);
            var blueL = new Color(0.40f, 0.56f, 0.82f, 1f);
            var line  = new Color(0.62f, 0.74f, 0.92f, 1f);

            // brass gears / mechanism (baked, darkish & saturated)
            var brassD = new Color(0.50f, 0.36f, 0.15f, 1f);
            var brassM = new Color(0.70f, 0.50f, 0.22f, 1f);
            var brassL = new Color(0.88f, 0.66f, 0.30f, 1f);

            // cool iron metal (lamp arm, calipers, set-square edge)
            var ironD = new Color(0.22f, 0.24f, 0.28f, 1f);
            var ironM = new Color(0.38f, 0.41f, 0.46f, 1f);
            var ironL = new Color(0.58f, 0.62f, 0.68f, 1f);

            // lamp glow (warm, slightly translucent)
            var glow  = new Color(1f, 0.86f, 0.42f, 0.55f);
            var lampC = new Color(1f, 0.92f, 0.60f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ---- BLUEPRINT SHEET pinned up behind the bench (upper area) ----
                // Drawn FIRST so the bench/lamp/gears overlay it. Sits high (fy 0.52..0.93).
                if (fx >= 0.14f && fx <= 0.86f && fy >= 0.52f && fy <= 0.93f) {
                    float tx = (fx - 0.14f) / 0.72f;   // 0..1 left->right
                    float ty = (fy - 0.52f) / 0.41f;   // 0..1 bottom->top
                    c = blueM;
                    if (tx < 0.32f && ty > 0.60f) c = blueL;          // upper-left lit corner
                    else if (tx > 0.72f || ty < 0.16f) c = blueD;     // lower-right shade
                    // light ruling grid lines
                    if (Frac(fx * 9f) < 0.085f || Frac(fy * 9f) < 0.085f) c = line;
                    // a bolder diagonal "drawing" stroke across the sheet
                    if (Mathf.Abs((fy - 0.58f) - (fx - 0.18f) * 0.5f) < 0.018f && fx <= 0.7f) c = line;
                    // pin dots in the top corners
                    if (Disc(fx - 0.20f, fy - 0.90f, 0.024f) || Disc(fx - 0.80f, fy - 0.90f, 0.024f)) c = ironL;
                }

                // ---- WORKBENCH BODY (white structural mass, lower half) ----
                // table top slab (fy 0.34..0.46)
                if (fx >= 0.07f && fx <= 0.93f && fy >= 0.34f && fy <= 0.46f) {
                    c = body;
                    if (fy >= 0.42f) c = bodyHi; else if (fy <= 0.37f) c = bodyShade;
                }
                // two legs
                if (fy >= 0.06f && fy < 0.34f) {
                    if (fx >= 0.13f && fx <= 0.24f) c = (fx <= 0.16f) ? bodyHi : (fx >= 0.21f ? bodyShade : body);
                    if (fx >= 0.76f && fx <= 0.87f) c = (fx <= 0.79f) ? bodyHi : (fx >= 0.84f ? bodyShade : body);
                }
                // lower cross-brace
                if (fx >= 0.13f && fx <= 0.87f && fy >= 0.13f && fy <= 0.18f) {
                    c = body; if (fy >= 0.165f) c = bodyShade; else c = bodyHi;
                }

                // ---- HALF-BUILT MECHANISM: two interlocking brass gears resting ON the bench top ----
                // big gear (centre at fy 0.575, sits on the table surface at 0.46)
                float g1x = fx - 0.37f, g1y = fy - 0.585f;
                float g1r = Mathf.Sqrt(g1x * g1x + g1y * g1y);
                float a1 = Mathf.Atan2(g1y, g1x);
                float tooth1 = 0.120f + 0.024f * Mathf.Cos(a1 * 9f);
                if (g1r <= tooth1) {
                    c = brassM;
                    if (g1r <= 0.030f) c = brassD;                         // hub hole
                    else if (g1r <= 0.046f) c = brassL;                    // hub ring
                    else if (g1x - g1y < -0.015f) c = brassL;              // upper-left lit
                    else if (g1x - g1y > 0.04f) c = brassD;                // lower-right shade
                    if (g1r > 0.096f && Frac(a1 * 1.432f) < 0.5f) c = brassD; // tooth gaps shadowed
                }
                // small gear, interlocking up-right
                float g2x = fx - 0.585f, g2y = fy - 0.555f;
                float g2r = Mathf.Sqrt(g2x * g2x + g2y * g2y);
                float a2 = Mathf.Atan2(g2y, g2x);
                float tooth2 = 0.076f + 0.020f * Mathf.Cos(a2 * 7f + 0.4f);
                if (g2r <= tooth2) {
                    c = brassM;
                    if (g2r <= 0.018f) c = brassD;
                    else if (g2r <= 0.030f) c = brassL;
                    else if (g2x - g2y < -0.01f) c = brassL;
                    else if (g2x - g2y > 0.03f) c = brassD;
                    if (g2r > 0.056f && Frac(a2 * 1.114f) < 0.5f) c = brassD;
                }

                // ---- DRAFTING LAMP: articulated arm rising from the bench's right back ----
                // base clamp on the bench top
                if (fx >= 0.74f && fx <= 0.84f && fy >= 0.46f && fy <= 0.51f) {
                    c = ironM; if (fx <= 0.77f) c = ironL; else if (fx >= 0.81f) c = ironD;
                }
                // lower arm (diagonal up-left): rises from clamp (0.79,0.51) toward elbow (0.60,0.70)
                if (Mathf.Abs((fy - 0.51f) - (-(fx - 0.79f)) * 1.0f) < 0.030f && fx >= 0.59f && fx <= 0.80f && fy >= 0.50f && fy <= 0.71f) {
                    c = ironM; if (fx < 0.70f) c = ironL; else c = ironD;
                }
                // elbow joint
                if (Disc(fx - 0.60f, fy - 0.705f, 0.038f)) { c = ironM; if (dxc < 0.10f && fy > 0.705f) c = ironL; else if (fy < 0.705f) c = ironD; }
                // upper arm (diagonal up-right toward head): from elbow (0.60,0.71) to head (0.78,0.86)
                if (Mathf.Abs((fy - 0.71f) - (fx - 0.60f) * 0.83f) < 0.028f && fx >= 0.59f && fx <= 0.79f && fy >= 0.70f && fy <= 0.87f) {
                    c = ironM; if (fx < 0.69f) c = ironL; else c = ironD;
                }
                // lamp head (shade) at top-right, casting glow
                float hx = fx - 0.78f, hy = fy - 0.875f;
                if (Disc(hx, hy, 0.085f) && fy <= 0.95f) {
                    c = ironM;
                    float rr = hx * hx + hy * hy;
                    if (rr <= 0.042f * 0.042f) c = lampC;           // bulb
                    else if (hx - hy < -0.02f) c = ironL; else c = ironD;
                }

                // ---- SET-SQUARE (right-angled triangle, leaning on the bench, left) ----
                // vertical left edge + hypotenuse, sits on the bench top
                if (fx >= 0.10f && fx <= 0.30f && fy >= 0.46f && fy <= 0.66f) {
                    float u = (fx - 0.10f) / 0.20f;     // 0..1 left->right
                    float v = (fy - 0.46f) / 0.20f;     // 0..1 bottom->top
                    float edge = 1f - u;                 // hypotenuse falls right as it goes up
                    if (v <= edge) {
                        bool inner = (v <= edge - 0.26f) && (fx >= 0.145f) && (v >= 0.18f);
                        if (inner) c = Clear;            // hollow interior
                        else { c = ironM; if (fx <= 0.135f) c = ironL; else if (v <= 0.10f) c = ironD; }
                    }
                }

                // ---- CALIPERS lying across the bench front (thin iron beam + two jaws) ----
                if (fy >= 0.475f && fy <= 0.505f && fx >= 0.30f && fx <= 0.62f) {
                    c = ironM; if (fy >= 0.495f) c = ironL; else c = ironD;
                }
                if (fx >= 0.30f && fx <= 0.335f && fy >= 0.475f && fy <= 0.56f) { c = ironM; if (fx <= 0.315f) c = ironL; } // left jaw
                if (fx >= 0.585f && fx <= 0.62f && fy >= 0.475f && fy <= 0.55f) { c = ironM; if (fx >= 0.605f) c = ironD; } // right jaw

                // ---- SCATTERED BOLTS / COGS along the bench front edge (fy ~0.40) ----
                // a small cog at bench-front left
                float cgx = fx - 0.22f, cgy = fy - 0.405f;
                float cgr = Mathf.Sqrt(cgx * cgx + cgy * cgy);
                float ca = Mathf.Atan2(cgy, cgx);
                if (cgr <= 0.042f + 0.012f * Mathf.Cos(ca * 6f)) {
                    c = brassM; if (cgr <= 0.015f) c = brassD; else if (cgx - cgy < 0f) c = brassL; else c = brassD;
                }
                // a hex bolt to the right
                if (Disc(fx - 0.74f, fy - 0.405f, 0.032f)) {
                    c = brassM; float bxr = (fx - 0.74f) - (fy - 0.405f);
                    if (bxr < -0.01f) c = brassL; else if (bxr > 0.012f) c = brassD;
                    if (Disc(fx - 0.74f, fy - 0.405f, 0.013f)) c = brassD; // socket
                }

                // ---- warm glow pool under the lamp head (translucent, only on empty pixels) ----
                if (c == Clear && Disc(fx - 0.74f, fy - 0.80f, 0.15f) && fy < 0.90f) c = glow;

                // ---- translucent ground shadow (NOT part of the silhouette) ----
                if (fy < 0.06f && fy >= 0.03f && fx >= 0.12f && fx <= 0.88f && c == Clear)
                    c = new Color(0.12f, 0.10f, 0.08f, 0.45f);

                px[y * s + x] = c;
            }

            // ---- DARK OUTLINE PASS (skip glow/translucent: only opaque pixels are silhouette) ----
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
            _ironEngineeringLab = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _ironEngineeringLab;
        }
    }
}
