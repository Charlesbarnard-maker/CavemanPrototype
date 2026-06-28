// Bespoke procedural art for "Basic Smelter" (Age 1). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _tribalBasicSmelter;
        public static Sprite TribalBasicSmelter()
        {
            if (_tribalBasicSmelter != null) return _tribalBasicSmelter;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            var body      = Color.white;                          // structural stone stack (tinted at runtime)
            var bodyHi    = new Color(1f, 1f, 1f, 1f);            // lit upper-left faces
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);  // shadow lower-right faces

            // baked stone chimney (slightly cool grey so it survives tint)
            var stoneD = new Color(0.40f, 0.40f, 0.43f, 1f);
            var stoneM = new Color(0.54f, 0.54f, 0.58f, 1f);
            var stoneL = new Color(0.70f, 0.70f, 0.74f, 1f);

            // animal-hide bellows (tan, Stone/Tribal hide cues)
            var hideD   = new Color(0.37f, 0.25f, 0.16f, 1f);
            var hideM   = new Color(0.52f, 0.37f, 0.25f, 1f);
            var hideL   = new Color(0.64f, 0.47f, 0.33f, 1f);
            var nozzleD = new Color(0.30f, 0.20f, 0.12f, 1f);

            // fire / molten heat
            var mouthDark = new Color(0.12f, 0.07f, 0.05f, 1f);  // throat interior behind the flame
            var fireC     = new Color(1f, 0.55f, 0.12f, 1f);
            var fireL     = new Color(1f, 0.84f, 0.36f, 1f);
            var fireDeep  = new Color(0.82f, 0.26f, 0.06f, 1f);
            var glow      = new Color(1f, 0.50f, 0.14f, 0.40f);

            // glowing ingot / ore lump on the ground at the mouth
            var ingotHot = new Color(1f, 0.78f, 0.30f, 1f);
            var ingotMid = new Color(0.95f, 0.45f, 0.12f, 1f);
            var ingotDk  = new Color(0.55f, 0.28f, 0.12f, 1f);

            var smoke = new Color(0.82f, 0.82f, 0.84f, 0.7f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ---- HEAT GLOW HALO around the fire mouth (translucent, behind everything) ----
                if (Disc(fx - 0.5f, fy - 0.20f, 0.22f)) c = glow;

                // ---- FURNACE STACK: a stout white-body stone tower that TAPERS upward ----
                // half-width shrinks with height: wide base, narrower top
                float stackHalf = 0.31f - (fy - 0.12f) * 0.17f;       // ~0.31 at base -> ~0.20 near top
                bool inStack = fy >= 0.12f && fy <= 0.74f && Mathf.Abs(dxc) <= stackHalf;
                if (inStack) {
                    c = body;
                    // 3-tone form on the white body: lit upper-left, shaded lower-right
                    float edge = dxc / stackHalf;                     // -1 left .. +1 right
                    if (edge > 0.45f || (fy < 0.22f && edge > 0.10f)) c = bodyShade;    // right / lower-right
                    else if (edge < -0.38f) c = bodyHi;                                 // left lit face
                    // baked stone-block seams so it reads as stacked stone (mortar lines)
                    float band = Frac(fy * 6.0f);
                    float stagger = (Frac(fy * 3.0f) < 0.5f) ? 0f : 0.5f;
                    float vseam = Frac((fx + stagger) * 5.0f);
                    if (band < 0.10f || vseam < 0.07f) c = bodyShade;  // mortar = shaded near-white (stays tintable)
                }

                // ---- FIRE MOUTH: dark arch at the base, glowing molten orange/yellow ----
                const float mouthCy = 0.235f;                          // heat + arch share one centre
                bool mouthArch   = Disc(dxc, fy - mouthCy, 0.135f) && fy >= 0.13f;
                bool mouthThroat = Mathf.Abs(dxc) <= 0.115f && fy >= 0.115f && fy <= mouthCy;
                if ((mouthArch || mouthThroat) && fy <= 0.32f && fy >= 0.115f) {
                    float md = Mathf.Sqrt(dxc * dxc + (fy - mouthCy) * (fy - mouthCy));
                    c = mouthDark;
                    if (md <= 0.045f) c = fireL;
                    else if (md <= 0.085f) c = fireC;
                    else if (md <= 0.115f) c = fireDeep;
                    // flickering tongues licking upward (deterministic via Sin)
                    float flick = mouthCy + 0.05f + 0.045f * Mathf.Sin(dxc * 34f) + 0.02f * Mathf.Sin(dxc * 70f);
                    if (fy <= flick && fy >= mouthCy && Mathf.Abs(dxc) <= 0.10f) c = fireL;
                    // dark stone rim surrounding the mouth opening
                    float rimd = Mathf.Sqrt(dxc * dxc + (fy - mouthCy) * (fy - mouthCy));
                    if (rimd > 0.115f && rimd <= 0.150f && fy >= 0.13f) c = outline;
                }

                // ---- STONE CHIMNEY at the top of the stack ----
                bool inChimney = fy > 0.74f && fy <= 0.85f && Mathf.Abs(dxc) <= 0.14f;
                if (inChimney) {
                    c = stoneM;
                    if (dxc < -0.05f) c = stoneL; else if (dxc > 0.06f) c = stoneD;
                    if (Frac(fy * 10f) < 0.16f) c = stoneD;            // courses
                    // hollow dark rim at the very top
                    if (fy >= 0.82f && Mathf.Abs(dxc) <= 0.085f) c = stoneD;
                }

                // ---- BELLOWS: tan animal-hide bag on the LEFT feeding air ----
                // teardrop/wedge bag hugging the left flank, with a nozzle into the stack
                float bx = fx - 0.135f, by = fy - 0.36f;
                bool bag = Disc(bx, by, 0.14f) && fx <= 0.245f;
                if (bag) {
                    c = hideM;
                    if (bx + by < -0.06f) c = hideL;                   // upper-left lit
                    else if (bx - by > 0.05f) c = hideD;               // lower-right shade
                    float fold = Frac((fx + fy * 1.6f) * 7.0f);
                    if (fold < 0.18f) c = hideD;                       // accordion fold lines
                }
                // bellows nozzle: short dark wooden snout from the bag into the furnace flank
                if (fy >= 0.30f && fy <= 0.345f && fx >= 0.215f && fx <= 0.40f) {
                    c = nozzleD;
                    if (fy >= 0.33f) c = hideD;                        // lit top edge of the snout
                }
                // bellows handle peg (top)
                if (Disc(fx - 0.135f, fy - 0.505f, 0.035f)) c = nozzleD;

                // ---- GLOWING INGOT / ORE LUMP on the ground at the mouth (right of centre) ----
                float ix = fx - 0.66f, iy = fy - 0.085f;
                bool ingot = Mathf.Abs(ix) <= 0.10f && fy >= 0.045f && fy <= 0.13f;
                if (ingot) {
                    c = ingotMid;
                    if (iy > 0.0f && ix < 0.0f) c = ingotHot;          // hot upper-left
                    else if (iy < -0.02f || ix > 0.06f) c = ingotDk;   // cooling lower-right
                    if (Disc(ix, iy - 0.005f, 0.035f)) c = ingotHot;   // white-hot core
                }
                // little warm glow puddle under the ingot (translucent, not silhouette)
                if (c == Clear && Disc(fx - 0.66f, fy - 0.06f, 0.12f) && fy < 0.09f)
                    c = glow;

                // ---- SMOKE puffs rising from the chimney ----
                float smx = dxc - 0.02f;
                if (c == Clear && fy > 0.85f) {
                    float drift = 0.02f * Mathf.Sin(fy * 22f);
                    if (Disc(smx - drift, fy - 0.90f, 0.060f) ||
                        Disc(smx - drift - 0.03f, fy - 0.965f, 0.050f))
                        c = smoke;
                }

                // ---- translucent ground shadow (NOT part of the silhouette) ----
                if (c == Clear && fy < 0.05f && fy >= 0.025f && fx >= 0.16f && fx <= 0.86f)
                    c = new Color(0.12f, 0.10f, 0.08f, 0.42f);

                px[y * s + x] = c;
            }

            // ---- DARK OUTLINE pass: 1px halo around opaque silhouette; skip fire/glow/shadow ----
            var outPx = new Color[s * s];
            for (int i = 0; i < px.Length; i++) outPx[i] = px[i];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                if (px[y * s + x].a > 0.05f) continue;                 // only paint into empty pixels
                bool near = false;
                for (int dy = -1; dy <= 1 && !near; dy++) for (int dx = -1; dx <= 1; dx++) {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                    Color n = px[ny * s + nx];
                    if (n.a <= 0.9f) continue;                         // neighbour must be fully opaque
                    // don't outline against bright fire/ingot glow — keep heat reading clean
                    bool warm = (n.r > 0.9f && n.g > 0.4f && n.b < 0.4f);
                    if (warm) continue;
                    near = true; break;
                }
                if (near) outPx[y * s + x] = outline;
            }
            px = outPx;

            tex.SetPixels(px); tex.Apply();
            _tribalBasicSmelter = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _tribalBasicSmelter;
        }
    }
}
