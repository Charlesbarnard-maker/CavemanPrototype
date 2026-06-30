// Bespoke procedural item art for the vehicle/electronics components. Partial of PlaceholderArt.
// Helpers reused from the other item files: Clear, Disc(dx,dy,r), Frac(v), FinishDecor(px,s,...).
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        // ===== Locomotive: a little steam-engine silhouette (boiler + cab + smokestack + driving wheels) =====
        private static Sprite _itemLocomotive;
        public static Sprite ItemLocomotive()
        {
            if (_itemLocomotive != null) return _itemLocomotive;
            const int s = 64;
            var px = new Color[s * s];
            Color body   = new Color(0.38f, 0.40f, 0.47f);
            Color dk     = new Color(0.18f, 0.19f, 0.24f);
            Color lit    = new Color(0.58f, 0.61f, 0.69f);
            Color wheelC = new Color(0.14f, 0.14f, 0.18f);
            Color glow   = new Color(0.98f, 0.78f, 0.28f); // firebox / cab window
            float[] wx = { 0.30f, 0.48f, 0.66f };

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    bool boiler = fy > 0.36f && fy < 0.58f && fx > 0.20f && fx < 0.70f;
                    bool nose   = Disc(fx - 0.68f, fy - 0.47f, 0.115f) && fx > 0.62f && fy > 0.36f;
                    bool cab    = fx > 0.20f && fx < 0.42f && fy > 0.36f && fy < 0.72f;
                    if (boiler || nose || cab)
                    {
                        float litf = Mathf.Clamp01(0.45f + (fy - 0.40f) * 1.4f);
                        c = Color.Lerp(dk, body, 0.55f + litf * 0.45f);
                        c = Color.Lerp(c, lit, Mathf.Clamp01(litf - 0.45f) * 0.6f);
                    }
                    // smokestack on top of the boiler
                    if (fx > 0.56f && fx < 0.66f && fy > 0.56f && fy < 0.74f) c = Color.Lerp(dk, body, 0.5f);
                    // cab roof highlight
                    if (cab && fy > 0.66f) c = Color.Lerp(c, lit, 0.45f);
                    // glowing cab window
                    if (fx > 0.25f && fx < 0.37f && fy > 0.46f && fy < 0.62f) c = glow;
                    // driving wheels (over the bottom of the body, sticking below it)
                    for (int w = 0; w < 3; w++)
                    {
                        float ddx = fx - wx[w], ddy = fy - 0.31f;
                        if (Disc(ddx, ddy, 0.085f)) c = wheelC;
                        if (Disc(ddx, ddy, 0.024f)) c = lit; // hub
                    }
                    px[y * s + x] = c;
                }

            _itemLocomotive = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemLocomotive;
        }

        // ===== Hull: a riveted steel ship-hull profile with a wooden deck strip + waterline =====
        private static Sprite _itemHull;
        public static Sprite ItemHull()
        {
            if (_itemHull != null) return _itemHull;
            const int s = 64;
            var px = new Color[s * s];
            Color body  = new Color(0.46f, 0.52f, 0.58f);
            Color dk    = new Color(0.26f, 0.30f, 0.36f);
            Color lit   = new Color(0.70f, 0.76f, 0.84f);
            Color rivet = new Color(0.30f, 0.34f, 0.40f);
            Color deck  = new Color(0.56f, 0.42f, 0.26f);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;

                    float keel = 0.22f + 1.5f * (fx - 0.5f) * (fx - 0.5f); // curved bottom, deepest amidships
                    bool hull = fx > 0.14f && fx < 0.86f && fy > keel && fy < 0.60f;
                    bool deckBand = fx > 0.12f && fx < 0.88f && fy >= 0.60f && fy < 0.67f;

                    if (hull)
                    {
                        float span = Mathf.Max(0.001f, 0.60f - keel);
                        float litf = Mathf.Clamp01(0.35f + (fy - keel) / span * 0.65f); // lighter near the deck
                        c = Color.Lerp(dk, body, litf);
                        // vertical hull-plate seams
                        if (Frac(fx * 7f) < 0.07f) c = Color.Lerp(c, dk, 0.45f);
                        // a row of rivets along the seam edges
                        if (Frac(fx * 7f) > 0.46f && Frac(fx * 7f) < 0.56f && Frac(fy * 9f) < 0.22f) c = rivet;
                        // bright waterline stripe
                        float band = keel + span * 0.62f;
                        if (fy > band && fy < band + 0.05f) c = Color.Lerp(c, lit, 0.55f);
                    }
                    else if (deckBand) c = Color.Lerp(deck, dk, 0.18f + (0.67f - fy) * 1.2f);

                    px[y * s + x] = c;
                }

            _itemHull = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemHull;
        }

        // ===== Copper Wiring: a wound coil of copper wire (concentric rings, end-on) on a small spool =====
        private static Sprite _itemWiring;
        public static Sprite ItemWiring()
        {
            if (_itemWiring != null) return _itemWiring;
            const int s = 64;
            var px = new Color[s * s];
            Color cop   = new Color(0.88f, 0.54f, 0.26f);
            Color copDk = new Color(0.56f, 0.30f, 0.12f);
            Color copLit= new Color(1.00f, 0.74f, 0.42f);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;
                    float dx = fx - 0.5f, dy = fy - 0.5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r < 0.42f && r > 0.10f)
                    {
                        // wound turns: alternating copper rings (the wire) and thin shadow gaps
                        float t = Frac(r * 11f);
                        c = t < 0.62f ? cop : copDk;
                        // top-left sheen on each rounded turn
                        if (t > 0.18f && t < 0.40f) c = Color.Lerp(c, copLit, 0.55f * Mathf.Clamp01(0.7f - dy - dx));
                        // overall round shading (darker lower-right)
                        c = Color.Lerp(c, copDk, Mathf.Clamp01((dy + dx) * 0.6f + 0.1f) * 0.35f);
                    }
                    px[y * s + x] = c;
                }

            _itemWiring = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemWiring;
        }

        // ===== Circuit: a green PCB with copper traces, gold edge-pads and a black chip =====
        private static Sprite _itemCircuit;
        public static Sprite ItemCircuit()
        {
            if (_itemCircuit != null) return _itemCircuit;
            const int s = 64;
            var px = new Color[s * s];
            Color green   = new Color(0.16f, 0.46f, 0.28f);
            Color greenLit= new Color(0.26f, 0.60f, 0.38f);
            Color trace   = new Color(0.80f, 0.58f, 0.26f); // copper trace
            Color gold    = new Color(0.92f, 0.78f, 0.32f);
            Color chip     = new Color(0.10f, 0.11f, 0.13f);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;
                    bool board = fx > 0.16f && fx < 0.84f && fy > 0.16f && fy < 0.84f;
                    if (board)
                    {
                        c = Color.Lerp(green, greenLit, Mathf.Clamp01((fy - 0.3f) + (0.6f - fx)) * 0.5f);
                        // copper traces — a sparse grid
                        if (Frac(fy * 5f) < 0.06f || Frac(fx * 5f) < 0.06f) c = trace;
                        // solder pads at some grid crossings
                        if (Frac(fy * 5f) < 0.10f && Frac(fx * 5f) < 0.10f) c = gold;
                        // central black chip with a corner marker
                        if (fx > 0.38f && fx < 0.62f && fy > 0.42f && fy < 0.64f) c = chip;
                        if (Disc(fx - 0.43f, fy - 0.59f, 0.02f)) c = greenLit; // chip pin-1 dot
                        // gold contact fingers along the bottom edge
                        if (fy < 0.24f && Frac(fx * 11f) < 0.55f) c = gold;
                    }
                    px[y * s + x] = c;
                }

            _itemCircuit = FinishDecor(px, s, 0.5f, 0.12f, 0.30f, 0.06f, 0f);
            return _itemCircuit;
        }
    }
}
