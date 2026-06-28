// Bespoke procedural art for "Coal Generator" (Age 3). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _ironCoalGenerator;
        public static Sprite IronCoalGenerator()
        {
            if (_ironCoalGenerator != null) return _ironCoalGenerator;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            // structural white body (tinted at runtime)
            var body      = Color.white;
            var bodyHi    = new Color(1f, 1f, 1f, 1f);
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);
            // dark cool-iron boiler bands / stack (baked)
            var ironD     = new Color(0.18f, 0.20f, 0.24f, 1f);
            var ironM     = new Color(0.30f, 0.33f, 0.38f, 1f);
            var ironL     = new Color(0.48f, 0.52f, 0.58f, 1f);
            // rivets
            var rivet     = new Color(0.62f, 0.66f, 0.72f, 1f);
            var rivetD    = new Color(0.14f, 0.16f, 0.19f, 1f);
            // firebox fire
            var fireDeep  = new Color(0.70f, 0.22f, 0.05f, 1f);
            var fireC     = new Color(1f, 0.45f, 0.10f, 1f);
            var fireL     = new Color(1f, 0.78f, 0.30f, 1f);
            var glow      = new Color(1f, 0.42f, 0.12f, 0.40f);
            // coal pile
            var coalD     = new Color(0.07f, 0.07f, 0.09f, 1f);
            var coalM     = new Color(0.14f, 0.14f, 0.17f, 1f);
            var coalL     = new Color(0.26f, 0.26f, 0.30f, 1f);
            // smoke (grey-black)
            var smokeD    = new Color(0.30f, 0.30f, 0.32f, 0.80f);
            var smokeM    = new Color(0.48f, 0.48f, 0.50f, 0.78f);
            var smokeL    = new Color(0.66f, 0.66f, 0.68f, 0.72f);
            // flywheel (brass/iron)
            var wheelD    = new Color(0.20f, 0.22f, 0.26f, 1f);
            var wheelM    = new Color(0.34f, 0.37f, 0.42f, 1f);
            var wheelFace = new Color(0.16f, 0.18f, 0.21f, 1f);
            var brassL    = new Color(0.82f, 0.62f, 0.28f, 1f);
            var brassM    = new Color(0.55f, 0.40f, 0.18f, 1f);
            // teal power terminal spark
            var tealC     = new Color(0.16f, 0.66f, 0.62f, 1f);
            var tealL     = new Color(0.50f, 0.92f, 0.86f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ===== SMOKE plume rising from stack (drawn first, behind body) =====
                // stack centred at fx ~0.34; plume drifts up and to the right
                float smx = fx - (0.34f + (fy - 0.80f) * 0.34f);
                if (fy >= 0.80f) {
                    float puff = 0.085f + 0.050f * Mathf.Sin(fy * 11f + 1.3f);
                    if (Disc(smx, 0f, puff) && Frac(fx * 5f + fy * 6f) > 0.12f) {
                        c = smokeM;
                        if (smx < -0.02f && fy < 0.92f) c = smokeL;
                        else if (smx > 0.03f || fy > 0.94f) c = smokeD;
                    }
                }

                // ===== TALL WIDE SMOKESTACK on top (fx 0.255..0.425, fy 0.60..0.80) =====
                if (fx >= 0.255f && fx <= 0.425f && fy >= 0.60f && fy <= 0.80f) {
                    float t = (fx - 0.255f) / 0.17f;
                    c = ironM; if (t < 0.30f) c = ironL; else if (t > 0.66f) c = ironD;
                    // rivet band near the stack rim
                    if (fy >= 0.755f && fy <= 0.775f) {
                        c = ironD;
                        if (Frac(fx * 14f) < 0.30f) c = rivet;
                    }
                }
                // stack lip (slightly wider rim at the very top)
                if (fx >= 0.245f && fx <= 0.435f && fy >= 0.785f && fy <= 0.815f) {
                    float t = (fx - 0.245f) / 0.19f;
                    c = ironM; if (t < 0.30f) c = ironL; else if (t > 0.66f) c = ironD;
                }

                // ===== BOILER HOUSING (white structural body, rounded) =====
                if (InRoundedRect(x, y, s, 0.10f, 0.12f) && fy >= 0.085f && fy <= 0.66f) {
                    // upper-left lit, lower-right shaded
                    c = body;
                    if (dxc < -0.04f && fy > 0.40f) c = bodyHi;
                    else if (dxc > 0.10f || fy < 0.16f) c = bodyShade;

                    // horizontal riveted iron BANDS across the boiler
                    float[] bandY = { 0.20f, 0.38f, 0.56f };
                    for (int b = 0; b < bandY.Length; b++) {
                        if (fy >= bandY[b] - 0.030f && fy <= bandY[b] + 0.030f) {
                            float bt = (fy - (bandY[b] - 0.030f)) / 0.060f;
                            c = ironM; if (bt < 0.32f) c = ironL; else if (bt > 0.68f) c = ironD;
                            // rivets along the band
                            if (Frac(fx * 11f) < 0.22f) c = (bt < 0.5f) ? rivet : rivetD;
                        }
                    }
                }

                // ===== FIREBOX door low-centre, glowing deep ORANGE =====
                float fbx = dxc, fby = fy - 0.20f;
                if (Mathf.Abs(fbx) <= 0.12f && fy >= 0.105f && fy <= 0.295f) {
                    // iron door frame
                    c = ironD;
                    if (Mathf.Abs(fbx) <= 0.092f && fy >= 0.125f && fy <= 0.275f) {
                        // glowing interior
                        float gr = Mathf.Sqrt(fbx * fbx + fby * fby);
                        c = fireDeep;
                        if (gr <= 0.085f) c = fireC;
                        if (gr <= 0.045f) c = fireL;
                        // flicker streaks
                        if (Frac(fx * 9f + fy * 7f) < 0.16f && gr <= 0.10f) c = fireL;
                    }
                    else if (Mathf.Abs(fbx) >= 0.095f && Frac(fy * 16f) < 0.40f) {
                        // door hinges/rivets on the frame
                        c = rivet;
                    }
                }
                // warm translucent glow spilling out around the firebox
                if (c == Clear && Disc(dxc, fy - 0.20f, 0.165f) && fy >= 0.06f)
                    c = glow;

                // ===== COAL PILE heaped at base, right side =====
                float coalTop = 0.155f - (fx - 0.78f) * (fx - 0.78f) * 3.2f;
                if (fx >= 0.62f && fx <= 0.96f && fy <= coalTop && fy >= 0.045f) {
                    float n = Frac(fx * 17f + fy * 23f);
                    c = coalM;
                    if (n < 0.30f) c = coalD; else if (n > 0.74f) c = coalL;
                    // chunk facets: upper-left lumps catch light
                    if (Frac(fx * 8f) < 0.14f && fy > 0.08f) c = coalL;
                }

                // ===== FLYWHEEL on the left flank (spoked) =====
                float wx = fx - 0.155f, wy = fy - 0.235f;
                float wr = Mathf.Sqrt(wx * wx + wy * wy);
                if (wr <= 0.135f) {
                    float wang = Mathf.Atan2(wy, wx);
                    if (wr >= 0.108f) {
                        // outer rim, 3-tone by upper-left vs lower-right
                        c = (wx - wy < 0f) ? wheelM : wheelD;
                    } else if (wr <= 0.034f) {
                        // hub
                        c = (wx - wy < 0f) ? brassL : brassM;
                    } else {
                        // 6 spokes over a solid dark face plate (keeps silhouette closed)
                        float spoke = Mathf.Abs(Frac(wang / 6.2831853f * 6f + 0.5f) - 0.5f);
                        c = (spoke < 0.14f) ? ((wx - wy < 0f) ? wheelM : wheelD) : wheelFace;
                    }
                }

                // ===== POWER TERMINALS (teal spark) mid-right flank =====
                float tx = fx - 0.86f, ty = fy - 0.40f;
                if (Disc(tx, ty, 0.058f)) {
                    c = ironD;
                    if (Disc(tx, ty, 0.032f)) c = tealC;
                    if (Disc(tx, ty, 0.015f)) c = tealL;
                }
                // little spark prongs sticking up from the terminal
                if (c == Clear && fy >= 0.46f && fy <= 0.55f) {
                    if (Mathf.Abs(fx - 0.825f) < 0.012f) c = tealL;
                    if (Mathf.Abs(fx - 0.895f) < 0.012f) c = tealL;
                }

                // translucent ground shadow (NOT part of the silhouette)
                if (fy < 0.055f && fy >= 0.025f && fx >= 0.08f && fx <= 0.94f && c == Clear)
                    c = new Color(0.10f, 0.09f, 0.08f, 0.45f);

                px[y * s + x] = c;
            }

            // DARK OUTLINE pass: only fully-opaque pixels count as silhouette;
            // skip fire/glow/teal sparks & the translucent ground shadow in the neighbour test.
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
            _ironCoalGenerator = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _ironCoalGenerator;
        }
    }
}
