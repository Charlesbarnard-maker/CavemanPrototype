// Bespoke procedural art for "Booster Pump" (Age 2). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _bronzeBoosterPump;
        public static Sprite BronzeBoosterPump()
        {
            if (_bronzeBoosterPump != null) return _bronzeBoosterPump;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            var body      = Color.white;                          // structural chamber (tinted at runtime)
            var bodyHi    = new Color(1f, 1f, 1f, 1f);            // lit upper-left faces
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);  // shadow lower-right faces

            // blue pipe (baked, darkish/saturated so it survives tint)
            var pipeD     = new Color(0.12f, 0.22f, 0.40f, 1f);
            var pipeM     = new Color(0.20f, 0.34f, 0.58f, 1f);
            var pipeL     = new Color(0.36f, 0.52f, 0.78f, 1f);
            // cool metal rims / bolts
            var metD      = new Color(0.20f, 0.30f, 0.44f, 1f);
            var metM      = new Color(0.32f, 0.46f, 0.66f, 1f);
            var metL      = new Color(0.52f, 0.66f, 0.86f, 1f);
            // brass gauge ring + face
            var brassD    = new Color(0.46f, 0.32f, 0.12f, 1f);
            var brassM    = new Color(0.70f, 0.50f, 0.20f, 1f);
            var brassL    = new Color(0.92f, 0.72f, 0.34f, 1f);
            var dialD     = new Color(0.60f, 0.58f, 0.50f, 1f);
            var dialM     = new Color(0.82f, 0.80f, 0.72f, 1f);
            var dialL     = new Color(0.94f, 0.93f, 0.88f, 1f);
            var needle    = new Color(0.62f, 0.14f, 0.12f, 1f);

            // chamber + pipe geometry (fy = 0 BOTTOM, fy = 1 TOP)
            const float pipeCY   = 0.36f;   // pipe centre-line height
            const float pipeHalf = 0.115f;  // pipe half-thickness
            const float chamCx   = 0.0f;    // chamber centre column (dxc)
            const float chamCy   = 0.46f;   // chamber centre height
            const float chamR    = 0.30f;   // chamber radius

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ---- HORIZONTAL PIPE running left->right through the centre ----
                float dpy = fy - pipeCY;
                if (Mathf.Abs(dpy) <= pipeHalf && fx >= 0.02f && fx <= 0.98f) {
                    float t = (dpy + pipeHalf) / (pipeHalf * 2f);          // 0 bottom .. 1 top
                    c = pipeM;
                    if (t > 0.62f) c = pipeL; else if (t < 0.30f) c = pipeD; // light on top, dark below
                    // pipe flange rims near the ends (cool metal collars)
                    if ((fx >= 0.05f && fx <= 0.135f) || (fx >= 0.865f && fx <= 0.95f)) {
                        c = metM; if (t > 0.60f) c = metL; else if (t < 0.32f) c = metD;
                    }
                }

                // ---- BULBOUS PUMP CHAMBER (white body) centred over the pipe ----
                float bx = dxc - chamCx, by = fy - chamCy;
                float br = Mathf.Sqrt(bx * bx + by * by);
                if (br <= chamR) {
                    c = body;
                    // form via two near-white bakes: lit upper-left, shaded lower-right
                    if (bx - by < -0.06f) c = bodyHi; else if (bx - by > 0.10f) c = bodyShade;
                    // dark seam ring round the housing rim so it reads as a round drum
                    if (br >= chamR - 0.028f) c = bodyShade;
                }

                // small domed cap on top of the chamber
                float capx = dxc, capy = fy - (chamCy + chamR + 0.02f);
                if (Disc(capx, capy, 0.085f)) {
                    c = body;
                    if (capx - capy < -0.02f) c = bodyHi; else if (capx - capy > 0.04f) c = bodyShade;
                }

                // ---- PRESSURE GAUGE on the front face of the chamber ----
                float gx = dxc, gy = fy - (chamCy + 0.02f);
                float gr = Mathf.Sqrt(gx * gx + gy * gy);
                if (gr <= 0.165f) {
                    if (gr >= 0.128f) {                            // brass bezel
                        c = brassM; if (gx - gy < -0.04f) c = brassL; else if (gx - gy > 0.05f) c = brassD;
                    } else {                                       // dial face
                        c = dialM;
                        if (gx < -0.02f && gy > 0.02f) c = dialL; else if (gy < -0.04f) c = dialD;
                        // tick marks around the rim
                        float gang = Mathf.Atan2(gy, gx);
                        if (gr >= 0.095f && Frac(gang * 1.91f) < 0.16f) c = dialD;
                        // NEEDLE pointing up-right
                        float nA = gang - 0.95f;
                        if (gr <= 0.108f && Mathf.Abs(nA) < 0.16f) c = needle;
                        // hub
                        if (gr <= 0.024f) c = metD;
                    }
                }

                // ---- BOLT HEADS flanking the gauge on the chamber face ----
                if (Disc(dxc - 0.205f, fy - chamCy, 0.034f) || Disc(dxc + 0.205f, fy - chamCy, 0.034f)) {
                    float ox = (dxc > 0f) ? dxc - 0.205f : dxc + 0.205f;
                    float oy = fy - chamCy;
                    c = metM; if (ox - oy < -0.01f) c = metL; else if (ox - oy > 0.012f) c = metD;
                }

                // ---- bolt heads on the pipe flanges ----
                if (Disc(fx - 0.095f, fy - pipeCY, 0.024f) || Disc(fx - 0.905f, fy - pipeCY, 0.024f)) {
                    float ox = (fx > 0.5f) ? fx - 0.905f : fx - 0.095f;
                    float oy = fy - pipeCY;
                    c = metM; if (ox - oy < -0.008f) c = metL; else if (ox - oy > 0.010f) c = metD;
                }

                // translucent ground shadow (NOT part of the silhouette)
                if (c == Clear && fy >= 0.045f && fy < 0.075f && fx >= 0.14f && fx <= 0.86f)
                    c = new Color(0.10f, 0.08f, 0.07f, 0.42f);

                px[y * s + x] = c;
            }

            // DARK OUTLINE pass: only fully-opaque pixels count as silhouette (translucent shadow ignored)
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
            _bronzeBoosterPump = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _bronzeBoosterPump;
        }
    }
}
