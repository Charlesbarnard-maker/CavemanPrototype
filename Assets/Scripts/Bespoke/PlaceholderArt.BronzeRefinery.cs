// Bespoke procedural art for "Refinery" (Age 2). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _bronzeRefinery;
        public static Sprite BronzeRefinery()
        {
            if (_bronzeRefinery != null) return _bronzeRefinery;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);

            // structural retort/tank mass (Color.white -> tinted at runtime)
            var body      = Color.white;
            var bodyHi    = new Color(1f, 1f, 1f, 1f);            // lit upper-left face
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);  // lower-right shadow face

            // cool grey metal (chimney, pipe flanges, catch basin)
            var metalD    = new Color(0.22f, 0.30f, 0.42f, 1f);
            var metalM    = new Color(0.32f, 0.50f, 0.72f, 1f);
            var metalL    = new Color(0.52f, 0.68f, 0.86f, 1f);

            // dark-oil tubing (pipes)
            var oilD      = new Color(0.14f, 0.12f, 0.16f, 1f);
            var oilM      = new Color(0.26f, 0.22f, 0.28f, 1f);
            var oilL      = new Color(0.38f, 0.33f, 0.42f, 1f);

            // bronze/copper sheen (rivet bands, gauge ring, spout, collar)
            var bronzeD   = new Color(0.55f, 0.34f, 0.18f, 1f);
            var bronzeM   = new Color(0.80f, 0.55f, 0.34f, 1f);
            var bronzeL   = new Color(0.95f, 0.72f, 0.42f, 1f);

            // amber fuel
            var fuelD     = new Color(0.62f, 0.34f, 0.06f, 1f);
            var fuelM     = new Color(0.88f, 0.55f, 0.12f, 1f);
            var fuelL     = new Color(1.00f, 0.74f, 0.26f, 1f);

            // gauge dial face
            var dialM     = new Color(0.86f, 0.84f, 0.74f, 1f);
            var dialD     = new Color(0.55f, 0.52f, 0.44f, 1f);

            // fire / smoke / glow
            var fireC     = new Color(1f, 0.55f, 0.12f, 1f);
            var fireL     = new Color(1f, 0.84f, 0.36f, 1f);
            var glow      = new Color(1f, 0.50f, 0.14f, 0.40f);
            var smoke     = new Color(0.82f, 0.82f, 0.84f, 0.70f);

            // ---- tank geometry (a vertical cylinder with a rounded dome cap) ----
            const float tankHalf = 0.20f;                 // half-width of the cylinder
            const float tankBot  = 0.16f;                 // bottom of the cylinder wall
            const float tankTop  = 0.66f;                 // top of the cylindrical section (dome springs here)
            const float domeR    = tankHalf;              // dome radius == half-width (a clean hemisphere)
            const float domeTop  = tankTop + domeR;       // crown of the dome (~0.86)

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ---- BASE PLINTH (dark-oil slab the still stands on) ----
                if (fy >= 0.05f && fy <= 0.155f && Mathf.Abs(dxc) <= 0.30f)
                {
                    c = oilM;
                    if (fy >= 0.13f) c = oilL;            // lit top lip
                    else if (fy <= 0.075f) c = oilD;      // shadow underside
                    else if (dxc > 0.18f) c = oilD;       // right end in shade
                }

                // ---- INLET PIPE (dark tube running into the tank base, lower-left) ----
                float pipeInY = fy - 0.225f;
                if (fx >= 0.04f && fx <= 0.32f && Mathf.Abs(pipeInY) <= 0.045f)
                {
                    float t = (pipeInY + 0.045f) / 0.09f;        // 0 bottom -> 1 top
                    c = oilM;
                    if (t > 0.66f) c = oilL;                     // lit upper curve
                    else if (t < 0.30f) c = oilD;                // shadow lower curve
                    if (Frac(fx * 9f) < 0.12f) c = oilD;         // tube ring seams
                }
                // inlet pipe rim/flange where it meets the tank
                if (fx >= 0.28f && fx <= 0.34f && Mathf.Abs(pipeInY) <= 0.065f)
                {
                    c = metalM;
                    if (fy > 0.225f) c = metalL; else c = metalD;
                }

                // ---- OUTLET PIPE (dark tube out the right side, upper) ----
                float pipeOutY = fy - 0.55f;
                if (fx >= 0.70f && fx <= 0.96f && Mathf.Abs(pipeOutY) <= 0.042f)
                {
                    float t = (pipeOutY + 0.042f) / 0.084f;
                    c = oilM;
                    if (t > 0.66f) c = oilL;
                    else if (t < 0.30f) c = oilD;
                    if (Frac(fx * 9f) < 0.12f) c = oilD;
                }
                // outlet flange at tank wall
                if (fx >= 0.68f && fx <= 0.74f && Mathf.Abs(pipeOutY) <= 0.062f)
                {
                    c = metalM;
                    if (pipeOutY < -0.02f) c = metalL; else c = metalD;
                }

                // ---- MAIN RETORT / TANK (white structural body: cylinder + dome cap) ----
                bool inCyl  = Mathf.Abs(dxc) <= tankHalf && fy >= tankBot && fy <= tankTop;
                bool inDome = Disc(dxc, fy - tankTop, domeR) && fy >= tankTop;
                if (inCyl || inDome)
                {
                    c = body;
                    if (dxc < -0.07f) c = bodyHi;            // lit left face (pure white)
                    else if (dxc > 0.07f) c = bodyShade;     // shadowed right face
                }

                // ---- RIVET BANDS (bronze hoops around the cylinder) ----
                if (inCyl)
                {
                    float[] bands = { 0.255f, 0.43f, 0.605f };
                    for (int b = 0; b < bands.Length; b++)
                    {
                        if (Mathf.Abs(fy - bands[b]) <= 0.020f)
                        {
                            c = bronzeM;
                            if (fy < bands[b]) c = bronzeD;      // shadow under-edge
                            else c = bronzeL;                    // lit top edge
                            if (Frac(fx * 11f) < 0.18f) c = bronzeL; // rivet dots
                        }
                    }
                }

                // ---- PRESSURE GAUGE (round bronze-rimmed dial on tank face) ----
                float ggx = fx - 0.40f, ggy = fy - 0.52f;
                float gr = Mathf.Sqrt(ggx * ggx + ggy * ggy);
                if (gr <= 0.085f)
                {
                    if (gr >= 0.062f)
                    {
                        c = bronzeM;
                        if (ggx + ggy < 0f) c = bronzeL; else c = bronzeD;   // lit upper-left rim
                    }
                    else
                    {
                        c = dialM;
                        if (ggx > 0.015f && ggy < -0.01f) c = dialD;          // shaded lower-right of face
                        if (Mathf.Abs(ggy - ggx * 0.8f) < 0.010f && ggx >= -0.005f && ggx <= 0.045f)
                            c = outline;                                       // needle pointing upper-right
                        if (Disc(ggx, ggy, 0.012f)) c = oilD;                 // hub
                    }
                }

                // ---- CHIMNEY / FLARE STACK (thin grey tube rising from the dome crown) ----
                if (Mathf.Abs(dxc) <= 0.045f && fy >= domeTop - 0.02f && fy <= 0.94f)
                {
                    c = metalM;
                    if (dxc < -0.012f) c = metalL; else if (dxc > 0.012f) c = metalD;
                    if (Frac(fy * 16f) < 0.16f) c = metalD;     // stack ribs
                }
                // stack collar at the dome crown
                if (Mathf.Abs(dxc) <= 0.065f && fy >= domeTop - 0.03f && fy <= domeTop + 0.005f)
                {
                    c = bronzeM;
                    if (dxc < 0f) c = bronzeL; else c = bronzeD;
                }

                // ---- FLAME at the flare top ----
                float fly = fy - 0.945f;
                if (fly >= 0f && fly <= 0.055f && Mathf.Abs(dxc) <= (0.040f - fly * 0.45f))
                {
                    c = (fly > 0.030f) ? fireL : fireC;
                    if (Mathf.Abs(dxc) < 0.014f && fly > 0.020f) c = fireL;
                }

                // ---- FUEL SPOUT (bronze nozzle out the lower-right of the tank) ----
                if (fx >= 0.66f && fx <= 0.78f && fy >= 0.255f && fy <= 0.305f)
                {
                    c = bronzeM;
                    if (fy >= 0.29f) c = bronzeL; else if (fy <= 0.27f) c = bronzeD;
                }
                // angled spout tip
                if (fx >= 0.76f && fx <= 0.83f && fy >= 0.225f && fy <= 0.275f)
                {
                    c = bronzeD; if (fx < 0.795f) c = bronzeM;
                }

                // ---- AMBER FUEL bead dripping from the spout tip ----
                if (Disc(fx - 0.80f, fy - 0.195f, 0.024f))
                {
                    c = fuelM;
                    if (fx < 0.79f && fy > 0.195f) c = fuelL;       // lit upper-left
                    else if (fy < 0.185f) c = fuelD;                // shadow underside
                }
                // catch basin (small dark-metal cup below the bead)
                if (fx >= 0.73f && fx <= 0.87f && fy >= 0.095f && fy <= 0.15f)
                {
                    c = metalD; if (fy >= 0.13f) c = metalM;        // lit top rim
                }
                // amber pool in the catch
                if (fx >= 0.745f && fx <= 0.855f && fy >= 0.125f && fy <= 0.145f)
                {
                    c = fuelM; if (fx < 0.80f) c = fuelL;
                }

                // ---- warm translucent glow halo around the flare (drawn only on empty pixels) ----
                if (c == Clear && Disc(dxc, fy - 0.945f, 0.075f) && fy >= 0.90f)
                    c = glow;

                // ---- small smoke puff above the flame ----
                if (c == Clear && Disc(dxc + 0.01f, fy - 1.0f, 0.04f) && fy <= 0.99f)
                    c = smoke;

                // ---- translucent ground shadow (NOT part of silhouette) ----
                if (c == Clear && fy < 0.06f && fy >= 0.03f && Mathf.Abs(dxc) <= 0.40f)
                    c = new Color(0.12f, 0.10f, 0.08f, 0.45f);

                px[y * s + x] = c;
            }

            // ---- DARK OUTLINE PASS (skip fire/glow/smoke/translucent-shadow pixels) ----
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
            _bronzeRefinery = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _bronzeRefinery;
        }
    }
}
