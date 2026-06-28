// Bespoke procedural art for "Oil Well" (Age 2). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _bronzeOilWell;
        public static Sprite BronzeOilWell()
        {
            if (_bronzeOilWell != null) return _bronzeOilWell;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline = new Color(0.09f, 0.07f, 0.05f, 1f);
            // timber/iron lattice (3 wood shades, bronze-age dark)
            var woodD   = new Color(0.30f, 0.18f, 0.10f, 1f);
            var woodM   = new Color(0.44f, 0.28f, 0.15f, 1f);
            var woodL   = new Color(0.60f, 0.41f, 0.23f, 1f);
            // bronze pipe / drill pipe sheen
            var brzD    = new Color(0.55f, 0.34f, 0.18f, 1f);
            var brzM    = new Color(0.80f, 0.55f, 0.34f, 1f);
            var brzL    = new Color(0.95f, 0.72f, 0.42f, 1f);
            // crown block + walking beam iron (cool metal)
            var ironD   = new Color(0.22f, 0.24f, 0.28f, 1f);
            var ironM   = new Color(0.34f, 0.37f, 0.42f, 1f);
            var ironL   = new Color(0.52f, 0.56f, 0.62f, 1f);
            // oil pool dark with purple sheen
            var oilD    = new Color(0.06f, 0.05f, 0.09f, 1f);
            var oilM    = new Color(0.12f, 0.09f, 0.16f, 1f);
            var oilSheen= new Color(0.34f, 0.20f, 0.42f, 1f);
            var body    = Color.white;                          // engine shed (tinted at runtime)
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ---- OIL POOL at the base (dark ellipse with purple sheen) ----
                // a flat-ish puddle: widest near the centre, fades by fy~0.10
                float poolTop = 0.105f - Mathf.Abs(dxc) * 0.16f;
                if (fy >= 0.02f && fy <= poolTop && Mathf.Abs(dxc) <= 0.47f) {
                    c = oilM;
                    if (fy <= 0.05f) c = oilD;                  // deeper/darker at the very bottom
                    // upper-left curved sheen streak (purple highlight)
                    if (dxc < -0.02f && dxc > -0.30f && fy >= 0.055f && Frac(dxc * 5f + fy * 3f) < 0.45f)
                        c = oilSheen;
                }

                // ---- LATTICE TOWER: A-frame pyramid of crossed beams ----
                // legs converge from a wide base (fy~0.10) to a narrow crown (fy~0.90)
                bool inTowerBand = fy >= 0.10f && fy <= 0.90f;
                float stage  = Mathf.Clamp01((fy - 0.10f) / 0.80f); // 0 at base, 1 at crown
                float taper  = Mathf.Lerp(0.42f, 0.085f, stage);     // half-width of the tower here
                float legW   = 0.032f;
                bool leftLeg  = inTowerBand && Mathf.Abs(fx - (0.5f - taper)) <= legW;
                bool rightLeg = inTowerBand && Mathf.Abs(fx - (0.5f + taper)) <= legW;

                // diagonal cross-bracing (X per stage) using normalised interior coord nx in -1..1
                float halfW = Mathf.Max(0.0001f, taper);
                float nx    = dxc / halfW;
                float band  = Frac(stage * 5f);                      // 5 bracing stages up the tower
                bool braceBand = inTowerBand && Mathf.Abs(nx) <= 1.02f;
                // two diagonals sweeping opposite ways form an X inside each band
                float d1 = Mathf.Abs(nx - (band * 2f - 1f));
                float d2 = Mathf.Abs(nx + (band * 2f - 1f));
                bool brace = braceBand && (d1 <= 0.18f || d2 <= 0.18f);
                // horizontal girts at the stage boundaries
                bool girt  = braceBand && Frac(stage * 5f) < 0.10f;

                if (leftLeg || rightLeg || brace || girt) {
                    c = woodM;
                    bool litFace = leftLeg || (brace && d1 <= d2);    // upper-left facing beams catch light
                    if (litFace) c = woodL;
                    if (rightLeg || (brace && d2 < d1)) c = woodD;    // right diagonals/leg in shadow
                    if (girt) c = woodD;
                    // plank grain flecks
                    if (Frac(fy * 9f + fx * 3f) < 0.10f) c = woodD;
                }

                // ---- CENTRAL DRILL PIPE rising up the core ----
                if (Mathf.Abs(dxc) <= 0.035f && fy >= 0.10f && fy <= 0.84f) {
                    c = brzM;
                    if (dxc < -0.005f) c = brzL; else if (dxc > 0.012f) c = brzD;
                    if (Frac(fy * 16f) < 0.16f) c = brzD;             // pipe joints
                }

                // ---- CROWN BLOCK at the apex ----
                if (fy >= 0.84f && fy <= 0.93f && Mathf.Abs(dxc) <= 0.105f) {
                    c = ironM;
                    if (dxc < -0.02f) c = ironL; else if (dxc > 0.03f) c = ironD;
                    if (fy >= 0.905f) c = ironD;                      // shadowed top cap
                }
                // small sheave pulley on the crown
                if (Disc(dxc, fy - 0.885f, 0.045f)) {
                    c = ironL;
                    if (Disc(dxc, fy - 0.885f, 0.020f)) c = ironD;
                }

                // ---- WALKING BEAM (pump-jack) tilted across the upper-left ----
                // beam runs from the left counterweight up toward the crown; slope rises to the right
                float beamY = 0.66f + (fx - 0.16f) * 0.32f;
                if (fx >= 0.13f && fx <= 0.44f && Mathf.Abs(fy - beamY) <= 0.030f) {
                    c = ironM;
                    if (fy - beamY < -0.008f) c = ironL; else if (fy - beamY > 0.010f) c = ironD;
                }
                // counterweight disc at the beam's lower-left end
                if (Disc(fx - 0.15f, fy - 0.62f, 0.058f)) {
                    c = ironD;
                    if (Disc(fx - 0.165f, fy - 0.635f, 0.026f)) c = ironM;
                }

                // ---- ENGINE SHED (white structural body) at the right foot ----
                if (fx >= 0.66f && fx <= 0.94f && fy >= 0.085f && fy <= 0.32f) {
                    c = body;
                    // light on the upper-left of the shed face, shadow to the lower-right
                    if (fx > 0.85f || fy < 0.13f) c = bodyShade;
                }
                // shed roof lip (baked iron) sloping down to the right
                float shedRoof = 0.375f - (fx - 0.64f) * 0.20f;
                if (fx >= 0.63f && fx <= 0.96f && fy <= shedRoof && fy >= shedRoof - 0.05f) {
                    c = ironM;
                    if (fx < 0.74f) c = ironL; else if (fx > 0.88f) c = ironD;
                }

                // ---- PIPE OUTLET STUB from the shed toward the pool ----
                if (fy >= 0.115f && fy <= 0.16f && fx >= 0.50f && fx <= 0.69f) {
                    c = brzM;
                    if (fy < 0.13f) c = brzL; else if (fy > 0.15f) c = brzD;
                }
                // elbow joint turning down into the pool
                if (Disc(fx - 0.515f, fy - 0.135f, 0.040f)) {
                    c = brzD;
                    if (Disc(fx - 0.505f, fy - 0.128f, 0.018f)) c = brzM;
                }

                // ---- translucent ground shadow (NOT silhouette) ----
                if (fy < 0.035f && fy >= 0.015f && Mathf.Abs(dxc) <= 0.47f && c == Clear)
                    c = new Color(0.10f, 0.08f, 0.07f, 0.42f);

                px[y * s + x] = c;
            }

            // DARK OUTLINE pass (1px halo; skip translucent ground-shadow pixels as neighbours)
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
            _bronzeOilWell = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _bronzeOilWell;
        }
    }
}
