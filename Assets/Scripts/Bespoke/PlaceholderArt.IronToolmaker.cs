// Bespoke procedural art for "Toolmaker" (Age 3). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _ironToolmaker;
        public static Sprite IronToolmaker()
        {
            if (_ironToolmaker != null) return _ironToolmaker;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            // body (structural mass, tinted at runtime)
            var body      = Color.white;
            var bodyHi    = new Color(1f, 1f, 1f, 1f);
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);
            // iron greys (anvil + rack tool shafts)
            var ironD     = new Color(0.24f, 0.26f, 0.30f, 1f);
            var ironM     = new Color(0.42f, 0.45f, 0.50f, 1f);
            var ironL     = new Color(0.62f, 0.66f, 0.72f, 1f);
            // wood (anvil block + rack board + bellows board)
            var woodD     = new Color(0.26f, 0.16f, 0.08f, 1f);
            var woodM     = new Color(0.40f, 0.25f, 0.13f, 1f);
            var woodL     = new Color(0.55f, 0.37f, 0.20f, 1f);
            // stone hearth
            var stoneD    = new Color(0.28f, 0.29f, 0.32f, 1f);
            var stoneM    = new Color(0.44f, 0.46f, 0.50f, 1f);
            var stoneL    = new Color(0.60f, 0.63f, 0.68f, 1f);
            // brass (rack hammer head + axe blade)
            var brassD    = new Color(0.45f, 0.32f, 0.13f, 1f);
            var brassM    = new Color(0.62f, 0.46f, 0.20f, 1f);
            var brassL    = new Color(0.82f, 0.62f, 0.28f, 1f);
            // hide bellows
            var hideD     = new Color(0.30f, 0.20f, 0.13f, 1f);
            var hideM     = new Color(0.46f, 0.32f, 0.22f, 1f);
            var hideL     = new Color(0.60f, 0.45f, 0.32f, 1f);
            // ember / fire
            var coalD     = new Color(0.55f, 0.16f, 0.06f, 1f);
            var coalM     = new Color(0.86f, 0.34f, 0.08f, 1f);
            var fireC     = new Color(1f, 0.55f, 0.12f, 1f);
            var fireL     = new Color(1f, 0.84f, 0.36f, 1f);
            var glow      = new Color(1f, 0.50f, 0.14f, 0.40f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // =====================================================================
                // WALL RACK (left): white-body backboard mounted on the wall
                // =====================================================================
                if (fx >= 0.05f && fx <= 0.27f && fy >= 0.46f && fy <= 0.92f)
                {
                    c = body;
                    if (fx < 0.11f) c = bodyHi;          // lit left edge
                    else if (fx > 0.21f) c = bodyShade;  // shadowed right edge
                    if (Frac(fy * 7f) < 0.10f) c = bodyShade; // horizontal plank seams
                }

                // rack HAMMER (iron shaft + brass head) hanging on the backboard
                if (fx >= 0.090f && fx <= 0.140f && fy >= 0.58f && fy <= 0.84f) // shaft
                {
                    c = (fx < 0.110f) ? ironL : ironM;
                    if (fx > 0.128f) c = ironD;
                }
                if (fx >= 0.065f && fx <= 0.170f && fy >= 0.84f && fy <= 0.905f) // head
                {
                    c = brassM;
                    if (fy < 0.862f || fx > 0.140f) c = brassD;  // lower / right shade
                    else if (fx < 0.100f && fy > 0.875f) c = brassL; // upper-left lit
                }

                // rack AXE (iron shaft + brass blade) on the right of the backboard
                if (fx >= 0.200f && fx <= 0.245f && fy >= 0.56f && fy <= 0.84f) // shaft
                {
                    c = (fx < 0.222f) ? ironM : ironD;
                }
                {
                    // brass blade arcs out to the upper-right of the shaft top
                    float ax = fx - 0.235f, ay = fy - 0.80f;
                    if (ax >= -0.01f && ax <= 0.085f && fy >= 0.745f && fy <= 0.865f &&
                        Disc(ax - 0.030f, ay, 0.075f))
                    {
                        c = brassM;
                        if (ay < -0.005f || ax > 0.045f) c = brassD; // lower / outer shade
                        else if (ax < 0.020f && ay > 0.015f) c = brassL;
                    }
                }

                // =====================================================================
                // STONE HEARTH (right): forge body with a glowing mouth
                // =====================================================================
                bool hearthBody = fx >= 0.66f && fx <= 0.97f && fy >= 0.10f && fy <= 0.64f;
                if (hearthBody)
                {
                    c = stoneM;
                    if (fx < 0.74f || fy > 0.56f) c = stoneL;   // upper-left lit face
                    if (fx > 0.90f || fy < 0.16f) c = stoneD;   // lower-right shade
                    // staggered block seams
                    bool rowOdd = Frac(fy * 5.5f) < 0.5f;
                    if (Frac(fy * 5.5f) < 0.10f) c = stoneD;
                    if (Frac(fx * 4.5f + (rowOdd ? 0f : 0.5f)) < 0.10f) c = stoneD;
                }

                // hearth MOUTH (dark recess) with a glowing coal bed
                float mx = fx - 0.815f;
                if (Mathf.Abs(mx) <= 0.095f && fy >= 0.22f && fy <= 0.46f)
                {
                    c = new Color(0.12f, 0.08f, 0.07f, 1f);     // dark recess
                    if (fy <= 0.32f)                            // coal bed at the bottom
                    {
                        float cv = Frac(fx * 13f + fy * 7f);
                        c = coalD;
                        if (cv > 0.42f) c = coalM;
                        if (cv > 0.78f) c = fireC;
                    }
                    // a small flame licking up from the coals
                    float flTop = 0.44f - Mathf.Abs(mx) * 1.6f - Frac(fx * 17f) * 0.03f;
                    if (fy >= 0.31f && fy <= flTop) c = (fy > 0.38f) ? fireL : fireC;
                }

                // =====================================================================
                // ANVIL BLOCK (wood stump, centre)
                // =====================================================================
                if (fx >= 0.385f && fx <= 0.605f && fy >= 0.10f && fy <= 0.40f)
                {
                    c = woodM;
                    if (fx < 0.45f) c = woodL; else if (fx > 0.54f) c = woodD;
                    if (Frac(fy * 9f) < 0.12f) c = woodD;       // bark rings
                }

                // =====================================================================
                // ANVIL (iron, 3 greys) sitting on the block
                // =====================================================================
                // waisted base
                if (fx >= 0.445f && fx <= 0.545f && fy >= 0.40f && fy <= 0.46f)
                {
                    c = ironM; if (fx < 0.475f) c = ironL; else if (fx > 0.52f) c = ironD;
                }
                // body / face slab
                if (fx >= 0.40f && fx <= 0.60f && fy >= 0.46f && fy <= 0.535f)
                {
                    c = ironM;
                    if (fx < 0.475f && fy < 0.51f) c = ironL;          // lit lower-left
                    if (fx > 0.555f || fy > 0.515f) c = ironD;         // shade right / top
                }
                // horn tapering to the LEFT off the body
                {
                    float cen = 0.50f;                                  // horn centre line
                    float t = (0.40f - fx) / 0.10f;                     // 0 at body .. 1 at tip
                    if (fx >= 0.30f && fx <= 0.40f && t >= 0f && t <= 1f)
                    {
                        float halfH = 0.030f * (1f - t) + 0.010f;
                        if (Mathf.Abs(fy - cen) <= halfH)
                        {
                            c = ironM; if (fy > cen) c = ironL; else c = ironD;
                        }
                    }
                }
                // glowing ORANGE-HOT bar lying on the anvil face
                if (fx >= 0.42f && fx <= 0.575f && fy >= 0.535f && fy <= 0.565f)
                {
                    float hv = Frac(fx * 11f);
                    c = fireC; if (fx < 0.50f) c = fireL; if (hv > 0.7f) c = coalM;
                }

                // =====================================================================
                // HIDE BELLOWS (below the hearth, low-right)
                // =====================================================================
                {
                    float bxr = fx - 0.86f, byr = fy - 0.18f;
                    if (Disc(bxr, byr, 0.105f) && fy <= 0.27f)
                    {
                        c = hideM;
                        if (bxr < -0.02f && byr > 0.0f) c = hideL;   // lit upper-left
                        else if (bxr * bxr + byr * byr > 0.0072f) c = hideD; // dark rim
                    }
                    // wooden nozzle board feeding the hearth mouth
                    if (fx >= 0.72f && fx <= 0.80f && fy >= 0.175f && fy <= 0.225f)
                        c = (fx < 0.76f) ? woodM : woodD;
                }

                // =====================================================================
                // SPARKS flying up-right off the hot bar
                // =====================================================================
                if (Disc(fx - 0.605f, fy - 0.62f, 0.013f)) c = fireL;
                if (Disc(fx - 0.660f, fy - 0.70f, 0.010f)) c = fireC;
                if (Disc(fx - 0.625f, fy - 0.75f, 0.008f)) c = fireL;
                if (Disc(fx - 0.705f, fy - 0.66f, 0.009f)) c = fireC;

                // =====================================================================
                // warm translucent glow (only over empty pixels)
                // =====================================================================
                if (c == Clear)
                {
                    if (Disc(fx - 0.815f, fy - 0.33f, 0.165f)) c = glow;       // hearth mouth
                    else if (Disc(dxc, fy - 0.55f, 0.095f)) c = glow;         // hot bar / anvil
                }

                // translucent ground shadow (NOT part of the silhouette)
                if (c == Clear && fy < 0.105f && fy >= 0.07f && fx >= 0.05f && fx <= 0.98f)
                    c = new Color(0.10f, 0.08f, 0.07f, 0.42f);

                px[y * s + x] = c;
            }

            // ---- DARK OUTLINE pass: only fully-opaque pixels are silhouette ----
            var outPx = new Color[s * s];
            for (int i = 0; i < px.Length; i++) outPx[i] = px[i];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                if (px[y * s + x].a > 0.05f) continue;
                bool near = false;
                for (int dy = -1; dy <= 1 && !near; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                    if (px[ny * s + nx].a > 0.9f) { near = true; break; }
                }
                if (near) outPx[y * s + x] = outline;
            }
            px = outPx;

            tex.SetPixels(px); tex.Apply();
            _ironToolmaker = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _ironToolmaker;
        }
    }
}
