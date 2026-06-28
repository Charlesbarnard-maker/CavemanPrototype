// Bespoke procedural art for "Clay Pile" (Age 1). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _tribalClayPile;
        public static Sprite TribalClayPile()
        {
            if (_tribalClayPile != null) return _tribalClayPile;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline = new Color(0.09f, 0.07f, 0.05f, 1f);
            // timber bin planks (raw logs brown — 3 shades, lit upper-left)
            var woodD = new Color(0.27f, 0.17f, 0.09f, 1f);
            var woodM = new Color(0.45f, 0.31f, 0.17f, 1f);
            var woodL = new Color(0.60f, 0.44f, 0.26f, 1f);
            // red clay heap (Tribal clay-pit palette — 3 shades)
            var clayD = new Color(0.52f, 0.33f, 0.23f, 1f);
            var clayM = new Color(0.68f, 0.45f, 0.33f, 1f);
            var clayL = new Color(0.80f, 0.57f, 0.43f, 1f);
            // formed clay balls (smoother, slightly warmer, wet sheen)
            var ballD = new Color(0.56f, 0.34f, 0.24f, 1f);
            var ballM = new Color(0.72f, 0.47f, 0.35f, 1f);
            var ballL = new Color(0.86f, 0.62f, 0.47f, 1f);
            var sheen = new Color(0.95f, 0.80f, 0.66f, 1f);

            // lump field for the heap (overlapping discs, each lit on its upper-left)
            float[] lcx = { 0.30f, 0.44f, 0.57f, 0.70f, 0.37f, 0.51f, 0.64f, 0.26f, 0.73f };
            float[] lcy = { 0.24f, 0.21f, 0.24f, 0.22f, 0.36f, 0.39f, 0.35f, 0.32f, 0.31f };
            float[] lr = { 0.090f, 0.100f, 0.092f, 0.082f, 0.084f, 0.090f, 0.080f, 0.072f, 0.070f };

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ---- TIMBER BIN (low box of planks framing the heap) ----
                bool inBin = fx >= 0.09f && fx <= 0.91f && fy >= 0.05f && fy <= 0.50f;
                bool inInner = fx >= 0.17f && fx <= 0.83f && fy >= 0.12f && fy <= 0.50f;
                if (inBin && !inInner)
                {
                    c = woodM;                                   // mid base
                    // left wall catches the light, right wall falls into shadow
                    if (fx <= 0.17f) c = woodL;
                    else if (fx >= 0.83f) c = woodD;
                    // top rail of the front planks is lit; the base sits in shadow
                    if (fy >= 0.43f && fy < 0.50f) c = woodL;
                    if (fy < 0.11f) c = woodD;
                    // vertical plank seams across the front face
                    if (fy >= 0.11f && fy < 0.43f && fx > 0.17f && fx < 0.83f && Frac(fx * 6f) < 0.13f) c = woodD;
                }

                // ---- CLAY HEAP (rounded lumps mounded inside the bin) ----
                if (inInner)
                {
                    // mounded silhouette: rises in the middle, dips at the sides
                    float heapTop = 0.50f - Mathf.Abs(dxc) * 0.26f;
                    if (fy <= heapTop)
                    {
                        c = clayM;                               // base fill
                        bool placed = false;
                        for (int i = 0; i < lcx.Length && !placed; i++)
                        {
                            float ux = fx - lcx[i], uy = fy - lcy[i];
                            if (Disc(ux, uy, lr[i]))
                            {
                                c = clayM;
                                float inner = lr[i] * 0.55f;
                                if (ux < -0.005f && uy > 0.005f && Disc(ux, uy, inner)) c = clayL; // upper-left highlight
                                else if (ux > 0.025f || uy < -0.025f) c = clayD;                   // lower-right shade
                                placed = true;
                            }
                        }
                        // crevices between lumps so the heap reads as many pieces
                        if (!placed && Frac(fx * 9f + fy * 7f) < 0.30f) c = clayD;
                        // base of the heap sits in shadow inside the bin
                        if (fy <= 0.15f) c = clayD;
                    }
                }

                // ---- FORMED CLAY BALLS resting on top (wet sheen) ----
                float ax = fx - 0.39f, ay = fy - 0.49f;          // ball A (left, larger)
                float bx = fx - 0.61f, by = fy - 0.45f;          // ball B (right, smaller)
                bool ballA = Disc(ax, ay, 0.110f);
                bool ballB = Disc(bx, by, 0.084f);
                if (ballA)
                {
                    c = ballM;
                    if (ax > 0.02f || ay < -0.025f) c = ballD;                       // lower-right shade
                    if (ax < -0.01f && ay > 0.005f && Disc(ax, ay, 0.075f)) c = ballL; // upper-left light
                    if (Disc(ax + 0.035f, ay - 0.045f, 0.022f)) c = sheen;            // wet specular
                }
                if (ballB && !ballA)
                {
                    c = ballM;
                    if (bx > 0.015f || by < -0.02f) c = ballD;
                    if (bx < -0.008f && by > 0.004f && Disc(bx, by, 0.056f)) c = ballL;
                    if (Disc(bx + 0.028f, by - 0.035f, 0.017f)) c = sheen;
                }

                // ---- translucent ground shadow (NOT part of the silhouette) ----
                if (c.a < 0.05f && fy >= 0.025f && fy < 0.05f && fx >= 0.11f && fx <= 0.90f)
                    c = new Color(0.12f, 0.08f, 0.06f, 0.42f);

                px[y * s + x] = c;
            }

            // ---- DARK OUTLINE PASS (1px halo; skip translucent ground-shadow pixels) ----
            var outPx = new Color[s * s];
            System.Array.Copy(px, outPx, px.Length);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                if (px[y * s + x].a > 0.05f) continue;            // only fill empty pixels
                bool near = false;
                for (int dy = -1; dy <= 1 && !near; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                    if (px[ny * s + nx].a > 0.9f) { near = true; break; } // neighbour is solid (not the a=0.42 shadow)
                }
                if (near) outPx[y * s + x] = outline;
            }
            px = outPx;

            tex.SetPixels(px); tex.Apply();
            _tribalClayPile = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _tribalClayPile;
        }
    }
}
