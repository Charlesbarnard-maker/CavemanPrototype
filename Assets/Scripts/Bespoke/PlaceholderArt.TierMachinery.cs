// Bespoke procedural art for the UPGRADE MACHINERY overlay — a cumulative cluster of tech that bolts onto a
// building's upper-right and GROWS with its upgrade tier, so a place visibly mechanises as you advance:
//   tier 1 = a bronze gear · tier 2 = + an iron gear & piston · tier 3 = + a smokestack (smoke) & a glowing
//   power panel. Drawn full colour on its OWN object (white tint), so it never fights the building's body tint.
// 64x64 to overlay the building 1:1; machinery lives in the upper-right quadrant. frame 0..3 spins the gears.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite[] _tierMach; // tier(1..3) * 4 + frame
        /// <summary>The machinery overlay for an upgrade tier (1..3), animated over frame 0..3. tier&lt;=0 → null.</summary>
        public static Sprite TierMachinery(int tier, int frame)
        {
            if (tier <= 0) return null;
            tier = Mathf.Clamp(tier, 1, 3); frame = ((frame % 4) + 4) % 4;
            if (_tierMach == null) _tierMach = new Sprite[3 * 4];
            int idx = (tier - 1) * 4 + frame;
            if (_tierMach[idx] != null) return _tierMach[idx];

            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];
            float phase = frame * 0.25f;

            var plate = new Color(0.34f, 0.36f, 0.40f, 1f);
            var plateD = new Color(0.22f, 0.23f, 0.27f, 1f);
            var bolt = new Color(0.62f, 0.64f, 0.70f, 1f);
            // bronze gear (tier 1)
            Color brH = new Color(0.88f, 0.62f, 0.32f, 1f), brM = new Color(0.70f, 0.46f, 0.22f, 1f), brD = new Color(0.48f, 0.30f, 0.14f, 1f);
            // iron gear / piston (tier 2)
            Color feH = new Color(0.80f, 0.83f, 0.89f, 1f), feM = new Color(0.56f, 0.59f, 0.66f, 1f), feD = new Color(0.34f, 0.36f, 0.42f, 1f);
            // smokestack + glow (tier 3)
            Color stackD = new Color(0.20f, 0.20f, 0.24f, 1f), stackM = new Color(0.32f, 0.32f, 0.37f, 1f);
            Color glow = new Color(0.40f, 1f, 0.55f, 1f), glowD = new Color(0.20f, 0.55f, 0.30f, 1f);
            var smoke = new Color(0.78f, 0.78f, 0.80f, 0.7f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                Color c = Clear;

                // compact machine HOUSING tucked into the upper-right corner (all tiers >= 1)
                if (fx >= 0.64f && fx <= 0.965f && fy >= 0.62f && fy <= 0.84f)
                {
                    c = plate;
                    if (fy >= 0.795f) c = new Color(0.46f, 0.48f, 0.53f, 1f);     // lit top edge
                    if (fy < 0.67f || fx > 0.91f) c = plateD;                     // lower / right shadow
                    if (fy >= 0.745f && fy <= 0.765f) c = plateD;                 // panel seam
                    if (fx >= 0.80f && fx <= 0.94f && fy >= 0.655f && fy <= 0.72f && Frac(fx * 30f) < 0.5f) c = plateD; // vent grille
                    if (Disc(fx - 0.665f, fy - 0.815f, 0.015f) || Disc(fx - 0.945f, fy - 0.815f, 0.015f) ||
                        Disc(fx - 0.665f, fy - 0.645f, 0.015f) || Disc(fx - 0.945f, fy - 0.645f, 0.015f)) c = bolt; // corner rivets
                }

                // TIER 1 — a bronze gear mounted on the housing (pokes above the rim)
                Color g1 = Gear(fx, fy, 0.725f, 0.78f, 0.092f, phase, brH, brM, brD);
                if (g1.a > 0f) c = g1;

                // TIER 2 — an iron gear + a piston rod
                if (tier >= 2)
                {
                    Color g2 = Gear(fx, fy, 0.885f, 0.74f, 0.072f, -phase * 1.3f, feH, feM, feD);
                    if (g2.a > 0f) c = g2;
                    float pe = 0.018f * Mathf.Sin(phase * 6.2832f);               // slides with the frame
                    if (fx >= 0.675f && fx <= 0.712f && fy >= 0.84f + pe && fy <= 0.965f + pe) { c = feM; if (fx > 0.70f) c = feD; }
                    if (fx >= 0.66f && fx <= 0.727f && fy >= 0.955f + pe && fy <= 0.99f + pe) c = feH; // piston head
                }

                // TIER 3 — a smokestack (rising puff) + a glowing power light
                if (tier >= 3)
                {
                    if (fx >= 0.85f && fx <= 0.915f && fy >= 0.84f && fy <= 0.99f) { c = stackM; if (fx > 0.885f) c = stackD; }
                    if (fx >= 0.84f && fx <= 0.925f && fy >= 0.96f && fy <= 0.99f) c = stackD; // rim
                    float pf = frame / 4f;
                    float sxp = 0.882f + 0.05f * pf, syp = 0.99f + 0.06f * pf;
                    if (c.a < 0.05f && Disc(fx - sxp, fy - syp, 0.05f - 0.02f * pf)) c = smoke;
                    if (fx >= 0.685f && fx <= 0.74f && fy >= 0.635f && fy <= 0.685f) c = (frame % 2 == 0) ? glow : glowD; // power light
                }

                px[y * s + x] = c;
            }

            // dark outline around the solid machinery (skip translucent smoke)
            var outline = new Color(0.08f, 0.06f, 0.06f, 1f);
            var outPx = new Color[s * s];
            System.Array.Copy(px, outPx, px.Length);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                if (px[y * s + x].a > 0.05f) continue;
                bool adj = false;
                for (int oy = -1; oy <= 1 && !adj; oy++) for (int ox = -1; ox <= 1; ox++)
                {
                    int nx = x + ox, ny = y + oy;
                    if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                    if (px[ny * s + nx].a > 0.9f) { adj = true; break; }
                }
                if (adj) outPx[y * s + x] = outline;
            }
            tex.SetPixels(outPx); tex.Apply();
            _tierMach[idx] = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _tierMach[idx];
        }

        // A toothed gear centred (cx,cy) radius r, teeth + spokes rotating by `phase` turns.
        private static Color Gear(float fx, float fy, float cx, float cy, float r, float phase, Color hi, Color mid, Color dk)
        {
            float dx = fx - cx, dy = fy - cy; float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > r * 1.18f) return Clear;
            float ang = Mathf.Atan2(dy, dx) / (Mathf.PI * 2f);
            float tooth = Frac(ang * 8f + phase);
            float rim = (tooth < 0.5f) ? r * 1.16f : r * 0.94f;  // 8 teeth poke out
            if (d > rim) return Clear;
            if (d <= r * 0.20f) return dk;                        // hub
            if (d >= r * 0.66f) return (tooth < 0.5f) ? hi : mid; // rim + teeth (lit on the tooth face)
            float sp = Frac(ang * 4f + phase);                    // 4 spokes
            if (sp < 0.13f || sp > 0.87f) return mid;
            return dk;                                            // gaps between spokes
        }
    }
}
