// Bespoke procedural item art. Partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        // ---- Wood: a bundle of round logs seen end-on ----
        private static Sprite _itemWood;
        public static Sprite ItemWood()
        {
            if (_itemWood != null) return _itemWood;
            const int s = 64;
            var px = new Color[s * s];

            Color bark = new Color(0.40f, 0.27f, 0.15f);
            Color barkLit = new Color(0.52f, 0.37f, 0.22f);
            Color barkShd = new Color(0.28f, 0.18f, 0.10f);
            Color grain = new Color(0.70f, 0.52f, 0.32f);
            Color grainLit = new Color(0.82f, 0.64f, 0.42f);
            Color grainShd = new Color(0.56f, 0.40f, 0.24f);
            Color tie = new Color(0.34f, 0.22f, 0.12f);

            // three logs: two on bottom, one on top
            float r = 0.24f;
            float[] cxs = { 0.31f, 0.69f, 0.50f };
            float[] cys = { 0.36f, 0.36f, 0.66f };

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // draw from back (top log first) to front so front logs overlap
                    for (int i = 0; i < 3; i++)
                    {
                        float cx = cxs[i], cy = cys[i];
                        float dx = fx - cx, dy = fy - cy;
                        float d2 = dx * dx + dy * dy;
                        if (d2 <= r * r)
                        {
                            float dist = Mathf.Sqrt(d2) / r; // 0 center .. 1 rim
                            Color lc;
                            if (dist > 0.82f)
                            {
                                // bark rim, lit upper-left / shadow lower-right
                                float lr = (-dx - dy) / (r * 1.4f);
                                lc = Color.Lerp(barkShd, barkLit, Mathf.Clamp01(lr * 0.5f + 0.5f));
                                lc = Color.Lerp(lc, bark, 0.35f);
                            }
                            else
                            {
                                // end-grain concentric rings
                                float ring = Frac(dist * 4.0f);
                                Color baseG = grain;
                                float lr = (-dx - dy) / (r * 1.4f);
                                baseG = Color.Lerp(grainShd, grainLit, Mathf.Clamp01(lr * 0.5f + 0.5f));
                                lc = (ring < 0.42f) ? Color.Lerp(baseG, grainShd, 0.55f) : baseG;
                                // small bright pith center
                                if (dist < 0.12f) lc = grainLit;
                            }
                            c = lc;
                        }
                    }

                    // tie/rope band across the middle of the bundle
                    if (c.a > 0f && Mathf.Abs(fy - 0.46f) < 0.045f)
                        c = Color.Lerp(c, tie, 0.7f);

                    px[y * s + x] = c;
                }

            _itemWood = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemWood;
        }

        // ---- Planks: a neat stack of horizontal sawn boards ----
        private static Sprite _itemPlanks;
        public static Sprite ItemPlanks()
        {
            if (_itemPlanks != null) return _itemPlanks;
            const int s = 64;
            var px = new Color[s * s];

            Color tan = new Color(0.74f, 0.58f, 0.34f);
            Color tanTop = new Color(0.86f, 0.70f, 0.46f);
            Color tanShd = new Color(0.58f, 0.44f, 0.24f);
            Color grainLine = new Color(0.50f, 0.37f, 0.20f);
            Color gap = new Color(0.30f, 0.21f, 0.12f);

            // 4 boards stacked vertically within fy [0.16 .. 0.86]
            int boards = 4;
            float top = 0.84f, bot = 0.16f;
            float span = top - bot;
            float boardH = span / boards;       // height of one board incl gap
            float gapH = 0.022f;                // shadow gap between boards
            float xL = 0.16f, xR = 0.84f;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    if (fx > xL && fx < xR && fy > bot && fy < top)
                    {
                        // which board (0 = bottom)
                        float rel = (fy - bot) / boardH;
                        int idx = (int)rel;
                        float within = rel - idx;          // 0 bottom .. 1 top of this board
                        float yInBoard = within;            // local 0..1

                        if (yInBoard * boardH < gapH)
                        {
                            // dark shadow gap at bottom of each board (between boards)
                            c = gap;
                        }
                        else
                        {
                            Color baseC = tan;
                            // lighter lit top edge of each board
                            if (yInBoard > 0.78f) baseC = tanTop;
                            else if (yInBoard < 0.22f) baseC = Color.Lerp(tan, tanShd, 0.5f);
                            else baseC = tan;

                            // left edge slightly lit, right edge shadow
                            float edge = (fx - xL) / (xR - xL);
                            baseC = Color.Lerp(baseC, tanShd, Mathf.Clamp01((edge - 0.6f) * 1.2f) * 0.45f);
                            baseC = Color.Lerp(baseC, tanTop, Mathf.Clamp01((0.25f - edge) * 1.2f) * 0.35f);

                            // thin darker horizontal grain lines along the board length
                            float g = Frac(fx * 9f + idx * 2.3f);
                            if (g < 0.10f) baseC = Color.Lerp(baseC, grainLine, 0.55f);
                            // a second finer grain
                            if (Mathf.Abs(yInBoard - 0.5f) < 0.04f) baseC = Color.Lerp(baseC, grainLine, 0.3f);

                            c = baseC;
                        }
                    }

                    px[y * s + x] = c;
                }

            _itemPlanks = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemPlanks;
        }

        // ---- Plant Fiber: a soft green fibrous tuft / cotton boll ----
        private static Sprite _itemFiber;
        public static Sprite ItemFiber()
        {
            if (_itemFiber != null) return _itemFiber;
            const int s = 64;
            var px = new Color[s * s];

            Color fib = new Color(0.62f, 0.74f, 0.45f);
            Color fibLit = new Color(0.76f, 0.86f, 0.58f);
            Color fibShd = new Color(0.46f, 0.58f, 0.32f);
            Color wisp = new Color(0.84f, 0.92f, 0.68f);
            Color stem = new Color(0.40f, 0.34f, 0.20f);
            Color stemShd = new Color(0.30f, 0.25f, 0.14f);

            // fluffy blob = union of several discs, biased upward
            float[] bx = { 0.50f, 0.34f, 0.66f, 0.42f, 0.58f, 0.50f };
            float[] by = { 0.58f, 0.50f, 0.50f, 0.68f, 0.68f, 0.44f };
            float[] br = { 0.20f, 0.16f, 0.16f, 0.15f, 0.15f, 0.17f };

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    bool inside = false;
                    float minEdge = 1f;     // how deep inside (0 = on edge)
                    for (int i = 0; i < bx.Length; i++)
                    {
                        float dx = fx - bx[i], dy = fy - by[i];
                        float d2 = dx * dx + dy * dy;
                        if (d2 <= br[i] * br[i])
                        {
                            inside = true;
                            float e = 1f - Mathf.Sqrt(d2) / br[i];
                            if (e < minEdge) { } // keep deepest later
                            minEdge = Mathf.Max(minEdge < 1f ? minEdge : 0f, e);
                        }
                    }

                    if (inside)
                    {
                        // soft shading: lit upper-left, shadow lower-right
                        float lr = (-(fx - 0.5f) - (fy - 0.55f));
                        Color baseC = Color.Lerp(fibShd, fibLit, Mathf.Clamp01(lr * 1.3f + 0.5f));
                        baseC = Color.Lerp(baseC, fib, 0.35f);

                        // fluffy strand wisps: light speckle
                        float w = Frac((fx * 6f + fy * 7f));
                        if (w < 0.14f && minEdge < 0.45f) baseC = Color.Lerp(baseC, wisp, 0.6f);
                        // darker tufts deep inside for cottony texture
                        float t = Frac((fx * 5f - fy * 6f));
                        if (t < 0.12f) baseC = Color.Lerp(baseC, fibShd, 0.4f);

                        c = baseC;
                    }

                    // small darker stem at the base (center-bottom)
                    if (Mathf.Abs(fx - 0.50f) < 0.045f && fy < 0.40f && fy > 0.16f)
                    {
                        Color sc = (fx > 0.50f) ? stemShd : stem;
                        c = sc;
                    }
                    // a couple of small leaf nubs on the stem
                    if (Disc(fx - 0.44f, fy - 0.30f, 0.05f) && fy < 0.34f)
                        c = Color.Lerp(fibShd, stem, 0.3f);

                    px[y * s + x] = c;
                }

            _itemFiber = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemFiber;
        }
    }
}
