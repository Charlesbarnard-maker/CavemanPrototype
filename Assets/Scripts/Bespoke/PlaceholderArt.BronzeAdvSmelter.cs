// Bespoke procedural art for "Advanced Smelter" (Age 2). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _bronzeAdvSmelter;
        public static Sprite BronzeAdvSmelter()
        {
            if (_bronzeAdvSmelter != null) return _bronzeAdvSmelter;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);

            // structural furnace body (white -> tinted at runtime). Keep bright so tint reads.
            var body      = Color.white;
            var bodyHi    = new Color(1f, 1f, 1f, 1f);                // lit upper-left faces
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);       // shadow lower-right faces

            // copper/bronze sheen accents (baked, darkish/saturated -> survive tinting)
            var bronzeD = new Color(0.52f, 0.33f, 0.16f, 1f);
            var bronzeM = new Color(0.80f, 0.55f, 0.34f, 1f);
            var bronzeL = new Color(0.95f, 0.72f, 0.42f, 1f);

            // oil-dark iron for grate bars / mould (3 tones)
            var ironD = new Color(0.14f, 0.12f, 0.16f, 1f);
            var ironM = new Color(0.26f, 0.22f, 0.28f, 1f);
            var ironL = new Color(0.40f, 0.36f, 0.44f, 1f);

            // fire / heat (orange -> yellow -> white-hot core)
            var fireCore = new Color(1f, 0.97f, 0.85f, 1f);          // white-hot molten core
            var fireY    = new Color(1f, 0.84f, 0.36f, 1f);          // bright yellow
            var fireO    = new Color(1f, 0.55f, 0.12f, 1f);          // orange
            var fireD    = new Color(0.78f, 0.28f, 0.06f, 1f);       // deep ember rim
            var glow     = new Color(1f, 0.50f, 0.14f, 0.40f);       // warm translucent glow

            // molten metal stream / pour (glowing)
            var moltenC  = new Color(1f, 0.95f, 0.78f, 1f);
            var moltenM  = new Color(1f, 0.74f, 0.26f, 1f);
            var moltenD  = new Color(1f, 0.48f, 0.10f, 1f);

            var smoke    = new Color(0.82f, 0.82f, 0.84f, 0.70f);
            var smokeD   = new Color(0.66f, 0.66f, 0.70f, 0.70f);
            var groundSh = new Color(0.12f, 0.10f, 0.08f, 0.45f);

            // -------- shared geometry helpers (used by fill + outline pass) --------
            // Main furnace mass: wide base, tapering toward a narrower upper stack.
            // Centred on the tile. fy from ~0.10 (feet) up to ~0.82 (stack top).
            System.Func<float, float, bool> isBody = (dx, fy) => {
                if (fy < 0.10f || fy > 0.82f) return false;
                // half-width: 0.32 at the base, shrinking above the shoulder (fy 0.52).
                float half = 0.32f - Mathf.Max(0f, fy - 0.52f) * 0.55f;
                if (half < 0.11f) half = 0.11f;
                return Mathf.Abs(dx) <= half;
            };
            // Tall chimney offset to the right of the stack.
            System.Func<float, float, bool> isChimney = (dx, fy) =>
                fy >= 0.62f && fy <= 0.94f && dx >= 0.14f && dx <= 0.28f;
            // Crucible: a tilted cup at the upper-left that pours into the mould.
            // Skew co-ordinates so it reads tipped toward the lower-right.
            System.Func<float, float, bool> isCrucible = (dx, fy) => {
                float cx = dx + 0.30f;          // local centre ~ fx 0.20
                float cy = fy - 0.66f;          // local centre ~ fy 0.66
                float tx = cx + cy * 0.35f;     // tilt
                return Disc(tx, cy, 0.115f);
            };
            // Mould: a low iron trough at the base catching the pour.
            System.Func<float, float, bool> isMould = (dx, fy) =>
                fy >= 0.075f && fy <= 0.17f && dx >= -0.05f && dx <= 0.20f;

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                // fy = 0 is the BOTTOM of the sprite.
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                bool inBody     = isBody(dxc, fy);
                bool inChimney  = isChimney(dxc, fy);
                bool inCrucible = isCrucible(dxc, fy);
                bool inMould    = isMould(dxc, fy);

                // ---- SMOKE rising from the chimney (drawn first, behind everything) ----
                float smx = dxc - (0.21f + 0.045f * Mathf.Sin(fy * 9f));
                if (fy >= 0.90f && fy <= 0.995f && Mathf.Abs(smx) <= 0.075f - (fy - 0.90f) * 0.45f) {
                    c = (smx < 0f) ? smoke : smokeD;
                }

                // ---- TALL CHIMNEY (white body, 3-tone, bronze cap band) ----
                if (inChimney) {
                    c = body;
                    if (dxc <= 0.18f) c = bodyHi; else if (dxc >= 0.24f) c = bodyShade;
                    // bronze cap band at the chimney top
                    if (fy >= 0.88f && fy <= 0.93f) {
                        c = bronzeM;
                        if (dxc <= 0.18f) c = bronzeL; else if (dxc >= 0.24f) c = bronzeD;
                    }
                }

                // ---- MAIN FURNACE BODY (white structural mass, tapered) ----
                if (inBody) {
                    c = body;
                    if (dxc < -0.11f) c = bodyHi; else if (dxc > 0.13f) c = bodyShade;

                    // bronze reinforcing bands across the body (lower + shoulder)
                    bool band = (fy >= 0.30f && fy <= 0.345f) || (fy >= 0.55f && fy <= 0.595f);
                    if (band) {
                        c = bronzeM;
                        if (dxc < -0.11f) c = bronzeL; else if (dxc > 0.13f) c = bronzeD;
                    }
                    // bronze rivets dotted along each band
                    bool rivetRow = (fy >= 0.31f && fy <= 0.335f) || (fy >= 0.56f && fy <= 0.585f);
                    if (rivetRow && Frac(fx * 8f) < 0.16f) c = bronzeD;
                }

                // ---- TWO FIRE OPENINGS (arched, orange->yellow->white-hot cores) ----
                // Both sit on the lower front face of the body, centred around fy 0.27.
                for (int k = 0; k < 2; k++) {
                    float ax = dxc - (k == 0 ? -0.135f : 0.135f);
                    float ay = fy - 0.27f;
                    float fr = Mathf.Sqrt(ax * ax + ay * ay);
                    // arched mouth: full disc up top, flat sill at the bottom
                    if (fr <= 0.105f && fy >= 0.155f) {
                        c = fireD;
                        if (fr <= 0.040f) c = fireCore;
                        else if (fr <= 0.066f) c = fireY;
                        else if (fr <= 0.090f) c = fireO;
                        // flickering tongues licking up out of the mouth
                        if (ay > 0.05f && Frac(ax * 16f + 0.3f) < 0.4f) c = fireY;
                    }
                }

                // ---- CRUCIBLE (tilted bronze cup, upper-left) pouring molten metal ----
                if (inCrucible) {
                    float cx = dxc + 0.30f;
                    float cy = fy - 0.66f;
                    float tx = cx + cy * 0.35f;
                    float r2 = tx * tx + cy * cy;
                    c = bronzeM;
                    if (tx - cy < -0.03f) c = bronzeL;          // lit upper-left
                    else if (tx + cy > 0.05f) c = bronzeD;      // shadow lower-right
                    // glowing molten metal at the pouring lip (lower-right of the cup)
                    if (tx > 0.03f && cy < 0.02f && r2 <= 0.085f * 0.085f) c = moltenM;
                    if (tx > 0.05f && cy < -0.02f && r2 <= 0.060f * 0.060f) c = moltenC;
                }

                // ---- POUR STREAM: thin glowing molten line crucible-lip -> mould ----
                // Falls from ~fy 0.62 down to the mould lip ~fy 0.16, drifting right.
                if (fy >= 0.16f && fy <= 0.63f) {
                    float pourX = -0.20f + (0.63f - fy) * 0.34f;   // lip at left, mould below-right
                    float d = dxc - pourX;
                    if (Mathf.Abs(d) <= 0.016f) {
                        c = moltenM;
                        if (d < -0.004f) c = moltenC; else if (d > 0.006f) c = moltenD;
                    }
                }

                // ---- MOULD at the base catching the pour (iron trough + molten pool) ----
                if (inMould) {
                    c = ironM;
                    if (fy >= 0.14f) c = ironL; else if (dxc >= 0.14f) c = ironD;
                    // glowing molten pool filling the trough
                    if (fy >= 0.105f && fy <= 0.15f && dxc >= -0.025f && dxc <= 0.175f) {
                        c = moltenM;
                        if (dxc <= 0.02f) c = moltenC; else if (dxc >= 0.13f) c = moltenD;
                    }
                }

                // ---- translucent warm GLOW around fire mouths & pour (not silhouette) ----
                if (c == Clear) {
                    float g1 = Mathf.Sqrt((dxc + 0.135f) * (dxc + 0.135f) + (fy - 0.27f) * (fy - 0.27f));
                    float g2 = Mathf.Sqrt((dxc - 0.135f) * (dxc - 0.135f) + (fy - 0.27f) * (fy - 0.27f));
                    bool nearFire = (g1 <= 0.16f || g2 <= 0.16f) && fy <= 0.42f;
                    bool nearPool = fy >= 0.10f && fy <= 0.18f && dxc >= -0.08f && dxc <= 0.22f;
                    if (nearFire || nearPool) c = glow;
                }

                // ---- translucent ground shadow (NOT part of the silhouette) ----
                if (c == Clear && fy >= 0.04f && fy < 0.075f && fx >= 0.10f && fx <= 0.90f)
                    c = groundSh;

                px[y * s + x] = c;
            }

            // ---- DARK OUTLINE pass: 1px halo around the opaque silhouette only ----
            // Skip fire / glow / translucent pixels in the neighbour test so we don't
            // outline the flames or the ground shadow.
            var outPx = new Color[s * s];
            for (int i = 0; i < px.Length; i++) outPx[i] = px[i];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                if (px[y * s + x].a > 0.05f) continue;            // skip already-filled pixels
                bool near = false;
                for (int dy = -1; dy <= 1 && !near; dy++) for (int dx = -1; dx <= 1; dx++) {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                    if (px[ny * s + nx].a > 0.9f) { near = true; break; }   // only opaque neighbours count
                }
                if (near) outPx[y * s + x] = outline;
            }
            px = outPx;

            tex.SetPixels(px); tex.Apply();
            _bronzeAdvSmelter = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _bronzeAdvSmelter;
        }
    }
}
