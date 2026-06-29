// Bespoke procedural art for the per-age player MOUNTS shown by PlayerAvatar.
// PlayerController names the travel tiers On Foot -> Horseback -> Ox Cart -> Wagon -> Motorbike
// (one per age). Age 0 (On Foot) reuses the walking Caveman; ages 1-4 get a side-profile mount
// with a small age-tinted rider, animated over 3 walk-cycle frames (trotting legs / spinning wheels).
// Baked full colour like Caveman (the avatar renders at white tint). 64x64, fy=0 bottom, centre pivot.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite[] _mounts; // (age-1)*3 + frame, for ages 1..4

        /// <summary>The player's age-appropriate travel mount. age 0 = on foot (the Caveman);
        /// 1=Horseback, 2=Ox Cart, 3=Wagon, 4=Motorbike. <paramref name="frame"/> 0..2 animates it.</summary>
        public static Sprite PlayerMount(int age, int frame = 0)
        {
            age = Mathf.Clamp(age, 0, 4); frame = Mathf.Clamp(frame, 0, 2);
            if (age == 0) return Caveman(0, frame);
            if (_mounts == null) _mounts = new Sprite[12];
            int key = (age - 1) * 3 + frame;
            if (_mounts[key] != null) return _mounts[key];
            Sprite sp = age switch
            {
                1 => BakeHorse(frame),
                2 => BakeOxCart(frame),
                3 => BakeWagon(frame),
                _ => BakeMotorbike(frame),
            };
            _mounts[key] = sp;
            return sp;
        }

        // ---- shared finish: thin dark outline around the silhouette, then make the sprite ----
        private static Sprite FinishMount(Color[] px, int s)
        {
            var dark = new Color(0.08f, 0.06f, 0.06f, 1f);
            var outPx = new Color[s * s];
            System.Array.Copy(px, outPx, px.Length);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                if (px[y * s + x].a > 0.01f) continue;
                bool adj = false;
                for (int oy = -1; oy <= 1 && !adj; oy++) for (int ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0) continue;
                    int nx = x + ox, ny = y + oy;
                    if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                    if (px[ny * s + nx].a > 0.01f) { adj = true; break; }
                }
                if (adj) outPx[y * s + x] = dark;
            }
            var tex = NewTex(s); tex.SetPixels(outPx); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        // Is (fx,fy) on a spoke/tyre of a wheel centred (wcx,wcy) radius r? Writes the wheel colours.
        // Returns the colour to paint, or Clear (a==0) for "not on wheel". phase rotates the spokes.
        private static Color WheelPixel(float fx, float fy, float wcx, float wcy, float r, float phase,
                                        Color tyre, Color spoke, Color hub)
        {
            float dx = fx - wcx, dy = fy - wcy;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > r) return Clear;
            if (d >= r * 0.80f) return tyre;                 // outer tyre ring
            if (d <= r * 0.18f) return hub;                  // hub
            float ang = Mathf.Atan2(dy, dx) / (Mathf.PI * 2f);
            float a = Frac(ang * 4f + phase);                // 4 spokes
            if (a < 0.11f || a > 0.89f) return spoke;        // spoke arms
            return new Color(0.16f, 0.15f, 0.17f, 1f);       // dark wheel interior
        }

        // Stamp a small seated/leaning rider centred on x=cx, hips at seatY (fy). Age sets the palette.
        // Overwrites whatever's under it (the rider sits on top of the mount).
        private static void StampRider(Color[] px, int s, float cx, float seatY, int age, bool lean)
        {
            var skin = new Color(0.84f, 0.65f, 0.49f, 1f);
            var skinD = new Color(0.66f, 0.47f, 0.34f, 1f);
            Color cloth, clothD, cap, capD;
            switch (age)
            {
                case 1: cloth = new Color(0.62f, 0.43f, 0.29f, 1f); clothD = new Color(0.44f, 0.29f, 0.18f, 1f);
                        cap = new Color(0.74f, 0.28f, 0.23f, 1f); capD = new Color(0.52f, 0.18f, 0.15f, 1f); break;   // tribal headband
                case 2: cloth = new Color(0.70f, 0.60f, 0.36f, 1f); clothD = new Color(0.52f, 0.43f, 0.24f, 1f);
                        cap = new Color(0.82f, 0.57f, 0.31f, 1f); capD = new Color(0.60f, 0.40f, 0.20f, 1f); break;   // bronze
                case 3: cloth = new Color(0.52f, 0.56f, 0.62f, 1f); clothD = new Color(0.36f, 0.40f, 0.46f, 1f);
                        cap = new Color(0.74f, 0.78f, 0.84f, 1f); capD = new Color(0.52f, 0.56f, 0.62f, 1f); break;   // iron helm
                default: cloth = new Color(0.26f, 0.42f, 0.62f, 1f); clothD = new Color(0.16f, 0.29f, 0.46f, 1f);
                        cap = new Color(0.93f, 0.80f, 0.24f, 1f); capD = new Color(0.70f, 0.58f, 0.16f, 1f); break;   // industrial hi-vis hard-hat
            }
            float headY = seatY + 0.20f;
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - cx;
                float lx = dxc - (lean ? (fy - seatY) * 0.45f : 0f); // forward lean shears the torso right as it rises
                Color c = Clear;
                // torso
                if (Mathf.Abs(lx) <= 0.055f && fy >= seatY && fy <= seatY + 0.155f)
                {
                    c = cloth; if (lx > 0.015f) c = clothD; else if (lx < -0.025f) c = cloth;
                }
                // forward arm reaching to reins/bars
                if (lx >= 0.02f && lx <= 0.12f && fy >= seatY + 0.05f && fy <= seatY + 0.085f) { c = skin; if (lx > 0.08f) c = skinD; }
                // head
                if (Disc(fx - (cx + (lean ? 0.075f : 0f)), fy - headY, 0.052f)) { c = skin; if (fx > cx + (lean ? 0.075f : 0f)) c = skinD; }
                // cap / hat / helmet over the crown
                float hcx = cx + (lean ? 0.075f : 0f);
                if (Disc(fx - hcx, fy - (headY + 0.028f), 0.062f) && fy >= headY + 0.012f) { c = cap; if (fx > hcx + 0.01f) c = capD; }
                if (age == 4 && Mathf.Abs(fx - hcx) <= 0.075f && fy >= headY + 0.018f && fy <= headY + 0.034f) { c = cap; } // hard-hat brim
                if (c.a > 0f) px[y * s + x] = c;
            }
        }

        // ============================ AGE 1 — HORSEBACK ============================
        private static Sprite BakeHorse(int frame)
        {
            const int s = 64; var px = new Color[s * s];
            var hideL = new Color(0.58f, 0.42f, 0.27f, 1f);
            var hideM = new Color(0.46f, 0.32f, 0.19f, 1f);
            var hideD = new Color(0.31f, 0.21f, 0.12f, 1f);
            var mane = new Color(0.20f, 0.13f, 0.08f, 1f);
            var hoof = new Color(0.13f, 0.11f, 0.09f, 1f);
            var eye = new Color(0.06f, 0.05f, 0.05f, 1f);

            float sw = frame == 1 ? 1f : frame == 2 ? -1f : 0f;
            float frontOff = sw * 0.030f, backOff = -sw * 0.030f;

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                Color c = Clear;

                // LEGS (drawn first, behind body). Two back, two front; near pair lighter.
                // back-far, back-near, front-far, front-near
                if (fy >= 0.07f && fy <= 0.32f)
                {
                    float legTopY = 0.30f;
                    // back legs around x 0.27 / 0.33, swing by backOff at the foot
                    float bxA = 0.255f + (fy < 0.18f ? backOff : 0f);
                    float bxB = 0.330f - (fy < 0.18f ? backOff : 0f);
                    float fxA = 0.560f + (fy < 0.18f ? frontOff : 0f);
                    float fxB = 0.635f - (fy < 0.18f ? frontOff : 0f);
                    if (Mathf.Abs(fx - bxA) <= 0.028f) c = hideD;
                    if (Mathf.Abs(fx - fxA) <= 0.028f) c = hideD;
                    if (Mathf.Abs(fx - bxB) <= 0.030f) c = hideM;
                    if (Mathf.Abs(fx - fxB) <= 0.030f) c = hideM;
                    // hooves
                    if (fy <= 0.095f)
                    {
                        if (Mathf.Abs(fx - bxA) <= 0.030f || Mathf.Abs(fx - fxA) <= 0.030f ||
                            Mathf.Abs(fx - bxB) <= 0.032f || Mathf.Abs(fx - fxB) <= 0.032f) c = hoof;
                    }
                }

                // BARREL BODY ellipse centre (0.45,0.40)
                float bx = (fx - 0.45f) / 0.275f, by = (fy - 0.40f) / 0.125f;
                if (bx * bx + by * by <= 1f)
                {
                    c = hideM;
                    if (fy > 0.44f && fx < 0.52f) c = hideL;            // lit upper-left back
                    else if (fy < 0.36f || fx > 0.62f) c = hideD;      // shaded belly / chest
                }

                // TAIL at the back-left, flowing down
                if (fx >= 0.12f && fx <= 0.21f && fy >= 0.20f && fy <= 0.46f)
                {
                    float t = (fx - 0.165f) + (0.33f - fy) * 0.05f;
                    if (Mathf.Abs(t) <= 0.035f) c = mane;
                }

                // NECK rising front-right + HEAD
                // neck: slanted band from (0.60,0.45) to (0.74,0.58)
                float nshear = (fy - 0.45f);
                float nx = fx - (0.60f + nshear * 0.95f);
                if (nx >= -0.045f && nx <= 0.055f && fy >= 0.44f && fy <= 0.60f)
                {
                    c = hideM; if (nx > 0.02f) c = hideD; else if (nx < -0.02f) c = hideL;
                }
                // mane down the back of the neck
                if (nx >= -0.065f && nx <= -0.035f && fy >= 0.46f && fy <= 0.62f) c = mane;
                // head: blocky muzzle pointing up-right at the top of the neck
                float hx = fx - 0.745f, hy = fy - 0.595f;
                if (Disc(hx, hy, 0.060f) || (fx >= 0.74f && fx <= 0.83f && fy >= 0.575f && fy <= 0.635f))
                {
                    c = hideM; if (fx > 0.78f || fy < 0.59f) c = hideD; else if (fy > 0.62f) c = hideL;
                }
                // ear
                if (fx >= 0.705f && fx <= 0.735f && fy >= 0.64f && fy <= 0.685f) c = hideD;
                // eye
                if (Disc(fx - 0.755f, fy - 0.615f, 0.012f)) c = eye;

                // SADDLE BLANKET under the rider (anchors him to the back)
                if (fx >= 0.355f && fx <= 0.515f && fy >= 0.45f && fy <= 0.495f)
                {
                    c = new Color(0.62f, 0.43f, 0.29f, 1f);
                    if (fy < 0.465f) c = new Color(0.44f, 0.29f, 0.18f, 1f);                          // shaded lower fringe
                    if (Frac(fx * 22f) < 0.18f && fy < 0.47f) c = new Color(0.74f, 0.28f, 0.23f, 1f); // tribal tassels
                }
                // RIDER near-side LEG bent down the flank, boot at the bottom — reads as "riding"
                if (fx >= 0.395f && fx <= 0.45f && fy >= 0.345f && fy <= 0.485f)
                {
                    c = new Color(0.50f, 0.34f, 0.22f, 1f);                       // breeches
                    if (fx > 0.43f) c = new Color(0.40f, 0.27f, 0.17f, 1f);       // shaded edge
                    if (fy <= 0.375f) c = new Color(0.24f, 0.18f, 0.13f, 1f);     // boot
                }

                px[y * s + x] = c;
            }

            // rider sits on the saddle
            StampRider(px, s, 0.44f, 0.485f, 1, false);
            return FinishMount(px, s);
        }

        // ============================ AGE 2 — OX CART ============================
        private static Sprite BakeOxCart(int frame)
        {
            const int s = 64; var px = new Color[s * s];
            var hideL = new Color(0.64f, 0.60f, 0.54f, 1f);   // pale grey ox
            var hideM = new Color(0.51f, 0.47f, 0.42f, 1f);
            var hideD = new Color(0.36f, 0.33f, 0.29f, 1f);
            var horn = new Color(0.86f, 0.83f, 0.73f, 1f);
            var hoof = new Color(0.14f, 0.12f, 0.10f, 1f);
            var woodL = new Color(0.66f, 0.49f, 0.31f, 1f);
            var woodM = new Color(0.51f, 0.37f, 0.23f, 1f);
            var woodD = new Color(0.34f, 0.24f, 0.14f, 1f);
            var tyre = new Color(0.30f, 0.21f, 0.13f, 1f);
            var spoke = new Color(0.60f, 0.45f, 0.28f, 1f);
            var hub = new Color(0.22f, 0.16f, 0.10f, 1f);
            var eye = new Color(0.06f, 0.05f, 0.05f, 1f);

            float sw = frame == 1 ? 1f : frame == 2 ? -1f : 0f;
            float oxOff = sw * 0.028f;
            float phase = frame * 0.18f;

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                Color c = Clear;

                // CART WHEEL (left), big
                Color w = WheelPixel(fx, fy, 0.255f, 0.205f, 0.155f, phase, tyre, spoke, hub);
                if (w.a > 0f) c = w;

                // OX LEGS — rear pair + front pair (trot)
                if (fy >= 0.07f && fy <= 0.31f)
                {
                    float rL = 0.575f + (fy < 0.17f ? oxOff : 0f);
                    float rR = 0.645f - (fy < 0.17f ? oxOff : 0f);
                    float fL = 0.760f + (fy < 0.17f ? oxOff : 0f);
                    float fR = 0.830f - (fy < 0.17f ? oxOff : 0f);
                    if (Mathf.Abs(fx - rR) <= 0.030f || Mathf.Abs(fx - fR) <= 0.030f) c = hideM;
                    if (Mathf.Abs(fx - rL) <= 0.030f || Mathf.Abs(fx - fL) <= 0.030f) c = hideD;
                    if (fy <= 0.095f && (Mathf.Abs(fx - rL) <= 0.032f || Mathf.Abs(fx - rR) <= 0.032f ||
                                         Mathf.Abs(fx - fL) <= 0.032f || Mathf.Abs(fx - fR) <= 0.032f)) c = hoof;
                }

                // CART BODY (left): a wooden box on the wheel, plank-lined
                if (fx >= 0.12f && fx <= 0.46f && fy >= 0.32f && fy <= 0.52f)
                {
                    c = woodM;
                    if (Frac(fy * 16f) < 0.16f) c = woodD;             // horizontal plank seams
                    if (fy > 0.49f) c = woodL;                         // lit top rail
                    if (fx < 0.16f) c = woodD;
                }
                // cart bed floor line
                if (fx >= 0.12f && fx <= 0.50f && fy >= 0.30f && fy <= 0.325f) c = woodD;

                // OX BODY — bulky barrel centre (0.68,0.42)
                float bx = (fx - 0.68f) / 0.175f, by = (fy - 0.42f) / 0.130f;
                if (bx * bx + by * by <= 1f)
                {
                    c = hideM;
                    if (fy > 0.46f && fx < 0.70f) c = hideL;          // lit top-back
                    else if (fy < 0.37f || fx > 0.80f) c = hideD;     // shaded belly / chest
                }
                // withers HUMP (zebu) over the shoulders
                if (Disc(fx - 0.62f, fy - 0.55f, 0.055f)) { c = hideL; if (fx > 0.63f) c = hideM; }
                // TAIL flicking at the rear-left
                if (fx >= 0.485f && fx <= 0.515f && fy >= 0.28f && fy <= 0.44f) c = hideD;

                // NECK + blocky HEAD with a muzzle at the front-right
                if (fx >= 0.80f && fx <= 0.985f && fy >= 0.34f && fy <= 0.50f && !(fx > 0.95f && fy < 0.39f))
                {
                    c = hideM;
                    if (fy > 0.45f) c = hideL; else if (fy < 0.39f || fx > 0.93f) c = hideD;
                }
                // muzzle tip (dark nose pad)
                if (fx >= 0.93f && fx <= 0.985f && fy >= 0.395f && fy <= 0.455f) c = new Color(0.24f, 0.21f, 0.19f, 1f);
                // EYE
                if (Disc(fx - 0.905f, fy - 0.46f, 0.013f)) c = eye;
                // EAR (dark, tucked behind the horns)
                if (fx >= 0.815f && fx <= 0.85f && fy >= 0.49f && fy <= 0.525f) c = hideD;
                // HORNS — two cream sweeps from the crown so they read as a horned ox
                {
                    float hb = fy - 0.50f;
                    if (fx >= 0.86f && fx <= 0.975f && Mathf.Abs(hb - (fx - 0.86f) * 0.95f) <= 0.020f)
                    { c = horn; if (fx > 0.93f) c = new Color(0.95f, 0.93f, 0.85f, 1f); }   // forward horn (up-right)
                    if (fx >= 0.79f && fx <= 0.86f && Mathf.Abs(hb - (0.86f - fx) * 0.95f) <= 0.020f) c = horn; // back horn (up-left)
                }

                // YOKE pole linking cart to ox (a low wooden bar)
                if (fx >= 0.46f && fx <= 0.66f && fy >= 0.345f && fy <= 0.375f) c = woodD;

                px[y * s + x] = c;
            }

            // driver sits on the cart bed
            StampRider(px, s, 0.30f, 0.50f, 2, false);
            return FinishMount(px, s);
        }

        // ============================ AGE 3 — WAGON ============================
        private static Sprite BakeWagon(int frame)
        {
            const int s = 64; var px = new Color[s * s];
            var woodL = new Color(0.62f, 0.45f, 0.28f, 1f);
            var woodM = new Color(0.48f, 0.34f, 0.20f, 1f);
            var woodD = new Color(0.33f, 0.23f, 0.13f, 1f);
            var canL = new Color(0.90f, 0.87f, 0.80f, 1f);   // canvas tilt
            var canM = new Color(0.78f, 0.74f, 0.66f, 1f);
            var canD = new Color(0.60f, 0.56f, 0.48f, 1f);
            var rim = new Color(0.46f, 0.48f, 0.54f, 1f);    // iron tyre
            var tyre = new Color(0.20f, 0.21f, 0.25f, 1f);
            var spoke = new Color(0.58f, 0.43f, 0.27f, 1f);
            var hub = new Color(0.28f, 0.22f, 0.16f, 1f);

            float phase = frame * 0.20f;

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                Color c = Clear;

                // two wheels: rear (left, bigger) + front (right, smaller)
                Color wr = WheelPixel(fx, fy, 0.275f, 0.195f, 0.150f, phase, tyre, spoke, hub);
                if (wr.a > 0f) c = wr;
                Color wf = WheelPixel(fx, fy, 0.715f, 0.175f, 0.120f, phase, tyre, spoke, hub);
                if (wf.a > 0f) c = wf;
                // bright iron rim flecks on the outer tyre
                if (c.a > 0f && c == tyre && Frac((fx + fy) * 26f) < 0.30f) c = rim;

                // WAGON BODY (wooden box spanning the axles)
                if (fx >= 0.16f && fx <= 0.84f && fy >= 0.30f && fy <= 0.45f)
                {
                    c = woodM;
                    if (Frac(fx * 11f) < 0.12f) c = woodD;             // vertical plank seams
                    if (fy > 0.42f) c = woodL;                         // top rail lit
                    if (fy < 0.32f) c = woodD;
                }

                // CANVAS TILT (arched cover) over the rear 2/3
                float ax = (fx - 0.42f) / 0.30f;
                float archTop = 0.45f + 0.20f * Mathf.Sqrt(Mathf.Max(0f, 1f - ax * ax)); // dome
                if (fx >= 0.13f && fx <= 0.72f && fy >= 0.44f && fy <= archTop)
                {
                    c = canM;
                    if (fx < 0.34f) c = canL; else if (fx > 0.58f) c = canD;   // lit left, shaded right
                    // canvas ribs
                    if (Frac(fx * 9f) < 0.10f) c = canD;
                }

                // DRIVER BENCH / footboard at the front-right
                if (fx >= 0.66f && fx <= 0.84f && fy >= 0.40f && fy <= 0.46f) { c = woodL; if (fx > 0.78f) c = woodM; }

                px[y * s + x] = c;
            }

            // driver on the front bench (leaning forward to the reins)
            StampRider(px, s, 0.745f, 0.46f, 3, true);
            return FinishMount(px, s);
        }

        // ============================ AGE 4 — MOTORBIKE ============================
        private static Sprite BakeMotorbike(int frame)
        {
            const int s = 64; var px = new Color[s * s];
            var frameM = new Color(0.30f, 0.32f, 0.38f, 1f);
            var frameD = new Color(0.18f, 0.19f, 0.24f, 1f);
            var tank = new Color(0.74f, 0.20f, 0.18f, 1f);     // red fuel tank
            var tankL = new Color(0.90f, 0.34f, 0.28f, 1f);
            var chrome = new Color(0.80f, 0.82f, 0.88f, 1f);
            var chromeD = new Color(0.52f, 0.55f, 0.62f, 1f);
            var seat = new Color(0.14f, 0.13f, 0.15f, 1f);
            var tyre = new Color(0.12f, 0.12f, 0.14f, 1f);
            var spoke = new Color(0.62f, 0.64f, 0.70f, 1f);
            var hub = new Color(0.40f, 0.42f, 0.48f, 1f);
            var puff = new Color(0.74f, 0.74f, 0.76f, 0.55f);

            float phase = frame * 0.30f;   // wheels spin faster

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                Color c = Clear;

                // wheels: rear (left) + front (right)
                Color wr = WheelPixel(fx, fy, 0.265f, 0.185f, 0.150f, phase, tyre, spoke, hub);
                if (wr.a > 0f) c = wr;
                Color wf = WheelPixel(fx, fy, 0.745f, 0.185f, 0.150f, phase, tyre, spoke, hub);
                if (wf.a > 0f) c = wf;

                // lower frame / engine block between the wheels
                if (fx >= 0.30f && fx <= 0.70f && fy >= 0.17f && fy <= 0.33f)
                {
                    c = frameM;
                    if (fy < 0.22f) c = frameD;
                    // engine fins
                    if (fx >= 0.42f && fx <= 0.60f && fy >= 0.18f && fy <= 0.30f && Frac(fx * 22f) < 0.45f) c = frameD;
                }
                // swingarm + forks (diagonals to the hubs)
                if (Mathf.Abs((fy - 0.185f) - (fx - 0.265f) * 0.0f) <= 0.018f && fx >= 0.26f && fx <= 0.50f) c = frameD; // rear arm
                {
                    float fork = fx - (0.62f + (fy - 0.185f) * 0.30f);
                    if (Mathf.Abs(fork) <= 0.020f && fy >= 0.18f && fy <= 0.46f) c = chromeD; // front fork up to bars
                }

                // FUEL TANK (rounded, mid)
                if (Disc((fx - 0.50f) / 1.4f, fy - 0.37f, 0.085f) && fy >= 0.33f)
                {
                    c = tank; if (fy > 0.40f && fx < 0.52f) c = tankL; else if (fx > 0.58f) c = new Color(0.55f, 0.14f, 0.13f, 1f);
                }
                // SEAT behind the tank
                if (fx >= 0.30f && fx <= 0.50f && fy >= 0.37f && fy <= 0.43f) { c = seat; if (fy > 0.41f) c = new Color(0.22f, 0.21f, 0.23f, 1f); }

                // HANDLEBAR up front
                if (fx >= 0.66f && fx <= 0.80f && fy >= 0.45f && fy <= 0.475f) c = chrome;
                if (fx >= 0.785f && fx <= 0.805f && fy >= 0.44f && fy <= 0.49f) c = chromeD;

                // EXHAUST pipe low-right + animated puff
                if (fx >= 0.60f && fx <= 0.84f && fy >= 0.135f && fy <= 0.175f) { c = chrome; if (fy < 0.155f) c = chromeD; }
                if (c.a < 0.01f)
                {
                    float pr = 0.03f + frame * 0.012f;
                    if (Disc(fx - (0.88f + frame * 0.01f), fy - 0.16f, pr)) c = puff;
                }

                px[y * s + x] = c;
            }

            // rider leaning forward over the tank
            StampRider(px, s, 0.45f, 0.42f, 4, true);
            return FinishMount(px, s);
        }
    }
}
