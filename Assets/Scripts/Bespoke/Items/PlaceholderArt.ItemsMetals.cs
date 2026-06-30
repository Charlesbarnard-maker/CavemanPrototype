// Bespoke procedural item art. Partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        // ===== Iron: stack of 2 silvery cool-grey trapezoid ingots =====
        private static Sprite _itemIron;
        public static Sprite ItemIron()
        {
            if (_itemIron != null) return _itemIron;
            const int s = 64;
            var px = new Color[s * s];

            Color frontMid = new Color(0.66f, 0.68f, 0.74f);
            Color frontDk  = new Color(0.50f, 0.52f, 0.58f);
            Color topLit   = new Color(0.82f, 0.84f, 0.88f);
            Color topDk    = new Color(0.70f, 0.72f, 0.78f);
            Color spec     = new Color(0.98f, 0.99f, 1.00f);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // Two stacked trapezoid ingots. Bottom ingot lower, top ingot upper.
                    // Trapezoid: wider at its base, narrower at its top face.
                    for (int ing = 0; ing < 2; ing++)
                    {
                        // bottom ingot: base fy 0.10..0.46 ; top ingot: base fy 0.46..0.82
                        float baseY = ing == 0 ? 0.12f : 0.48f;
                        float topY  = ing == 0 ? 0.44f : 0.80f;
                        float topFace = topY - 0.10f; // boundary between front face and top face
                        if (fy < baseY || fy > topY) continue;

                        // half-width tapers: wide at baseY, narrower at topY
                        float t = (fy - baseY) / (topY - baseY);        // 0 base -> 1 top
                        float halfW = Mathf.Lerp(0.40f, 0.30f, t);
                        float dx = Mathf.Abs(fx - 0.5f);
                        if (dx > halfW) continue;

                        bool onTop = fy >= topFace;
                        if (onTop)
                        {
                            // top face: lighter, lit upper-left
                            float lit = Mathf.Clamp01(0.5f + (0.5f - fx) * 0.6f + (fy - topFace) * 1.2f);
                            c = Color.Lerp(topDk, topLit, lit);
                        }
                        else
                        {
                            // front face: mid, darker toward lower-right
                            float sh = Mathf.Clamp01(0.55f + (0.5f - fx) * 0.7f - (baseY - fy) * 0.5f + (fy - baseY) * 0.4f);
                            c = Color.Lerp(frontDk, frontMid, sh);
                            // bevel highlight along upper edge of front face
                            if (fy > topFace - 0.045f) c = Color.Lerp(c, topLit, 0.4f);
                        }

                        // thin diagonal specular streak across each front face
                        if (!onTop && Mathf.Abs((fx - 0.5f) + (fy - (baseY + 0.10f)) * 1.3f) < 0.035f)
                            c = Color.Lerp(c, spec, 0.85f);
                    }

                    px[y * s + x] = c;
                }

            _itemIron = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemIron;
        }

        // ===== Copper: single shiny orange-copper bar/ingot =====
        private static Sprite _itemCopper;
        public static Sprite ItemCopper()
        {
            if (_itemCopper != null) return _itemCopper;
            const int s = 64;
            var px = new Color[s * s];

            Color baseC = new Color(0.85f, 0.55f, 0.35f);
            Color dkC   = new Color(0.60f, 0.34f, 0.18f);
            Color topC  = new Color(0.95f, 0.70f, 0.48f);
            Color spec  = new Color(1.00f, 0.96f, 0.88f);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // single chunky trapezoid bar, fills the frame
                    float baseY = 0.18f, topY = 0.78f;
                    if (fy >= baseY && fy <= topY)
                    {
                        float t = (fy - baseY) / (topY - baseY);
                        float halfW = Mathf.Lerp(0.42f, 0.30f, t);
                        float dx = Mathf.Abs(fx - 0.5f);
                        if (dx <= halfW)
                        {
                            float topFace = topY - 0.16f;
                            bool onTop = fy >= topFace;
                            if (onTop)
                            {
                                float lit = Mathf.Clamp01(0.45f + (0.5f - fx) * 0.6f + (fy - topFace) * 1.4f);
                                c = Color.Lerp(baseC, topC, lit);
                            }
                            else
                            {
                                float sh = Mathf.Clamp01(0.55f + (0.5f - fx) * 0.7f + (fy - baseY) * 0.3f);
                                c = Color.Lerp(dkC, baseC, sh);
                                if (fy > topFace - 0.05f) c = Color.Lerp(c, topC, 0.45f);
                            }

                            // strong bright specular streak (diagonal) on the front face
                            if (!onTop && Mathf.Abs((fx - 0.42f) + (fy - 0.42f) * 1.1f) < 0.05f)
                                c = Color.Lerp(c, spec, 0.9f);
                            // small glint speck upper-left
                            if (Disc(fx - 0.34f, fy - 0.60f, 0.045f))
                                c = Color.Lerp(c, spec, 0.8f);
                        }
                    }

                    px[y * s + x] = c;
                }

            _itemCopper = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemCopper;
        }

        // ===== Bronze Plate: flat hammered sheet at a slight angle =====
        private static Sprite _itemBronzePlate;
        public static Sprite ItemBronzePlate()
        {
            if (_itemBronzePlate != null) return _itemBronzePlate;
            const int s = 64;
            var px = new Color[s * s];

            Color baseC = new Color(0.78f, 0.58f, 0.32f);
            Color dkC   = new Color(0.55f, 0.40f, 0.20f);
            Color litC  = new Color(0.92f, 0.74f, 0.46f);
            Color bevel = new Color(0.98f, 0.84f, 0.58f);

            // hammer dent centres
            float[] dx = { 0.36f, 0.58f, 0.44f, 0.66f };
            float[] dy = { 0.58f, 0.62f, 0.40f, 0.44f };

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // flat parallelogram sheet, sheared for a slight angle
                    float shear = (fy - 0.5f) * 0.18f;
                    float px0 = fx - shear;
                    if (px0 > 0.16f && px0 < 0.84f && fy > 0.24f && fy < 0.76f)
                    {
                        // base shading: lit upper-left across the broad flat face
                        float lit = Mathf.Clamp01(0.5f + (0.5f - fx) * 0.5f + (fy - 0.5f) * 0.5f);
                        c = Color.Lerp(dkC, baseC, 0.6f + lit * 0.4f);
                        c = Color.Lerp(c, litC, lit * 0.4f);

                        // faint hammered grain
                        if (Frac((px0 + fy) * 9f) < 0.15f) c = Color.Lerp(c, dkC, 0.18f);

                        // round hammer dents: dark Disc with a tiny lit rim
                        for (int d = 0; d < dx.Length; d++)
                        {
                            if (Disc(px0 - dx[d], fy - dy[d], 0.052f))
                                c = Color.Lerp(c, dkC, 0.55f);
                            else if (Disc(px0 - dx[d], fy - dy[d], 0.068f))
                                c = Color.Lerp(c, litC, 0.30f);
                        }

                        // beveled bright edges (top + left)
                        if (fy > 0.715f) c = Color.Lerp(c, bevel, 0.6f);
                        if (px0 < 0.205f) c = Color.Lerp(c, bevel, 0.5f);
                        // darker edges (bottom + right)
                        if (fy < 0.285f) c = Color.Lerp(c, dkC, 0.5f);
                        if (px0 > 0.795f) c = Color.Lerp(c, dkC, 0.45f);
                    }

                    px[y * s + x] = c;
                }

            _itemBronzePlate = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemBronzePlate;
        }

        // ===== Steel: bright blue-grey ingot with strong diagonal sheen =====
        private static Sprite _itemSteel;
        public static Sprite ItemSteel()
        {
            if (_itemSteel != null) return _itemSteel;
            const int s = 64;
            var px = new Color[s * s];

            Color baseC = new Color(0.60f, 0.64f, 0.72f);
            Color dkC   = new Color(0.42f, 0.46f, 0.56f);
            Color topC  = new Color(0.78f, 0.82f, 0.90f);
            Color spec  = new Color(1.00f, 1.00f, 1.00f);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    float baseY = 0.18f, topY = 0.78f;
                    if (fy >= baseY && fy <= topY)
                    {
                        float t = (fy - baseY) / (topY - baseY);
                        float halfW = Mathf.Lerp(0.42f, 0.30f, t);
                        float dxv = Mathf.Abs(fx - 0.5f);
                        if (dxv <= halfW)
                        {
                            float topFace = topY - 0.16f;
                            bool onTop = fy >= topFace;
                            if (onTop)
                            {
                                float lit = Mathf.Clamp01(0.45f + (0.5f - fx) * 0.6f + (fy - topFace) * 1.4f);
                                c = Color.Lerp(baseC, topC, lit);
                            }
                            else
                            {
                                float sh = Mathf.Clamp01(0.55f + (0.5f - fx) * 0.7f + (fy - baseY) * 0.3f);
                                c = Color.Lerp(dkC, baseC, sh);
                                if (fy > topFace - 0.05f) c = Color.Lerp(c, topC, 0.45f);
                            }

                            // strong wide diagonal white specular sheen — shiniest metal
                            float band = Mathf.Abs((fx - 0.5f) + (fy - 0.48f) * 0.9f);
                            if (!onTop && band < 0.09f)
                                c = Color.Lerp(c, spec, Mathf.Clamp01(1f - band / 0.09f) * 0.95f);
                            // secondary thin glint
                            if (!onTop && Mathf.Abs((fx - 0.62f) + (fy - 0.40f) * 0.9f) < 0.025f)
                                c = Color.Lerp(c, spec, 0.7f);
                        }
                    }

                    px[y * s + x] = c;
                }

            _itemSteel = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemSteel;
        }
    }
}
