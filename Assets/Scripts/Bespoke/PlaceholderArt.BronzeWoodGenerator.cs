// Bespoke procedural art for "Wood Generator" (Age 2). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _bronzeWoodGenerator;
        public static Sprite BronzeWoodGenerator()
        {
            if (_bronzeWoodGenerator != null) return _bronzeWoodGenerator;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            var body      = Color.white;                          // structural housing, tinted at runtime
            var bodyHi    = new Color(1f, 1f, 1f, 1f);            // lit (upper-left) faces
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);  // shadow (lower-right) faces

            // smokestack metal (cool iron, baked dark so it survives tinting)
            var stackD = new Color(0.18f, 0.20f, 0.24f, 1f);
            var stackM = new Color(0.30f, 0.33f, 0.38f, 1f);
            var stackL = new Color(0.46f, 0.50f, 0.56f, 1f);
            // copper band on the stack
            var bandD  = new Color(0.40f, 0.26f, 0.12f, 1f);
            var bandL  = new Color(0.66f, 0.44f, 0.22f, 1f);
            // smoke puffs
            var smokeM = new Color(0.78f, 0.78f, 0.80f, 0.72f);
            var smokeL = new Color(0.88f, 0.88f, 0.90f, 0.72f);

            // firebox door + fire
            var fireboxD = new Color(0.10f, 0.09f, 0.10f, 1f);
            var fireboxM = new Color(0.20f, 0.18f, 0.18f, 1f);
            var fireC    = new Color(1f, 0.55f, 0.12f, 1f);       // fire core
            var fireL    = new Color(1f, 0.84f, 0.36f, 1f);       // fire bright
            var glow     = new Color(1f, 0.50f, 0.14f, 0.40f);    // warm translucent glow

            // cut log ends (wood) inside the hopper
            var woodD = new Color(0.28f, 0.17f, 0.09f, 1f);
            var woodM = new Color(0.44f, 0.28f, 0.15f, 1f);
            var woodL = new Color(0.60f, 0.42f, 0.24f, 1f);
            // terracotta hopper frame
            var hopperD = new Color(0.40f, 0.23f, 0.18f, 1f);
            var hopperM = new Color(0.58f, 0.35f, 0.28f, 1f);
            var hopperL = new Color(0.76f, 0.50f, 0.40f, 1f);

            // flywheel (dark iron) + brass hub + drive belt
            var wheelD = new Color(0.12f, 0.12f, 0.14f, 1f);
            var wheelM = new Color(0.22f, 0.23f, 0.26f, 1f);
            var wheelL = new Color(0.38f, 0.40f, 0.44f, 1f);
            var hubD   = new Color(0.40f, 0.26f, 0.12f, 1f);
            var hubL   = new Color(0.74f, 0.54f, 0.24f, 1f);
            var beltD  = new Color(0.13f, 0.11f, 0.09f, 1f);

            // copper power terminal / coil + teal-green spark
            var copperD = new Color(0.42f, 0.25f, 0.12f, 1f);
            var copperM = new Color(0.62f, 0.40f, 0.20f, 1f);
            var copperL = new Color(0.86f, 0.60f, 0.30f, 1f);
            var sparkC  = new Color(0.20f, 0.82f, 0.66f, 1f);
            var sparkL  = new Color(0.60f, 1f, 0.90f, 1f);
            var sparkG  = new Color(0.24f, 0.84f, 0.68f, 0.38f); // translucent teal glow

            // flywheel centre + radius (sits on the LEFT, partly proud of the body)
            const float wcx = 0.165f, wcy = 0.34f, wR = 0.150f;

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ===== WHITE-BODY HOUSING (boxy machine block) =====
                if (fx >= 0.20f && fx <= 0.82f && fy >= 0.12f && fy <= 0.74f)
                {
                    c = body;
                    if (fx <= 0.30f) c = bodyHi;                 // lit left face
                    else if (fx >= 0.72f) c = bodyShade;         // shadow right face
                    if (fy >= 0.68f) c = bodyHi;                 // lit top face
                    else if (fy <= 0.18f) c = bodyShade;         // shadow base
                    // top bevel highlight
                    if (fy >= 0.70f && fy <= 0.74f && fx >= 0.22f && fx <= 0.80f) c = bodyHi;
                    // riveted seam down the shadow side
                    if (fx >= 0.69f && fx <= 0.715f && fy >= 0.20f && fy <= 0.72f) c = bodyShade;
                }

                // ===== SMOKESTACK (top-right, rising above the housing) =====
                if (fx >= 0.62f && fx <= 0.76f && fy >= 0.74f && fy <= 0.95f)
                {
                    c = stackM;
                    if (fx <= 0.66f) c = stackL; else if (fx >= 0.72f) c = stackD;
                    if (fy >= 0.84f && fy <= 0.88f) c = (fx <= 0.69f) ? bandL : bandD; // copper band
                    if (fy >= 0.91f) c = (fx <= 0.69f) ? stackL : stackD;              // rim cap
                }

                // ===== SMOKE puffs venting from the stack top =====
                float smy = fy - 0.95f;
                bool smoke = fy >= 0.95f && (
                    Disc(fx - 0.69f, smy - 0.010f, 0.045f) ||
                    Disc(fx - 0.77f, smy - 0.040f, 0.050f) ||
                    Disc(fx - 0.64f, smy - 0.055f, 0.038f));
                if (smoke)
                    c = (fx <= 0.68f) ? smokeL : smokeM;

                // ===== HOPPER (right side) holding stacked cut WOOD =====
                if (fx >= 0.66f && fx <= 0.86f && fy >= 0.36f && fy <= 0.66f)
                {
                    c = hopperM;
                    if (fx <= 0.70f) c = hopperL; else if (fx >= 0.81f) c = hopperD; // 3-tone frame
                    if (fy >= 0.62f) c = hopperL;                                    // lit top rim
                    else if (fy <= 0.40f) c = hopperD;                               // shadow lip
                    // stacked log ends (concentric rings) tucked inside the hopper
                    float[] lxs = { 0.725f, 0.805f, 0.765f };
                    float[] lys = { 0.50f,  0.50f,  0.575f };
                    for (int i = 0; i < 3; i++)
                    {
                        float lx = fx - lxs[i], ly = fy - lys[i];
                        if (Disc(lx, ly, 0.040f))
                        {
                            float r2 = lx * lx + ly * ly;
                            c = woodM;
                            if (r2 <= 0.016f * 0.016f) c = woodL;          // pale heart
                            else if (r2 >= 0.030f * 0.030f) c = woodD;     // dark bark ring
                            if (lx < -0.014f && ly > 0.014f) c = woodL;    // lit upper-left rim
                        }
                    }
                }

                // ===== FIREBOX door (low-centre) glowing ORANGE =====
                float ax = fx - 0.42f, ay = fy - 0.26f;
                bool arch = (Mathf.Abs(ax) <= 0.095f && fy >= 0.15f && fy <= 0.26f) || Disc(ax, ay, 0.095f);
                if (arch && fy <= 0.36f)
                {
                    c = fireboxM;
                    if (fx >= 0.47f || fy <= 0.17f) c = fireboxD;     // shadowed frame edges
                    // inner glowing mouth
                    bool inner = (Mathf.Abs(ax) <= 0.060f && fy >= 0.16f && fy <= 0.26f) || Disc(ax, ay, 0.060f);
                    if (inner)
                    {
                        c = fireC;
                        if (Disc(ax, ay - 0.005f, 0.034f)) c = fireL;    // bright core
                        // flicker tongues licking upward
                        if (fy >= 0.22f && Frac(fx * 11f + fy * 4f) < 0.30f) c = fireL;
                    }
                }
                // warm translucent glow spilling out below the door (skipped by outline)
                if (c == Clear && Disc(ax, ay, 0.135f) && !arch && fy >= 0.12f)
                    c = glow;

                // ===== FLYWHEEL (left side): dark spoked disc + brass hub =====
                float wx = fx - wcx, wy = fy - wcy;
                float wr = Mathf.Sqrt(wx * wx + wy * wy);
                if (wr <= wR)
                {
                    float ang = Mathf.Atan2(wy, wx);
                    if (wr >= 0.124f)                               // outer rim
                        c = (wx - wy < 0f) ? wheelL : wheelD;
                    else if (wr >= 0.050f)                          // spoke field
                    {
                        float sp = Frac(ang * (6f / 6.2831853f) + 0.5f);
                        bool spoke = sp < 0.16f || sp > 0.84f;
                        c = spoke ? ((wx - wy < 0f) ? wheelL : wheelM) : wheelD;
                    }
                    else                                            // brass hub
                        c = (wx - wy < 0f) ? hubL : hubD;
                }
                // drive belt: thin band over the wheel's top rim, climbing into the housing
                if (fx >= wcx - 0.012f && fx <= wcx + 0.018f && fy >= wcy + 0.08f && fy <= 0.60f)
                    c = beltD;

                // ===== COPPER POWER TERMINAL / coil (low-right of housing) =====
                if (fx >= 0.66f && fx <= 0.78f && fy >= 0.15f && fy <= 0.30f)
                {
                    c = copperM;
                    if (fx <= 0.70f) c = copperL; else if (fx >= 0.75f) c = copperD; // 3-tone
                    if (Frac(fy * 14f) < 0.5f) c = (fx <= 0.72f) ? copperL : copperD; // coil windings
                }
                // teal spark crackling above the terminal
                float spx = fx - 0.72f, spy = fy - 0.355f;
                bool spark =
                    Disc(spx, spy, 0.020f) ||
                    (Mathf.Abs(spx) <= 0.010f && fy >= 0.31f && fy <= 0.40f) ||
                    (Mathf.Abs(spy) <= 0.010f && fx >= 0.69f && fx <= 0.75f);
                if (spark)
                {
                    c = sparkC;
                    if (Disc(spx, spy, 0.009f)) c = sparkL;
                }
                else if (c == Clear && Disc(spx, spy, 0.045f))      // soft teal halo (skipped by outline)
                    c = sparkG;

                // ===== translucent ground shadow (NOT part of the silhouette) =====
                if (c == Clear && fy < 0.12f && fy >= 0.085f && fx >= 0.04f && fx <= 0.86f)
                    c = new Color(0.10f, 0.08f, 0.06f, 0.45f);

                px[y * s + x] = c;
            }

            // ===== DARK OUTLINE pass (skip fire/glow + translucent pixels) =====
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
            _bronzeWoodGenerator = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _bronzeWoodGenerator;
        }
    }
}
