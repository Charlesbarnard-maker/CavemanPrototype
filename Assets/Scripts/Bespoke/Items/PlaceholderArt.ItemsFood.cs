// Bespoke procedural item art. Partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        // ===== Food: cluster of red berries with a green leaf =====
        private static Sprite _itemFood;
        public static Sprite ItemFood()
        {
            if (_itemFood != null) return _itemFood;
            const int s = 64;
            var px = new Color[s * s];

            Color berryMid = new Color(0.85f, 0.35f, 0.35f);
            Color berryLit = new Color(0.95f, 0.52f, 0.50f);
            Color berryDark = new Color(0.58f, 0.18f, 0.20f);
            Color leafMid = new Color(0.34f, 0.62f, 0.26f);
            Color leafLit = new Color(0.50f, 0.78f, 0.36f);
            Color leafDark = new Color(0.20f, 0.42f, 0.16f);

            // 4 berries: three lower, one upper-left tucked in
            float[] bx = { 0.34f, 0.62f, 0.48f, 0.40f };
            float[] by = { 0.34f, 0.36f, 0.18f, 0.55f };
            float[] br = { 0.20f, 0.19f, 0.18f, 0.15f };

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // small green leaf at the top
                    float lcx = 0.56f, lcy = 0.80f;
                    float ldx = fx - lcx, ldy = fy - lcy;
                    // rotated narrow ellipse for a leaf shape
                    float lu = ldx * 0.8f + ldy * 0.6f;
                    float lv = -ldx * 0.6f + ldy * 0.8f;
                    if ((lu * lu) / (0.16f * 0.16f) + (lv * lv) / (0.07f * 0.07f) <= 1f)
                    {
                        c = leafMid;
                        if (lv < 0f) c = leafLit;            // upper face lit
                        if (lu > 0.05f) c = leafDark;        // far tip shadow
                        if (Mathf.Abs(lv) < 0.012f) c = leafDark; // central vein
                    }

                    // berries (drawn over leaf base, front-to-back)
                    for (int i = 0; i < bx.Length; i++)
                    {
                        float dx = fx - bx[i], dy = fy - by[i];
                        if (Disc(dx, dy, br[i]))
                        {
                            float lit = (-dx + dy);             // upper-left brighter
                            c = berryMid;
                            if (lit > 0.06f) c = berryLit;
                            if (lit < -0.06f) c = berryDark;
                            // tiny white highlight glint
                            if (Disc(dx + br[i] * 0.4f, dy - br[i] * 0.4f, br[i] * 0.16f))
                                c = new Color(1f, 0.95f, 0.92f);
                        }
                    }

                    px[y * s + x] = c;
                }

            _itemFood = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemFood;
        }

        // ===== Cooked Food: roasted drumstick on bone =====
        private static Sprite _itemCookedFood;
        public static Sprite ItemCookedFood()
        {
            if (_itemCookedFood != null) return _itemCookedFood;
            const int s = 64;
            var px = new Color[s * s];

            Color meatMid = new Color(0.80f, 0.50f, 0.26f);
            Color meatLit = new Color(0.92f, 0.64f, 0.36f);
            Color meatDark = new Color(0.52f, 0.30f, 0.14f);
            Color glaze = new Color(0.62f, 0.34f, 0.14f);
            Color boneMid = new Color(0.92f, 0.90f, 0.80f);
            Color boneLit = new Color(1f, 0.98f, 0.92f);
            Color boneDark = new Color(0.70f, 0.66f, 0.56f);

            // drumstick: fat rounded meat bulb lower-left, bone shaft to upper-right
            float meatCx = 0.38f, meatCy = 0.38f;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // bone shaft (diagonal capsule from meat toward upper-right)
                    // axis direction (0.7,0.7); test distance from the segment line
                    float ax = 0.7f, ay = 0.7f;
                    // point relative to a start near meat top
                    float sx = fx - 0.52f, sy = fy - 0.54f;
                    float t = sx * ax + sy * ay;            // projection
                    float px2 = sx - t * ax, py2 = sy - t * ay; // perpendicular comp
                    float perp = Mathf.Sqrt(px2 * px2 + py2 * py2);
                    if (t > -0.02f && t < 0.26f && perp < 0.055f)
                    {
                        c = boneMid;
                        if (px2 - py2 < 0f) c = boneLit; else c = boneDark;
                        if (perp < 0.03f) c = boneMid;
                    }
                    // white bone nub knob at the narrow end (upper-right)
                    if (Disc(fx - 0.74f, fy - 0.74f, 0.085f))
                    {
                        c = boneMid;
                        float lit = (-(fx - 0.74f) + (fy - 0.74f));
                        if (lit > 0.02f) c = boneLit;
                        if (lit < -0.03f) c = boneDark;
                    }

                    // meat bulb (big rounded blob; slightly drumstick-flared)
                    float dx = fx - meatCx, dy = fy - meatCy;
                    float rr = 0.245f;
                    // squash a touch so it reads as a drumstick lump
                    if ((dx * dx) / (rr * rr) + (dy * dy) / ((rr * 1.05f) * (rr * 1.05f)) <= 1f)
                    {
                        float lit = (-dx + dy);
                        c = meatMid;
                        if (lit > 0.05f) c = meatLit;
                        if (lit < -0.05f) c = meatDark;
                        // glaze sheen bands (cooked/roasted streaks)
                        if (Frac((fx + fy) * 9f) < 0.18f && lit < 0.04f) c = glaze;
                        // bright roasted glint
                        if (Disc(dx + 0.10f, dy - 0.09f, 0.04f)) c = new Color(0.98f, 0.78f, 0.46f);
                    }

                    px[y * s + x] = c;
                }

            _itemCookedFood = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemCookedFood;
        }

        // ===== Meat: raw red haunch/steak with bone end =====
        private static Sprite _itemMeat;
        public static Sprite ItemMeat()
        {
            if (_itemMeat != null) return _itemMeat;
            const int s = 64;
            var px = new Color[s * s];

            Color rawMid = new Color(0.74f, 0.30f, 0.30f);
            Color rawLit = new Color(0.88f, 0.44f, 0.42f);
            Color rawDark = new Color(0.50f, 0.18f, 0.20f);
            Color marble = new Color(0.90f, 0.62f, 0.58f);
            Color boneMid = new Color(0.92f, 0.90f, 0.82f);
            Color boneLit = new Color(1f, 0.98f, 0.92f);
            Color boneDark = new Color(0.72f, 0.68f, 0.58f);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // white bone end sticking out top-right
                    if (Disc(fx - 0.72f, fy - 0.76f, 0.10f))
                    {
                        c = boneMid;
                        float lit = (-(fx - 0.72f) + (fy - 0.76f));
                        if (lit > 0.02f) c = boneLit;
                        if (lit < -0.03f) c = boneDark;
                    }
                    // short bone shaft connecting into the meat
                    {
                        float bx = fx - 0.60f, by = fy - 0.62f;
                        float u = bx * 0.7f + by * 0.7f;
                        float v = -bx * 0.7f + by * 0.7f;
                        if (u > -0.10f && u < 0.10f && Mathf.Abs(v) < 0.05f)
                        {
                            c = boneMid;
                            if (v < 0f) c = boneLit; else c = boneDark;
                        }
                    }

                    // big rounded haunch of meat (lower-left, teardrop)
                    float dx = fx - 0.42f, dy = fy - 0.40f;
                    float rr = 0.28f;
                    if ((dx * dx) / (rr * rr) + (dy * dy) / ((rr * 0.92f) * (rr * 0.92f)) <= 1f)
                    {
                        float lit = (-dx + dy);
                        c = rawMid;
                        if (lit > 0.05f) c = rawLit;
                        if (lit < -0.05f) c = rawDark;
                        // marbling streaks (white fat veins)
                        if (Frac((fx * 0.6f + fy) * 7f) < 0.12f) c = marble;
                        if (Frac((fx - fy * 0.5f) * 6f) < 0.10f) c = Color.Lerp(c, marble, 0.6f);
                        // glossy raw glint
                        if (Disc(dx + 0.10f, dy - 0.10f, 0.05f)) c = new Color(0.95f, 0.60f, 0.56f);
                    }

                    px[y * s + x] = c;
                }

            _itemMeat = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemMeat;
        }

        // ===== Grain: golden wheat sheaf =====
        private static Sprite _itemGrain;
        public static Sprite ItemGrain()
        {
            if (_itemGrain != null) return _itemGrain;
            const int s = 64;
            var px = new Color[s * s];

            Color stalkMid = new Color(0.86f, 0.76f, 0.36f);
            Color stalkLit = new Color(0.96f, 0.88f, 0.50f);
            Color stalkDark = new Color(0.62f, 0.52f, 0.20f);
            Color headMid = new Color(0.90f, 0.74f, 0.30f);
            Color headLit = new Color(1f, 0.90f, 0.46f);
            Color bandMid = new Color(0.55f, 0.36f, 0.16f);
            Color bandLit = new Color(0.70f, 0.50f, 0.26f);

            // five stalks fanning out from a tie at center
            float tieY = 0.42f;
            float[] sx = { 0.28f, 0.39f, 0.50f, 0.61f, 0.72f }; // x at the TOP
            // all converge near x=0.5 at the band

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // stalks: above tie they fan, below tie they bunch downward
                    for (int i = 0; i < sx.Length; i++)
                    {
                        // interpolate stalk x from base (0.5 area) up to spread top
                        float topX = sx[i];
                        float baseX = 0.42f + i * 0.04f; // slight spread at the bottom too
                        // param along height
                        float lineX;
                        if (fy >= tieY)
                        {
                            float k = (fy - tieY) / (0.96f - tieY);
                            lineX = Mathf.Lerp(0.46f + (i - 2) * 0.012f, topX, Mathf.Clamp01(k));
                        }
                        else
                        {
                            float k = (tieY - fy) / (tieY - 0.06f);
                            lineX = Mathf.Lerp(0.50f + (i - 2) * 0.012f, baseX, Mathf.Clamp01(k));
                        }
                        if (Mathf.Abs(fx - lineX) < 0.022f && fy > 0.06f && fy < 0.92f)
                        {
                            c = stalkMid;
                            if (fx < lineX) c = stalkLit; else c = stalkDark;
                        }

                        // grain-head blob at the top of each upper stalk
                        if (fy >= tieY)
                        {
                            float hx = Mathf.Lerp(0.47f, topX, 1f);
                            float hy = 0.84f - Mathf.Abs(i - 2) * 0.02f;
                            float ddx = fx - hx, ddy = fy - hy;
                            if ((ddx * ddx) / (0.045f * 0.045f) + (ddy * ddy) / (0.085f * 0.085f) <= 1f)
                            {
                                c = headMid;
                                if (-ddx + ddy > 0f) c = headLit;
                                // little awns / kernel notches
                                if (Frac(fy * 22f) < 0.4f && Mathf.Abs(ddx) > 0.018f) c = stalkDark;
                            }
                        }
                    }

                    // tie band across the middle
                    if (fy > tieY - 0.045f && fy < tieY + 0.045f && Mathf.Abs(fx - 0.50f) < 0.14f)
                    {
                        c = bandMid;
                        if (fy > tieY) c = bandLit;
                    }

                    px[y * s + x] = c;
                }

            _itemGrain = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemGrain;
        }

        // ===== Flour: cinched cream sack with dust puff =====
        private static Sprite _itemFlour;
        public static Sprite ItemFlour()
        {
            if (_itemFlour != null) return _itemFlour;
            const int s = 64;
            var px = new Color[s * s];

            Color sackMid = new Color(0.90f, 0.86f, 0.72f);
            Color sackLit = new Color(0.98f, 0.95f, 0.84f);
            Color sackDark = new Color(0.70f, 0.65f, 0.50f);
            Color tie = new Color(0.55f, 0.42f, 0.24f);
            Color tieLit = new Color(0.70f, 0.56f, 0.34f);
            Color spill = new Color(0.97f, 0.95f, 0.88f);

            float neckY = 0.74f;     // where it cinches
            float cx = 0.5f;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // faint flour-dust puff above the neck
                    if (fy > neckY + 0.04f)
                    {
                        if (Disc(fx - 0.40f, fy - 0.88f, 0.05f) ||
                            Disc(fx - 0.58f, fy - 0.90f, 0.045f) ||
                            Disc(fx - 0.50f, fy - 0.84f, 0.035f))
                            c = new Color(0.98f, 0.96f, 0.90f, 0.85f);
                    }

                    // sack body: bulges wide at base, narrows toward neck
                    if (fy < neckY)
                    {
                        // half-width as a function of height: wide near 0.30, narrow at neck
                        float k = Mathf.Clamp01((neckY - fy) / (neckY - 0.10f));
                        float halfW = Mathf.Lerp(0.10f, 0.30f, k);   // narrow at neck, fat at base
                        // round off the very bottom
                        float botRound = 1f;
                        if (fy < 0.16f) botRound = Mathf.Clamp01((fy - 0.06f) / 0.10f);
                        halfW *= Mathf.Lerp(0.6f, 1f, botRound);

                        if (Mathf.Abs(fx - cx) < halfW)
                        {
                            float lit = (cx - fx) + (fy - 0.4f) * 0.3f;
                            c = sackMid;
                            if (fx < cx - halfW * 0.25f) c = sackLit;
                            if (fx > cx + halfW * 0.35f) c = sackDark;
                            // soft vertical creases
                            if (Frac((fx - cx) * 14f) < 0.10f) c = Color.Lerp(c, sackDark, 0.4f);
                            // bright sheen patch upper-left of belly
                            if (Disc(fx - 0.40f, fy - 0.50f, 0.06f)) c = sackLit;
                        }
                    }

                    // cinched neck + tie band
                    if (fy >= neckY - 0.02f && fy <= neckY + 0.10f && Mathf.Abs(fx - cx) < 0.11f)
                    {
                        // gathered top (ruffled bunch above the tie)
                        c = sackMid;
                        if (fx < cx) c = sackLit; else c = sackDark;
                        // the tie itself
                        if (fy >= neckY && fy <= neckY + 0.04f)
                        {
                            c = tie;
                            if (fx < cx) c = tieLit;
                        }
                        // ruffle tips above the tie
                        if (fy > neckY + 0.04f && Frac(fx * 16f) < 0.4f) c = sackLit;
                    }

                    // small spilled flour pile at the bottom
                    if (fy < 0.14f)
                    {
                        if (Disc(fx - 0.30f, fy - 0.07f, 0.07f) ||
                            Disc(fx - 0.66f, fy - 0.06f, 0.06f))
                            c = spill;
                    }

                    px[y * s + x] = c;
                }

            _itemFlour = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemFlour;
        }

        // ===== Bread: golden domed loaf with score marks =====
        private static Sprite _itemBread;
        public static Sprite ItemBread()
        {
            if (_itemBread != null) return _itemBread;
            const int s = 64;
            var px = new Color[s * s];

            Color crustMid = new Color(0.80f, 0.58f, 0.30f);
            Color crustLit = new Color(0.92f, 0.72f, 0.42f);
            Color crustDark = new Color(0.56f, 0.36f, 0.16f);
            Color floured = new Color(0.94f, 0.84f, 0.62f);
            Color scoreDark = new Color(0.46f, 0.28f, 0.12f);

            float cx = 0.5f, cy = 0.40f;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // loaf body: wide oval with a domed top, flatter base
                    float dx = fx - cx, dy = fy - cy;
                    float rx = 0.36f, ry = 0.30f;
                    // flatten the bottom a little
                    float ery = (dy < 0f) ? ry * 1.1f : ry;
                    if ((dx * dx) / (rx * rx) + (dy * dy) / (ery * ery) <= 1f && fy > 0.12f)
                    {
                        float lit = (-dx + dy);
                        c = crustMid;
                        if (lit > 0.06f) c = crustLit;
                        if (lit < -0.06f) c = crustDark;

                        // lighter floured highlight across the domed top
                        if (dy > 0.06f && (dx * dx) / (rx * rx) + (dy * dy) / (ery * ery) <= 0.85f)
                        {
                            if (Disc(dx, dy - 0.14f, 0.20f)) c = Color.Lerp(c, floured, 0.55f);
                        }

                        // 3 diagonal score marks on the top
                        float diag = fx - fy;            // constant along the up-right diagonals
                        float band = Frac(diag * 6.0f);
                        if (dy > -0.02f && band < 0.12f &&
                            (dx * dx) / (rx * rx) + (dy * dy) / (ery * ery) <= 0.80f)
                        {
                            c = scoreDark;
                            // a little lit lip on the lower edge of each score
                            if (band < 0.04f) c = crustLit;
                        }

                        // bright crust glint upper-left
                        if (Disc(dx + 0.14f, dy - 0.10f, 0.05f)) c = new Color(0.98f, 0.84f, 0.56f);
                    }

                    px[y * s + x] = c;
                }

            _itemBread = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemBread;
        }
    }
}
