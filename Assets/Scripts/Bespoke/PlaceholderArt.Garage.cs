// Bespoke procedural art for the "Garage" (mount shed). A timber cart-shed: white plank walls (tinted at
// runtime) under a gabled roof, with a big open BAY showing a parked wheel inside and a horseshoe sign over
// the arch. Style matches the other bespoke buildings: 64x64, fy=0 bottom, 3-shade detail + a dark outline.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _garageShed;
        public static Sprite GarageShed()
        {
            if (_garageShed != null) return _garageShed;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.08f, 0.06f, 0.05f, 1f);
            var body      = Color.white;                          // plank wall, tinted timber at runtime
            var bodyHi    = new Color(1f, 1f, 1f, 1f);
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);
            var seam      = new Color(0.60f, 0.50f, 0.38f, 1f);   // plank seam
            var roofL     = new Color(0.52f, 0.40f, 0.30f, 1f);
            var roofM     = new Color(0.40f, 0.30f, 0.22f, 1f);
            var roofD     = new Color(0.27f, 0.19f, 0.13f, 1f);
            var bayDark   = new Color(0.15f, 0.12f, 0.11f, 1f);   // bay interior (shadow)
            var bayDim    = new Color(0.24f, 0.20f, 0.18f, 1f);
            var shoe      = new Color(0.82f, 0.82f, 0.86f, 1f);   // horseshoe sign (light metal)
            var tyre      = new Color(0.22f, 0.18f, 0.14f, 1f);   // parked wheel
            var spoke     = new Color(0.56f, 0.45f, 0.30f, 1f);
            var hub       = new Color(0.30f, 0.24f, 0.16f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ---- PLANK WALL body ----
                bool inBody = fx >= 0.10f && fx <= 0.90f && fy >= 0.075f && fy <= 0.60f;
                if (inBody)
                {
                    c = body;
                    if (dxc < -0.04f && fy > 0.32f) c = bodyHi;            // lit upper-left
                    else if (dxc > 0.20f || fy < 0.15f) c = bodyShade;     // shaded right / base
                    if (Frac(fx * 8f) < 0.07f) c = seam;                   // vertical plank seams
                }

                // ---- OPEN BAY (dark), rounded top ----
                bool bayRect = Mathf.Abs(dxc) <= 0.23f && fy >= 0.075f && fy <= 0.40f;
                bool bayTop  = Disc(dxc, fy - 0.40f, 0.23f) && fy >= 0.40f && fy <= 0.50f;
                if (bayRect || bayTop)
                {
                    c = (fy > 0.30f) ? bayDim : bayDark;
                    // parked WHEEL inside the bay (shared helper), with a short cart-bed bar over it
                    Color w = WheelPixel(fx, fy, 0.40f, 0.165f, 0.095f, 0f, tyre, spoke, hub);
                    if (w.a > 0f) c = w;
                    if (fx >= 0.31f && fx <= 0.56f && fy >= 0.235f && fy <= 0.275f) c = spoke;
                }

                // ---- HORSESHOE sign on the wall over the arch ----
                {
                    float hsx = dxc, hsy = fy - 0.545f;
                    float r = Mathf.Sqrt(hsx * hsx + hsy * hsy);
                    if (r >= 0.038f && r <= 0.060f && hsy >= -0.004f) c = shoe;           // upper arc
                    if (Mathf.Abs(Mathf.Abs(hsx) - 0.049f) <= 0.012f && fy >= 0.505f && fy <= 0.547f) c = shoe; // two legs
                }

                // ---- GABLED ROOF on top (drawn last so it caps the wall) ----
                if (fy >= 0.58f && fy <= 0.86f)
                {
                    float t = (fy - 0.58f) / 0.28f;                        // 0 eaves -> 1 apex
                    float halfW = Mathf.Lerp(0.46f, 0.02f, t);
                    if (Mathf.Abs(dxc) <= halfW)
                    {
                        c = roofM;
                        if (dxc < -0.02f) c = roofL; else if (dxc > 0.06f) c = roofD;
                        if (Frac(fy * 20f) < 0.30f) c = roofD;            // shingle courses
                    }
                }

                px[y * s + x] = c;
            }

            // ---- DARK OUTLINE pass around the silhouette ----
            var outPx = new Color[s * s];
            System.Array.Copy(px, outPx, px.Length);
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
            _garageShed = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _garageShed;
        }
    }
}
