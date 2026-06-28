// Bespoke procedural art for "Drafting Table" (Age 2). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _bronzeDraftingTable;
        public static Sprite BronzeDraftingTable()
        {
            if (_bronzeDraftingTable != null) return _bronzeDraftingTable;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            var body      = Color.white;                          // structural board, tinted at runtime
            var bodyHi    = new Color(1f, 1f, 1f, 1f);            // lit upper-left face
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);   // shadow lower-right face

            // trestle legs / table wood (baked, leans dark/saturated)
            var woodD = new Color(0.30f, 0.18f, 0.09f, 1f);
            var woodM = new Color(0.44f, 0.27f, 0.14f, 1f);
            var woodL = new Color(0.60f, 0.42f, 0.24f, 1f);

            // blueprint schematic panel (identity accent)
            var blueD = new Color(0.13f, 0.24f, 0.44f, 1f);
            var blueM = new Color(0.20f, 0.34f, 0.58f, 1f);
            var blueL = new Color(0.32f, 0.48f, 0.74f, 1f);
            var gridC = new Color(0.56f, 0.70f, 0.92f, 1f);       // pale grid / drawn lines

            // brass instruments (T-square fittings + compasses)
            var brassD = new Color(0.55f, 0.40f, 0.18f, 1f);
            var brassM = new Color(0.80f, 0.58f, 0.26f, 1f);
            var brassL = new Color(0.97f, 0.80f, 0.42f, 1f);

            // T-square blade (pale bone/light wood)
            var tsqD = new Color(0.50f, 0.40f, 0.26f, 1f);
            var tsqM = new Color(0.66f, 0.56f, 0.40f, 1f);
            var tsqL = new Color(0.82f, 0.74f, 0.56f, 1f);

            // rolled plan tube (terracotta paper) leaning on a leg
            var tubeD    = new Color(0.50f, 0.33f, 0.23f, 1f);
            var tubeM    = new Color(0.66f, 0.45f, 0.34f, 1f);
            var tubeL    = new Color(0.82f, 0.62f, 0.50f, 1f);
            var paperEnd = new Color(0.86f, 0.80f, 0.66f, 1f);

            // stool
            var stoolD = new Color(0.28f, 0.17f, 0.09f, 1f);
            var stoolM = new Color(0.42f, 0.26f, 0.13f, 1f);
            var stoolL = new Color(0.58f, 0.40f, 0.22f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ---- STOOL (front-right, tucked low under the board) ----
                // seat slab
                if (fy >= 0.300f && fy <= 0.355f && fx >= 0.715f && fx <= 0.930f)
                {
                    c = stoolM;
                    if (fy >= 0.340f) c = stoolL;        // lit top of seat
                    else if (fx >= 0.890f) c = stoolD;   // shaded right edge
                }
                // stool legs
                if (fy >= 0.070f && fy < 0.300f)
                {
                    if (fx >= 0.730f && fx <= 0.770f) { c = stoolD; if (fx <= 0.748f) c = stoolM; }
                    if (fx >= 0.880f && fx <= 0.920f) c = stoolD;
                    // cross-rung
                    if (fy >= 0.175f && fy <= 0.205f && fx >= 0.730f && fx <= 0.920f) c = stoolM;
                }

                // ---- TRESTLE LEGS (two A-frames, baked wood, splayed to the feet) ----
                // A leg = a near-vertical band whose centre drifts outward as fy drops.
                float splay = (0.66f - fy) * 0.14f;                 // wider apart near the floor
                // left trestle: front + rear leg
                float lFront = 0.215f - splay;
                float lRear  = 0.315f - splay * 0.45f;
                // right trestle: front + rear leg
                float rFront = 0.785f + splay;
                float rRear  = 0.685f + splay * 0.45f;
                if (fy >= 0.060f && fy <= 0.620f)
                {
                    if (Mathf.Abs(fx - lFront) <= 0.026f) { c = woodM; if (fx < lFront - 0.006f) c = woodL; else if (fx > lFront + 0.006f) c = woodD; }
                    if (Mathf.Abs(fx - lRear)  <= 0.022f && c == Clear) c = woodD;
                    if (Mathf.Abs(fx - rFront) <= 0.026f) { c = woodM; if (fx < rFront - 0.006f) c = woodL; else if (fx > rFront + 0.006f) c = woodD; }
                    if (Mathf.Abs(fx - rRear)  <= 0.022f && c == Clear) c = woodD;
                }
                // horizontal stretcher rail tying the two trestles together
                if (fy >= 0.300f && fy <= 0.340f && fx >= 0.20f && fx <= 0.80f && c == Clear)
                {
                    c = woodM; if (fy >= 0.325f) c = woodD; else if (fy <= 0.312f) c = woodL;
                }

                // ---- DRAFTING BOARD (white structural body, tilted; left edge low, right edge high) ----
                // Board is a thick slab. Top (back) edge and bottom (front) edge both rise to the right.
                float boardTop = 0.90f + dxc * 0.14f;               // back edge, higher on the right
                float boardBot = 0.46f + dxc * 0.14f;               // front edge, also higher on the right
                bool inBoard = fy <= boardTop && fy >= boardBot && fx >= 0.10f && fx <= 0.90f;
                if (inBoard)
                {
                    c = body;                                        // mid (tinted) base
                    float fromTop = boardTop - fy;                   // 0 at back edge .. grows toward front
                    if (fromTop <= 0.045f) c = bodyShade;            // thin back rim in shadow
                    else if (dxc < 0.04f) c = bodyHi;                // lit upper-left drawing face
                    else c = bodyShade;                              // right side falls into shadow
                    if (fy <= boardBot + 0.05f) c = bodyShade;       // front lip/frame edge shaded
                }

                // ---- BLUEPRINT SCHEMATIC panel inset on the board face ----
                float bpTop = boardTop - 0.085f;
                float bpBot = boardBot + 0.075f;
                bool inBlue = fy <= bpTop && fy >= bpBot && fx >= 0.205f && fx <= 0.795f;
                if (inBlue)
                {
                    c = blueM;
                    if (dxc < -0.04f) c = blueL; else if (dxc > 0.20f) c = blueD;   // 3-tone panel
                    // pale grid in both axes
                    if (Frac(fx * 9f) < 0.10f) c = gridC;
                    if (Frac(fy * 10f) < 0.09f) c = gridC;
                    // a drawn compass ARC
                    float ax = fx - 0.40f, ay = fy - (bpBot + 0.17f);
                    float ar = Mathf.Sqrt(ax * ax + ay * ay);
                    if (ar >= 0.085f && ar <= 0.105f) c = gridC;
                    // a drawn rectangle (plan outline) on the upper-right of the panel
                    float rdx = Mathf.Abs(fx - 0.63f), rdy = Mathf.Abs(fy - (bpTop - 0.11f));
                    if (rdx <= 0.075f && rdy <= 0.060f && (rdx >= 0.058f || rdy >= 0.044f)) c = gridC;
                }

                // ---- T-SQUARE across the board (long blade + stock at left edge) ----
                // blade: a band running along a shallow diagonal that rises to the right (parallel-ish to the board)
                float bladeY = 0.345f + dxc * 0.14f;
                if (Mathf.Abs(fy - bladeY) <= 0.017f && fx >= 0.135f && fx <= 0.83f && fy <= boardTop && fy >= boardBot)
                {
                    c = tsqM; if (fy > bladeY) c = tsqL; else c = tsqD;
                }
                // stock (head) - short fat block riding the left edge of the board, crossing the blade
                if (fx >= 0.135f && fx <= 0.185f && fy <= bladeY + 0.085f && fy >= bladeY - 0.085f)
                {
                    c = tsqM; if (fx <= 0.155f) c = tsqL; else c = tsqD;
                }

                // ---- COMPASSES (brass) resting open on the upper board ----
                float hingeX = 0.585f, hingeY = boardTop - 0.155f;
                float hdx = fx - hingeX, hdy = fy - hingeY;
                // two straight legs splaying down from the hinge (drawn only below the hinge)
                if (hdy <= 0.002f && Disc(hdx, hdy, 0.20f))
                {
                    float legA = hdx + hdy * 0.55f;       // leg leaning one way
                    float legB = hdx - hdy * 0.45f;       // leg leaning the other way
                    if (Mathf.Abs(legA) <= 0.015f) c = (fx < hingeX) ? brassL : brassM;
                    if (Mathf.Abs(legB) <= 0.015f) c = brassD;
                }
                // hinge knob
                if (Disc(hdx, hdy, 0.030f)) { c = brassM; if (hdx < 0f && hdy > 0f) c = brassL; else if (hdx > 0.012f) c = brassD; }

                // ---- ROLLED PLAN TUBE leaning on the right front leg ----
                // slanted cylinder: axis tilts up toward the board on the right side
                float tubeAxis = (fx - 0.71f) - (fy - 0.10f) * 0.28f;    // 0 = tube centreline
                if (Mathf.Abs(tubeAxis) <= 0.045f && fy >= 0.075f && fy <= 0.44f)
                {
                    c = tubeM;
                    if (tubeAxis < -0.012f) c = tubeL; else if (tubeAxis > 0.016f) c = tubeD;
                }
                // light paper disc at the top end of the tube
                float pex = fx - 0.805f, pey = fy - 0.435f;
                if (Disc(pex, pey, 0.050f))
                {
                    c = paperEnd; if (pex > 0.012f && pey < -0.012f) c = tubeD; else if (pex < -0.012f && pey > 0.012f) c = paperEnd;
                }

                // ---- translucent ground shadow (NOT part of the silhouette) ----
                if (c == Clear && fy < 0.060f && fy >= 0.030f && fx >= 0.12f && fx <= 0.92f)
                    c = new Color(0.12f, 0.10f, 0.08f, 0.42f);

                px[y * s + x] = c;
            }

            // ---- DARK OUTLINE pass (only fully-opaque pixels count as silhouette) ----
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
            _bronzeDraftingTable = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _bronzeDraftingTable;
        }
    }
}
