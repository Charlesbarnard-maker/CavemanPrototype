// Bespoke procedural art for "Water Pump" (Age 2). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _bronzeWaterPump;
        public static Sprite BronzeWaterPump()
        {
            if (_bronzeWaterPump != null) return _bronzeWaterPump;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            var body      = Color.white;                          // structural housing/column (tinted at runtime)
            var bodyHi    = new Color(1f, 1f, 1f, 1f);            // lit upper-left faces
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);   // shadow lower-right faces

            // cool iron metal (pipe + handle arm + pivot)
            var metD = new Color(0.20f, 0.30f, 0.44f, 1f);
            var metM = new Color(0.32f, 0.50f, 0.72f, 1f);
            var metL = new Color(0.55f, 0.72f, 0.92f, 1f);
            // bronze sheen (spout + rivets)
            var bronD = new Color(0.46f, 0.28f, 0.14f, 1f);
            var bronM = new Color(0.66f, 0.42f, 0.22f, 1f);
            var bronL = new Color(0.86f, 0.60f, 0.34f, 1f);
            // wood (handle grip)
            var woodD = new Color(0.30f, 0.18f, 0.09f, 1f);
            var woodM = new Color(0.45f, 0.29f, 0.15f, 1f);
            var woodL = new Color(0.60f, 0.42f, 0.23f, 1f);
            // water (drop + puddle)
            var watD = new Color(0.12f, 0.34f, 0.62f, 1f);
            var watM = new Color(0.20f, 0.52f, 0.84f, 1f);
            var watL = new Color(0.46f, 0.74f, 0.98f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // PIPE going down into the ground (narrow column under the housing, just left of centre)
                if (fx >= 0.40f && fx <= 0.52f && fy >= 0.04f && fy <= 0.26f)
                {
                    c = metM;
                    if (fx <= 0.44f) c = metL; else if (fx >= 0.49f) c = metD;
                    if (Frac(fy * 9f) < 0.16f) c = metD;          // pipe ring seams
                }

                // PUMP HOUSING / COLUMN (white structural body)
                // main column
                if (fx >= 0.36f && fx <= 0.60f && fy >= 0.24f && fy <= 0.74f)
                {
                    c = body;
                    if (fx <= 0.43f) c = bodyHi;                  // lit left face
                    else if (fx >= 0.55f) c = bodyShade;          // shadow right face
                }
                // wider base block for a stable footing
                if (fx >= 0.30f && fx <= 0.66f && fy >= 0.20f && fy <= 0.30f)
                {
                    c = body;
                    if (fx <= 0.40f) c = bodyHi; else if (fx >= 0.58f) c = bodyShade;
                }
                // rounded cap/head on top of the column
                if (Disc(dxc - (-0.02f), fy - 0.74f, 0.15f) && fy >= 0.66f)
                {
                    c = body;
                    if (dxc < -0.04f) c = bodyHi; else if (dxc > 0.05f) c = bodyShade;
                }

                // RIVETS on the housing (baked bronze studs, upper-left local highlight)
                bool riv = Disc(fx - 0.40f, fy - 0.36f, 0.028f) || Disc(fx - 0.40f, fy - 0.56f, 0.028f)
                        || Disc(fx - 0.56f, fy - 0.36f, 0.028f) || Disc(fx - 0.56f, fy - 0.56f, 0.028f);
                if (riv)
                {
                    c = bronM;
                    if (Disc(fx - 0.39f, fy - 0.37f, 0.012f) || Disc(fx - 0.39f, fy - 0.57f, 0.012f)
                     || Disc(fx - 0.55f, fy - 0.37f, 0.012f) || Disc(fx - 0.55f, fy - 0.57f, 0.012f)) c = bronL;
                    else if (fx > 0.41f || fx > 0.57f) c = bronD;
                }

                // CURVED METAL SPOUT near the top (bronze): neck out the right, then arcs down
                // horizontal neck out of the head
                if (fx >= 0.54f && fx <= 0.78f && fy >= 0.66f && fy <= 0.74f)
                {
                    c = bronM; if (fy >= 0.71f) c = bronL; else if (fy <= 0.68f) c = bronD;
                }
                // curved downturn (quarter ring) at the spout tip, centred at (0.74, 0.66)
                float spx = fx - 0.74f, spy = fy - 0.66f;
                float spr = Mathf.Sqrt(spx * spx + spy * spy);
                if (spr >= 0.045f && spr <= 0.095f && spx >= -0.005f && spy <= 0.005f)
                {
                    c = bronM; if (spy < -0.04f) c = bronL; else if (spx > 0.04f) c = bronD;
                }
                // spout mouth (open end pointing down)
                if (fx >= 0.78f && fx <= 0.86f && fy >= 0.54f && fy <= 0.62f)
                {
                    c = bronM; if (fx <= 0.80f) c = bronL; else if (fx >= 0.84f) c = bronD;
                }

                // PUMP HANDLE LEVER out the left side, angled up to the left.
                // line from pivot (0.37, 0.60) up-left to tip (0.07, 0.72)
                float t = (0.37f - fx) / 0.30f;                   // 0 at pivot, 1 at far tip
                if (t >= 0f && t <= 1f)
                {
                    float handleY = 0.60f + t * 0.12f;            // rises toward the left
                    if (Mathf.Abs(fy - handleY) <= 0.038f)
                    {
                        if (t > 0.40f)
                        {
                            c = woodM;
                            if (fy - handleY < -0.012f) c = woodD; else if (fy - handleY > 0.012f) c = woodL;
                        }
                        else
                        {
                            c = metM;
                            if (fy - handleY < -0.012f) c = metD; else if (fy - handleY > 0.012f) c = metL;
                        }
                    }
                }
                // handle pivot bolt
                if (Disc(fx - 0.37f, fy - 0.60f, 0.045f))
                {
                    c = metM;
                    if (Disc(fx - 0.36f, fy - 0.61f, 0.02f)) c = metL; else if (fx > 0.39f) c = metD;
                }

                // WATER DROP falling from the spout mouth (a teardrop with a thin tail)
                float drx = fx - 0.82f, dry = fy - 0.44f;
                if (Disc(drx, dry, 0.040f) || (fy >= 0.44f && fy <= 0.52f && Mathf.Abs(drx) <= 0.016f))
                {
                    c = watM; if (drx < -0.008f && dry > 0.008f) c = watL; else if (drx > 0.012f) c = watD;
                }

                // small PUDDLE on the ground beneath the drop (flat ellipse, low fy)
                float pdx = fx - 0.80f;
                if (fy >= 0.04f && fy <= 0.10f && Mathf.Abs(pdx) <= 0.16f - (0.10f - fy) * 1.4f)
                {
                    c = watM; if (pdx < -0.04f) c = watL; else if (pdx > 0.06f) c = watD;
                }

                // translucent ground shadow (NOT part of the silhouette)
                if (fy >= 0.02f && fy < 0.045f && fx >= 0.28f && fx <= 0.70f && c == Clear)
                    c = new Color(0.12f, 0.10f, 0.08f, 0.45f);

                px[y * s + x] = c;
            }

            // DARK OUTLINE pass: only fully-opaque pixels count as silhouette (translucent shadow ignored)
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
            _bronzeWaterPump = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _bronzeWaterPump;
        }
    }
}
