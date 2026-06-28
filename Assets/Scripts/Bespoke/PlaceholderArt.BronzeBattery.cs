// Bespoke procedural art for "Battery" (Age 2). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _bronzeBattery;
        public static Sprite BronzeBattery()
        {
            if (_bronzeBattery != null) return _bronzeBattery;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            var body      = Color.white;                        // structural jar glass, tinted at runtime
            var bodyHi    = new Color(1f, 1f, 1f, 1f);          // lit (upper-left) face
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f); // shadow (lower-right) face

            // dark electrolyte / liquid (baked, saturated so it survives tinting)
            var liqD = new Color(0.08f, 0.12f, 0.16f, 1f);
            var liqM = new Color(0.14f, 0.20f, 0.26f, 1f);
            var liqL = new Color(0.22f, 0.30f, 0.38f, 1f);

            // copper terminal posts + connecting bar (bronze sheen)
            var copD = new Color(0.42f, 0.25f, 0.12f, 1f);
            var copM = new Color(0.64f, 0.40f, 0.20f, 1f);
            var copL = new Color(0.88f, 0.62f, 0.34f, 1f);

            // teal-green charge indicator
            var chargeC = new Color(0.32f, 0.62f, 0.50f, 1f);
            var chargeL = new Color(0.54f, 0.88f, 0.72f, 1f);
            var chargeD = new Color(0.20f, 0.44f, 0.36f, 1f);
            var glow    = new Color(0.40f, 0.80f, 0.64f, 0.40f);

            // three cell centre columns and the jar half-width
            float[] cx = { 0.235f, 0.5f, 0.765f };
            const float halfW  = 0.115f;                 // jar half-width
            const float jarBot = 0.12f, jarTop = 0.70f;  // jar vertical span
            const float liqTop = 0.50f;                  // electrolyte fill height
            const float postTop = 0.80f;                 // top of copper posts
            const float barY = 0.80f, barTopY = 0.875f;  // connecting bar across post tops

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                Color c = Clear;

                // ---- THREE JAR CELLS (white structural body) ----
                for (int j = 0; j < 3; j++)
                {
                    float dx = fx - cx[j];

                    // jar glass body with 3-tone form
                    if (dx >= -halfW && dx <= halfW && fy >= jarBot && fy <= jarTop)
                    {
                        c = body;
                        if (dx < -0.045f) c = bodyHi;        // lit left face
                        else if (dx > 0.050f) c = bodyShade; // shadowed right face

                        // dark electrolyte filling the lower part of each jar
                        if (fy <= liqTop && fy >= jarBot + 0.02f && Mathf.Abs(dx) <= halfW - 0.018f)
                        {
                            c = liqM;
                            if (dx < -0.04f) c = liqL;           // lit liquid (left)
                            else if (dx > 0.04f) c = liqD;       // shadow side (right)
                            if (fy <= jarBot + 0.06f) c = liqD;  // sediment at the bottom
                            if (fy >= liqTop - 0.03f) c = liqL;  // bright meniscus at the top
                        }
                    }

                    // ---- COPPER TERMINAL POST on top of each jar ----
                    if (Mathf.Abs(dx) <= 0.030f && fy > jarTop - 0.01f && fy <= postTop)
                    {
                        c = copM;
                        if (dx < -0.008f) c = copL;          // lit left
                        else if (dx > 0.010f) c = copD;      // shadow right
                    }
                    // rounded cap knob on each post (sits just under the bar)
                    if (Disc(dx, fy - postTop, 0.038f) && fy <= postTop + 0.005f)
                    {
                        c = copM;
                        if (dx < -0.010f) c = copL;
                        else if (dx > 0.012f) c = copD;
                    }
                }

                // ---- CONNECTING BAR across the post tops ----
                if (fy >= barY && fy <= barTopY && fx >= cx[0] - 0.045f && fx <= cx[2] + 0.045f)
                {
                    c = copM;
                    if (fy >= barTopY - 0.022f) c = copL;    // lit top edge
                    else if (fy <= barY + 0.018f) c = copD;  // shadow under-edge
                }

                // ---- TEAL-GREEN CHARGE INDICATOR (front of centre jar) ----
                float gx = fx - cx[1], gy = fy - 0.32f;
                if (Disc(gx, gy, 0.055f))
                {
                    c = chargeC;
                    if (Disc(gx + 0.015f, gy + 0.015f, 0.026f)) c = chargeL; // bright upper-left core
                    if (gx > 0.028f && gy < -0.028f) c = chargeD;            // shadow rim (lower-right)
                }
                // soft glow halo around the indicator (translucent, not outlined)
                if (c == Clear && Disc(gx, gy, 0.082f) && !Disc(gx, gy, 0.055f))
                    c = glow;

                // ---- translucent ground shadow (NOT part of silhouette) ----
                if (c == Clear && fy < jarBot && fy >= jarBot - 0.035f && fx >= 0.11f && fx <= 0.89f)
                    c = new Color(0.12f, 0.10f, 0.08f, 0.45f);

                px[y * s + x] = c;
            }

            // ---- DARK OUTLINE pass (skip translucent glow + ground shadow) ----
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
            _bronzeBattery = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _bronzeBattery;
        }
    }
}
