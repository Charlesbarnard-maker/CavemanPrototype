// Bespoke procedural item art. Partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        // ===== Idea Tablet: tan clay/stone tablet with an engraved lightbulb glyph =====
        private static Sprite _itemIdeaTablet;
        public static Sprite ItemIdeaTablet()
        {
            if (_itemIdeaTablet != null) return _itemIdeaTablet;
            const int s = 64;
            var px = new Color[s * s];

            Color baseTan = new Color(0.86f, 0.80f, 0.50f);
            Color litTan = new Color(0.95f, 0.90f, 0.63f);
            Color darkTan = new Color(0.68f, 0.61f, 0.36f);
            Color edgeTan = new Color(0.57f, 0.50f, 0.28f);
            Color engrave = new Color(0.34f, 0.28f, 0.15f);

            // tablet body extents
            float left = 0.20f, right = 0.80f, bot = 0.14f, top = 0.86f;
            float corner = 0.10f;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    bool inBox = fx > left && fx < right && fy > bot && fy < top;
                    // round the corners
                    bool cornerCut = false;
                    if (inBox)
                    {
                        float cx = 0f, cy = 0f; bool nearCorner = false;
                        if (fx < left + corner && fy < bot + corner) { cx = left + corner; cy = bot + corner; nearCorner = true; }
                        else if (fx > right - corner && fy < bot + corner) { cx = right - corner; cy = bot + corner; nearCorner = true; }
                        else if (fx < left + corner && fy > top - corner) { cx = left + corner; cy = top - corner; nearCorner = true; }
                        else if (fx > right - corner && fy > top - corner) { cx = right - corner; cy = top - corner; nearCorner = true; }
                        if (nearCorner && !Disc(fx - cx, fy - cy, corner)) cornerCut = true;
                    }

                    if (inBox && !cornerCut)
                    {
                        // base shading: lit upper-left -> dark lower-right
                        float diag = Mathf.Clamp01((fx - left) / (right - left) * 0.5f + (top - fy) / (top - bot) * 0.5f);
                        c = Color.Lerp(darkTan, litTan, diag);
                        c = Color.Lerp(c, baseTan, 0.35f);

                        // bevel rim
                        float rim = 0.045f;
                        if (fx < left + rim || fy < bot + rim) c = Color.Lerp(c, edgeTan, 0.6f);
                        if (fx > right - rim || fy > top - rim) c = Color.Lerp(c, litTan, 0.35f);

                        // grain speckle
                        if (Frac((fx * 13.0f + fy * 7.0f)) < 0.06f) c = Color.Lerp(c, darkTan, 0.4f);

                        // ---- engraved lightbulb glyph ----
                        float gx = 0.50f, gby = 0.56f; // bulb center
                        float bulbR = 0.135f;
                        float dB = Mathf.Sqrt((fx - gx) * (fx - gx) + (fy - gby) * (fy - gby));
                        bool bulbRing = dB < bulbR && dB > bulbR - 0.040f;

                        // filament: a small inner squiggle
                        bool filament = (Mathf.Abs(fx - gx) < 0.045f && fy > gby - 0.02f && fy < gby + 0.07f)
                                        || (fy > gby + 0.05f && fy < gby + 0.075f && Mathf.Abs(fx - gx) < 0.05f);

                        // base/screw of bulb (below the circle)
                        bool screw = Mathf.Abs(fx - gx) < 0.075f && fy < gby - bulbR + 0.05f && fy > gby - bulbR - 0.075f;
                        bool screwLines = screw && Frac(fy * 36.0f) < 0.5f;

                        // rays around bulb top
                        bool ray = false;
                        if (fy > gby + bulbR - 0.01f)
                        {
                            if (Mathf.Abs(fx - gx) < 0.018f && fy < gby + bulbR + 0.07f) ray = true;
                            float rdx = fx - gx, rdy = fy - (gby + bulbR);
                            if (Mathf.Abs(rdx - rdy) < 0.02f && rdx > 0.03f && rdx < 0.09f) ray = true;
                            if (Mathf.Abs(rdx + rdy) < 0.02f && rdx < -0.03f && rdx > -0.09f) ray = true;
                        }

                        if (bulbRing || filament || screwLines || ray)
                            c = Color.Lerp(c, engrave, 0.85f);
                    }

                    px[y * s + x] = c;
                }

            _itemIdeaTablet = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemIdeaTablet;
        }

        // ===== Study Scroll: rolled cream parchment, horizontal cylinder with spiral ends =====
        private static Sprite _itemStudyScroll;
        public static Sprite ItemStudyScroll()
        {
            if (_itemStudyScroll != null) return _itemStudyScroll;
            const int s = 64;
            var px = new Color[s * s];

            Color baseCream = new Color(0.92f, 0.86f, 0.60f);
            Color litCream = new Color(0.99f, 0.95f, 0.74f);
            Color darkCream = new Color(0.74f, 0.67f, 0.42f);
            Color rollEnd = new Color(0.66f, 0.58f, 0.34f);
            Color rollDark = new Color(0.50f, 0.43f, 0.23f);
            Color ink = new Color(0.42f, 0.34f, 0.20f);

            float midY = 0.50f;
            float halfH = 0.18f;          // cylinder half-height
            float left = 0.12f, right = 0.88f;
            float endW = 0.16f;           // width of the rolled end caps

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    float dy = fy - midY;
                    bool inCyl = fx > left && fx < right && Mathf.Abs(dy) < halfH;

                    if (inCyl)
                    {
                        // cylindrical vertical shading (lit just above middle, dark at edges)
                        float t = Mathf.Clamp01((dy + halfH) / (2f * halfH)); // 0 bottom .. 1 top
                        // light band near upper third
                        float shade = 1f - Mathf.Abs(dy + 0.03f) / halfH;     // peak slightly above center
                        shade = Mathf.Clamp01(shade);
                        c = Color.Lerp(darkCream, litCream, shade);
                        c = Color.Lerp(c, baseCream, 0.25f);

                        bool isLeftEnd = fx < left + endW;
                        bool isRightEnd = fx > right - endW;

                        if (isLeftEnd || isRightEnd)
                        {
                            // rolled spiral end caps
                            float ecx = isLeftEnd ? (left + endW * 0.5f) : (right - endW * 0.5f);
                            float ddx = (fx - ecx) / (endW * 0.5f);
                            float ddy = dy / halfH;
                            float rr = Mathf.Sqrt(ddx * ddx + ddy * ddy);
                            c = Color.Lerp(rollEnd, baseCream, Mathf.Clamp01(1f - rr * 0.4f));
                            // concentric spiral rings
                            if (Frac(rr * 3.2f) < 0.34f) c = Color.Lerp(c, rollDark, 0.65f);
                            // center hub
                            if (rr < 0.30f) c = Color.Lerp(c, rollDark, 0.5f);
                            // outer edge shadow on lower side
                            if (ddy > 0.6f) c = Color.Lerp(c, rollDark, 0.3f);
                        }
                        else
                        {
                            // middle sheet: faint lines of writing
                            float bandTop = midY + 0.10f, bandBot = midY - 0.10f;
                            if (fy < bandTop && fy > bandBot)
                            {
                                // three writing lines
                                float lineWave = Frac(fy * 12.0f);
                                bool onLine = lineWave < 0.16f;
                                // break the ink into dashes to read as text
                                bool dash = Frac(fx * 22.0f) < 0.62f;
                                if (onLine && dash && fx > left + endW + 0.02f && fx < right - endW - 0.02f)
                                    c = Color.Lerp(c, ink, 0.55f);
                            }
                            // top & bottom rim shading of the cylinder
                            if (Mathf.Abs(dy) > halfH - 0.03f) c = Color.Lerp(c, darkCream, 0.55f);
                            if (dy < 0.02f && dy > -0.04f) c = Color.Lerp(c, litCream, 0.25f);
                        }
                    }

                    px[y * s + x] = c;
                }

            _itemStudyScroll = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemStudyScroll;
        }

        // ===== Schematic: blue technical sheet with white grid + a circle-and-lines shape =====
        private static Sprite _itemSchematic;
        public static Sprite ItemSchematic()
        {
            if (_itemSchematic != null) return _itemSchematic;
            const int s = 64;
            var px = new Color[s * s];

            Color baseBlue = new Color(0.40f, 0.60f, 0.82f);
            Color litBlue = new Color(0.52f, 0.71f, 0.92f);
            Color darkBlue = new Color(0.28f, 0.45f, 0.66f);
            Color edgeBlue = new Color(0.22f, 0.37f, 0.56f);
            Color gridWhite = new Color(0.80f, 0.90f, 1.00f);
            Color drawWhite = new Color(0.96f, 0.99f, 1.00f);

            float left = 0.16f, right = 0.84f, bot = 0.14f, top = 0.86f;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    bool inBox = fx > left && fx < right && fy > bot && fy < top;
                    if (inBox)
                    {
                        // base shading lit upper-left
                        float diag = Mathf.Clamp01((fx - left) / (right - left) * 0.5f + (top - fy) / (top - bot) * 0.5f);
                        c = Color.Lerp(darkBlue, litBlue, diag);
                        c = Color.Lerp(c, baseBlue, 0.4f);

                        // grid lines (evenly spaced via Frac)
                        float gridN = 7.0f;
                        bool vLine = Frac(fx * gridN) < 0.085f;
                        bool hLine = Frac(fy * gridN) < 0.085f;
                        if (vLine || hLine) c = Color.Lerp(c, gridWhite, 0.45f);

                        // inner border frame
                        float bf = 0.05f;
                        bool frame = (fx < left + bf || fx > right - bf || fy < bot + bf || fy > top - bf)
                                     && fx > left + 0.02f && fx < right - 0.02f && fy > bot + 0.02f && fy < top - 0.02f;
                        if (frame) c = Color.Lerp(c, drawWhite, 0.55f);

                        // ---- technical shape: a circle + crosshair lines ----
                        float scx = 0.50f, scy = 0.52f, rad = 0.16f;
                        float dC = Mathf.Sqrt((fx - scx) * (fx - scx) + (fy - scy) * (fy - scy));
                        bool circRing = dC < rad && dC > rad - 0.030f;
                        // crosshair / center lines extending past the circle
                        bool hCross = Mathf.Abs(fy - scy) < 0.014f && Mathf.Abs(fx - scx) < rad + 0.10f;
                        bool vCross = Mathf.Abs(fx - scx) < 0.014f && Mathf.Abs(fy - scy) < rad + 0.10f;
                        // a small inner hub circle
                        bool hub = dC < 0.045f;
                        // a dimension line near the bottom
                        bool dimLine = Mathf.Abs(fy - (bot + 0.07f)) < 0.012f && fx > left + 0.10f && fx < right - 0.10f;
                        bool dimTickL = Mathf.Abs(fx - (left + 0.10f)) < 0.012f && Mathf.Abs(fy - (bot + 0.07f)) < 0.035f;
                        bool dimTickR = Mathf.Abs(fx - (right - 0.10f)) < 0.012f && Mathf.Abs(fy - (bot + 0.07f)) < 0.035f;

                        if (circRing || hCross || vCross || hub || dimLine || dimTickL || dimTickR)
                            c = Color.Lerp(c, drawWhite, 0.85f);

                        // sheet edge bevel
                        float rim = 0.03f;
                        if (fx < left + rim || fy > top - rim) c = Color.Lerp(c, litBlue, 0.3f);
                        if (fx > right - rim || fy < bot + rim) c = Color.Lerp(c, edgeBlue, 0.5f);
                    }

                    px[y * s + x] = c;
                }

            _itemSchematic = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemSchematic;
        }

        // ===== Blueprint: darker blue partially-unrolled sheet with curled ends + building plan =====
        private static Sprite _itemBlueprint;
        public static Sprite ItemBlueprint()
        {
            if (_itemBlueprint != null) return _itemBlueprint;
            const int s = 64;
            var px = new Color[s * s];

            Color baseBlue = new Color(0.30f, 0.50f, 0.82f);
            Color litBlue = new Color(0.42f, 0.62f, 0.92f);
            Color darkBlue = new Color(0.18f, 0.33f, 0.60f);
            Color curlBlue = new Color(0.14f, 0.27f, 0.50f);
            Color curlLit = new Color(0.46f, 0.66f, 0.95f);
            Color planWhite = new Color(0.95f, 0.98f, 1.00f);

            float midY = 0.50f, halfH = 0.30f;
            float left = 0.10f, right = 0.90f;
            float curlW = 0.15f;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    float dy = fy - midY;
                    bool inSheet = fx > left && fx < right && Mathf.Abs(dy) < halfH;

                    if (inSheet)
                    {
                        bool leftCurl = fx < left + curlW;
                        bool rightCurl = fx > right - curlW;

                        if (leftCurl || rightCurl)
                        {
                            // curled rolled ends — a tube cross-section
                            float ccx = leftCurl ? (left + curlW * 0.5f) : (right - curlW * 0.5f);
                            float ddx = (fx - ccx) / (curlW * 0.5f);
                            float ddy = dy / halfH;
                            float rr = Mathf.Sqrt(ddx * ddx + ddy * ddy);
                            if (rr < 1.0f)
                            {
                                // round tube shading: lit top, dark bottom
                                float lit = Mathf.Clamp01(0.5f - ddy);
                                c = Color.Lerp(curlBlue, curlLit, lit);
                                // inner hole of the roll
                                if (rr < 0.34f) c = Color.Lerp(c, darkBlue, 0.7f);
                                if (rr < 0.16f) c = Color.Lerp(c, curlBlue, 0.6f);
                                // rim highlight
                                if (rr > 0.82f && ddy < 0f) c = Color.Lerp(c, curlLit, 0.4f);
                            }
                            else
                            {
                                c = Clear; // outside the round end
                            }
                        }
                        else
                        {
                            // flat unrolled middle sheet
                            float shade = Mathf.Clamp01(0.55f - dy * 0.8f + (fx - 0.5f) * 0.2f);
                            c = Color.Lerp(darkBlue, litBlue, shade);
                            c = Color.Lerp(c, baseBlue, 0.4f);

                            // top & bottom edge shading of the sheet
                            if (dy > halfH - 0.035f) c = Color.Lerp(c, darkBlue, 0.5f);
                            if (dy < -halfH + 0.035f) c = Color.Lerp(c, darkBlue, 0.45f);
                            if (dy > -0.02f && dy < 0.04f) c = Color.Lerp(c, litBlue, 0.2f);

                            // ---- white building / floor-plan outline ----
                            float pl = left + curlW + 0.04f, pr = right - curlW - 0.04f;
                            float ptop = midY + 0.16f, pbot = midY - 0.16f;
                            // outer rectangle of the building
                            bool rectOutline =
                                ((Mathf.Abs(fx - pl) < 0.016f || Mathf.Abs(fx - pr) < 0.016f) && fy > pbot && fy < ptop) ||
                                ((Mathf.Abs(fy - ptop) < 0.016f || Mathf.Abs(fy - pbot) < 0.016f) && fx > pl && fx < pr);
                            // a roof / diagonal across the top
                            float roofMid = (pl + pr) * 0.5f;
                            bool roof = (Mathf.Abs((fy - ptop) - (fx - pl) * 0.45f) < 0.016f && fx < roofMid && fy > ptop - 0.02f)
                                      || (Mathf.Abs((fy - ptop) - (pr - fx) * 0.45f) < 0.016f && fx > roofMid && fy > ptop - 0.02f);
                            // an interior dividing wall
                            bool wall = Mathf.Abs(fx - roofMid) < 0.014f && fy > pbot && fy < midY + 0.04f;
                            // a door gap indicator (small) on bottom
                            bool door = Mathf.Abs(fy - pbot) < 0.05f && Mathf.Abs(fx - (pl + 0.10f)) < 0.013f;

                            if (rectOutline || roof || wall || door)
                                c = Color.Lerp(c, planWhite, 0.85f);
                        }
                    }

                    px[y * s + x] = c;
                }

            _itemBlueprint = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemBlueprint;
        }
    }
}
