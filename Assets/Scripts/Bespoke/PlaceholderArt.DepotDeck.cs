// Bespoke procedural art for the 3×1 DEPOT platforms (Station / Harbour), split so a depot can be laid as ONE
// tile PER footprint cell instead of a single square sprite stretched 3:1 (which also flipped when the depot was
// rotated vertical). Each depot gets a SEAMLESS deck tile — full-bleed left↔right so N cells read as one
// continuous platform — plus a single landmark ACCENT (a station shelter / a harbour crane) dropped on the
// centre cell. The deck art is authored for a horizontal (east–west) lane; Depot rotates each tile 90° for a
// vertical lane, exactly as it already does for the track overlay. Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        // ---- STATION deck: one cell of raised plank platform, seamless so a run of cells is one deck --------
        private static Sprite _stationDeckTile;
        public static Sprite StationDeckTile()
        {
            if (_stationDeckTile != null) return _stationDeckTile;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            var fascia    = new Color(0.20f, 0.13f, 0.07f, 1f);
            var bodyHi    = new Color(1f, 1f, 1f, 1f);            // structural white body (tinted at runtime)
            var body      = new Color(0.84f, 0.84f, 0.86f, 1f);
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);
            var legShade  = new Color(0.52f, 0.52f, 0.55f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                Color c = Clear;

                // support under-structure (a solid base band, full width → tiles seamlessly)
                if (fy >= 0.14f && fy < 0.24f) c = legShade;

                // raised plank DECK slab (fills most of the cell so tiles form a solid platform strip)
                if (fy >= 0.24f && fy <= 0.80f) {
                    if (fy <= 0.32f) c = fascia;                        // dark front lip
                    else {
                        float t = (fy - 0.32f) / 0.48f;                 // 0 above lip .. 1 deck top
                        c = (t > 0.62f) ? bodyHi : (t < 0.22f ? bodyShade : body);
                        if (Frac(fx * 4f) < 0.07f) c = fascia;          // vertical plank seams (integer freq → seamless)
                        if (fy >= 0.77f) c = bodyHi;                    // top decking-lip highlight
                    }
                }

                // translucent ground shadow along the very bottom (constant across x → seamless)
                if (fy < 0.135f && fy >= 0.09f && c.a == 0f)
                    c = new Color(0.12f, 0.10f, 0.08f, 0.42f);

                px[y * s + x] = c;
            }
            OutlineInPlace(px, s, outline); // full-width bands have no left/right silhouette, so no seam at cell joins
            tex.SetPixels(px); tex.Apply();
            _stationDeckTile = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _stationDeckTile;
        }

        // ---- STATION shelter: the centre-cell landmark (two posts + thatch gable + hanging sign) ------------
        // Lifted from the original StationPlatform composition so it reads exactly as before, now at its true
        // 1:1 proportions on a transparent field instead of stretched across the whole footprint.
        private static Sprite _stationShelter;
        public static Sprite StationShelter()
        {
            if (_stationShelter != null) return _stationShelter;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            var woodD     = new Color(0.27f, 0.17f, 0.09f, 1f);
            var woodM     = new Color(0.45f, 0.31f, 0.17f, 1f);
            var woodL     = new Color(0.60f, 0.44f, 0.26f, 1f);
            var roofD     = new Color(0.20f, 0.24f, 0.12f, 1f);
            var roofM     = new Color(0.31f, 0.35f, 0.17f, 1f);
            var roofL     = new Color(0.45f, 0.49f, 0.26f, 1f);
            var bodyHi    = new Color(1f, 1f, 1f, 1f);
            var body      = new Color(0.84f, 0.84f, 0.86f, 1f);
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // two shelter posts
                bool sPostL = fx >= 0.400f && fx <= 0.445f && fy >= 0.36f && fy <= 0.78f;
                bool sPostR = fx >= 0.555f && fx <= 0.600f && fy >= 0.36f && fy <= 0.78f;
                if (sPostL || sPostR) {
                    float lp = sPostL ? (fx - 0.400f) / 0.045f : (fx - 0.555f) / 0.045f;
                    c = (lp < 0.40f) ? bodyHi : (lp > 0.75f ? bodyShade : body);
                }

                // thatched gable roof (apex at centre, slopes to eaves)
                float roofBase = 0.78f;
                float roofLine = 0.95f - Mathf.Abs(dxc) * 0.70f;
                if (fx >= 0.35f && fx <= 0.65f && fy >= roofBase && fy <= roofLine) {
                    float t = (roofLine - fy) / 0.17f;
                    c = (dxc < 0f) ? roofL : roofD;
                    if (t < 0.22f) c = roofD;
                    else if (Mathf.Abs(dxc) > 0.015f) c = roofM;
                    if (t >= 0.80f) c = roofL;
                    if (Frac(fx * 18f) < 0.12f) c = roofD;
                    if (Mathf.Abs(dxc) < 0.014f) c = roofD;
                }
                if (fx >= 0.35f && fx <= 0.65f && fy >= roofBase - 0.02f && fy < roofBase) c = woodD; // eave shadow

                // hanging sign board between the posts
                if (fx >= 0.455f && fx <= 0.545f && fy >= 0.520f && fy <= 0.605f) {
                    c = woodM;
                    if (fx <= 0.478f) c = woodL; else if (fx >= 0.522f) c = woodD;
                    if (fy >= 0.590f || fy <= 0.532f) c = woodD;
                    if (fy >= 0.558f && fy <= 0.568f) c = woodD;
                }

                px[y * s + x] = c;
            }
            OutlineInPlace(px, s, outline);
            tex.SetPixels(px); tex.Apply();
            _stationShelter = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _stationShelter;
        }

        // ---- HARBOUR deck: one cell of timber jetty with a mooring pile, seamless left↔right ----------------
        private static Sprite _harbourDeckTile;
        public static Sprite HarbourDeckTile()
        {
            if (_harbourDeckTile != null) return _harbourDeckTile;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            // Lighter weathered-oak planks so the dark blue-grey harbour tint doesn't render the deck near-black.
            var outline = new Color(0.09f, 0.07f, 0.05f, 1f);
            var pileD = new Color(0.40f, 0.28f, 0.15f, 1f); var pileM = new Color(0.66f, 0.50f, 0.32f, 1f); var pileL = new Color(0.80f, 0.64f, 0.44f, 1f);
            var deckD = new Color(0.50f, 0.37f, 0.22f, 1f); var deckM = new Color(0.72f, 0.57f, 0.38f, 1f); var deckL = new Color(0.86f, 0.72f, 0.52f, 1f);
            var seam  = new Color(0.34f, 0.23f, 0.13f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                Color c = Clear;

                // mooring PILE — one round post per tile, centred, rising above the deck (drawn first, deck over)
                float pdx = fx - 0.5f;
                if (Mathf.Abs(pdx) <= 0.05f && fy >= 0.18f && fy <= 0.86f) {
                    c = pileM;
                    if (pdx < -0.018f) c = pileL; else if (pdx > 0.018f) c = pileD;
                    if (fy >= 0.80f) c = pileL;                        // lit rounded cap
                }

                // jetty DECK planks (full width → seamless), with a fascia lip and top highlight
                if (fy >= 0.24f && fy <= 0.62f) {
                    c = deckM;
                    float t = (fy - 0.24f) / 0.38f;
                    if (t < 0.18f) c = deckD;                          // shaded front lip
                    else if (t > 0.86f) c = deckL;                    // sunlit deck top
                    if (Frac(fx * 4f) < 0.08f) c = seam;              // plank seams (integer freq → seamless)
                }

                // translucent ground/water shadow along the bottom (constant across x → seamless)
                if (fy < 0.135f && fy >= 0.09f && c.a == 0f)
                    c = new Color(0.10f, 0.12f, 0.14f, 0.40f);

                px[y * s + x] = c;
            }
            OutlineInPlace(px, s, outline);
            tex.SetPixels(px); tex.Apply();
            _harbourDeckTile = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _harbourDeckTile;
        }

        // ---- HARBOUR crane: the centre-cell landmark (bollards + rope + timber hoist) -----------------------
        // Lifted from the original HarbourDock composition, now standalone at 1:1 on a transparent field.
        private static Sprite _harbourCrane;
        public static Sprite HarbourCrane()
        {
            if (_harbourCrane != null) return _harbourCrane;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            var woodD     = new Color(0.27f, 0.17f, 0.09f, 1f);
            var woodM     = new Color(0.45f, 0.31f, 0.17f, 1f);
            var woodL     = new Color(0.60f, 0.44f, 0.26f, 1f);
            var ropeD     = new Color(0.46f, 0.38f, 0.22f, 1f);
            var ropeM     = new Color(0.66f, 0.56f, 0.34f, 1f);
            var ropeL     = new Color(0.80f, 0.70f, 0.46f, 1f);
            var bodyHi    = new Color(1f, 1f, 1f, 1f);
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                Color c = Clear;

                // two BOLLARD posts standing on the deck
                float b1 = fx - 0.30f, b2 = fx - 0.70f;
                bool boll1 = Mathf.Abs(b1) <= 0.05f && fy >= 0.28f && fy <= 0.52f;
                bool boll2 = Mathf.Abs(b2) <= 0.05f && fy >= 0.28f && fy <= 0.46f;
                if (boll1 || boll2) {
                    float bdx = boll1 ? b1 : b2;
                    c = bodyHi;
                    if (bdx > 0.018f) c = bodyShade;
                    float capY = boll1 ? 0.485f : 0.425f;
                    if (fy >= capY && Mathf.Abs(bdx) <= 0.062f) c = (bdx > 0.018f) ? bodyShade : bodyHi;
                }

                // rope coil wrapped around the first bollard
                for (int r = 0; r < 3; r++) {
                    float ry = 0.300f + r * 0.034f;
                    if (Mathf.Abs(fx - 0.30f) <= 0.060f && fy >= ry && fy <= ry + 0.018f) {
                        c = ropeM; if (fx < 0.285f) c = ropeL; else if (fx > 0.315f) c = ropeD;
                    }
                }
                // slung rope line sagging from bollard to bollard
                if (fx >= 0.30f && fx <= 0.70f) {
                    float t2 = (fx - 0.30f) / 0.40f;
                    float sag = 0.400f - 0.075f * Mathf.Sin(t2 * 3.14159f);
                    if (fy >= sag && fy <= sag + 0.016f) { c = ropeM; if (fx < 0.50f) c = ropeL; else c = ropeD; }
                }

                // timber crane: vertical mast + diagonal boom + hoist line & hook
                if (Mathf.Abs(fx - 0.80f) <= 0.032f && fy >= 0.30f && fy <= 0.92f) {
                    c = woodM; if (fx < 0.786f) c = woodL; else if (fx > 0.814f) c = woodD;
                }
                float boomY = 0.905f - (0.80f - fx) * 0.55f;
                if (fx >= 0.50f && fx <= 0.82f && fy <= boomY && fy >= boomY - 0.05f) {
                    c = woodM; if (fy >= boomY - 0.02f) c = woodL; else c = woodD;
                }
                if (Mathf.Abs(fx - 0.52f) <= 0.008f && fy >= 0.40f && fy <= 0.70f) c = woodD;      // hoist line
                if (Mathf.Abs(fx - 0.52f) <= 0.026f && fy >= 0.375f && fy <= 0.415f) {              // hook block
                    c = woodM; if (fx < 0.52f) c = woodL; else c = woodD;
                }

                px[y * s + x] = c;
            }
            OutlineInPlace(px, s, outline);
            tex.SetPixels(px); tex.Apply();
            _harbourCrane = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _harbourCrane;
        }

        // Shared dark-outline pass: any transparent pixel touching a fully-opaque one becomes outline. Mutates
        // `px` in place. Full-bleed bands (deck rows that span the whole width) have no left/right transparent
        // neighbour, so they get NO side outline — which is what keeps adjacent deck tiles seamless.
        private static void OutlineInPlace(Color[] px, int s, Color outline)
        {
            var outPx = new Color[px.Length];
            System.Array.Copy(px, outPx, px.Length);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                if (px[y * s + x].a > 0.05f) continue;
                bool near = false;
                for (int dy = -1; dy <= 1 && !near; dy++) for (int dx = -1; dx <= 1; dx++) {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                    if (px[ny * s + nx].a > 0.9f) { near = true; break; }
                }
                if (near) outPx[y * s + x] = outline;
            }
            System.Array.Copy(outPx, px, px.Length);
        }
    }
}
