// Bespoke procedural art for "Brick Yard" (Age 2). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _bronzeBrickYard;
        public static Sprite BronzeBrickYard()
        {
            if (_bronzeBrickYard != null) return _bronzeBrickYard;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline = new Color(0.09f, 0.07f, 0.05f, 1f);
            // terracotta brick (3 baked shades, lean dark/saturated so they survive tinting)
            var brickD  = new Color(0.46f, 0.26f, 0.20f, 1f);
            var brickM  = new Color(0.66f, 0.40f, 0.34f, 1f);
            var brickL  = new Color(0.80f, 0.52f, 0.42f, 1f);
            var mortar  = new Color(0.24f, 0.16f, 0.13f, 1f); // dark gap between bricks
            // timber pallet / frame (baked brown, 3 shades)
            var woodD   = new Color(0.28f, 0.17f, 0.09f, 1f);
            var woodM   = new Color(0.40f, 0.25f, 0.13f, 1f);
            var woodL   = new Color(0.56f, 0.38f, 0.21f, 1f);

            // One small loose brick, shaded from the UPPER-LEFT.
            // cu = 0..1 left->right, vy = 0..1 bottom->top.
            System.Func<float, float, Color> looseBrick = (cu, vy) =>
            {
                if (cu < 0.07f || cu > 0.93f || vy < 0.12f || vy > 0.88f) return mortar; // dark border
                Color c = brickM;
                if (cu < 0.40f && vy > 0.55f) c = brickL;        // upper-left lit face
                else if (cu > 0.62f || vy < 0.30f) c = brickD;   // lower-right shade
                return c;
            };

            // One course (horizontal band) of offset bricks inside a stack.
            // Returns a brick/mortar colour for fx/fy if inside the band, else Clear.
            System.Func<float, float, float, float, float, float, float, Color> brickAt =
                (fx, fy, x0, x1, yLo, yHi, phase) =>
            {
                if (fx < x0 || fx > x1 || fy < yLo || fy > yHi) return Clear;
                const float bw = 0.105f;                 // brick width in fx
                float u = (fx - x0) / bw + phase;        // brick coord with course offset
                float cellU = Frac(u);                   // 0..1 across a single brick
                float vy = (fy - yLo) / (yHi - yLo);     // 0..1 up the course (one brick tall)

                // mortar gaps: thin dark border around each brick
                if (cellU < 0.10f || cellU > 0.92f) return mortar;
                if (vy < 0.12f || vy > 0.90f) return mortar;

                // pick one of three terracotta shades deterministically per brick
                float bi = Mathf.Floor(u);
                float h = Frac(Mathf.Sin(bi * 12.9898f + yLo * 78.233f) * 43758.5453f);
                Color c = brickM;
                if (h < 0.34f) c = brickL; else if (h > 0.70f) c = brickD;

                // hand-drawn form: upper-left (low cellU, high vy) lit; lower-right shaded
                if (cellU < 0.34f && vy > 0.58f) c = Color.Lerp(c, brickL, 0.45f);
                else if (cellU > 0.70f && vy < 0.42f) c = Color.Lerp(c, brickD, 0.45f);
                return c;
            };

            // Draws a whole stack of stacked offset courses; writes into c if a brick is hit.
            System.Func<float, float, float, float, float, int, Color, Color> stackAt =
                (fx, fy, x0, x1, baseY, courses, prior) =>
            {
                Color c = prior;
                if (fx < x0 || fx > x1) return c;
                const float ch = 0.075f;                 // course height
                for (int k = 0; k < courses; k++) {
                    float yLo = baseY + k * ch, yHi = yLo + ch;
                    if (fy >= yLo && fy <= yHi) {
                        Color bc = brickAt(fx, fy, x0, x1, yLo, yHi, (k % 2) * 0.5f);
                        if (bc.a > 0f) c = bc;
                    }
                }
                return c;
            };

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                Color c = Clear;

                // ---- TIMBER PALLET / FRAME under the stacks ----
                // pallet feet / gaps (lowest band, three blocks with lit-left / dark-right form)
                if (fy >= 0.045f && fy < 0.135f) {
                    if ((fx >= 0.12f && fx <= 0.22f) || (fx >= 0.45f && fx <= 0.55f) || (fx >= 0.78f && fx <= 0.88f)) {
                        float fpos = Frac(fx * 4.0f);
                        c = woodM;
                        if (fpos < 0.35f) c = woodL; else if (fpos > 0.70f) c = woodD;
                    }
                }
                // top deck board (lit upper face, shaded lower edge, plank seams)
                if (fx >= 0.10f && fx <= 0.92f && fy >= 0.135f && fy <= 0.205f) {
                    c = woodM;
                    if (fy >= 0.180f) c = woodL; else if (fy <= 0.155f) c = woodD;
                    if (Frac(fx * 9f) < 0.10f) c = woodD; // plank seams
                }

                // ---- THREE BRICK STACKS of differing height, offset-bonded courses ----
                // Left  : fx 0.10..0.40, tall (8 courses -> top ~0.805)
                // Middle: fx 0.42..0.66, short (4 courses -> top ~0.505)
                // Right : fx 0.68..0.92, medium (6 courses -> top ~0.655)
                c = stackAt(fx, fy, 0.10f, 0.40f, 0.205f, 8, c);
                c = stackAt(fx, fy, 0.42f, 0.66f, 0.205f, 4, c);
                c = stackAt(fx, fy, 0.68f, 0.92f, 0.205f, 6, c);

                // ---- A COUPLE OF LOOSE BRICKS ON THE GROUND ----
                // loose brick bottom-left corner
                if (fx >= 0.015f && fx <= 0.105f && fy >= 0.045f && fy <= 0.095f) {
                    float cu = (fx - 0.015f) / 0.090f, vy = (fy - 0.045f) / 0.050f;
                    c = looseBrick(cu, vy);
                }
                // loose brick bottom-right corner
                if (fx >= 0.905f && fx <= 0.990f && fy >= 0.055f && fy <= 0.105f) {
                    float cu = (fx - 0.905f) / 0.085f, vy = (fy - 0.055f) / 0.050f;
                    c = looseBrick(cu, vy);
                }

                // ---- translucent ground shadow (NOT part of the silhouette) ----
                if (fy < 0.045f && fy >= 0.020f && fx >= 0.10f && fx <= 0.92f && c == Clear)
                    c = new Color(0.12f, 0.10f, 0.08f, 0.40f);

                px[y * s + x] = c;
            }

            // DARK OUTLINE pass: only fully-opaque pixels count as silhouette (translucent shadow ignored)
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
            _bronzeBrickYard = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _bronzeBrickYard;
        }
    }
}
