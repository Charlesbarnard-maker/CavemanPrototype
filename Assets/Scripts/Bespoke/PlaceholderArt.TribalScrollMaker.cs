// Bespoke procedural art for "Scroll Maker" (Age 1). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _tribalScrollMaker;
        public static Sprite TribalScrollMaker()
        {
            if (_tribalScrollMaker != null) return _tribalScrollMaker;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            var bodyHi    = new Color(1f, 1f, 1f, 1f);
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);
            // wood (Tribal warm brown) - baked
            var woodD     = new Color(0.30f, 0.18f, 0.10f, 1f);
            var woodM     = new Color(0.44f, 0.28f, 0.15f, 1f);
            var woodL     = new Color(0.60f, 0.42f, 0.24f, 1f);
            // parchment (cream) - baked
            var parchD    = new Color(0.70f, 0.62f, 0.44f, 1f);
            var parchM    = new Color(0.90f, 0.84f, 0.66f, 1f);
            var parchL    = new Color(0.97f, 0.93f, 0.78f, 1f);
            // baked script lines on the scroll
            var scriptC   = new Color(0.26f, 0.20f, 0.14f, 1f);
            // inkpot + ink - baked dark
            var inkpotD   = new Color(0.12f, 0.12f, 0.16f, 1f);
            var inkpotM   = new Color(0.20f, 0.20f, 0.26f, 1f);
            var inkpotL   = new Color(0.30f, 0.30f, 0.38f, 1f);
            var inkC      = new Color(0.08f, 0.09f, 0.18f, 1f);
            // reed pen - baked
            var penD      = new Color(0.52f, 0.38f, 0.20f, 1f);
            var penM      = new Color(0.70f, 0.54f, 0.30f, 1f);
            var penTip    = new Color(0.14f, 0.13f, 0.18f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ---- DESK LEGS (baked wood, light upper-left / dark lower-right) ----
                // front-left leg
                if (fx >= 0.17f && fx <= 0.26f && fy >= 0.07f && fy <= 0.52f) {
                    c = woodM; if (fx <= 0.20f) c = woodL; else if (fx >= 0.235f) c = woodD;
                }
                // front-right leg
                if (fx >= 0.74f && fx <= 0.83f && fy >= 0.07f && fy <= 0.52f) {
                    c = woodM; if (fx <= 0.77f) c = woodL; else if (fx >= 0.805f) c = woodD;
                }
                // cross brace between legs
                if (fx >= 0.17f && fx <= 0.83f && fy >= 0.24f && fy <= 0.31f) {
                    c = woodM; if (fy >= 0.285f) c = woodD; else if (fy <= 0.265f) c = woodL;
                }

                // ---- SLANTED DESK TOP (white structural body, tinted at runtime) ----
                // a slab rising to the left; top surface line, raised to fill the tile
                float deskTop = 0.80f - dxc * 0.20f;            // higher on the left
                float deskBot = deskTop - 0.115f;               // thickness of the slab
                if (fx >= 0.12f && fx <= 0.88f && fy <= deskTop && fy >= deskBot) {
                    float t = (deskTop - fy) / 0.115f;          // 0 at top edge .. 1 at under-edge
                    c = (t < 0.45f) ? bodyHi : bodyShade;       // lit upper face, shaded under-edge
                    if (dxc > 0.18f) c = bodyShade;             // right side falls into shadow
                }
                // desk front skirt (vertical band under the slab, white body)
                float skirtBot = deskBot - 0.085f;
                if (fx >= 0.15f && fx <= 0.85f && fy <= deskBot && fy >= skirtBot) {
                    c = bodyShade; if (dxc < -0.10f) c = bodyHi;
                }

                // ---- PARCHMENT SCROLL lying on the slope (baked cream) ----
                float sheetTop = deskTop - 0.018f;              // sits just on the surface
                float sheetBot = sheetTop - 0.095f;
                if (fx >= 0.22f && fx <= 0.74f && fy <= sheetTop && fy >= sheetBot) {
                    float t = (sheetTop - fy) / 0.095f;          // 0 at top lip .. 1 at lower edge
                    c = parchM;
                    if (t < 0.16f) c = parchL;                   // top lip catches light
                    else if (t > 0.84f) c = parchD;              // lower edge shaded
                    if (fx <= 0.255f || fx >= 0.715f) c = parchD; // side edges
                    // baked dark script lines (two dashed rows of strokes)
                    if ((t > 0.32f && t < 0.44f) || (t > 0.58f && t < 0.70f)) {
                        if (Frac(fx * 22f) < 0.62f && fx >= 0.28f && fx <= 0.68f) c = scriptC;
                    }
                }
                // rolled scroll bulge at the right end of the sheet
                float rbx = fx - 0.72f, rby = fy - (sheetTop - 0.045f);
                if (Disc(rbx, rby, 0.060f)) {
                    float r2 = rbx * rbx + rby * rby;
                    c = parchM;
                    if (r2 <= 0.024f * 0.024f) c = parchL; else if (r2 >= 0.046f * 0.046f) c = parchD;
                    if (rbx < -0.02f && rby > 0.01f) c = parchL; // upper-left rim light
                }

                // ---- INKPOT (baked dark vessel) with ink, on the high-left of the slope ----
                float potBase = deskTop + 0.002f;               // sits on the surface (left, high)
                if (fx >= 0.26f && fx <= 0.345f && fy >= potBase && fy <= potBase + 0.085f) {
                    c = inkpotM;
                    if (fx <= 0.285f) c = inkpotL; else if (fx >= 0.32f) c = inkpotD;
                    if (fy >= potBase + 0.068f) c = inkC;        // ink surface at the rim
                }

                // ---- REED PEN angled up-right out of the inkpot (baked) ----
                float penX0 = 0.305f, penY0 = potBase + 0.066f;
                float pdx = fx - penX0, pdy = fy - penY0;
                float along = pdx * 0.80f + pdy * 0.60f;         // projection along the shaft
                float perp  = -pdx * 0.60f + pdy * 0.80f;        // distance from the shaft axis
                if (along >= 0f && along <= 0.30f && Mathf.Abs(perp) <= 0.020f) {
                    c = penM; if (perp > 0.004f) c = penD;       // shaded lower-right side
                    if (along >= 0.26f) c = penTip;              // dark nib at the far tip
                    if (along <= 0.03f) c = penTip;              // inked tip near the pot
                }

                // ---- RACK OF ROLLED SCROLL TUBES (right side, seen end-on) ----
                float t1x = fx - 0.795f, t1y = fy - 0.235f;      // lower tube
                float t2x = fx - 0.795f, t2y = fy - 0.345f;      // upper tube
                bool tubeA = Disc(t1x, t1y, 0.058f);
                bool tubeB = Disc(t2x, t2y, 0.058f);
                if (tubeA || tubeB) {
                    float cx = tubeA ? t1x : t2x;
                    float cy = tubeA ? t1y : t2y;
                    float r2 = cx * cx + cy * cy;
                    if (r2 <= 0.020f * 0.020f) c = parchD;       // dark hole centre
                    else if (r2 <= 0.038f * 0.038f) c = parchL;  // inner ring (lit)
                    else c = parchD;                             // outer rim shaded
                    if (cx < -0.02f && cy > 0.02f) c = parchL;   // upper-left rim light
                }
                // small wooden rack lip holding the tubes (just under the lower tube)
                if (fx >= 0.715f && fx <= 0.875f && fy >= 0.155f && fy <= 0.195f) {
                    c = woodM; if (fy >= 0.182f) c = woodD; else if (fy <= 0.168f) c = woodL;
                }

                // ---- translucent ground shadow (NOT part of the silhouette) ----
                if (fy < 0.07f && fy >= 0.045f && fx >= 0.14f && fx <= 0.88f && c == Clear)
                    c = new Color(0.12f, 0.10f, 0.08f, 0.45f);

                px[y * s + x] = c;
            }

            // ---- DARK OUTLINE pass (only fully-opaque pixels count as silhouette) ----
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
            _tribalScrollMaker = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _tribalScrollMaker;
        }
    }
}
