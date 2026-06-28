// Bespoke procedural art for "Potter" (Age 2). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _bronzePotter;
        public static Sprite BronzePotter()
        {
            if (_bronzePotter != null) return _bronzePotter;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline = new Color(0.09f, 0.07f, 0.05f, 1f);
            // wood (workbench / wheel post / shelf / stool)
            var woodD   = new Color(0.28f, 0.17f, 0.09f, 1f);
            var woodM   = new Color(0.40f, 0.25f, 0.13f, 1f);
            var woodL   = new Color(0.56f, 0.38f, 0.21f, 1f);
            // stone/cool-metal wheel head (Bronze cool-metal cue)
            var wheelD  = new Color(0.26f, 0.30f, 0.36f, 1f);
            var wheelM  = new Color(0.40f, 0.46f, 0.54f, 1f);
            var wheelL  = new Color(0.56f, 0.64f, 0.74f, 1f);
            // finished terracotta clay pots (Bronze terracotta family)
            var clayD   = new Color(0.46f, 0.26f, 0.20f, 1f);
            var clayM   = new Color(0.66f, 0.40f, 0.34f, 1f);
            var clayL   = new Color(0.80f, 0.52f, 0.42f, 1f);
            // raw wet clay on the wheel (darker, cooler terracotta)
            var wetD    = new Color(0.40f, 0.22f, 0.17f, 1f);
            var wetM    = new Color(0.58f, 0.34f, 0.28f, 1f);
            var wetL    = new Color(0.72f, 0.46f, 0.37f, 1f);
            var body      = Color.white;                            // structural body, tinted at runtime
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ---- WHITE-BODY workbench slab (the mass under the wheel) ----
                if (fx >= 0.18f && fx <= 0.66f && fy >= 0.18f && fy <= 0.30f) {
                    // lit on the upper-left, shaded toward the lower-right
                    c = (dxc - (fy - 0.24f) < 0.04f) ? body : bodyShade;
                    if (fy >= 0.285f) c = bodyShade;                // shaded front lip of the slab
                }
                // bench legs (front pair), lit left / shaded right
                if (fy >= 0.045f && fy < 0.18f) {
                    if (fx >= 0.21f && fx <= 0.28f) c = body;
                    else if (fx >= 0.56f && fx <= 0.63f) c = bodyShade;
                }

                // ---- WOODEN WHEEL POST (short, centre under the wheel head) ----
                if (fx >= 0.405f && fx <= 0.485f && fy >= 0.30f && fy <= 0.50f) {
                    c = woodM;
                    if (fx <= 0.43f) c = woodL; else if (fx >= 0.46f) c = woodD;
                }

                // ---- WHEEL HEAD (horizontal disc, seen as a flat ellipse) ----
                // ellipse centred at (0.445, 0.52): wide in x, thin in y
                float wxe = (fx - 0.445f) / 0.205f;
                float wye = (fy - 0.52f) / 0.060f;
                if (wxe * wxe + wye * wye <= 1f) {
                    c = wheelM;
                    if (fy >= 0.52f) c = (wxe < 0.05f) ? wheelL : wheelM;  // lit top face, left side brightest
                    else c = wheelD;                                       // shaded front lip
                    if (Mathf.Abs(fx - 0.445f) <= 0.016f && fy >= 0.52f) c = wheelD; // centre seam on the top face
                }

                // ---- HALF-FORMED POT rising on the wheel (centre) ----
                // belly widest near mid-height, narrowing to a small mouth on top
                float pcx  = fx - 0.445f;
                float potR = 0.090f - Mathf.Abs(fy - 0.625f) * 0.30f;     // bulged belly
                if (fy >= 0.555f && fy <= 0.715f && Mathf.Abs(pcx) <= potR && potR > 0f) {
                    c = wetM;
                    if (pcx < -0.020f) c = wetL;                          // lit left flank
                    else if (pcx > 0.025f) c = wetD;                      // shaded right flank
                    if (fy >= 0.695f) c = wetL;                           // thrown rim highlight
                }
                // open dark mouth at the very top of the wet pot
                if (fy >= 0.700f && fy <= 0.715f && Mathf.Abs(pcx) <= 0.045f) c = wetD;

                // ---- WOODEN SHELF on the RIGHT holding finished pots ----
                // two horizontal planks
                if (fx >= 0.66f && fx <= 0.96f) {
                    if (fy >= 0.40f && fy <= 0.45f) c = (fy >= 0.435f) ? woodD : woodL;
                    if (fy >= 0.62f && fy <= 0.67f) c = (fy >= 0.655f) ? woodD : woodL;
                }
                // shelf uprights (left and right edges of the shelf), lit left / shaded right
                if (fy >= 0.40f && fy <= 0.71f) {
                    if (fx >= 0.66f && fx <= 0.705f) c = (fx >= 0.685f) ? woodM : woodL;
                    else if (fx >= 0.915f && fx <= 0.96f) c = (fx >= 0.94f) ? woodD : woodM;
                }

                // finished pots standing ON the planks (terracotta) ----
                // lower plank: two squat pots, flat-bottomed (sit on the plank top at fy=0.45)
                float pa = fx - 0.775f, pb = fx - 0.875f;
                if (fy >= 0.45f && Disc(pa, fy - 0.49f, 0.045f)) {
                    c = clayM; if (pa < -0.012f) c = clayL; else if (pa > 0.016f) c = clayD;
                    if (fy >= 0.525f) c = clayL;                          // little rim
                }
                if (fy >= 0.45f && Disc(pb, fy - 0.487f, 0.038f)) {
                    c = clayM; if (pb < -0.010f) c = clayL; else if (pb > 0.014f) c = clayD;
                    if (fy >= 0.518f) c = clayL;
                }
                // upper plank: one taller jar (sits on the plank top at fy=0.67)
                float jx   = fx - 0.815f;
                float jarR = 0.052f - Mathf.Abs(fy - 0.725f) * 0.20f;
                if (fy >= 0.67f && fy <= 0.78f && Mathf.Abs(jx) <= jarR && jarR > 0f) {
                    c = clayM; if (jx < -0.012f) c = clayL; else if (jx > 0.016f) c = clayD;
                    if (fy >= 0.770f) c = clayL;                          // jar rim
                }

                // ---- STOOL (small, lower-left) ----
                if (fx >= 0.055f && fx <= 0.195f && fy >= 0.105f && fy <= 0.145f) {  // seat
                    c = (fy >= 0.13f) ? woodD : woodL;
                }
                if (fy >= 0.035f && fy < 0.105f &&
                    ((fx >= 0.075f && fx <= 0.10f) || (fx >= 0.155f && fx <= 0.18f)))
                    c = woodD;                                            // stool legs

                // ---- CLAY SPLATTER specks on the base/ground ----
                if (c == Clear && fy >= 0.045f && fy <= 0.165f && fx >= 0.22f && fx <= 0.70f) {
                    float n = Frac(Mathf.Sin(fx * 47.3f + fy * 23.1f) * 91.7f);
                    if (n > 0.985f) c = clayL;
                    else if (n > 0.94f) c = clayM;
                }

                // ---- translucent ground shadow (NOT part of the silhouette) ----
                if (c == Clear && fy < 0.045f && fy >= 0.02f && fx >= 0.10f && fx <= 0.94f)
                    c = new Color(0.12f, 0.10f, 0.08f, 0.45f);

                px[y * s + x] = c;
            }

            // ---- DARK OUTLINE pass: only fully-opaque pixels count as silhouette ----
            var outPx = new Color[s * s];
            for (int i = 0; i < px.Length; i++) outPx[i] = px[i];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                if (px[y * s + x].a > 0.05f) continue;                    // skip filled + translucent-shadow handled below
                bool near = false;
                for (int dy = -1; dy <= 1 && !near; dy++) for (int dx = -1; dx <= 1; dx++) {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                    if (px[ny * s + nx].a > 0.9f) { near = true; break; } // only solid pixels seed the halo
                }
                if (near) outPx[y * s + x] = outline;
            }
            px = outPx;

            tex.SetPixels(px); tex.Apply();
            _bronzePotter = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _bronzePotter;
        }
    }
}
