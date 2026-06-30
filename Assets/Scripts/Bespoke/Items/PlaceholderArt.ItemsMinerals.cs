// Bespoke procedural item art. Partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        // ===== Stone: cluster of angular grey rock chunks =====
        private static Sprite _itemStone;
        public static Sprite ItemStone()
        {
            if (_itemStone != null) return _itemStone;
            const int s = 64;
            var px = new Color[s * s];

            Color baseG = new Color(0.52f, 0.53f, 0.56f);
            Color litG = new Color(0.72f, 0.73f, 0.75f);
            Color darkG = new Color(0.42f, 0.43f, 0.46f);
            Color crev = new Color(0.30f, 0.31f, 0.34f);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // Chunk A: big left chunk (angular). Body region + faceted top.
                    // Left chunk occupies lower-left, rises to a peak.
                    bool inA = fx > 0.10f && fx < 0.52f && fy > 0.16f && fy < 0.74f
                               && (fy < 0.74f - (fx - 0.10f) * 0.6f)            // sloped top-right edge
                               && (fy > 0.16f + (0.30f - fx) * 0.5f);           // bottom-left bevel
                    // Chunk B: right chunk, taller peak.
                    bool inB = fx > 0.46f && fx < 0.88f && fy > 0.18f && fy < 0.80f
                               && (fy < 0.80f - Mathf.Abs(fx - 0.62f) * 1.1f)   // pointed top
                               && (fy > 0.18f + (fx - 0.70f) * 0.4f);
                    // Chunk C: small front chunk, low and wide.
                    bool inC = fx > 0.28f && fx < 0.66f && fy > 0.12f && fy < 0.40f
                               && (fy < 0.40f - Mathf.Abs(fx - 0.47f) * 0.4f);

                    if (inA || inB || inC)
                    {
                        c = baseG;
                        // Facet shading: top faces lighter, lower-right darker.
                        if (inB && fy > 0.50f && fx < 0.66f) c = litG;          // lit left face of B
                        else if (inB && fx > 0.66f) c = darkG;                  // shadow right face of B
                        else if (inA && fy > 0.50f) c = litG;                   // top face of A
                        else if (inA && fx > 0.40f) c = darkG;
                        else if (inC) c = Color.Lerp(baseG, litG, 0.4f);

                        // Crevices between chunks (dark seams).
                        if (Mathf.Abs(fx - 0.49f) < 0.025f && fy > 0.20f && fy < 0.62f) c = crev;
                        if (inC && fy > 0.36f) c = crev;                        // seam above front chunk

                        // Flat-facet grain speckle for stone texture.
                        if (Frac((fx * 7f + fy * 5f)) < 0.12f) c = Color.Lerp(c, darkG, 0.4f);
                        if (Frac((fx * 11f - fy * 9f)) < 0.08f) c = Color.Lerp(c, litG, 0.5f);
                    }

                    px[y * s + x] = c;
                }

            _itemStone = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemStone;
        }

        // ===== Iron Ore: rounded grey-brown boulder with rusty flecks =====
        private static Sprite _itemIronOre;
        public static Sprite ItemIronOre()
        {
            if (_itemIronOre != null) return _itemIronOre;
            const int s = 64;
            var px = new Color[s * s];

            Color baseR = new Color(0.50f, 0.46f, 0.40f);
            Color litR = new Color(0.64f, 0.60f, 0.53f);
            Color darkR = new Color(0.36f, 0.33f, 0.29f);
            Color rust = new Color(0.62f, 0.40f, 0.28f);
            Color rustLit = new Color(0.74f, 0.50f, 0.34f);

            float cx = 0.50f, cy = 0.46f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // Lumpy boulder = union of a big disc + a couple of bumps.
                    bool body = Disc(fx - cx, (fy - cy) * 1.08f, 0.36f)
                                || Disc(fx - 0.34f, fy - 0.36f, 0.20f)
                                || Disc(fx - 0.66f, fy - 0.40f, 0.21f)
                                || Disc(fx - 0.52f, fy - 0.66f, 0.18f);

                    if (body)
                    {
                        // Round shading: lit upper-left, dark lower-right.
                        float lobe = (fx - cx) * 0.7f - (fy - cy) * 0.7f; // <0 = upper-left
                        c = baseR;
                        if (lobe < -0.10f) c = litR;
                        else if (lobe > 0.12f) c = darkR;

                        // Subtle mottling.
                        if (Frac((fx * 6f + fy * 7f)) < 0.14f) c = Color.Lerp(c, darkR, 0.35f);

                        // Rusty metallic flecks scattered over the surface.
                        if (Frac((fx * 13.7f + fy * 9.3f)) < 0.07f) c = rust;
                        if (Frac((fx * 21.3f - fy * 17.1f)) < 0.04f) c = rustLit; // bright glint flecks
                    }

                    px[y * s + x] = c;
                }

            _itemIronOre = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemIronOre;
        }

        // ===== Copper Ore: grey boulder with vivid orange veins + green specks =====
        private static Sprite _itemCopperOre;
        public static Sprite ItemCopperOre()
        {
            if (_itemCopperOre != null) return _itemCopperOre;
            const int s = 64;
            var px = new Color[s * s];

            Color baseG = new Color(0.48f, 0.49f, 0.50f);
            Color litG = new Color(0.62f, 0.63f, 0.64f);
            Color darkG = new Color(0.34f, 0.35f, 0.37f);
            Color copper = new Color(0.85f, 0.52f, 0.25f);
            Color copperLit = new Color(0.95f, 0.66f, 0.38f);
            Color greenSpk = new Color(0.40f, 0.60f, 0.42f);

            float cx = 0.50f, cy = 0.46f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    bool body = Disc(fx - cx, (fy - cy) * 1.05f, 0.37f)
                                || Disc(fx - 0.32f, fy - 0.42f, 0.19f)
                                || Disc(fx - 0.68f, fy - 0.36f, 0.20f);

                    if (body)
                    {
                        float lobe = (fx - cx) * 0.7f - (fy - cy) * 0.7f;
                        c = baseG;
                        if (lobe < -0.10f) c = litG;
                        else if (lobe > 0.12f) c = darkG;

                        // Bright orange copper veins: diagonal repeating bands.
                        float vein = Frac((fx * 2.2f + fy * 3.1f) * 2.0f);
                        if (vein < 0.16f) c = copper;
                        if (vein < 0.05f) c = copperLit;
                        // A second crossing vein set for that ore look.
                        float vein2 = Frac((fx * 3.4f - fy * 1.6f) * 1.7f);
                        if (vein2 < 0.10f) c = copper;

                        // A couple of green-tinged specks.
                        if (Frac((fx * 17.3f + fy * 11.9f)) < 0.05f) c = greenSpk;

                        // Grain mottle on the grey only (don't overwrite veins).
                        if (vein >= 0.16f && vein2 >= 0.10f && Frac((fx * 7f + fy * 6f)) < 0.10f)
                            c = Color.Lerp(c, darkG, 0.35f);
                    }

                    px[y * s + x] = c;
                }

            _itemCopperOre = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemCopperOre;
        }

        // ===== Clay: smooth glossy reddish-brown blob =====
        private static Sprite _itemClay;
        public static Sprite ItemClay()
        {
            if (_itemClay != null) return _itemClay;
            const int s = 64;
            var px = new Color[s * s];

            Color baseC = new Color(0.66f, 0.42f, 0.32f);
            Color litC = new Color(0.80f, 0.56f, 0.45f);
            Color darkC = new Color(0.50f, 0.30f, 0.22f);
            Color gloss = new Color(0.92f, 0.74f, 0.66f);

            float cx = 0.50f, cy = 0.44f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // Smooth blobby lump: wide squashed disc + a small top bump.
                    bool body = Disc((fx - cx) * 0.92f, (fy - cy) * 1.18f, 0.36f)
                                || Disc(fx - 0.42f, fy - 0.60f, 0.18f);

                    if (body)
                    {
                        // Smooth radial gradient shading (no facets).
                        float d = Mathf.Sqrt((fx - 0.40f) * (fx - 0.40f) + (fy - 0.58f) * (fy - 0.58f));
                        c = Color.Lerp(litC, baseC, Mathf.Clamp01(d * 2.0f));
                        // Lower-right falls into shadow.
                        float lobe = (fx - cx) * 0.7f + (cy - fy) * 0.0f + (fy - cy) * -0.0f;
                        if ((fx - cx) - (fy - cy) > 0.22f) c = Color.Lerp(c, darkC, 0.6f);
                        if (fy < cy - 0.18f) c = Color.Lerp(c, darkC, 0.45f); // underside shadow

                        // Soft glossy highlight blob on the upper-left.
                        if (Disc(fx - 0.40f, fy - 0.60f, 0.10f)) c = Color.Lerp(c, gloss, 0.7f);
                        if (Disc(fx - 0.39f, fy - 0.62f, 0.05f)) c = gloss; // bright core glint
                    }

                    px[y * s + x] = c;
                }

            _itemClay = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemClay;
        }

        // ===== Gems: cluster of faceted teal crystal shards =====
        private static Sprite _itemGems;
        public static Sprite ItemGems()
        {
            if (_itemGems != null) return _itemGems;
            const int s = 64;
            var px = new Color[s * s];

            Color baseT = new Color(0.40f, 0.85f, 0.80f);
            Color litT = new Color(0.62f, 0.96f, 0.92f);
            Color darkT = new Color(0.22f, 0.58f, 0.58f);
            Color deepT = new Color(0.14f, 0.42f, 0.46f);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // Three pointed shards rising from a shared base. Each is a triangle.
                    // Shard 1 (tall, center).
                    float a1 = Mathf.Abs(fx - 0.50f);
                    bool sh1 = fy > 0.12f && fy < 0.86f && a1 < (0.86f - fy) * 0.34f;
                    // Shard 2 (left, shorter, leaning).
                    float a2 = Mathf.Abs(fx - 0.30f - (fy - 0.12f) * 0.06f);
                    bool sh2 = fy > 0.12f && fy < 0.64f && a2 < (0.64f - fy) * 0.30f;
                    // Shard 3 (right, shorter).
                    float a3 = Mathf.Abs(fx - 0.70f + (fy - 0.12f) * 0.04f);
                    bool sh3 = fy > 0.12f && fy < 0.60f && a3 < (0.60f - fy) * 0.30f;

                    if (sh1 || sh2 || sh3)
                    {
                        c = baseT;
                        // Facet split down each shard: left face lit, right face dark.
                        if (sh1)
                        {
                            c = (fx < 0.50f) ? litT : darkT;
                            if (fx < 0.44f) c = baseT;          // central lit band
                            if (fx > 0.56f) c = deepT;          // far right shadow facet
                        }
                        else if (sh2)
                        {
                            c = (fx < 0.30f) ? litT : darkT;
                        }
                        else if (sh3)
                        {
                            c = (fx < 0.70f) ? baseT : deepT;
                        }

                        // Bright white glints near the tips and a facet edge.
                        if (sh1 && Disc(fx - 0.485f, fy - 0.74f, 0.035f)) c = Color.white;
                        if (sh2 && Disc(fx - 0.295f, fy - 0.54f, 0.028f)) c = Color.white;
                        if (sh3 && Disc(fx - 0.705f, fy - 0.50f, 0.028f)) c = Color.white;
                        // Sparkle specks on the lit faces.
                        if ((sh1 && fx < 0.50f) || (sh2 && fx < 0.30f))
                            if (Frac((fx * 19f + fy * 23f)) < 0.05f) c = Color.Lerp(c, Color.white, 0.8f);
                    }

                    px[y * s + x] = c;
                }

            _itemGems = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemGems;
        }

        // ===== Charcoal: chunky near-black briquettes with ember specks =====
        private static Sprite _itemCharcoal;
        public static Sprite ItemCharcoal()
        {
            if (_itemCharcoal != null) return _itemCharcoal;
            const int s = 64;
            var px = new Color[s * s];

            Color baseK = new Color(0.16f, 0.15f, 0.16f);
            Color litK = new Color(0.30f, 0.31f, 0.34f);   // cool-grey edge highlight
            Color darkK = new Color(0.09f, 0.085f, 0.10f);
            Color ember = new Color(0.55f, 0.22f, 0.12f);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    // Two stacked angular briquettes (rounded-rectangular lumps).
                    // Lower-left briquette.
                    bool b1 = fx > 0.12f && fx < 0.62f && fy > 0.14f && fy < 0.54f
                              && !(Disc(fx - 0.12f, fy - 0.14f, 0.06f) == false && false); // (always body in band)
                    b1 = fx > 0.12f && fx < 0.62f && fy > 0.14f && fy < 0.54f;
                    // Round its corners a touch.
                    if (b1)
                    {
                        if (fx < 0.18f && fy < 0.20f && !Disc(fx - 0.18f, fy - 0.20f, 0.06f)) b1 = false;
                        if (fx > 0.56f && fy > 0.48f && !Disc(fx - 0.56f, fy - 0.48f, 0.06f)) b1 = false;
                    }
                    // Upper-right briquette (overlapping).
                    bool b2 = fx > 0.40f && fx < 0.86f && fy > 0.40f && fy < 0.82f;
                    if (b2)
                    {
                        if (fx > 0.80f && fy > 0.76f && !Disc(fx - 0.80f, fy - 0.76f, 0.06f)) b2 = false;
                        if (fx < 0.46f && fy < 0.46f && !Disc(fx - 0.46f, fy - 0.46f, 0.06f)) b2 = false;
                    }

                    if (b1 || b2)
                    {
                        c = baseK;
                        // Lit upper-left facet, dark lower-right per briquette.
                        if (b2)
                        {
                            if (fy > 0.62f && fx < 0.66f) c = litK;     // top-left lit face
                            else if (fx > 0.70f) c = darkK;
                        }
                        else
                        {
                            if (fy > 0.40f && fx < 0.40f) c = litK;
                            else if (fx > 0.46f && fy < 0.30f) c = darkK;
                        }

                        // Dark seam between the two briquettes.
                        if (b1 && b2 && fy > 0.40f && fy < 0.54f) c = darkK;

                        // Cool-grey edge highlight along upper rims.
                        if (b2 && fy > 0.78f && fy < 0.82f) c = litK;
                        if (b1 && fy > 0.50f && fy < 0.54f && fx < 0.50f) c = litK;

                        // Matte grain mottle.
                        if (Frac((fx * 9f + fy * 8f)) < 0.16f) c = Color.Lerp(c, darkK, 0.5f);

                        // One or two faint warm ember-red specks.
                        if (Disc(fx - 0.34f, fy - 0.30f, 0.022f)) c = ember;
                        if (Disc(fx - 0.62f, fy - 0.58f, 0.018f)) c = ember;
                    }

                    px[y * s + x] = c;
                }

            _itemCharcoal = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemCharcoal;
        }
    }
}
