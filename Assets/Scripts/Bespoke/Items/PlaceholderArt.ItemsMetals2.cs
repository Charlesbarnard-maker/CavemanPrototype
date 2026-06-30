// Bespoke procedural item art. Partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        // ===== Copper Plate: flat hammered copper sheet with a bright rolled sheen =====
        private static Sprite _itemCopperPlate;
        public static Sprite ItemCopperPlate()
        {
            if (_itemCopperPlate != null) return _itemCopperPlate;
            const int s = 64;
            var px = new Color[s * s];

            Color baseC = new Color(0.90f, 0.60f, 0.34f);
            Color dkC   = new Color(0.62f, 0.38f, 0.20f);
            Color litC  = new Color(0.99f, 0.78f, 0.52f);
            Color bevel = new Color(1.00f, 0.90f, 0.70f);

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

                        // bright rolled sheen: broad diagonal sweep across the plate
                        float band = Mathf.Abs((px0 - 0.5f) + (fy - 0.52f) * 0.8f);
                        if (band < 0.12f) c = Color.Lerp(c, litC, Mathf.Clamp01(1f - band / 0.12f) * 0.7f);
                        // thin crisp glint inside the sheen
                        if (Mathf.Abs((px0 - 0.46f) + (fy - 0.50f) * 0.8f) < 0.03f)
                            c = Color.Lerp(c, bevel, 0.85f);

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

            _itemCopperPlate = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemCopperPlate;
        }

        // ===== Iron Rod: bundle of 3 cylindrical cool blue-grey rods lying together =====
        private static Sprite _itemIronRod;
        public static Sprite ItemIronRod()
        {
            if (_itemIronRod != null) return _itemIronRod;
            const int s = 64;
            var px = new Color[s * s];

            Color baseC = new Color(0.72f, 0.74f, 0.80f);
            Color dkC   = new Color(0.46f, 0.48f, 0.56f);
            Color litC  = new Color(0.88f, 0.90f, 0.96f);
            Color spec  = new Color(1.00f, 1.00f, 1.00f);
            Color capLit = new Color(0.80f, 0.82f, 0.88f);
            Color capDk  = new Color(0.54f, 0.56f, 0.64f);

            // three horizontal rod centre-lines (fy), stacked lower-front to upper-back
            float[] cy = { 0.36f, 0.50f, 0.64f };
            const float rodHalf = 0.085f;   // vertical half-thickness of each rod
            const float endL = 0.20f, endR = 0.80f; // rods run left->right

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // draw from back (top) rod to front (bottom) rod so nearer rods overlap
                    for (int r = cy.Length - 1; r >= 0; r--)
                    {
                        float dyv = fy - cy[r];
                        if (Mathf.Abs(dyv) > rodHalf) continue;
                        if (fx < endL - 0.02f || fx > endR + 0.04f) continue;

                        // rounded end cap on the right; straight cylinder body to the left
                        bool onCap = fx > endR;
                        if (onCap)
                        {
                            // elliptical end face
                            float ex = (fx - endR) / 0.045f;
                            float ey = dyv / rodHalf;
                            if (ex * ex + ey * ey > 1f) continue;
                            float lit = Mathf.Clamp01(0.55f - ey * 0.5f - ex * 0.2f);
                            c = Color.Lerp(capDk, capLit, lit);
                        }
                        else
                        {
                            if (fx < endL) continue;
                            // cylindrical cross-section shading: lit upper, dark lower
                            float t = dyv / rodHalf;             // -1 top .. +1 bottom
                            float round = Mathf.Clamp01(1f - t * t); // brightest at centre line
                            float sh = Mathf.Clamp01(0.5f - t * 0.7f);
                            c = Color.Lerp(dkC, baseC, sh);
                            // upper rounded highlight band
                            c = Color.Lerp(c, litC, Mathf.Clamp01(0.6f - t) * 0.5f * round);
                            // crisp specular line just above centre, running the rod length
                            if (t > -0.55f && t < -0.25f)
                                c = Color.Lerp(c, spec, 0.6f);
                            // dark contact shadow at the very bottom of each rod
                            if (t > 0.78f) c = Color.Lerp(c, dkC, 0.55f);
                        }
                    }

                    px[y * s + x] = c;
                }

            _itemIronRod = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemIronRod;
        }
    }
}
