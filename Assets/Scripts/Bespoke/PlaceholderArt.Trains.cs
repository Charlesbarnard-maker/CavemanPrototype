// Bespoke procedural art for the TRAIN parts — per-age locomotives (the puller) + the wagons you couple behind
// them. Side profile facing RIGHT, baked full colour (the route vehicle renders near-white), 64x64, fy=0 bottom.
// Locos by age: 0 Donkey · 1 Ox · 2 Horse · 3 Steam loco · 4 Diesel loco. Wagons: a cargo box + a liquid tanker.
// frame 0..2 animates legs (animals) / wheels (machines). A dark outline pass finishes each.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite[] _trainLoco;  // tier(0..4)*3 + frame
        private static Sprite[] _cargoWagon; // frame 0..2 (cargo tint applied by the renderer)
        private static Sprite[] _liquidWagon;

        /// <summary>The locomotive/puller for an age tier (0 Donkey…4 Diesel), animated over frame 0..2.</summary>
        public static Sprite TrainLoco(int tier, int frame)
        {
            tier = Mathf.Clamp(tier, 0, 4); frame = Mathf.Clamp(frame, 0, 2);
            if (_trainLoco == null) _trainLoco = new Sprite[5 * 3];
            int k = tier * 3 + frame;
            if (_trainLoco[k] != null) return _trainLoco[k];
            const int s = 64; var px = new Color[s * s];
            switch (tier)
            {
                case 0: BakeDraftAnimal(px, s, frame, new Color(0.55f, 0.50f, 0.46f, 1f), new Color(0.44f, 0.40f, 0.36f, 1f), new Color(0.30f, 0.27f, 0.24f, 1f), donkey: true); break;
                case 1: BakeDraftAnimal(px, s, frame, new Color(0.50f, 0.40f, 0.30f, 1f), new Color(0.40f, 0.31f, 0.22f, 1f), new Color(0.28f, 0.21f, 0.14f, 1f), ox: true); break;
                case 2: BakeDraftAnimal(px, s, frame, new Color(0.55f, 0.39f, 0.24f, 1f), new Color(0.44f, 0.30f, 0.17f, 1f), new Color(0.30f, 0.20f, 0.11f, 1f), horse: true); break;
                case 3: BakeSteamLoco(px, s, frame); break;
                default: BakeDieselLoco(px, s, frame); break;
            }
            _trainLoco[k] = Finish(px, s);
            return _trainLoco[k];
        }

        /// <summary>An open cargo wagon (its load area is tinted by the renderer to the commodity colour).</summary>
        public static Sprite CargoWagon(int frame)
        {
            frame = Mathf.Clamp(frame, 0, 2);
            if (_cargoWagon == null) _cargoWagon = new Sprite[3];
            if (_cargoWagon[frame] != null) return _cargoWagon[frame];
            const int s = 64; var px = new Color[s * s];
            BakeWagon(px, s, frame, liquid: false);
            _cargoWagon[frame] = Finish(px, s);
            return _cargoWagon[frame];
        }

        /// <summary>A liquid tanker wagon (cylindrical barrel; tinted by the renderer to the liquid colour).</summary>
        public static Sprite LiquidWagon(int frame)
        {
            frame = Mathf.Clamp(frame, 0, 2);
            if (_liquidWagon == null) _liquidWagon = new Sprite[3];
            if (_liquidWagon[frame] != null) return _liquidWagon[frame];
            const int s = 64; var px = new Color[s * s];
            BakeWagon(px, s, frame, liquid: true);
            _liquidWagon[frame] = Finish(px, s);
            return _liquidWagon[frame];
        }

        // ---- shared finish: dark outline + sprite ----
        private static Sprite Finish(Color[] px, int s)
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
                    int nx = x + ox, ny = y + oy; if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                    if (px[ny * s + nx].a > 0.01f) { adj = true; break; }
                }
                if (adj) outPx[y * s + x] = dark;
            }
            var tex = NewTex(s); tex.SetPixels(outPx); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        // ---- draft animals (donkey / ox / horse): a side-profile body + 4 trotting legs, head to the RIGHT ----
        private static void BakeDraftAnimal(Color[] px, int s, int frame, Color hideL, Color hideM, Color hideD,
                                            bool donkey = false, bool ox = false, bool horse = false)
        {
            var mane = new Color(0.16f, 0.12f, 0.09f, 1f);
            var hoof = new Color(0.12f, 0.10f, 0.09f, 1f);
            var horn = new Color(0.86f, 0.83f, 0.73f, 1f);
            var eye = new Color(0.06f, 0.05f, 0.05f, 1f);
            float sw = frame == 1 ? 1f : frame == 2 ? -1f : 0f;
            float fOff = sw * 0.03f, bOff = -sw * 0.03f;

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1); Color c = Clear;

                // legs (back pair ~0.28/0.36, front pair ~0.60/0.68)
                if (fy >= 0.07f && fy <= 0.34f)
                {
                    float bA = 0.275f + (fy < 0.18f ? bOff : 0f), bB = 0.355f - (fy < 0.18f ? bOff : 0f);
                    float fA = 0.585f + (fy < 0.18f ? fOff : 0f), fB = 0.665f - (fy < 0.18f ? fOff : 0f);
                    if (Mathf.Abs(fx - bA) <= 0.028f || Mathf.Abs(fx - fA) <= 0.028f) c = hideD;
                    if (Mathf.Abs(fx - bB) <= 0.030f || Mathf.Abs(fx - fB) <= 0.030f) c = hideM;
                    if (fy <= 0.095f && (Mathf.Abs(fx - bA) <= 0.032f || Mathf.Abs(fx - bB) <= 0.032f ||
                                         Mathf.Abs(fx - fA) <= 0.032f || Mathf.Abs(fx - fB) <= 0.032f)) c = hoof;
                }
                // barrel body
                float by = ox ? 0.13f : 0.115f;
                if (((fx - 0.47f) / 0.28f) * ((fx - 0.47f) / 0.28f) + ((fy - 0.42f) / by) * ((fy - 0.42f) / by) <= 1f)
                { c = hideM; if (fy > 0.46f && fx < 0.5f) c = hideL; else if (fy < 0.37f || fx > 0.64f) c = hideD; }
                // tail at back-left
                if (fx >= 0.15f && fx <= 0.22f && fy >= 0.30f && fy <= 0.50f && Mathf.Abs((fx - 0.185f) + (0.40f - fy) * 0.05f) <= 0.03f) c = mane;
                // neck + head to the upper-right
                float ns = (fy - 0.45f); float nx = fx - (0.62f + ns * (horse ? 0.9f : 0.5f));
                if (nx >= -0.05f && nx <= 0.05f && fy >= 0.44f && fy <= 0.60f) { c = hideM; if (nx > 0.02f) c = hideD; else c = hideL; }
                if (Disc(fx - 0.76f, fy - (donkey ? 0.55f : 0.58f), 0.06f) || (fx >= 0.75f && fx <= 0.86f && fy >= 0.50f && fy <= 0.585f))
                { c = hideM; if (fx > 0.80f || fy < 0.53f) c = hideD; else c = hideL; }
                // ears (donkey = tall) / horns (ox)
                if (donkey) { if ((fx >= 0.74f && fx <= 0.765f) || (fx >= 0.785f && fx <= 0.81f)) { if (fy >= 0.60f && fy <= 0.72f) c = hideD; } }
                else if (fx >= 0.74f && fx <= 0.78f && fy >= 0.60f && fy <= 0.65f) c = hideD; // ear
                if (ox && fy >= 0.575f && fy <= 0.61f && fx >= 0.78f && fx <= 0.90f) c = horn; // forward horn
                if (horse && nx >= -0.065f && nx <= -0.035f && fy >= 0.47f && fy <= 0.62f) c = mane; // horse mane
                if (Disc(fx - 0.79f, fy - 0.575f, 0.012f)) c = eye;

                if (c.a > 0f) px[y * s + x] = c;
            }
        }

        // ---- steam locomotive: boiler + smokestack (smoke) + cab + big driving wheels + cowcatcher ----
        private static void BakeSteamLoco(Color[] px, int s, int frame)
        {
            Color bodyM = new Color(0.30f, 0.34f, 0.40f, 1f), bodyL = new Color(0.46f, 0.50f, 0.57f, 1f), bodyD = new Color(0.18f, 0.20f, 0.25f, 1f);
            var brass = new Color(0.82f, 0.62f, 0.28f, 1f);
            Color wheel = new Color(0.16f, 0.16f, 0.19f, 1f), spoke = new Color(0.55f, 0.57f, 0.62f, 1f), hub = new Color(0.34f, 0.36f, 0.40f, 1f);
            var fire = new Color(1f, 0.6f, 0.16f, 1f);
            var smoke = new Color(0.80f, 0.80f, 0.82f, 0.7f);
            float phase = frame * 0.33f;

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1); Color c = Clear;
                // driving wheels
                Color w1 = WheelPixel(fx, fy, 0.40f, 0.17f, 0.13f, phase, wheel, spoke, hub);
                Color w2 = WheelPixel(fx, fy, 0.66f, 0.17f, 0.13f, phase, wheel, spoke, hub);
                if (w1.a > 0f) c = w1; if (w2.a > 0f) c = w2;
                // boiler (horizontal cylinder)
                if (fx >= 0.16f && fx <= 0.70f && fy >= 0.30f && fy <= 0.52f) { c = bodyM; if (fy > 0.46f) c = bodyL; else if (fy < 0.35f) c = bodyD; if (Frac(fx * 9f) < 0.06f) c = bodyD; }
                if (fx >= 0.66f && fx <= 0.72f && fy >= 0.30f && fy <= 0.55f) c = brass; // boiler-front band
                // cab at the back
                if (fx >= 0.13f && fx <= 0.34f && fy >= 0.30f && fy <= 0.66f) { c = bodyM; if (fx < 0.18f) c = bodyD; }
                if (fx >= 0.18f && fx <= 0.30f && fy >= 0.50f && fy <= 0.62f) c = new Color(0.55f, 0.78f, 0.92f, 1f); // cab window
                if (fx >= 0.155f && fx <= 0.20f && fy >= 0.31f && fy <= 0.40f) c = fire; // firebox glow
                // smokestack + smoke
                if (fx >= 0.21f && fx <= 0.275f && fy >= 0.52f && fy <= 0.66f) { c = bodyD; if (fy > 0.62f) c = bodyM; }
                float pf = frame / 3f; if (c.a < 0.05f && Disc(fx - (0.243f + 0.05f * pf), fy - (0.70f + 0.06f * pf), 0.05f - 0.015f * pf)) c = smoke;
                // dome + whistle on the boiler top
                if (Disc(fx - 0.46f, fy - 0.52f, 0.04f)) c = brass;
                // cowcatcher at the front
                if (fx >= 0.70f && fx <= 0.82f && fy >= 0.09f && fy <= 0.30f && (fx - 0.70f) > (0.30f - fy) * 0.4f) c = bodyD;
                if (c.a > 0f) px[y * s + x] = c;
            }
        }

        // ---- diesel/electric loco: a sleek boxy body, cab window, hi-vis stripe, small bogie wheels, headlight ----
        private static void BakeDieselLoco(Color[] px, int s, int frame)
        {
            Color bodyM = new Color(0.66f, 0.26f, 0.22f, 1f), bodyL = new Color(0.82f, 0.36f, 0.30f, 1f), bodyD = new Color(0.44f, 0.16f, 0.13f, 1f);
            var grey = new Color(0.34f, 0.36f, 0.42f, 1f);
            Color wheel = new Color(0.14f, 0.14f, 0.17f, 1f), spoke = new Color(0.5f, 0.52f, 0.57f, 1f), hub = new Color(0.3f, 0.32f, 0.36f, 1f);
            var stripe = new Color(0.95f, 0.80f, 0.22f, 1f);
            var glass = new Color(0.55f, 0.78f, 0.92f, 1f);
            var light = new Color(1f, 0.95f, 0.6f, 1f);
            float phase = frame * 0.4f;

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1); Color c = Clear;
                Color w1 = WheelPixel(fx, fy, 0.34f, 0.15f, 0.085f, phase, wheel, spoke, hub);
                Color w2 = WheelPixel(fx, fy, 0.50f, 0.15f, 0.085f, phase, wheel, spoke, hub);
                Color w3 = WheelPixel(fx, fy, 0.70f, 0.15f, 0.085f, phase, wheel, spoke, hub);
                if (w1.a > 0f) c = w1; if (w2.a > 0f) c = w2; if (w3.a > 0f) c = w3;
                // underframe
                if (fx >= 0.14f && fx <= 0.86f && fy >= 0.16f && fy <= 0.24f) c = grey;
                // body (sloped nose to the right)
                if (fx >= 0.14f && fx <= 0.86f && fy >= 0.24f && fy <= 0.56f && !(fx > 0.78f && fy > 0.46f))
                { c = bodyM; if (fy > 0.48f) c = bodyL; else if (fy < 0.30f) c = bodyD; }
                if (fx >= 0.14f && fx <= 0.86f && fy >= 0.30f && fy <= 0.345f) c = stripe; // hi-vis stripe
                // cab window (left/back) + nose window (right)
                if (fx >= 0.20f && fx <= 0.38f && fy >= 0.42f && fy <= 0.54f) c = glass;
                if (fx >= 0.70f && fx <= 0.80f && fy >= 0.40f && fy <= 0.50f) c = glass;
                if (Disc(fx - 0.835f, fy - 0.30f, 0.022f)) c = light; // headlight
                if (c.a > 0f) px[y * s + x] = c;
            }
        }

        // ---- a wagon: open cargo box (liquid=false) or a cylindrical tanker (liquid=true), on two bogies ----
        private static void BakeWagon(Color[] px, int s, int frame, bool liquid)
        {
            Color frameM = new Color(0.42f, 0.34f, 0.24f, 1f), frameL = new Color(0.56f, 0.45f, 0.31f, 1f), frameD = new Color(0.28f, 0.22f, 0.15f, 1f);
            Color metalM = new Color(0.46f, 0.48f, 0.54f, 1f), metalL = new Color(0.64f, 0.66f, 0.72f, 1f), metalD = new Color(0.30f, 0.31f, 0.36f, 1f);
            var cargo = new Color(0.78f, 0.66f, 0.44f, 1f); // the load zone — renderer tints this to the commodity
            Color wheel = new Color(0.14f, 0.14f, 0.17f, 1f), spoke = new Color(0.5f, 0.52f, 0.57f, 1f), hub = new Color(0.3f, 0.32f, 0.36f, 1f);
            float phase = frame * 0.4f;

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1); Color c = Clear;
                Color w1 = WheelPixel(fx, fy, 0.30f, 0.15f, 0.085f, phase, wheel, spoke, hub);
                Color w2 = WheelPixel(fx, fy, 0.70f, 0.15f, 0.085f, phase, wheel, spoke, hub);
                if (w1.a > 0f) c = w1; if (w2.a > 0f) c = w2;
                // chassis bar
                if (fx >= 0.14f && fx <= 0.86f && fy >= 0.16f && fy <= 0.22f) c = frameD;
                // couplers poking out each end
                if ((fx >= 0.06f && fx <= 0.14f) || (fx >= 0.86f && fx <= 0.94f)) { if (fy >= 0.17f && fy <= 0.21f) c = metalD; }

                if (!liquid)
                {
                    // open-top cargo box: wooden sides + a heaped load
                    if (fx >= 0.16f && fx <= 0.84f && fy >= 0.22f && fy <= 0.50f)
                    {
                        bool wall = fx < 0.20f || fx > 0.80f || fy < 0.26f;
                        c = wall ? frameM : cargo;
                        if (wall && (fx > 0.80f || fy < 0.26f)) c = frameD; else if (wall) c = frameL;
                        if (!wall) { if (Frac(fx * 7f) < 0.2f) c = new Color(0.66f, 0.55f, 0.36f, 1f); } // load lumps
                    }
                    // heaped top
                    if (fy >= 0.50f && fy <= 0.58f && Mathf.Abs(fx - 0.5f) <= 0.30f && Frac(fx * 9f) < 0.6f) c = cargo;
                }
                else
                {
                    // cylindrical tanker barrel
                    if (((fx - 0.5f) / 0.34f) * ((fx - 0.5f) / 0.34f) + ((fy - 0.40f) / 0.17f) * ((fy - 0.40f) / 0.17f) <= 1f)
                    { c = metalM; if (fy > 0.44f) c = metalL; else if (fy < 0.34f) c = metalD; if (Mathf.Abs(fx - 0.5f) < 0.02f) c = metalD; }
                    // end caps + a top hatch
                    if ((fx >= 0.15f && fx <= 0.20f || fx >= 0.80f && fx <= 0.85f) && fy >= 0.28f && fy <= 0.52f) c = metalD;
                    if (fx >= 0.46f && fx <= 0.54f && fy >= 0.54f && fy <= 0.60f) c = metalD; // hatch
                }
                if (c.a > 0f) px[y * s + x] = c;
            }
        }
    }
}
