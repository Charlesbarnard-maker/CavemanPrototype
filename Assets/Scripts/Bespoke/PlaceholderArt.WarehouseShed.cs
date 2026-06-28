// Bespoke procedural art for "Warehouse" (Age 0). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _warehouseShed;
        public static Sprite WarehouseShed()
        {
            if (_warehouseShed != null) return _warehouseShed;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            var bodyHi    = new Color(1f, 1f, 1f, 1f);
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);
            var roofD     = new Color(0.30f, 0.18f, 0.09f, 1f);
            var roofM     = new Color(0.42f, 0.26f, 0.13f, 1f);
            var roofL     = new Color(0.55f, 0.36f, 0.19f, 1f);
            var woodD     = new Color(0.28f, 0.17f, 0.09f, 1f);
            var woodM     = new Color(0.40f, 0.25f, 0.13f, 1f);
            var woodL     = new Color(0.56f, 0.38f, 0.21f, 1f);
            var doorD     = new Color(0.20f, 0.13f, 0.07f, 1f);
            var doorM     = new Color(0.30f, 0.20f, 0.11f, 1f);
            var doorL     = new Color(0.40f, 0.27f, 0.15f, 1f);
            var braceD    = new Color(0.46f, 0.31f, 0.17f, 1f);
            var braceL    = new Color(0.62f, 0.44f, 0.25f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // WHITE-BODY shed wall (structural mass, tinted at runtime)
                if (fx >= 0.10f && fx <= 0.90f && fy >= 0.07f && fy <= 0.58f) {
                    c = bodyHi;
                    if (dxc > 0.12f) c = bodyShade;              // right faces fall into shadow
                    if (fy > 0.52f) c = bodyShade;               // narrow shade band under the eave
                }

                // EXPOSED CORNER POSTS (white body, form from outline + shade)
                if (fx >= 0.10f && fx <= 0.17f && fy >= 0.07f && fy <= 0.58f) {
                    c = (fx < 0.135f) ? bodyHi : bodyShade;      // left post: lit edge / inner shadow
                }
                if (fx >= 0.83f && fx <= 0.90f && fy >= 0.07f && fy <= 0.58f) {
                    c = (fx < 0.865f) ? bodyHi : bodyShade;      // right post
                }

                // GABLE ROOF (low-pitch baked-brown planks, apex at centre ~fy0.86)
                float apex = 0.86f;
                float eave = 0.58f;
                float roofTop = apex - Mathf.Abs(dxc) * (apex - eave) / 0.46f; // slope down to eaves
                if (fy <= roofTop && fy >= roofTop - 0.14f && fx >= 0.04f && fx <= 0.96f) {
                    float t = (roofTop - fy) / 0.14f;            // 0 at ridge edge .. 1 at eave
                    c = (dxc < 0f) ? roofL : roofD;             // left slope lit, right slope dark
                    if (t < 0.20f) c = roofL;                   // ridge highlight band
                    else if (t > 0.78f) c = roofD;              // eave shadow band
                    else if (Mathf.Abs(dxc) > 0.02f) c = roofM; // mid field of each slope
                    if (Frac(fx * 9f) < 0.11f) c = roofD;       // plank seams
                    if (Mathf.Abs(dxc) < 0.013f) c = roofD;     // ridge line
                }

                // LOADING AWNING / LINTEL over the doors (small baked-brown shelf)
                if (fy <= 0.485f && fy >= 0.440f && fx >= 0.28f && fx <= 0.72f) {
                    c = (fy > 0.465f) ? woodL : woodM;          // top of shelf lit
                    if (fx > 0.64f) c = woodD;                  // right end in shadow
                }
                // awning underside lip (dark)
                if (fy < 0.440f && fy >= 0.420f && fx >= 0.30f && fx <= 0.70f) c = woodD;

                // DOUBLE DOORS (large dark baked panel, centre, fy 0.075..0.405)
                if (fx >= 0.30f && fx <= 0.70f && fy >= 0.075f && fy <= 0.405f) {
                    c = doorM;
                    if (fx > 0.56f) c = doorD;                  // right leaf in shadow
                    else if (fx < 0.42f) c = doorL;             // left leaf lit
                    if (Mathf.Abs(fx - 0.50f) < 0.014f) c = doorD; // central seam between doors

                    // X-BRACE on each leaf (baked timber diagonals)
                    bool leftLeaf = fx < 0.50f;
                    float lx0 = leftLeaf ? 0.30f : 0.50f;
                    float u = (fx - lx0) / 0.20f;               // 0..1 across the leaf
                    float v = (fy - 0.075f) / 0.330f;           // 0..1 up the leaf
                    float dDiag = Mathf.Abs(u - v);             // bottom-left -> top-right
                    float aDiag = Mathf.Abs(u - (1f - v));      // top-left -> bottom-right
                    if (dDiag < 0.10f || aDiag < 0.10f) {
                        c = (u + v < 1f) ? braceL : braceD;     // light lower-left, dark upper-right
                    }
                }

                // translucent ground shadow (NOT part of the silhouette)
                if (fy < 0.07f && fy >= 0.045f && fx >= 0.10f && fx <= 0.92f && c == Clear)
                    c = new Color(0.12f, 0.10f, 0.08f, 0.45f);

                px[y * s + x] = c;
            }

            // DARK OUTLINE pass: only fully-opaque pixels count as silhouette
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
            _warehouseShed = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _warehouseShed;
        }
    }
}
