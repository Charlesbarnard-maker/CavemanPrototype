// Bespoke procedural art for "Oil Tank" (Age 2). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _bronzeOilTank;
        public static Sprite BronzeOilTank()
        {
            if (_bronzeOilTank != null) return _bronzeOilTank;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            var body      = Color.white;                              // structural cylinder, tinted at runtime
            var bodyHi    = new Color(1f, 1f, 1f, 1f);                // lit upper-left face
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);       // shadow lower-right face
            var rimD      = new Color(0.14f, 0.12f, 0.16f, 1f);       // oil-dark metal band shadow
            var rimM      = new Color(0.24f, 0.22f, 0.28f, 1f);       // oil-dark metal band mid
            var rimL      = new Color(0.40f, 0.38f, 0.46f, 1f);       // oil-dark metal band light
            var rivetD    = new Color(0.10f, 0.09f, 0.12f, 1f);
            var rivetL    = new Color(0.52f, 0.50f, 0.58f, 1f);
            var oilD      = new Color(0.10f, 0.08f, 0.13f, 1f);       // gauge oil shadow
            var oilM      = new Color(0.18f, 0.14f, 0.20f, 1f);       // gauge oil mid (dark oil)
            var glassL    = new Color(0.46f, 0.42f, 0.54f, 1f);       // gauge glass frame highlight
            var pipeD     = new Color(0.18f, 0.20f, 0.26f, 1f);       // cool metal pipe shadow
            var pipeM     = new Color(0.32f, 0.34f, 0.42f, 1f);       // cool metal pipe mid
            var pipeL     = new Color(0.52f, 0.55f, 0.66f, 1f);       // cool metal pipe light
            var ladderD   = new Color(0.20f, 0.18f, 0.22f, 1f);       // ladder shadow
            var ladderL   = new Color(0.48f, 0.46f, 0.52f, 1f);       // ladder rung highlight
            var sheen     = new Color(0.58f, 0.40f, 0.72f, 0.45f);    // oily purple sheen (translucent)

            // tank body extents
            const float tankL = 0.16f, tankR = 0.84f;                 // left/right walls
            const float tankBot = 0.12f, tankCapY = 0.80f;            // body straight up to cap base
            const float capR = (tankR - tankL) * 0.5f;                // rounded top cap radius
            const float capCx = 0.5f, capCy = tankCapY;

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;
                bool plainBody = false;                               // true only where we painted untouched body

                // membership: straight cylinder body OR rounded top cap
                bool inBody = fx >= tankL && fx <= tankR && fy >= tankBot && fy <= tankCapY;
                bool inCap  = fy > tankCapY && Disc(fx - capCx, fy - capCy, capR);
                bool inTank = inBody || inCap;

                if (inTank) {
                    // base white-body cylinder shading: curved left->right (light upper-left, dark lower-right)
                    float across = (fx - tankL) / (tankR - tankL);    // 0 at left, 1 at right
                    c = body; plainBody = true;
                    if (across < 0.30f) { c = bodyHi; plainBody = false; }
                    else if (across > 0.66f) { c = bodyShade; plainBody = false; }
                    // subtle vertical falloff: darker toward the very bottom of the straight body
                    if (inBody && fy < tankBot + 0.05f && across >= 0.30f) { c = bodyShade; plainBody = false; }

                    // ROUNDED CAP TOP: brighter highlight on its upper-left curve
                    if (inCap && (fx - capCx) < -0.02f) { c = bodyHi; plainBody = false; }
                    if (inCap && (fx - capCx) > 0.16f) { c = bodyShade; plainBody = false; }

                    // METAL RIM BANDS top & bottom of the straight body (baked dark, 3-tone across the curve)
                    bool topBand = inBody && fy >= tankCapY - 0.07f && fy <= tankCapY;
                    bool botBand = inBody && fy >= tankBot && fy <= tankBot + 0.07f;
                    if (topBand || botBand) {
                        c = rimM; plainBody = false;
                        if (across < 0.30f) c = rimL; else if (across > 0.66f) c = rimD;
                    }

                    // ROW OF RIVETS along the top band
                    if (topBand) {
                        float rvx = Frac(fx * 9f);
                        if (rvx < 0.34f && fy >= tankCapY - 0.055f && fy <= tankCapY - 0.025f)
                            c = (across < 0.45f) ? rivetL : rivetD;
                    }
                    // ROW OF RIVETS along the bottom band
                    if (botBand) {
                        float rvx = Frac(fx * 9f);
                        if (rvx < 0.34f && fy >= tankBot + 0.025f && fy <= tankBot + 0.055f)
                            c = (across < 0.45f) ? rivetL : rivetD;
                    }

                    // OIL-LEVEL GAUGE window strip down the LEFT side (vertical), between the bands
                    if (fx >= 0.255f && fx <= 0.315f && fy >= tankBot + 0.10f && fy <= tankCapY - 0.10f) {
                        plainBody = false;
                        // glass frame edges
                        if (fx <= 0.268f || fx >= 0.302f) c = glassL;
                        else {
                            c = oilM;                                 // dark oil fill
                            if (fx >= 0.288f) c = oilD;               // right side of strip in shadow
                            // a faint meniscus tick midway
                            if (fy >= 0.46f && fy <= 0.485f) c = glassL;
                        }
                    }

                    // OILY PURPLE SHEEN highlight (diagonal streak, upper-left) only over untouched body
                    float sh = dxc + (fy - 0.55f) * 0.6f;
                    if (plainBody && sh > -0.10f && sh < 0.02f && across >= 0.18f && across <= 0.58f
                        && fy >= tankBot + 0.10f && fy <= tankCapY - 0.06f)
                        c = sheen;

                    // LADDER up the RIGHT side: two rails + rungs spanning between them
                    bool railA = fx >= 0.70f && fx <= 0.725f;
                    bool railB = fx >= 0.775f && fx <= 0.80f;
                    bool inLadderZone = fy >= tankBot + 0.06f && fy <= tankCapY - 0.04f;
                    if (inLadderZone && (railA || railB)) c = railA ? ladderL : ladderD;
                    if (inLadderZone && fx >= 0.70f && fx <= 0.80f && Frac(fy * 11f) < 0.20f)
                        c = ladderL;                                  // rungs
                }

                // PIPE inlet stub at the base (protrudes to the lower-LEFT)
                bool pipeStub   = fx >= 0.04f && fx <= 0.18f && fy >= 0.085f && fy <= 0.165f;
                bool pipeFlange = fx >= 0.155f && fx <= 0.205f && fy >= 0.07f && fy <= 0.18f;
                if (pipeStub || pipeFlange) {
                    c = pipeM;
                    if (fy >= 0.135f) c = pipeL;                      // top of pipe lit (upper)
                    else if (fy <= 0.105f) c = pipeD;                 // underside shadow (lower)
                    if (pipeFlange && fx >= 0.185f) c = pipeD;        // flange right edge in shadow
                }

                // translucent ground shadow (NOT part of the silhouette)
                if (fy < 0.085f && fy >= 0.055f && fx >= 0.14f && fx <= 0.86f && c.a < 0.05f)
                    c = new Color(0.12f, 0.10f, 0.08f, 0.45f);

                px[y * s + x] = c;
            }

            // DARK OUTLINE pass: only fully-opaque pixels count as silhouette (translucent shadow/sheen ignored)
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
            _bronzeOilTank = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _bronzeOilTank;
        }
    }
}
