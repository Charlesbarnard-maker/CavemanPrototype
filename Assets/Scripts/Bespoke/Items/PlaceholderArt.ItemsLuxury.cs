// Bespoke procedural item art. Partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        // ---------------------------------------------------------------
        // 1. Jewelry — a standing gold ring (torus) with a teal gem on top
        // ---------------------------------------------------------------
        private static Sprite _itemJewelry;
        public static Sprite ItemJewelry()
        {
            if (_itemJewelry != null) return _itemJewelry;
            const int s = 64;
            var px = new Color[s * s];

            // Gold tones
            Color goldBase = new Color(0.92f, 0.78f, 0.35f);
            Color goldLit = new Color(1.00f, 0.92f, 0.58f);
            Color goldDark = new Color(0.62f, 0.48f, 0.16f);
            // Gem tones (teal)
            Color gemBase = new Color(0.45f, 0.85f, 0.80f);
            Color gemLit = new Color(0.70f, 0.97f, 0.92f);
            Color gemDark = new Color(0.22f, 0.55f, 0.55f);

            // Ring band centred lower; torus in the lower-mid frame
            float rcx = 0.50f, rcy = 0.40f;
            float rOuter = 0.30f, rInner = 0.165f;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    float dx = fx - rcx, dy = fy - rcy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // --- Gold torus band ---
                    if (dist <= rOuter && dist >= rInner)
                    {
                        // shade across band thickness + by quadrant (lit upper-left)
                        float t = (dist - rInner) / (rOuter - rInner); // 0 inner .. 1 outer
                        Color band = Color.Lerp(goldLit, goldDark, t * 0.85f);
                        // global lighting: upper-left brighter
                        float lit = Mathf.Clamp01(0.5f - (dx * 0.9f + dy * -0.9f));
                        band = Color.Lerp(band, goldDark, lit * 0.35f);
                        // bright rim glint on upper-left arc
                        if (dx < -0.04f && dy > 0.06f && Mathf.Abs(dist - (rInner + rOuter) * 0.5f) < 0.05f)
                            band = goldLit;
                        c = band;
                    }

                    // --- Teal gem mounted on top of the ring ---
                    // gem sits just above the band top, a faceted diamond shape
                    float gcx = 0.50f, gcy = 0.80f;
                    float gdx = fx - gcx, gdy = fy - gcy;
                    // diamond: |x|/w + |y|/h <= 1
                    float gw = 0.14f, gh = 0.16f;
                    float diamond = Mathf.Abs(gdx) / gw + Mathf.Abs(gdy) / gh;
                    if (diamond <= 1f)
                    {
                        // facet shading: left face lit, right face dark, crown top brightest
                        Color g;
                        if (gdy > 0.03f) g = gemLit;                 // top crown
                        else if (gdx < 0f) g = Color.Lerp(gemBase, gemLit, 0.4f); // left facet
                        else g = gemDark;                            // right facet
                        // faceted grain lines
                        if (Frac((gdx + gdy) * 18f) < 0.18f) g = Color.Lerp(g, gemDark, 0.3f);
                        c = g;
                    }
                    // little gold prongs/mount where gem meets ring
                    if (fy > 0.66f && fy < 0.74f && Mathf.Abs(fx - 0.5f) < 0.10f)
                        c = goldBase;
                    // white glint on the gem's upper-left
                    if (Disc(fx - 0.455f, fy - 0.85f, 0.028f))
                        c = Color.white;

                    px[y * s + x] = c;
                }

            _itemJewelry = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemJewelry;
        }

        // ---------------------------------------------------------------
        // 2. Cloth — a folded bolt of off-white fabric, stacked folds
        // ---------------------------------------------------------------
        private static Sprite _itemCloth;
        public static Sprite ItemCloth()
        {
            if (_itemCloth != null) return _itemCloth;
            const int s = 64;
            var px = new Color[s * s];

            Color clothBase = new Color(0.88f, 0.86f, 0.80f);
            Color clothLit = new Color(0.97f, 0.96f, 0.91f);
            Color clothDark = new Color(0.66f, 0.64f, 0.58f);
            Color clothCrease = new Color(0.54f, 0.52f, 0.47f);

            // Bolt occupies a rounded rectangle of stacked horizontal folds
            float left = 0.18f, right = 0.82f;
            float bottom = 0.18f, top = 0.82f;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    bool inBox = fx > left && fx < right && fy > bottom && fy < top;
                    if (inBox)
                    {
                        // 5 stacked horizontal folds
                        float vrange = top - bottom;
                        float fv = (fy - bottom) / vrange;   // 0 bottom .. 1 top
                        float folds = fv * 5f;
                        float fInFold = Frac(folds);          // 0..1 within a fold

                        // each fold: lighter at its upper rounded ridge, darker crease at the bottom seam
                        Color band;
                        if (fInFold > 0.78f) band = clothCrease;            // seam line between folds (dark)
                        else if (fInFold > 0.55f) band = clothDark;         // lower shadow of fold
                        else if (fInFold > 0.25f) band = clothBase;         // mid
                        else band = clothLit;                              // lit ridge (upper of each fold)

                        // gentle left->right shading (lit left)
                        band = Color.Lerp(band, clothDark, (fx - left) / (right - left) * 0.18f);

                        // rounded ends: soften the vertical edges
                        float edge = 0.05f;
                        if (fx < left + edge)
                            band = Color.Lerp(band, clothDark, (left + edge - fx) / edge * 0.5f);
                        if (fx > right - edge)
                            band = Color.Lerp(band, clothDark, (fx - (right - edge)) / edge * 0.55f);

                        c = band;

                        // top fold a touch brighter overall (slightly lit top)
                        if (fv > 0.80f) c = Color.Lerp(c, clothLit, 0.30f);
                    }

                    // crop the outer corners slightly for a soft bolt silhouette
                    if (fx < left + 0.04f && (fy < bottom + 0.04f || fy > top - 0.04f)) c = Clear;
                    if (fx > right - 0.04f && (fy < bottom + 0.04f || fy > top - 0.04f)) c = Clear;

                    px[y * s + x] = c;
                }

            _itemCloth = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemCloth;
        }

        // ---------------------------------------------------------------
        // 3. Clothes — a little folded blue tunic / shirt silhouette
        // ---------------------------------------------------------------
        private static Sprite _itemClothes;
        public static Sprite ItemClothes()
        {
            if (_itemClothes != null) return _itemClothes;
            const int s = 64;
            var px = new Color[s * s];

            Color shirtBase = new Color(0.45f, 0.55f, 0.78f);
            Color shirtLit = new Color(0.62f, 0.72f, 0.92f);
            Color shirtDark = new Color(0.28f, 0.36f, 0.56f);
            Color collar = new Color(0.74f, 0.80f, 0.94f);

            // shirt silhouette: body + two short sleeves + neck notch
            float bodyL = 0.34f, bodyR = 0.66f;     // body torso column
            float bodyB = 0.16f, bodyT = 0.72f;     // body vertical span
            float slvB = 0.50f, slvT = 0.74f;       // sleeve vertical span
            float slvOutL = 0.14f, slvOutR = 0.86f; // outer sleeve extents

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    bool body = fx > bodyL && fx < bodyR && fy > bodyB && fy < bodyT;
                    bool leftSleeve = fx > slvOutL && fx < bodyL + 0.02f && fy > slvB && fy < slvT;
                    bool rightSleeve = fx > bodyR - 0.02f && fx < slvOutR && fy > slvB && fy < slvT;

                    bool inShirt = body || leftSleeve || rightSleeve;

                    if (inShirt)
                    {
                        Color sh = shirtBase;
                        // directional lighting: upper-left lit, lower-right shadow
                        float lit = Mathf.Clamp01(0.5f + (fx - 0.5f) * -0.6f + (fy - 0.45f) * 0.6f);
                        sh = Color.Lerp(shirtDark, shirtLit, lit);
                        sh = Color.Lerp(sh, shirtBase, 0.35f);

                        // sleeve seams slightly darker where they meet body
                        if ((leftSleeve && fx > bodyL - 0.04f) || (rightSleeve && fx < bodyR + 0.04f))
                            sh = Color.Lerp(sh, shirtDark, 0.35f);

                        // sleeve cuffs darker line at outer ends
                        if (leftSleeve && fx < slvOutL + 0.04f) sh = shirtDark;
                        if (rightSleeve && fx > slvOutR - 0.04f) sh = shirtDark;

                        // subtle woven grain
                        if (Frac((fx + fy) * 22f) < 0.14f) sh = Color.Lerp(sh, shirtDark, 0.18f);

                        c = sh;
                    }

                    // collar/neck notch at top centre (cut out a small V, paint lighter collar around it)
                    float ncx = 0.50f, ncTop = bodyT;
                    if (body && fy > bodyT - 0.10f)
                    {
                        // neck hole
                        if (Disc(fx - ncx, fy - (ncTop - 0.005f), 0.075f))
                            c = Clear;
                        // collar band just below neck hole
                        else if (Disc(fx - ncx, fy - (ncTop - 0.045f), 0.115f) && fy < bodyT)
                            c = collar;
                    }

                    // a centre fold/placket line down the body
                    if (body && Mathf.Abs(fx - 0.5f) < 0.012f && fy < bodyT - 0.10f)
                        c = shirtDark;

                    // bottom hem darker line
                    if (body && fy < bodyB + 0.04f)
                        c = Color.Lerp(c, shirtDark, 0.5f);

                    px[y * s + x] = c;
                }

            _itemClothes = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemClothes;
        }

        // ---------------------------------------------------------------
        // 4. Pottery — a terracotta amphora: belly, neck, two side handles
        // ---------------------------------------------------------------
        private static Sprite _itemPottery;
        public static Sprite ItemPottery()
        {
            if (_itemPottery != null) return _itemPottery;
            const int s = 64;
            var px = new Color[s * s];

            Color clayBase = new Color(0.74f, 0.48f, 0.36f);
            Color clayLit = new Color(0.88f, 0.62f, 0.48f);
            Color clayDark = new Color(0.50f, 0.30f, 0.22f);
            Color clayRim = new Color(0.64f, 0.40f, 0.30f);

            float cx = 0.50f;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // --- Belly: a fat rounded body, lower portion ---
                    // ellipse centred at (0.5, 0.36), wider than tall
                    float bdx = (fx - cx) / 0.30f;
                    float bdy = (fy - 0.36f) / 0.30f;
                    bool belly = (bdx * bdx + bdy * bdy) <= 1f;

                    // --- Neck: narrow vertical column ---
                    bool neck = Mathf.Abs(fx - cx) < 0.085f && fy > 0.55f && fy < 0.82f;

                    // --- Rim/mouth: small flare at very top ---
                    bool rim = Mathf.Abs(fx - cx) < 0.13f && fy >= 0.80f && fy < 0.88f;

                    // --- Handles: two small arcs on each side connecting neck to shoulder ---
                    // left handle ring
                    float hlx = fx - 0.30f, hly = fy - 0.62f;
                    float hld = Mathf.Sqrt(hlx * hlx + hly * hly);
                    bool leftHandle = hld <= 0.115f && hld >= 0.06f && fx < cx;
                    float hrx = fx - 0.70f, hry = fy - 0.62f;
                    float hrd = Mathf.Sqrt(hrx * hrx + hry * hry);
                    bool rightHandle = hrd <= 0.115f && hrd >= 0.06f && fx > cx;

                    bool inPot = belly || neck || rim || leftHandle || rightHandle;

                    if (inPot)
                    {
                        Color clay = clayBase;
                        // base directional shading (lit upper-left, dark lower-right)
                        float lit = Mathf.Clamp01(0.5f + (fx - 0.5f) * -0.7f + (fy - 0.4f) * 0.5f);
                        clay = Color.Lerp(clayDark, clayLit, lit);
                        clay = Color.Lerp(clay, clayBase, 0.30f);

                        // round the belly with curvature shading (darker toward edges)
                        if (belly)
                        {
                            float edgeShade = Mathf.Clamp01(bdx * bdx + bdy * bdy);
                            clay = Color.Lerp(clay, clayDark, edgeShade * 0.4f);
                            // bright vertical highlight stripe on the belly (slightly left of centre)
                            if (Mathf.Abs(fx - 0.43f) < 0.045f)
                                clay = Color.Lerp(clay, clayLit, 0.65f);
                        }

                        if (rim) clay = clayRim;
                        if (leftHandle || rightHandle)
                            clay = Color.Lerp(clayBase, clayDark, 0.25f);

                        // horizontal throwing-rings on the belly for texture
                        if (belly && Frac(fy * 18f) < 0.12f)
                            clay = Color.Lerp(clay, clayDark, 0.22f);

                        c = clay;
                    }

                    px[y * s + x] = c;
                }

            _itemPottery = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemPottery;
        }
    }
}
