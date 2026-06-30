// Bespoke procedural item art. Partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        // ============================================================
        // Water — a classic blue teardrop droplet with a white glint.
        // ============================================================
        private static Sprite _itemWater;
        public static Sprite ItemWater()
        {
            if (_itemWater != null) return _itemWater;
            const int s = 64;
            var px = new Color[s * s];

            Color baseBlue = new Color(0.30f, 0.55f, 0.85f);
            Color litBlue = new Color(0.55f, 0.78f, 0.98f);
            Color darkBlue = new Color(0.16f, 0.34f, 0.62f);

            // Teardrop: round bottom bulb + a tapering point at the top.
            float cx = 0.5f, cyBulb = 0.36f, rBulb = 0.30f;
            float tipY = 0.92f; // top point

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    bool inBulb = Disc(fx - cx, fy - cyBulb, rBulb);

                    // Tapering cone above the bulb that meets at the tip.
                    bool inCone = false;
                    if (fy >= cyBulb && fy <= tipY)
                    {
                        float t = (fy - cyBulb) / (tipY - cyBulb);     // 0 at bulb center -> 1 at tip
                        float halfW = Mathf.Lerp(rBulb, 0f, t);         // shrink to a point
                        // ease the widest part to blend with the circle
                        halfW *= Mathf.Lerp(1f, 0.9f, t);
                        if (Mathf.Abs(fx - cx) < halfW) inCone = true;
                    }

                    if (inBulb || inCone)
                    {
                        // Directional shading: lighter upper-left, darker lower-right.
                        float diag = ((fx) + (1f - fy)) * 0.5f; // upper-left bright
                        c = Color.Lerp(darkBlue, litBlue, Mathf.Clamp01(diag * 1.15f));
                        c = Color.Lerp(c, baseBlue, 0.35f);

                        // Rounded rim shadow on the lower-right edge of the bulb.
                        float edge = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cyBulb) * (fy - cyBulb));
                        if (edge > rBulb * 0.78f && fy < cyBulb + 0.1f && fx > cx)
                            c = Color.Lerp(c, darkBlue, 0.4f);

                        // Bright white glint highlight in upper-left of bulb.
                        if (Disc(fx - 0.40f, fy - 0.30f, 0.085f))
                            c = Color.Lerp(c, Color.white, 0.85f);
                        // small secondary sparkle
                        if (Disc(fx - 0.34f, fy - 0.42f, 0.035f))
                            c = Color.Lerp(c, Color.white, 0.7f);
                    }

                    px[y * s + x] = c;
                }

            _itemWater = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemWater;
        }

        // ============================================================
        // Oil — a dark, glossy near-black-purple blob with iridescence.
        // ============================================================
        private static Sprite _itemOil;
        public static Sprite ItemOil()
        {
            if (_itemOil != null) return _itemOil;
            const int s = 64;
            var px = new Color[s * s];

            Color baseOil = new Color(0.20f, 0.16f, 0.24f);
            Color litOil = new Color(0.34f, 0.28f, 0.40f);
            Color darkOil = new Color(0.09f, 0.07f, 0.12f);
            Color sheenMag = new Color(0.62f, 0.30f, 0.66f);
            Color sheenTeal = new Color(0.28f, 0.62f, 0.62f);

            // A fat, slightly squashed droplet/blob — distinct rounder silhouette from Water.
            float cx = 0.5f, cy = 0.42f, rMain = 0.33f;
            float tipY = 0.86f;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // squashed main body (wider than tall)
                    float dxe = (fx - cx) / 1.05f;
                    float dye = (fy - cy) / 0.92f;
                    bool inBody = (dxe * dxe + dye * dye) <= rMain * rMain;

                    bool inCone = false;
                    if (fy >= cy && fy <= tipY)
                    {
                        float t = (fy - cy) / (tipY - cy);
                        float halfW = Mathf.Lerp(rMain * 1.0f, 0f, t) * Mathf.Lerp(1f, 0.85f, t);
                        if (Mathf.Abs(fx - cx) < halfW) inCone = true;
                    }

                    if (inBody || inCone)
                    {
                        float diag = ((fx) + (1f - fy)) * 0.5f;
                        c = Color.Lerp(darkOil, litOil, Mathf.Clamp01(diag * 1.05f));
                        c = Color.Lerp(c, baseOil, 0.4f);

                        // Lower-right deep shadow.
                        if (fx > cx && fy < cy + 0.05f)
                        {
                            float er = Mathf.Sqrt(dxe * dxe + dye * dye);
                            if (er > rMain * 0.7f) c = Color.Lerp(c, darkOil, 0.55f);
                        }

                        // Iridescent sheen band — magenta arc upper-left.
                        if (Disc(fx - 0.40f, fy - 0.50f, 0.13f) && !Disc(fx - 0.40f, fy - 0.50f, 0.09f))
                            c = Color.Lerp(c, sheenMag, 0.55f);
                        // teal sheen lower-left
                        if (Disc(fx - 0.36f, fy - 0.33f, 0.10f) && !Disc(fx - 0.36f, fy - 0.33f, 0.065f))
                            c = Color.Lerp(c, sheenTeal, 0.5f);

                        // Glossy white glint.
                        if (Disc(fx - 0.42f, fy - 0.55f, 0.06f))
                            c = Color.Lerp(c, Color.white, 0.9f);
                        if (Disc(fx - 0.37f, fy - 0.62f, 0.025f))
                            c = Color.Lerp(c, Color.white, 0.7f);
                    }

                    px[y * s + x] = c;
                }

            _itemOil = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemOil;
        }

        // ============================================================
        // Fuel — an amber jerry-can / canister with cap and spout.
        // ============================================================
        private static Sprite _itemFuel;
        public static Sprite ItemFuel()
        {
            if (_itemFuel != null) return _itemFuel;
            const int s = 64;
            var px = new Color[s * s];

            Color baseAmber = new Color(0.88f, 0.66f, 0.26f);
            Color litAmber = new Color(0.98f, 0.82f, 0.45f);
            Color darkAmber = new Color(0.60f, 0.42f, 0.13f);
            Color metalCap = new Color(0.45f, 0.40f, 0.36f);
            Color metalCapLit = new Color(0.68f, 0.63f, 0.58f);

            // Body bounds (rounded rectangular can).
            float bx0 = 0.24f, bx1 = 0.76f, by0 = 0.10f, by1 = 0.74f;
            float corner = 0.07f;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // --- rounded-rect body ---
                    bool inRect = fx > bx0 && fx < bx1 && fy > by0 && fy < by1;
                    if (inRect)
                    {
                        // knock out the four corners with discs for rounding
                        bool cut = false;
                        if (fx < bx0 + corner && fy < by0 + corner && !Disc(fx - (bx0 + corner), fy - (by0 + corner), corner)) cut = true;
                        if (fx > bx1 - corner && fy < by0 + corner && !Disc(fx - (bx1 - corner), fy - (by0 + corner), corner)) cut = true;
                        if (fx < bx0 + corner && fy > by1 - corner && !Disc(fx - (bx0 + corner), fy - (by1 - corner), corner)) cut = true;
                        if (fx > bx1 - corner && fy > by1 - corner && !Disc(fx - (bx1 - corner), fy - (by1 - corner), corner)) cut = true;
                        if (cut) inRect = false;
                    }

                    if (inRect)
                    {
                        // base body colour with upper-left light.
                        float diag = ((fx) + (1f - fy)) * 0.5f;
                        c = Color.Lerp(darkAmber, litAmber, Mathf.Clamp01(diag));
                        c = Color.Lerp(c, baseAmber, 0.4f);

                        // Darker right-side panel for shading (jerry-can side face).
                        if (fx > 0.58f)
                            c = Color.Lerp(c, darkAmber, Mathf.Clamp01((fx - 0.58f) * 3.2f) * 0.55f);

                        // Embossed central X-ridge panel (typical jerry-can): darker recessed band.
                        float pinch = Mathf.Abs(fx - 0.5f);
                        if (pinch < 0.04f && fy > 0.18f && fy < 0.66f)
                            c = Color.Lerp(c, darkAmber, 0.35f);

                        // Bright vertical highlight strip on the left lit face.
                        if (fx > 0.30f && fx < 0.36f && fy > 0.16f && fy < 0.68f)
                            c = Color.Lerp(c, litAmber, 0.6f);
                        // small glint speck
                        if (Disc(fx - 0.33f, fy - 0.58f, 0.03f))
                            c = Color.Lerp(c, Color.white, 0.55f);
                    }

                    // --- cap on top (left of center) ---
                    bool inCap = fx > 0.30f && fx < 0.50f && fy > 0.74f && fy < 0.84f;
                    if (inCap)
                    {
                        float capDiag = ((fx) + (1f - fy)) * 0.5f;
                        c = Color.Lerp(metalCap, metalCapLit, Mathf.Clamp01(capDiag * 1.2f));
                        if (fy > 0.81f) c = Color.Lerp(c, metalCap, 0.5f); // top rim shadow
                    }

                    // --- short spout on top (right of center), angled up-right ---
                    bool inSpout = false;
                    if (fy > 0.74f && fy < 0.90f && fx > 0.54f && fx < 0.74f)
                    {
                        // diagonal-ish stubby spout: width that holds across the height
                        float t = (fy - 0.74f) / (0.90f - 0.74f);
                        float sxc = Mathf.Lerp(0.60f, 0.66f, t);
                        if (Mathf.Abs(fx - sxc) < 0.05f) inSpout = true;
                    }
                    if (inSpout)
                    {
                        float spDiag = ((fx) + (1f - fy)) * 0.5f;
                        c = Color.Lerp(darkAmber, litAmber, Mathf.Clamp01(spDiag));
                        c = Color.Lerp(c, baseAmber, 0.4f);
                        if (fx > 0.64f) c = Color.Lerp(c, darkAmber, 0.4f);
                    }

                    px[y * s + x] = c;
                }

            _itemFuel = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemFuel;
        }

        // ============================================================
        // Monument Block — carved pale-stone ashlar with sun glyph.
        // ============================================================
        private static Sprite _itemMonumentBlock;
        public static Sprite ItemMonumentBlock()
        {
            if (_itemMonumentBlock != null) return _itemMonumentBlock;
            const int s = 64;
            var px = new Color[s * s];

            Color baseStone = new Color(0.90f, 0.86f, 0.62f);
            Color litStone = new Color(0.98f, 0.95f, 0.78f);
            Color midStone = new Color(0.80f, 0.74f, 0.50f);
            Color darkStone = new Color(0.58f, 0.52f, 0.34f);
            Color engrave = new Color(0.34f, 0.29f, 0.18f);

            // Block bounds (an ashlar cube, faintly isometric look via top bevel).
            float bx0 = 0.20f, bx1 = 0.80f, by0 = 0.14f, by1 = 0.84f;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    bool inBlock = fx > bx0 && fx < bx1 && fy > by0 && fy < by1;
                    if (inBlock)
                    {
                        // base stone, gently lit upper-left.
                        float diag = ((fx) + (1f - fy)) * 0.5f;
                        c = Color.Lerp(midStone, litStone, Mathf.Clamp01(diag * 1.1f));
                        c = Color.Lerp(c, baseStone, 0.35f);

                        // Beveled lit edges: bright top + left rims.
                        if (fy > by1 - 0.06f) c = Color.Lerp(c, litStone, 0.7f);          // top bevel
                        if (fx < bx0 + 0.05f) c = Color.Lerp(c, litStone, 0.55f);         // left bevel
                        // Dark shadowed bottom + right rims.
                        if (fy < by0 + 0.05f) c = Color.Lerp(c, darkStone, 0.6f);          // bottom bevel
                        if (fx > bx1 - 0.06f) c = Color.Lerp(c, darkStone, 0.6f);          // right bevel

                        // faint stone grain speckle
                        if (Frac((fx * 9f + fy * 7f)) < 0.10f)
                            c = Color.Lerp(c, midStone, 0.25f);

                        // ---- Engraved sun emblem on the face ----
                        float gcx = 0.5f, gcy = 0.50f;
                        float dr = Mathf.Sqrt((fx - gcx) * (fx - gcx) + (fy - gcy) * (fy - gcy));

                        // central sun disc (recessed/dark engraving)
                        if (dr < 0.11f)
                        {
                            c = engrave;
                            // tiny lit lip on upper-left of the carved disc
                            if (Disc(fx - (gcx - 0.04f), fy - (gcy + 0.04f), 0.025f))
                                c = Color.Lerp(engrave, litStone, 0.4f);
                        }
                        else if (dr < 0.135f)
                        {
                            // engraved ring outline
                            c = Color.Lerp(c, engrave, 0.7f);
                        }

                        // sun rays: 4 cardinal + radial spokes via angle test using Frac of pseudo-angle.
                        if (dr > 0.135f && dr < 0.22f)
                        {
                            float ax = (fx - gcx);
                            float ay = (fy - gcy);
                            // approximate 8 rays by testing alignment to axes & diagonals
                            bool ray =
                                (Mathf.Abs(ax) < 0.022f) ||                 // vertical
                                (Mathf.Abs(ay) < 0.022f) ||                 // horizontal
                                (Mathf.Abs(ax - ay) < 0.025f) ||            // diagonal /
                                (Mathf.Abs(ax + ay) < 0.025f);              // diagonal \
                            if (ray) c = Color.Lerp(c, engrave, 0.75f);
                        }
                    }

                    px[y * s + x] = c;
                }

            _itemMonumentBlock = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemMonumentBlock;
        }
    }
}
