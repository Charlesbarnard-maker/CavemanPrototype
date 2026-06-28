// Bespoke procedural art for "Kiln" (Age 2). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _bronzeKiln;
        public static Sprite BronzeKiln()
        {
            if (_bronzeKiln != null) return _bronzeKiln;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            var body      = Color.white;                          // structural dome, tinted terracotta at runtime
            var bodyHi    = new Color(1f, 1f, 1f, 1f);            // lit upper-left face
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);  // lower-right shadow face
            var seam      = new Color(0.40f, 0.22f, 0.16f, 1f);  // baked brick-course seam line

            var brickD    = new Color(0.46f, 0.26f, 0.20f, 1f);  // finished red bricks (terracotta family)
            var brickM    = new Color(0.66f, 0.40f, 0.34f, 1f);
            var brickL    = new Color(0.80f, 0.52f, 0.42f, 1f);

            var stoneD    = new Color(0.30f, 0.30f, 0.34f, 1f);  // chimney vent (cool metal/stone)
            var stoneM    = new Color(0.46f, 0.47f, 0.52f, 1f);
            var stoneL    = new Color(0.62f, 0.64f, 0.70f, 1f);

            var mouthDark = new Color(0.12f, 0.07f, 0.05f, 1f);  // fire-mouth interior
            var fireC     = new Color(1f, 0.55f, 0.12f, 1f);
            var fireL     = new Color(1f, 0.84f, 0.36f, 1f);
            var glow      = new Color(1f, 0.50f, 0.14f, 0.40f);
            var smoke     = new Color(0.82f, 0.82f, 0.84f, 0.7f);

            // dome geometry: a tall beehive half-disc sitting on a low brick skirt
            const float domeCx = 0.0f;    // dxc centre column
            const float domeCy = 0.14f;   // fy of the dome spring line (where dome meets skirt)
            const float domeR  = 0.46f;   // dome radius -> top reaches fy ~0.60
            const float ventTop = domeCy + domeR; // fy of dome crown

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                float ddx = dxc - domeCx, ddy = fy - domeCy;
                float dr  = Mathf.Sqrt(ddx * ddx + ddy * ddy);
                bool inDome = (dr <= domeR) && (fy >= domeCy - 0.01f);                 // upper half-disc beehive
                bool inBase = (fy >= 0.05f && fy < domeCy + 0.01f && Mathf.Abs(dxc) <= domeR - 0.02f); // squat skirt

                // ---- WHITE BODY (dome + base skirt), formed with two near-white bakes ----
                if (inDome || inBase) {
                    c = body;
                    // light upper-left, shadow lower-right (offset by height so the curve reads round)
                    if (ddx + (fy - domeCy) * 0.30f < -0.06f) c = bodyHi;
                    else if (ddx - (fy - domeCy) * 0.30f > 0.10f) c = bodyShade;

                    // baked BRICK-COURSE seams: concentric arcs curving over the dome
                    float courseT = Frac(dr * 8.5f);
                    int course = (int)Mathf.Floor(dr * 8.5f);
                    if (inDome && dr > 0.05f && courseT < 0.13f) c = seam;
                    // staggered vertical brick joints between the curved courses
                    float ang = Mathf.Atan2(ddy, ddx);
                    float vt = Frac(ang * 2.55f + course * 0.5f);
                    if (inDome && dr > 0.10f && courseT >= 0.13f && vt < 0.05f) c = seam;
                    // horizontal mortar lines on the base skirt
                    if (inBase && !inDome && Frac(fy * 22f) < 0.18f) c = seam;
                }

                // ---- ARCHED FIRE MOUTH low-centre ----
                float mx = dxc, my = fy - 0.115f;
                bool archDisc = Disc(mx, my, 0.13f) && fy >= 0.085f;                  // rounded top
                bool archBody = Mathf.Abs(mx) <= 0.11f && fy >= 0.05f && fy <= 0.115f; // squared bottom
                if (archDisc || archBody) {
                    c = mouthDark;
                    // glowing orange fire inside, brightest near the floor
                    float gx = mx, gy = fy - 0.095f;
                    float gr = Mathf.Sqrt(gx * gx + gy * gy);
                    if (gr <= 0.095f) {
                        c = fireC;
                        if (gr <= 0.045f) c = fireL;
                        if (gy < -0.015f) c = fireL;                                   // hotter low in the mouth
                    } else if (gr <= 0.11f && fy <= 0.12f) {
                        c = fireC;
                    }
                }

                // ---- CHIMNEY VENT at dome crown ----
                bool vent = Mathf.Abs(dxc) <= 0.05f && fy >= ventTop - 0.04f && fy <= ventTop + 0.075f;
                if (vent) {
                    c = stoneM;
                    if (dxc < -0.01f) c = stoneL; else if (dxc > 0.015f) c = stoneD;
                    if (fy >= ventTop + 0.05f) c = stoneD;                            // dark rim / opening
                }

                // ---- STACK OF FINISHED RED BRICKS at base, right side ----
                if (fx >= 0.67f && fx <= 0.95f && fy >= 0.05f && fy <= 0.235f) {
                    int row = (int)Mathf.Floor((fy - 0.05f) / 0.06f);
                    float rowOff = (row % 2 == 0) ? 0f : 0.045f;
                    float u = Frac((fx - 0.67f + rowOff) * 5.4f);
                    float vrow = Frac((fy - 0.05f) / 0.06f);
                    c = brickM;
                    if (u < 0.12f || vrow < 0.14f) c = brickD;                        // mortar gaps
                    else if (vrow > 0.62f) c = brickD;                                // lower edge shade
                    else if (vrow < 0.40f && u > 0.45f) c = brickL;                   // upper-left face highlight
                }

                // ---- SMOKE WISP rising from the vent ----
                float sBase = ventTop + 0.075f;
                float sx = dxc - 0.015f - Mathf.Sin((fy - sBase) * 7f) * 0.05f;
                if (fy >= sBase && fy <= 0.97f && Mathf.Abs(sx) <= 0.045f - (fy - sBase) * 0.06f) {
                    if (c == Clear) c = smoke;
                }

                // ---- translucent warm glow spilling from the fire mouth (not silhouette) ----
                if (c == Clear) {
                    float gx = dxc, gy = fy - 0.11f;
                    if (Disc(gx, gy, 0.20f) && fy >= 0.04f && fy <= 0.27f) c = glow;
                }

                // ---- translucent ground shadow ----
                if (c == Clear && fy < 0.06f && fy >= 0.03f && Mathf.Abs(dxc) <= 0.47f)
                    c = new Color(0.12f, 0.10f, 0.08f, 0.45f);

                px[y * s + x] = c;
            }

            // DARK OUTLINE pass: only fully-opaque pixels count as silhouette (skip fire/glow/smoke/shadow)
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
            _bronzeKiln = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _bronzeKiln;
        }
    }
}
