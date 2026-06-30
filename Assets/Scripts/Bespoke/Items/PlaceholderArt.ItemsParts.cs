// Bespoke procedural item art. Partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        // ===== 1. Tools: crossed hammer + wrench (an X) =====
        private static Sprite _itemTools;
        public static Sprite ItemTools()
        {
            if (_itemTools != null) return _itemTools;
            const int s = 64;
            var px = new Color[s * s];

            // metal tones
            Color metMid = new Color(0.60f, 0.64f, 0.70f);
            Color metLit = new Color(0.78f, 0.82f, 0.88f);
            Color metDrk = new Color(0.42f, 0.46f, 0.52f);
            // handle tones (tan/brown)
            Color hMid = new Color(0.52f, 0.38f, 0.22f);
            Color hLit = new Color(0.66f, 0.50f, 0.32f);
            Color hDrk = new Color(0.38f, 0.26f, 0.14f);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // ----- WRENCH along the diagonal going / (bottom-left to top-right) -----
                    // shaft: distance from line fy = fx (i.e. perpendicular coord d1 = fx - fy)
                    float d1 = fx - fy;          // 0 on the / diagonal
                    float along1 = (fx + fy) * 0.5f; // position along the diagonal
                    // wrench shaft
                    if (Mathf.Abs(d1) < 0.075f && along1 > 0.22f && along1 < 0.78f)
                    {
                        c = (d1 < -0.01f) ? metLit : (d1 > 0.03f ? metDrk : metMid);
                    }
                    // wrench open jaw head at top-right end (around fx~0.80, fy~0.80)
                    {
                        float hx = 0.80f, hy = 0.80f;
                        if (Disc(fx - hx, fy - hy, 0.135f))
                        {
                            c = (fx - hx) + (fy - hy) < -0.04f ? metLit : (((fx - hx) + (fy - hy) > 0.05f) ? metDrk : metMid);
                        }
                        // carve open notch in the jaw (transparent V facing out)
                        if (Disc(fx - (hx + 0.055f), fy - (hy + 0.055f), 0.07f)) c = Clear;
                    }
                    // wrench closed ring head at bottom-left end (fx~0.20, fy~0.20)
                    {
                        float hx = 0.20f, hy = 0.20f;
                        if (Disc(fx - hx, fy - hy, 0.135f))
                            c = (fx - hx) + (fy - hy) < -0.04f ? metLit : (((fx - hx) + (fy - hy) > 0.05f) ? metDrk : metMid);
                        if (Disc(fx - hx, fy - hy, 0.06f)) c = Clear; // ring hole
                    }

                    // ----- HAMMER along the diagonal going \ (top-left to bottom-right) -----
                    float d2 = fx + fy - 1f;     // 0 on the \ diagonal
                    float along2 = (fx - fy) * 0.5f + 0.5f; // 0..1 from top-left to bottom-right
                    // handle (wooden) from middle toward bottom-right
                    if (Mathf.Abs(d2) < 0.06f && along2 > 0.5f && along2 < 0.86f)
                    {
                        c = (d2 < -0.01f) ? hLit : (d2 > 0.02f ? hDrk : hMid);
                    }
                    // hammer head: a chunky block near top-left end (fx~0.22, fy~0.78)
                    {
                        float cx = 0.24f, cy = 0.76f;
                        // rotate-ish box: use bands along the \ perpendicular
                        float u = (fx - cx) + (fy - cy);   // along \ (perp of head length)
                        float v = (fx - cx) - (fy - cy);   // across
                        if (Mathf.Abs(u) < 0.085f && Mathf.Abs(v) < 0.17f)
                        {
                            c = (v < -0.03f) ? metLit : (v > 0.05f ? metDrk : metMid);
                            // claw split on the far side
                            if (v < -0.10f && Mathf.Abs(u) < 0.03f) c = Clear;
                        }
                    }

                    // small glint specks
                    if (c.a > 0f && Frac((fx + fy) * 26f) < 0.06f && fy > 0.6f) c = Color.Lerp(c, Color.white, 0.5f);

                    px[y * s + x] = c;
                }

            _itemTools = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemTools;
        }

        // ===== 2. Bronze Gear: toothed cog =====
        private static Sprite _itemBronzeGear;
        public static Sprite ItemBronzeGear()
        {
            if (_itemBronzeGear != null) return _itemBronzeGear;
            const int s = 64;
            var px = new Color[s * s];

            Color brMid = new Color(0.80f, 0.56f, 0.28f);
            Color brLit = new Color(0.94f, 0.72f, 0.42f);
            Color brDrk = new Color(0.58f, 0.38f, 0.16f);
            Color hole = new Color(0.30f, 0.20f, 0.10f);

            float cx = 0.5f, cy = 0.5f;
            float rBody = 0.34f;    // main disc radius
            float rTooth = 0.45f;   // outer tooth reach

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    float dx = fx - cx, dy = fy - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    bool inGear = false;
                    // main disc body
                    if (dist <= rBody) inGear = true;

                    // 8 rectangular teeth around the rim
                    if (!inGear && dist <= rTooth)
                    {
                        float ang = Mathf.Atan2(dy, dx); // -pi..pi
                        float seg = (ang / 6.2831853f + 1f) * 8f; // 0..8
                        float fr = Frac(seg);
                        // tooth occupies the central ~55% of each segment
                        if (fr > 0.225f && fr < 0.775f) inGear = true;
                    }

                    if (inGear)
                    {
                        // shade by light from upper-left
                        float lit = (-dx - dy);
                        c = lit > 0.06f ? brLit : (lit < -0.06f ? brDrk : brMid);
                        // rim shadow ring
                        if (dist > rBody - 0.04f && dist <= rBody) c = Color.Lerp(c, brDrk, 0.5f);
                    }

                    // central hole
                    if (Disc(dx, dy, 0.10f)) c = hole;
                    if (Disc(dx, dy, 0.13f) && !Disc(dx, dy, 0.10f) && inGear)
                        c = Color.Lerp(c, brDrk, 0.6f); // bevel ring around hole

                    // glint speck upper-left of body
                    if (inGear && Disc(dx + 0.16f, dy + 0.16f, 0.05f)) c = Color.Lerp(c, Color.white, 0.45f);

                    px[y * s + x] = c;
                }

            _itemBronzeGear = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemBronzeGear;
        }

        // ===== 3. Steel Beam: capital-I cross-section girder =====
        private static Sprite _itemSteelBeam;
        public static Sprite ItemSteelBeam()
        {
            if (_itemSteelBeam != null) return _itemSteelBeam;
            const int s = 64;
            var px = new Color[s * s];

            Color stMid = new Color(0.62f, 0.66f, 0.74f);
            Color stLit = new Color(0.82f, 0.86f, 0.94f);
            Color stDrk = new Color(0.42f, 0.46f, 0.54f);
            Color bolt = new Color(0.30f, 0.34f, 0.40f);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    bool topFlange = (fy > 0.74f && fy < 0.88f) && Mathf.Abs(fx - 0.5f) < 0.34f;
                    bool botFlange = (fy > 0.12f && fy < 0.26f) && Mathf.Abs(fx - 0.5f) < 0.34f;
                    bool web = (fy >= 0.26f && fy <= 0.74f) && Mathf.Abs(fx - 0.5f) < 0.12f;

                    if (topFlange || botFlange || web)
                    {
                        // base shade: light favours upper-left
                        c = stMid;
                        if (fx < 0.46f) c = web && !topFlange && !botFlange ? Color.Lerp(stMid, stLit, 0.4f) : stMid;
                        // face shading by horizontal position
                        if (web)
                            c = (fx < 0.5f) ? Color.Lerp(stMid, stLit, 0.35f) : Color.Lerp(stMid, stDrk, 0.35f);
                        else
                            c = (fx < 0.5f) ? Color.Lerp(stMid, stLit, 0.25f) : Color.Lerp(stMid, stDrk, 0.25f);

                        // lit top edge of the top flange
                        if (topFlange && fy > 0.855f) c = stLit;
                        // dark underside of top flange / bottom flange
                        if (topFlange && fy < 0.762f) c = Color.Lerp(c, stDrk, 0.5f);
                        if (botFlange && fy < 0.142f) c = Color.Lerp(c, stDrk, 0.6f);
                        if (botFlange && fy > 0.238f) c = Color.Lerp(c, stLit, 0.25f);
                    }

                    // bolt dots on the flanges
                    if (topFlange)
                    {
                        if (Disc(fx - 0.30f, fy - 0.81f, 0.028f)) c = bolt;
                        if (Disc(fx - 0.70f, fy - 0.81f, 0.028f)) c = bolt;
                    }
                    if (botFlange)
                    {
                        if (Disc(fx - 0.30f, fy - 0.19f, 0.028f)) c = bolt;
                        if (Disc(fx - 0.70f, fy - 0.19f, 0.028f)) c = bolt;
                    }

                    px[y * s + x] = c;
                }

            _itemSteelBeam = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemSteelBeam;
        }

        // ===== 4. Machine Part: piston / cylinder assembly =====
        private static Sprite _itemMachinePart;
        public static Sprite ItemMachinePart()
        {
            if (_itemMachinePart != null) return _itemMachinePart;
            const int s = 64;
            var px = new Color[s * s];

            Color cyMid = new Color(0.50f, 0.58f, 0.66f);
            Color cyLit = new Color(0.70f, 0.78f, 0.86f);
            Color cyDrk = new Color(0.34f, 0.40f, 0.48f);
            Color rodMid = new Color(0.64f, 0.70f, 0.78f);
            Color rodLit = new Color(0.86f, 0.90f, 0.96f);
            Color bolt = new Color(0.28f, 0.32f, 0.40f);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // cylinder body: vertical rounded box fy 0.12..0.66, centred
                    bool body = (fy > 0.12f && fy < 0.66f) && Mathf.Abs(fx - 0.5f) < 0.20f;
                    // round the bottom corners a touch
                    if (body && fy < 0.16f && Mathf.Abs(fx - 0.5f) > 0.16f) body = false;
                    if (body)
                    {
                        // cylindrical shading across x
                        float t = (fx - 0.30f) / 0.40f; // 0..1 left->right
                        c = t < 0.42f ? Color.Lerp(cyLit, cyMid, t / 0.42f)
                                      : Color.Lerp(cyMid, cyDrk, (t - 0.42f) / 0.58f);
                        // horizontal banding rings on the cylinder
                        if (Frac(fy * 9f) < 0.12f) c = Color.Lerp(c, cyDrk, 0.4f);
                    }

                    // top cap (wider flange) fy 0.62..0.70
                    bool cap = (fy >= 0.62f && fy < 0.72f) && Mathf.Abs(fx - 0.5f) < 0.255f;
                    if (cap)
                    {
                        c = (fx < 0.5f) ? Color.Lerp(cyMid, cyLit, 0.4f) : Color.Lerp(cyMid, cyDrk, 0.4f);
                        if (fy > 0.69f) c = cyLit; // lit top edge of cap
                    }

                    // piston rod sticking out the top: narrow vertical bar fy 0.70..0.93
                    bool rod = (fy >= 0.70f && fy < 0.93f) && Mathf.Abs(fx - 0.5f) < 0.065f;
                    if (rod)
                    {
                        c = (fx < 0.5f) ? rodLit : rodMid;
                        if (fy > 0.90f) c = rodLit; // bright top
                    }

                    // bolt heads on the cap corners
                    if (Disc(fx - 0.33f, fy - 0.665f, 0.032f)) c = bolt;
                    if (Disc(fx - 0.67f, fy - 0.665f, 0.032f)) c = bolt;
                    // bolt on lower body
                    if (Disc(fx - 0.5f, fy - 0.22f, 0.035f)) c = bolt;

                    // glint on rod
                    if (rod && Disc(fx - 0.475f, fy - 0.82f, 0.02f)) c = Color.white;

                    px[y * s + x] = c;
                }

            _itemMachinePart = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemMachinePart;
        }

        // ===== 5. Engine: boxy motor block w/ cylinder humps + exhaust =====
        private static Sprite _itemEngine;
        public static Sprite ItemEngine()
        {
            if (_itemEngine != null) return _itemEngine;
            const int s = 64;
            var px = new Color[s * s];

            Color blMid = new Color(0.36f, 0.42f, 0.50f);
            Color blLit = new Color(0.52f, 0.58f, 0.66f);
            Color blDrk = new Color(0.24f, 0.28f, 0.36f);
            Color humpMid = new Color(0.44f, 0.50f, 0.58f);
            Color humpLit = new Color(0.62f, 0.68f, 0.76f);
            Color exMid = new Color(0.48f, 0.42f, 0.36f);
            Color exDrk = new Color(0.30f, 0.26f, 0.22f);
            Color bolt = new Color(0.18f, 0.20f, 0.26f);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // main block: wide box fy 0.14..0.60, fx 0.16..0.80
                    bool block = (fy > 0.14f && fy < 0.60f) && (fx > 0.16f && fx < 0.80f);
                    if (block)
                    {
                        // shading: lit upper-left, dark lower-right
                        float lit = (fx - 0.5f) * -0.5f + (fy - 0.37f) * 1.0f;
                        c = lit > 0.05f ? blLit : (lit < -0.10f ? blDrk : blMid);
                        // cooling fin lines (vertical grooves)
                        if (Frac(fx * 16f) < 0.18f) c = Color.Lerp(c, blDrk, 0.45f);
                    }

                    // three cylinder humps on top (rounded caps along fy~0.60..0.74)
                    float[] hxs = { 0.30f, 0.50f, 0.70f };
                    for (int i = 0; i < 3; i++)
                    {
                        float hx = hxs[i], hy = 0.60f;
                        if (Disc(fx - hx, (fy - hy) * 1.3f, 0.115f) && fy >= 0.58f)
                        {
                            float l = (-(fx - hx) - (fy - hy) * 0.6f);
                            c = l > 0.03f ? humpLit : (l < -0.04f ? Color.Lerp(humpMid, blDrk, 0.4f) : humpMid);
                            // tiny spark-plug nub on top centre of each hump
                            if (Disc(fx - hx, fy - 0.72f, 0.022f)) c = bolt;
                        }
                    }

                    // exhaust pipe out the right side: horizontal stub fy 0.30..0.42, fx 0.78..0.94
                    bool exhaust = (fy > 0.30f && fy < 0.42f) && (fx > 0.78f && fx < 0.95f);
                    if (exhaust)
                    {
                        c = (fy > 0.37f) ? Color.Lerp(exMid, Color.white, 0.25f) : (fy < 0.33f ? exDrk : exMid);
                        // open pipe end
                        if (fx > 0.91f && Mathf.Abs(fy - 0.36f) < 0.025f) c = Color.black;
                    }

                    // bolt dots on the block face
                    if (Disc(fx - 0.22f, fy - 0.20f, 0.028f)) c = bolt;
                    if (Disc(fx - 0.74f, fy - 0.20f, 0.028f)) c = bolt;
                    if (Disc(fx - 0.22f, fy - 0.54f, 0.026f)) c = bolt;

                    px[y * s + x] = c;
                }

            _itemEngine = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemEngine;
        }

        // ===== 6. Bricks: stacked red-clay bricks in offset courses =====
        private static Sprite _itemBricks;
        public static Sprite ItemBricks()
        {
            if (_itemBricks != null) return _itemBricks;
            const int s = 64;
            var px = new Color[s * s];

            Color brMid = new Color(0.74f, 0.40f, 0.32f);
            Color brLit = new Color(0.88f, 0.54f, 0.44f);
            Color brDrk = new Color(0.56f, 0.28f, 0.22f);
            Color mortar = new Color(0.80f, 0.76f, 0.70f);

            // the brick stack region
            float xL = 0.14f, xR = 0.86f, yB = 0.18f, yT = 0.82f;
            int courses = 4;
            float courseH = (yT - yB) / courses;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    if (fx > xL && fx < xR && fy > yB && fy < yT)
                    {
                        // which course (row), 0 at bottom
                        float yc = (fy - yB) / courseH;
                        int row = (int)yc;
                        float yIn = Frac(yc); // 0..1 within course

                        // horizontal offset: alternate rows shift by half a brick
                        float offset = (row % 2 == 0) ? 0f : 0.5f;
                        float brickW = (xR - xL) / 2.6f; // ~2.6 bricks per course visible
                        float xpos = (fx - xL) / brickW + offset;
                        float xIn = Frac(xpos);

                        bool mortarLine = (yIn < 0.12f) || (xIn < 0.07f) || (xIn > 0.93f);

                        if (mortarLine)
                        {
                            c = mortar;
                            // slight shading on mortar
                            if (yIn < 0.06f) c = Color.Lerp(mortar, brDrk, 0.3f);
                        }
                        else
                        {
                            // brick face: lit upper-left within each brick, plus per-brick variation
                            float vary = Frac((row * 3 + (int)Mathf.Floor(xpos)) * 0.37f);
                            Color baseC = Color.Lerp(brMid, brDrk, vary * 0.4f);
                            c = baseC;
                            if (yIn > 0.55f && xIn < 0.45f) c = Color.Lerp(baseC, brLit, 0.45f); // lit
                            if (yIn < 0.30f || xIn > 0.7f) c = Color.Lerp(baseC, brDrk, 0.35f);  // shadow
                            // grain speckle
                            if (Frac((fx + fy) * 40f) < 0.08f) c = Color.Lerp(c, brDrk, 0.3f);
                            // top course slightly brighter overall
                            if (row == courses - 1) c = Color.Lerp(c, brLit, 0.18f);
                        }
                    }

                    px[y * s + x] = c;
                }

            _itemBricks = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemBricks;
        }

        // ===== 7. Stone Block: dressed ashlar cube =====
        private static Sprite _itemStoneBlock;
        public static Sprite ItemStoneBlock()
        {
            if (_itemStoneBlock != null) return _itemStoneBlock;
            const int s = 64;
            var px = new Color[s * s];

            Color stMid = new Color(0.58f, 0.60f, 0.66f);
            Color stLit = new Color(0.74f, 0.76f, 0.82f);
            Color stTop = new Color(0.82f, 0.84f, 0.90f);
            Color stDrk = new Color(0.42f, 0.44f, 0.50f);
            Color bevel = new Color(0.90f, 0.92f, 0.96f);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // Isometric-ish cube: a top parallelogram + a front face.
                    // Front face: box fx 0.18..0.82, fy 0.16..0.62
                    bool front = (fx > 0.18f && fx < 0.82f) && (fy > 0.16f && fy < 0.62f);
                    // Top face: a slab fy 0.62..0.84 that we skew so it reads as a lit top.
                    // approximate skew by shrinking width with height
                    bool top = false;
                    if (fy >= 0.62f && fy < 0.84f)
                    {
                        float t = (fy - 0.62f) / 0.22f; // 0 at front edge, 1 at back
                        float inset = 0.06f * t;        // back edge slightly narrower -> depth
                        float shift = 0.05f * t;         // shift right for iso feel
                        if (fx > 0.18f + inset + shift && fx < 0.82f - inset + shift) top = true;
                    }

                    if (front)
                    {
                        // front face shading: lit upper-left, darker lower-right
                        float l = (-(fx - 0.5f) - (0.39f - fy)) ;
                        c = stMid;
                        if (fx < 0.45f) c = Color.Lerp(stMid, stLit, 0.30f);
                        if (fx > 0.62f || fy < 0.28f) c = Color.Lerp(stMid, stDrk, 0.35f);
                        // subtle stone grain
                        if (Frac((fx * 7f + fy * 5f)) < 0.07f) c = Color.Lerp(c, stDrk, 0.25f);
                    }
                    if (top)
                    {
                        c = stTop;
                        // grain on top, lighter
                        if (Frac((fx * 6f + fy * 9f)) < 0.07f) c = Color.Lerp(c, stMid, 0.3f);
                    }

                    // beveled bright edges
                    // top edge of top face
                    if (top && fy > 0.815f) c = bevel;
                    // front-top seam (where top meets front)
                    if (fy >= 0.60f && fy < 0.64f && fx > 0.18f && fx < 0.82f) c = bevel;
                    // left edge of front
                    if (front && fx < 0.205f) c = bevel;
                    // right + bottom darker edges
                    if (front && fx > 0.80f) c = stDrk;
                    if (front && fy < 0.185f) c = stDrk;

                    px[y * s + x] = c;
                }

            _itemStoneBlock = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemStoneBlock;
        }
    }
}
