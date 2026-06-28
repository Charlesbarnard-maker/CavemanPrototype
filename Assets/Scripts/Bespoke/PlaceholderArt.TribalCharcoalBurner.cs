// Bespoke procedural art for "Charcoal Burner" (Age 1). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _tribalCharcoalBurner;
        public static Sprite TribalCharcoalBurner()
        {
            if (_tribalCharcoalBurner != null) return _tribalCharcoalBurner;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline = new Color(0.09f, 0.07f, 0.05f, 1f);

            // earthen sooty mound (3 shades, brown-black) - lit upper-left, dark lower-right
            var moundD  = new Color(0.12f, 0.09f, 0.07f, 1f);
            var moundM  = new Color(0.22f, 0.17f, 0.13f, 1f);
            var moundL  = new Color(0.34f, 0.27f, 0.20f, 1f);
            // turf flecks dappled over the mound (muddy green)
            var turfM   = new Color(0.26f, 0.29f, 0.15f, 1f);
            var turfD   = new Color(0.17f, 0.19f, 0.10f, 1f);
            // cut logs at the base (raw brown, 3 shades)
            var woodD   = new Color(0.27f, 0.17f, 0.09f, 1f);
            var woodM   = new Color(0.45f, 0.31f, 0.17f, 1f);
            var woodL   = new Color(0.60f, 0.44f, 0.26f, 1f);
            // rake pole (paler dry wood, 3 shades)
            var poleD   = new Color(0.34f, 0.24f, 0.13f, 1f);
            var poleM   = new Color(0.50f, 0.37f, 0.21f, 1f);
            var poleL   = new Color(0.64f, 0.50f, 0.30f, 1f);
            // fire / ember glowing through cracks (baked-dark/saturated, survives tint)
            var emberC  = new Color(1f, 0.45f, 0.10f, 1f);
            var emberL  = new Color(1f, 0.78f, 0.30f, 1f);
            var glow    = new Color(1f, 0.42f, 0.12f, 0.40f);
            // smoke rising from the vent (translucent)
            var smokeA  = new Color(0.70f, 0.70f, 0.72f, 0.78f);
            var smokeB  = new Color(0.82f, 0.82f, 0.84f, 0.60f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ---- EARTHEN MOUND (big turf-covered dome, fills most of the tile) ----
                float mdx = fx - 0.48f;                        // mound centre column
                float mound = 0.80f - (mdx * mdx) * 2.65f;     // parabolic dome top, apex ~0.80
                bool inMound = fy >= 0.085f && fy <= mound && Mathf.Abs(mdx) <= 0.43f;
                if (inMound) {
                    // 3-tone by surface normal: lit upper-left, dark lower-right + base contact
                    float shade = (-mdx) * 1.1f + (mound - fy) * 0.5f; // higher = more lit
                    c = moundM;
                    if (shade > 0.16f) c = moundL;
                    else if (shade < -0.06f) c = moundD;
                    if (fy < 0.16f) c = moundD;                 // dark ground-contact band

                    // soot streaking / darkening just below the apex around the vent
                    if (fy > mound - 0.16f && Mathf.Abs(mdx) < 0.20f) {
                        if (Frac(fx * 9f + fy * 5f) < 0.34f) c = moundD;
                    }

                    // turf flecks dappled over the surface (deterministic)
                    float t = Frac(fx * 13.7f + fy * 8.3f);
                    if (t < 0.15f && fy < mound - 0.03f && fy > 0.18f)
                        c = (Frac(fx * 5f + fy * 11f) < 0.5f) ? turfM : turfD;

                    // glowing EMBER cracks low on the mound (peeking through)
                    float crack = Frac(fx * 7.5f + fy * 3.1f);
                    if (fy < 0.32f && fy > 0.13f && crack < 0.12f && Mathf.Abs(mdx) < 0.34f)
                        c = (Frac(fx * 17f) < 0.5f) ? emberC : emberL;
                }

                // ---- VENT HOLE at the apex (dark socket with a hot throat) ----
                float vx = fx - 0.48f, vy = fy - 0.755f;
                if (Disc(vx, vy * 1.3f, 0.060f)) {
                    c = moundD;
                    if (Disc(vx, vy * 1.3f, 0.030f)) c = emberC; // hot glow in the throat
                }

                // ---- LOG STACK leaning at the base (left side, three log ends) ----
                float lbx = fx - 0.165f;
                bool lg1 = Disc(lbx, fy - 0.165f, 0.072f);
                bool lg2 = Disc(lbx + 0.052f, fy - 0.260f, 0.072f);
                bool lg3 = Disc(lbx - 0.034f, fy - 0.300f, 0.072f);
                if (lg1 || lg2 || lg3) {
                    float cx, cy;
                    if (lg3) { cx = lbx - 0.034f; cy = fy - 0.300f; }
                    else if (lg2) { cx = lbx + 0.052f; cy = fy - 0.260f; }
                    else { cx = lbx; cy = fy - 0.165f; }
                    float r2 = cx * cx + cy * cy;
                    c = woodM;
                    if (r2 <= 0.034f * 0.034f) c = woodL;        // bright end-grain core
                    else if (r2 >= 0.060f * 0.060f) c = woodD;   // dark outer rim
                    if (cx < -0.022f && cy > 0.018f) c = woodL;  // lit upper-left rim
                }

                // ---- RAKE / POLE leaning up to the right ----
                float shaft = fy - (0.18f + (fx - 0.55f) * 1.18f);
                if (Mathf.Abs(shaft) <= 0.026f && fx >= 0.55f && fx <= 0.93f && fy <= 0.78f) {
                    c = poleM;
                    if (shaft < -0.006f) c = poleL; else if (shaft > 0.010f) c = poleD;
                }
                // rake head (short cross-teeth) at the upper-right tip
                if (fx >= 0.84f && fx <= 0.94f && fy >= 0.60f && fy <= 0.74f) {
                    if (Frac(fy * 22f) < 0.55f) c = poleD; else c = poleM;
                }

                // ---- EMBER GLOW halo at the cracked base (translucent ring) ----
                if (c == Clear && fy < 0.30f && fy > 0.10f && Mathf.Abs(mdx) < 0.44f &&
                    Disc(mdx, fy - 0.20f, 0.42f) && !Disc(mdx, fy - 0.20f, 0.36f))
                    c = glow;

                // ---- SMOKE rising from the vent (translucent, wavering) ----
                if (c == Clear) {
                    float sxc = dxc - 0.02f - 0.10f * Mathf.Sin((fy - 0.80f) * 7.0f);
                    if (fy > 0.80f && fy < 0.985f && Mathf.Abs(sxc) < 0.085f) {
                        float band = Frac(fy * 6f);
                        if (band < 0.72f) c = (fy < 0.90f) ? smokeA : smokeB;
                    }
                }

                // ---- translucent ground shadow (not part of the silhouette) ----
                if (c == Clear && fy < 0.085f && fy >= 0.050f && fx >= 0.08f && fx <= 0.93f)
                    c = new Color(0.10f, 0.08f, 0.06f, 0.45f);

                px[y * s + x] = c;
            }

            // DARK OUTLINE pass: only fully-opaque, non-fire pixels count as silhouette.
            // Skip fire/ember/glow and translucent pixels so we don't halo the glow or smoke.
            var outPx = new Color[s * s];
            for (int i = 0; i < px.Length; i++) outPx[i] = px[i];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                if (px[y * s + x].a > 0.05f) continue;
                bool near = false;
                for (int dy = -1; dy <= 1 && !near; dy++) for (int dx = -1; dx <= 1; dx++) {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                    var n = px[ny * s + nx];
                    // only opaque, non-fire pixels form the silhouette edge
                    if (n.a > 0.9f && !(n.r > 0.95f && n.g > 0.40f && n.g < 0.85f && n.b < 0.40f))
                        { near = true; break; }
                }
                if (near) outPx[y * s + x] = outline;
            }
            px = outPx;

            tex.SetPixels(px); tex.Apply();
            _tribalCharcoalBurner = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _tribalCharcoalBurner;
        }
    }
}
