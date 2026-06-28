// Bespoke procedural art for "Monument" (Age 4). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _industrialMonument;
        public static Sprite IndustrialMonument()
        {
            if (_industrialMonument != null) return _industrialMonument;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            // structural pale stone (white-ish body, tinted at runtime); 3-tone so form reads
            var bodyHi    = new Color(1f, 1f, 1f, 1f);                // lit upper-left faces
            var body      = new Color(0.84f, 0.84f, 0.86f, 1f);      // mid face
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);      // shadow lower-right faces
            var relief    = new Color(0.18f, 0.14f, 0.10f, 1f);      // carved recessed lines
            // gold capstone / finial (3-tone, baked saturated)
            var goldD     = new Color(0.72f, 0.55f, 0.12f, 1f);
            var goldM     = new Color(1f, 0.84f, 0.30f, 1f);
            var goldL     = new Color(1f, 0.95f, 0.70f, 1f);
            // torch post wood (baked, 3-tone)
            var torchD    = new Color(0.27f, 0.16f, 0.08f, 1f);
            var torchM    = new Color(0.42f, 0.27f, 0.14f, 1f);
            var torchL    = new Color(0.56f, 0.38f, 0.21f, 1f);
            // fire (baked, NOT outlined)
            var fireC     = new Color(1f, 0.55f, 0.12f, 1f);
            var fireL     = new Color(1f, 0.84f, 0.36f, 1f);
            var glow      = new Color(1f, 0.50f, 0.14f, 0.40f);

            // Six stacked tiers, widest at the bottom (low fy), narrowing as fy rises.
            // tierBot/tierTop give the fy band; tierHalf the max |dxc| half-width.
            float[] tierBot  = { 0.18f, 0.30f, 0.42f, 0.54f, 0.66f, 0.78f };
            float[] tierTop  = { 0.30f, 0.42f, 0.54f, 0.66f, 0.78f, 0.86f };
            float[] tierHalf = { 0.40f, 0.345f, 0.29f, 0.235f, 0.18f, 0.125f };

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                float adx = Mathf.Abs(dxc);
                Color c = Clear;

                // ===== WIDE STEPPED GROUND PLINTH (two broad symmetric base steps) =====
                // lowest, widest step
                if (fy >= 0.045f && fy < 0.105f && adx <= 0.47f) {
                    c = body;
                    if (fy >= 0.082f) c = bodyHi;            // lit upper tread
                    else c = bodyShade;                       // shadowed riser
                    if (dxc > 0.43f) c = bodyShade;           // right outer face in shade
                    else if (dxc < -0.43f) c = bodyHi;        // left outer face lit
                }
                // second step (narrower, sits on the first)
                if (fy >= 0.105f && fy < 0.165f && adx <= 0.44f) {
                    c = body;
                    if (fy >= 0.142f) c = bodyHi;
                    else c = bodyShade;
                    if (dxc > 0.40f) c = bodyShade;
                    else if (dxc < -0.40f) c = bodyHi;
                }

                // ===== STEPPED ZIGGURAT BODY (symmetric tiers, narrowing as fy rises) =====
                for (int t = 0; t < 6; t++) {
                    if (fy >= tierBot[t] && fy < tierTop[t] && adx <= tierHalf[t]) {
                        c = body;
                        // 3-tone form: lit left, shaded right
                        if (dxc < -0.04f) c = bodyHi;
                        else if (dxc > 0.04f) c = bodyShade;
                        // top lip of each tier catches light
                        if (fy >= tierTop[t] - 0.022f) c = bodyHi;
                        // outer edge reveals: left lit, right shaded
                        if (dxc > tierHalf[t] - 0.025f) c = bodyShade;
                        else if (dxc < -(tierHalf[t] - 0.025f)) c = bodyHi;
                        // carved RELIEF band (recessed dark line) across the middle of the tier
                        float bandMid = (tierBot[t] + tierTop[t]) * 0.5f;
                        if (Mathf.Abs(fy - bandMid) < 0.012f && adx <= tierHalf[t] - 0.035f) c = relief;
                        // vertical centre seam relief for grandeur (symmetric, broken into dashes)
                        if (adx < 0.011f && Frac(fy * 22f) < 0.5f) c = relief;
                    }
                }

                // ===== GOLD CAPSTONE / FINIAL (tapered pyramid crown) =====
                // sits just above the top tier, narrowing to a point at the apex.
                float capBase = 0.86f, capApex = 0.965f;
                if (fy >= capBase && fy <= capApex) {
                    float capT = (fy - capBase) / (capApex - capBase);   // 0 base -> 1 apex
                    float capHalf = 0.115f * (1f - capT);                 // taper to a point
                    if (adx <= capHalf) {
                        c = goldM;
                        if (dxc < -0.015f) c = goldL;                     // lit left face
                        else if (dxc > 0.015f) c = goldD;                 // shadow right face
                        if (Frac(capT * 4f) < 0.16f) c = goldD;           // horizontal banding
                        if (capT > 0.80f && adx < 0.05f) c = goldL;       // bright apex glint
                    }
                }
                // tiny finial point / glint at the very apex
                if (Disc(dxc, fy - 0.965f, 0.022f)) c = goldL;
                if (Disc(dxc + 0.006f, fy - 0.972f, 0.010f)) c = new Color(1f, 1f, 0.92f, 1f);

                // ===== FLANKING FLAMING TORCHES at the base corners =====
                for (int side = -1; side <= 1; side += 2) {
                    float tx = dxc - side * 0.42f;            // local x about each torch centre
                    float atx = Mathf.Abs(tx);
                    // torch post (vertical, baked wood, 3-tone)
                    if (atx <= 0.035f && fy >= 0.07f && fy <= 0.36f) {
                        c = torchM;
                        if (tx < -0.012f) c = torchL;         // lit left of post
                        else if (tx > 0.012f) c = torchD;     // shaded right of post
                    }
                    // bowl / cup at top of post
                    if (Disc(tx, fy - 0.375f, 0.055f) && fy >= 0.355f && fy <= 0.41f) {
                        c = torchM;
                        if (tx < -0.01f) c = torchL; else if (tx > 0.012f) c = torchD;
                    }
                    // warm translucent glow halo behind each flame (drawn before flame so flame sits on top)
                    if (Disc(tx, fy - 0.46f, 0.09f) && fy >= 0.38f && c == Clear) c = glow;
                    // flame (baked fire, teardrop tapering up)
                    float flameSway = 0.012f * Mathf.Sin(side * 3f + 6f);
                    float fxl = tx - flameSway;
                    float flameTop = 0.55f - Mathf.Abs(fxl) * 1.7f;
                    if (fy >= 0.40f && fy <= flameTop && Mathf.Abs(fxl) <= 0.055f) {
                        c = fireC;
                        if (Mathf.Abs(fxl) < 0.022f && fy < flameTop - 0.03f) c = fireL; // hot core
                    }
                }

                // translucent ground shadow (NOT part of the silhouette)
                if (fy < 0.045f && fy >= 0.018f && adx <= 0.47f && c == Clear)
                    c = new Color(0.12f, 0.10f, 0.08f, 0.45f);

                px[y * s + x] = c;
            }

            // ===== DARK OUTLINE PASS (silhouette halo; skip fire/glow & translucent shadow) =====
            var outPx = new Color[s * s];
            for (int i = 0; i < px.Length; i++) outPx[i] = px[i];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                if (px[y * s + x].a > 0.05f) continue;          // only outline empty pixels
                bool near = false;
                for (int dy = -1; dy <= 1 && !near; dy++) for (int dx = -1; dx <= 1; dx++) {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                    Color n = px[ny * s + nx];
                    if (n.a <= 0.9f) continue;                  // ignore glow/shadow (translucent) & empty
                    if (n == fireC || n == fireL) continue;     // don't halo the flames
                    near = true; break;
                }
                if (near) outPx[y * s + x] = outline;
            }
            px = outPx;

            tex.SetPixels(px); tex.Apply();
            _industrialMonument = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _industrialMonument;
        }
    }
}
