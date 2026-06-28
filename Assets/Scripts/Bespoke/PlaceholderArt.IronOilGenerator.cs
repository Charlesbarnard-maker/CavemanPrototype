// Bespoke procedural art for "Oil Generator" (Age 3). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _ironOilGenerator;
        public static Sprite IronOilGenerator()
        {
            if (_ironOilGenerator != null) return _ironOilGenerator;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);

            // baked iron-grey detail (rivets, flywheel, pistons, exhaust)
            var ironD     = new Color(0.20f, 0.22f, 0.26f, 1f);
            var ironM     = new Color(0.36f, 0.39f, 0.44f, 1f);
            var ironL     = new Color(0.56f, 0.60f, 0.66f, 1f);

            // dark fuel tank
            var tankD     = new Color(0.12f, 0.13f, 0.16f, 1f);
            var tankM     = new Color(0.20f, 0.21f, 0.25f, 1f);
            var tankL     = new Color(0.30f, 0.32f, 0.37f, 1f);

            // amber fuel window
            var fuelD     = new Color(0.55f, 0.31f, 0.06f, 1f);
            var fuelM     = new Color(0.82f, 0.50f, 0.10f, 1f);
            var fuelL     = new Color(1.00f, 0.74f, 0.26f, 1f);

            // brass piston caps / fittings
            var brassD    = new Color(0.45f, 0.31f, 0.12f, 1f);
            var brassM    = new Color(0.66f, 0.48f, 0.20f, 1f);
            var brassL    = new Color(0.86f, 0.66f, 0.30f, 1f);

            // drive belt (dark oily leather)
            var beltD     = new Color(0.10f, 0.09f, 0.10f, 1f);
            var beltM     = new Color(0.17f, 0.15f, 0.16f, 1f);

            // teal power glow terminals
            var tealC     = new Color(0.16f, 0.78f, 0.80f, 1f);
            var tealL     = new Color(0.55f, 0.96f, 0.98f, 1f);
            var tealGlow  = new Color(0.20f, 0.82f, 0.86f, 0.40f);

            var exhaustD  = new Color(0.16f, 0.17f, 0.20f, 1f);
            var exhaustM  = new Color(0.26f, 0.28f, 0.32f, 1f);

            var smoke     = new Color(0.82f, 0.82f, 0.84f, 0.70f);
            var smokeT    = new Color(0.78f, 0.78f, 0.80f, 0.45f);

            var oily      = new Color(0.62f, 0.74f, 0.78f, 1f);
            var groundSh  = new Color(0.12f, 0.10f, 0.08f, 0.45f);

            var body      = Color.white;                               // structural engine block (tinted at runtime)
            var bodyHi    = new Color(1f, 1f, 1f, 1f);
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ---- EXHAUST STACK (back-right, rises high above the deck) ----
                if (fx >= 0.72f && fx <= 0.84f && fy >= 0.50f && fy <= 0.90f) {
                    c = exhaustM;
                    if (fx <= 0.755f) c = ironM; else if (fx >= 0.805f) c = exhaustD; // left lit / right shade
                    if (Frac(fy * 9f) < 0.16f) c = exhaustD;                          // horizontal bands
                    if (fy >= 0.865f) c = ironM;                                       // rim lip catches light
                }

                // ---- ENGINE BLOCK (white structural mass) ----
                if (fx >= 0.16f && fx <= 0.70f && fy >= 0.18f && fy <= 0.66f) {
                    c = body;
                    if (dxc < -0.08f && fy > 0.30f) c = bodyHi;        // upper-left lit faces
                    if (fx > 0.58f || fy < 0.26f) c = bodyShade;       // lower-right shaded faces
                    // top deck plate edge (shadow line under pistons)
                    if (fy >= 0.62f) c = bodyShade;
                    // rivet rows (baked dark dots)
                    float rvx = Frac(fx * 7.2f), rvy = Frac(fy * 6.0f);
                    if (rvx < 0.16f && rvy < 0.16f && fx >= 0.22f && fx <= 0.64f && fy >= 0.28f && fy <= 0.60f) c = ironD;
                    // oily sheen streak (baked teal-blue highlight, diagonal)
                    if (Frac((fx + fy) * 5.0f) < 0.055f && fx >= 0.22f && fx <= 0.64f && fy >= 0.28f && fy <= 0.60f) c = oily;
                }

                // ---- PISTON CYLINDERS on top of the block ----
                for (int p = 0; p < 2; p++) {
                    float pcx = 0.30f + p * 0.16f;
                    if (Mathf.Abs(fx - pcx) <= 0.062f && fy >= 0.64f && fy <= 0.82f) {
                        float t = (fx - (pcx - 0.062f)) / 0.124f;     // 0 left .. 1 right
                        c = ironM; if (t < 0.34f) c = ironL; else if (t > 0.70f) c = ironD;
                        if (fy >= 0.785f) { c = brassM; if (t < 0.34f) c = brassL; else if (t > 0.70f) c = brassD; } // brass cap
                    }
                    // piston rod stub up out of the cap
                    if (Mathf.Abs(fx - pcx) <= 0.016f && fy >= 0.82f && fy <= 0.87f) c = ironL;
                }

                // ---- SIDE FUEL TANK (left, dark upright cylinder) ----
                if (fx >= 0.05f && fx <= 0.21f && fy >= 0.20f && fy <= 0.58f) {
                    float t = (fx - 0.05f) / 0.16f;                    // 0 left .. 1 right
                    c = tankM; if (t < 0.34f) c = tankL; else if (t > 0.70f) c = tankD;
                    if (fy >= 0.555f || fy <= 0.225f) c = tankD;       // top + bottom caps
                    // amber fuel window
                    if (fx >= 0.085f && fx <= 0.175f && fy >= 0.30f && fy <= 0.50f) {
                        c = fuelM; if (fx <= 0.115f) c = fuelL; else if (fx >= 0.155f) c = fuelD;
                        if (fy > 0.46f) c = fuelD;                     // darker at top (air gap above fuel)
                    }
                }

                // ---- FLYWHEEL (big spoked wheel, front-right face) ----
                float wx = fx - 0.60f, wy = fy - 0.36f;
                float wr = Mathf.Sqrt(wx * wx + wy * wy);
                float wang = Mathf.Atan2(wy, wx);
                if (wr <= 0.225f) {
                    if (wr >= 0.170f) {                                // outer rim
                        c = ironM; if (wx - wy < -0.06f) c = ironL; else if (wx - wy > 0.06f) c = ironD;
                    } else if (wr <= 0.045f) {                         // hub
                        c = ironM; if (wr <= 0.026f) c = ironL;
                    } else {
                        // 6 spokes via angular bands; gaps recess to dark wheel back
                        float spoke = Mathf.Cos(wang * 6f);
                        if (spoke > 0.80f) {
                            c = ironM; if (wx < -0.02f) c = ironL; else if (wx > 0.04f) c = ironD;
                        } else {
                            c = ironD;                                 // recessed back of wheel
                            if (wr >= 0.150f) c = ironM;               // inner rim ring
                        }
                    }
                }

                // ---- DRIVE BELT (left tangent run from flywheel up to a small pulley) ----
                if (fx >= 0.385f && fx <= 0.435f && fy >= 0.36f && fy <= 0.54f) {
                    c = beltM; if (fx <= 0.41f) c = beltD;
                }
                // small drive pulley on the block where belt terminates
                if (Disc(fx - 0.41f, fy - 0.555f, 0.05f)) {
                    c = ironM; if (fx < 0.395f) c = ironL; else if (fx > 0.425f) c = ironD;
                }

                // ---- TEAL POWER TERMINALS (right edge of block) ----
                for (int tg = 0; tg < 2; tg++) {
                    float tcy = 0.36f + tg * 0.13f;
                    if (Disc(fx - 0.685f, fy - tcy, 0.028f)) {
                        c = tealC; if (fx < 0.68f && fy > tcy) c = tealL;
                    }
                }

                // ---- EXHAUST SMOKE plume (above stack) ----
                float smx = fx - (0.78f + Mathf.Sin(fy * 7f) * 0.04f);
                if (fy >= 0.90f && fy <= 0.99f && Mathf.Abs(smx) <= 0.06f && c == Clear) c = smoke;
                if (fy >= 0.86f && fy <= 0.98f && Disc(smx, fy - 0.93f, 0.06f) && c == Clear) c = smokeT;

                // ---- BASE SKID / FEET (white structural, runs under everything) ----
                if (fy >= 0.10f && fy <= 0.18f && fx >= 0.12f && fx <= 0.72f && c == Clear) {
                    c = body; if (fy < 0.13f) c = bodyShade;
                }

                // ---- TEAL GLOW HALO (translucent, only on empty pixels) ----
                for (int tg = 0; tg < 2; tg++) {
                    float tcy = 0.36f + tg * 0.13f;
                    if (c == Clear && Disc(fx - 0.685f, fy - tcy, 0.055f)) c = tealGlow;
                }

                // ---- translucent ground shadow (not part of silhouette) ----
                if (fy < 0.10f && fy >= 0.05f && fx >= 0.12f && fx <= 0.84f && c == Clear) c = groundSh;

                px[y * s + x] = c;
            }

            // DARK OUTLINE pass (skip fire/glow + translucent ground shadow)
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
            _ironOilGenerator = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _ironOilGenerator;
        }
    }
}
