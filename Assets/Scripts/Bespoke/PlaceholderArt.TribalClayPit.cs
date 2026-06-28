// Bespoke procedural art for "Clay Pit" (Age 1). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _tribalClayPit;
        public static Sprite TribalClayPit()
        {
            if (_tribalClayPit != null) return _tribalClayPit;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            // RED CLAY terraced walls (3 dug bands, darker lower)
            var clayD     = new Color(0.40f, 0.24f, 0.16f, 1f);
            var clayM     = new Color(0.52f, 0.33f, 0.23f, 1f);
            var clayL     = new Color(0.68f, 0.45f, 0.33f, 1f);
            var clayLt    = new Color(0.80f, 0.57f, 0.43f, 1f); // brightest rim
            // wet clay floor at pit bottom (darker, damp)
            var pitD      = new Color(0.30f, 0.17f, 0.11f, 1f);
            var pitM      = new Color(0.38f, 0.22f, 0.15f, 1f);
            // wet heaped clay mound (saturated, with sheen)
            var moundD    = new Color(0.44f, 0.25f, 0.17f, 1f);
            var moundM    = new Color(0.58f, 0.35f, 0.24f, 1f);
            var moundL    = new Color(0.74f, 0.50f, 0.36f, 1f);
            var sheen     = new Color(0.90f, 0.70f, 0.56f, 1f); // wet sheen highlight
            // wood for ladder rails/rungs and spade handle
            var woodD     = new Color(0.27f, 0.17f, 0.09f, 1f);
            var woodM     = new Color(0.40f, 0.26f, 0.14f, 1f);
            var woodL     = new Color(0.55f, 0.38f, 0.22f, 1f);
            // clay-smeared spade blade (smeared red clay over dark wood)
            var spadeD    = new Color(0.34f, 0.22f, 0.16f, 1f);
            var spadeM    = new Color(0.50f, 0.32f, 0.24f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ============ TERRACED PIT (concentric stepped bowl) ============
                // radial distance from pit centre, elliptical (wider than tall so it reads as a dug bowl)
                float pcx = dxc;
                float pcy = (fy - 0.50f) * 1.18f;
                float pr  = Mathf.Sqrt(pcx * pcx + pcy * pcy);

                bool inPit = pr <= 0.46f;
                if (inPit) {
                    // terraces: outer rim -> floor. 3 clay bands stepping inward, each darker as it
                    // goes deeper. Shade by facing so upper-left walls catch light, lower-right fall dark.
                    float lightFace = (-pcx - pcy); // >0 = upper-left facing
                    if (pr > 0.39f) {
                        // top terrace band (rim of the pit, brightest red clay)
                        c = clayL;
                        if (lightFace > 0.06f) c = clayLt; else if (lightFace < -0.10f) c = clayM;
                        if (Frac(pr * 30f) < 0.16f) c = clayM; // dug step seam
                    } else if (pr > 0.30f) {
                        // second terrace band
                        c = clayM;
                        if (lightFace > 0.05f) c = clayL; else if (lightFace < -0.10f) c = clayD;
                        if (Frac(pr * 30f) < 0.16f) c = clayD;
                    } else if (pr > 0.20f) {
                        // third (deepest) terrace band
                        c = clayD;
                        if (lightFace > 0.04f) c = clayM; else if (lightFace < -0.12f) c = pitD;
                        if (Frac(pr * 30f) < 0.16f) c = pitD;
                    } else {
                        // wet clay floor of the pit
                        c = pitM;
                        if (lightFace < -0.04f) c = pitD;
                        // damp mottling
                        if (Frac(fx * 11f + fy * 7f) < 0.14f) c = pitD;
                    }
                }

                // ============ DIGGING LADDER leaning into the pit ============
                // Two thin parallel rails running diagonally from the lower-left floor up to the
                // upper-right rim, with rungs between them. Lean: centre-x grows with height.
                // Draw BEFORE the mound so the wet heap sits in front of the ladder foot.
                float ladT = (fy - 0.20f) / 0.62f;            // 0 at foot (fy 0.20) -> 1 at top (fy 0.82)
                if (ladT >= 0f && ladT <= 1f) {
                    float ladCx   = 0.34f + ladT * 0.30f;     // centre-x of ladder, leaning up-right
                    float ladHalf = 0.085f;                   // half rail-to-rail span
                    float dCentre = fx - ladCx;
                    float dl = dCentre + ladHalf;             // dist from left rail
                    float dr = dCentre - ladHalf;             // dist from right rail
                    bool leftRail  = Mathf.Abs(dl) <= 0.020f;
                    bool rightRail = Mathf.Abs(dr) <= 0.020f;
                    // rungs: horizontal bars every ~0.135 in fy, only between the rails
                    bool rung = Frac(ladT * 6f) < 0.22f && Mathf.Abs(dCentre) <= ladHalf;
                    if (leftRail) c = woodL;                  // left rail catches the light
                    else if (rightRail) c = woodD;           // right rail in shadow
                    else if (rung) c = woodM;
                }

                // ============ HEAPED WET CLAY MOUND (front-left of pit) ============
                // Rounded heap sitting on the near rim. moundTop is the heap's upper profile at this x.
                float mx = fx - 0.30f, my = fy - 0.26f;
                float moundTop = 0.17f - Mathf.Abs(mx) * 0.62f; // rounded heap profile (peak at mx=0)
                if (Mathf.Abs(mx) <= 0.235f && my >= -0.05f && my <= moundTop) {
                    c = moundM;
                    // light upper-left, dark lower-right
                    if (mx < -0.02f && my > 0.04f) c = moundL; else if (mx > 0.04f || my < -0.01f) c = moundD;
                    // wet sheen streak on the upper-left shoulder of the heap
                    if (mx > -0.12f && mx < -0.02f && my > moundTop - 0.06f) c = sheen;
                }

                // ============ CLAY-SMEARED SPADE stuck in the mound ============
                // handle: a wooden shaft angled up-right out of the mound top
                float shCx = 0.30f + (fy - 0.30f) * 0.30f;   // shaft centre-x, leaning up-right
                if (fy >= 0.30f && fy <= 0.66f && Mathf.Abs(fx - shCx) <= 0.024f) {
                    c = woodM;
                    if (fx < shCx - 0.006f) c = woodL; else if (fx > shCx + 0.008f) c = woodD;
                }
                // T-grip across the top of the handle
                float gripY = 0.66f;
                float gripCx = 0.30f + (gripY - 0.30f) * 0.30f;
                if (fy >= gripY - 0.01f && fy <= gripY + 0.035f && Mathf.Abs(fx - gripCx) <= 0.075f) {
                    c = woodM;
                    if (fy > gripY + 0.013f) c = woodL; else c = woodD;
                }
                // spade blade: clay-smeared, buried where the shaft meets the mound
                float blx = fx - 0.305f, bly = fy - 0.30f;
                if (Mathf.Abs(blx) <= 0.060f && bly <= 0.05f && bly >= -0.055f) {
                    c = spadeM;
                    if (blx < -0.012f) c = spadeD; // shadow side of the blade
                    // clay smear streaks dragged across the blade
                    if (Frac(fx * 22f) < 0.30f) c = moundD;
                }

                // ============ raised earth lip around the pit rim ============
                if (!inPit && pr > 0.46f && pr <= 0.49f) {
                    float lf = (-pcx - pcy);
                    c = (lf > 0f) ? clayLt : clayM;
                }

                // translucent ground shadow (NOT part of silhouette; lower-right of the lip)
                if (c == Clear && pr > 0.49f && pr <= 0.535f && (pcx + pcy) > 0f)
                    c = new Color(0.10f, 0.07f, 0.05f, 0.40f);

                px[y * s + x] = c;
            }

            // ============ DARK OUTLINE PASS ============
            // 1px dark halo around the whole silhouette. Only fully-opaque pixels count as the
            // silhouette, so fire/glow and the translucent ground shadow are skipped.
            var outPx = new Color[s * s];
            System.Array.Copy(px, outPx, px.Length);
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
            _tribalClayPit = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _tribalClayPit;
        }
    }
}
