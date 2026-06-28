// Bespoke procedural art for "Copper Mine" (Age 1). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _tribalCopperMine;
        public static Sprite TribalCopperMine()
        {
            if (_tribalCopperMine != null) return _tribalCopperMine;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline = new Color(0.09f, 0.07f, 0.05f, 1f);

            // rock face (3 greys, lit upper-left)
            var rockD = new Color(0.33f, 0.34f, 0.37f, 1f);
            var rockM = new Color(0.48f, 0.50f, 0.54f, 1f);
            var rockL = new Color(0.64f, 0.67f, 0.72f, 1f);

            // dark shaft mouth
            var dark   = new Color(0.06f, 0.05f, 0.06f, 1f);
            var darkRim = new Color(0.14f, 0.12f, 0.13f, 1f);

            // copper ore (baked orange, leans dark/saturated to survive tinting)
            var oreD = new Color(0.62f, 0.34f, 0.10f, 1f);
            var oreM = new Color(0.92f, 0.56f, 0.20f, 1f);
            var oreL = new Color(1.00f, 0.74f, 0.36f, 1f);

            // wooden cart (3 browns)
            var cartD = new Color(0.24f, 0.15f, 0.08f, 1f);
            var cartM = new Color(0.36f, 0.22f, 0.11f, 1f);
            var cartL = new Color(0.50f, 0.33f, 0.18f, 1f);

            // iron wheel (cool grey)
            var ironD = new Color(0.22f, 0.23f, 0.25f, 1f);
            var ironL = new Color(0.46f, 0.48f, 0.52f, 1f);

            // spoil pile (warm grey rubble)
            var spoilD = new Color(0.38f, 0.34f, 0.27f, 1f);
            var spoilM = new Color(0.52f, 0.47f, 0.37f, 1f);
            var spoilL = new Color(0.66f, 0.60f, 0.47f, 1f);

            // structural timber frame -> Color.white so runtime tint reads
            var body      = Color.white;
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);

            // baked dark wood-grain seam (detail on the white frame)
            var woodSeam = new Color(0.22f, 0.14f, 0.07f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ---------- ROCK FACE / HILLSIDE: humped mass filling the tile ----------
                float hillTop = 0.92f - dxc * dxc * 0.45f + Mathf.Sin(fx * 7f) * 0.018f;
                bool rock = fy >= 0.05f && fy <= hillTop && fx >= 0.04f && fx <= 0.96f;
                if (rock) {
                    float n = Frac(Mathf.Sin(fx * 12.9f + fy * 7.3f) * 43.7f);
                    c = rockM;
                    // directional light: upper-left lit, lower-right shadow
                    float lit = (-dxc) + (fy - 0.45f) * 0.7f;
                    if (lit > 0.14f || n > 0.80f) c = rockL;
                    else if (lit < -0.12f || n < 0.20f) c = rockD;
                    // blocky crack seams
                    if (Frac(fx * 5f + fy * 1.3f) < 0.05f) c = rockD;
                }

                // ---------- SHAFT MOUTH: arched black opening, centre-low ----------
                float archR = 0.205f;
                bool archDome = Disc(dxc, fy - 0.40f, archR) && fy >= 0.40f;
                bool archBody = Mathf.Abs(dxc) <= archR && fy >= 0.12f && fy <= 0.40f;
                if ((archDome || archBody) && fy <= 0.605f) {
                    c = dark;
                    // faint lit rim along the top of the hole for depth
                    if (Disc(dxc, fy - 0.40f, archR) && !Disc(dxc, fy - 0.40f, archR - 0.035f) && fy >= 0.42f)
                        c = darkRim;
                }

                // ---------- TIMBER FRAME: two white-body posts + lintel ----------
                bool postL  = fx >= 0.225f && fx <= 0.295f && fy >= 0.10f && fy <= 0.56f;
                bool postR  = fx >= 0.705f && fx <= 0.775f && fy >= 0.10f && fy <= 0.56f;
                bool lintel = fx >= 0.205f && fx <= 0.795f && fy >= 0.56f && fy <= 0.63f;
                if (postL || postR || lintel) {
                    c = body;
                    // bake faint form so the white still reads as timber (lower-right shaded)
                    if (postR) c = bodyShade;                       // right post in shadow
                    else if (lintel && fy < 0.585f) c = bodyShade;  // underside of lintel
                    // wood-grain seam lines
                    if (lintel && Frac(fx * 9f) < 0.12f) c = woodSeam;
                    if ((postL || postR) && Frac(fy * 11f) < 0.12f) c = woodSeam;
                }

                // ---------- COPPER ORE veins/specks glinting in the rock ----------
                if (c == rockM || c == rockL || c == rockD) {
                    float vein = Frac(Mathf.Sin((fx * 18f + fy * 23f)) * 91.3f);
                    float near = Mathf.Abs(dxc) - archR; // band just outside the mouth
                    bool band = near > -0.02f && near < 0.22f && fy >= 0.16f && fy <= 0.80f;
                    if (band && vein > 0.85f) {
                        c = oreM;
                        if (vein > 0.955f) c = oreL; else if (vein < 0.89f) c = oreD;
                    }
                    // fixed bright glints scattered across the face
                    if (Disc(fx - 0.16f, fy - 0.58f, 0.018f)) c = oreL;
                    if (Disc(fx - 0.84f, fy - 0.50f, 0.018f)) c = oreM;
                    if (Disc(fx - 0.78f, fy - 0.66f, 0.016f)) c = oreL;
                    if (Disc(fx - 0.20f, fy - 0.42f, 0.016f)) c = oreM;
                    if (Disc(fx - 0.50f, fy - 0.74f, 0.016f)) c = oreL;
                }

                // ---------- SPOIL PILE: low rubble mound, right of the mouth ----------
                float spx = fx - 0.70f;
                float spoilTop = 0.170f - Mathf.Abs(spx) * 0.45f;
                if (fy <= spoilTop && fy >= 0.03f && Mathf.Abs(spx) <= 0.22f) {
                    float n2 = Frac(Mathf.Sin(fx * 21f + fy * 15f) * 57.1f);
                    c = spoilM;
                    if (spx < -0.03f && fy > 0.07f) c = spoilL;   // upper-left lit
                    else if (fy <= 0.06f) c = spoilD;             // base shadow
                    if (n2 > 0.80f) c = spoilL; else if (n2 < 0.22f) c = spoilD;
                }

                // ---------- WOODEN ORE CART (left-low) with iron wheel ----------
                bool cartBox = fx >= 0.085f && fx <= 0.275f && fy >= 0.085f && fy <= 0.225f;
                if (cartBox) {
                    c = cartM;
                    if (fx < 0.16f) c = cartL;                      // left face lit
                    else if (fx > 0.235f || fy < 0.12f) c = cartD;  // right/bottom shadow
                    if (fy >= 0.205f) c = cartL;                    // top rim
                    if (Frac(fx * 16f) < 0.14f) c = cartD;          // plank seams
                }
                // copper ore heaped in the cart
                if (fy >= 0.195f && fy <= 0.255f && fx >= 0.10f && fx <= 0.26f) {
                    float oc = Frac(Mathf.Sin(fx * 30f + fy * 11f) * 33.0f);
                    if (oc > 0.50f) { c = oreM; if (oc > 0.80f) c = oreL; else if (oc < 0.62f) c = oreD; }
                }
                // iron wheel
                float wx = fx - 0.18f, wy = fy - 0.085f; float wr = Mathf.Sqrt(wx * wx + wy * wy);
                if (wr <= 0.058f && fy >= 0.03f) {
                    c = ironD;
                    if (wr <= 0.022f) c = ironL;                           // hub
                    else if (wr >= 0.044f) c = (wx - wy < 0f) ? ironL : ironD; // rim (upper-left lit)
                }

                // ---------- translucent ground shadow (not part of silhouette) ----------
                if (fy < 0.045f && fy >= 0.018f && fx >= 0.06f && fx <= 0.94f && c == Clear)
                    c = new Color(0.12f, 0.10f, 0.08f, 0.45f);

                px[y * s + x] = c;
            }

            // ---------- DARK OUTLINE pass: 1px halo around opaque silhouette ----------
            var outPx = new Color[s * s];
            System.Array.Copy(px, outPx, px.Length);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                if (px[y * s + x].a > 0.05f) continue; // already painted
                bool near = false;
                for (int dy = -1; dy <= 1 && !near; dy++)
                for (int dx = -1; dx <= 1; dx++) {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                    if (px[ny * s + nx].a > 0.9f) { near = true; break; } // skip translucent shadow
                }
                if (near) outPx[y * s + x] = outline;
            }
            px = outPx;

            tex.SetPixels(px); tex.Apply();
            _tribalCopperMine = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _tribalCopperMine;
        }
    }
}
