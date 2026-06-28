// Bespoke procedural art for "Station" (Age 0). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _stationPlatform;
        public static Sprite StationPlatform()
        {
            if (_stationPlatform != null) return _stationPlatform;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline   = new Color(0.09f, 0.07f, 0.05f, 1f);
            // baked timber (deck planks, posts, fascia, sleepers, sign)
            var woodD     = new Color(0.27f, 0.17f, 0.09f, 1f);
            var woodM     = new Color(0.45f, 0.31f, 0.17f, 1f);
            var woodL     = new Color(0.60f, 0.44f, 0.26f, 1f);
            var fascia    = new Color(0.20f, 0.13f, 0.07f, 1f); // dark deck edge / seams
            // grey steel rails (3-tone)
            var railD     = new Color(0.30f, 0.33f, 0.37f, 1f);
            var railM     = new Color(0.48f, 0.51f, 0.56f, 1f);
            var railL     = new Color(0.66f, 0.70f, 0.76f, 1f);
            // muddy-green thatch for the shelter roof (3-tone)
            var roofD     = new Color(0.20f, 0.24f, 0.12f, 1f);
            var roofM     = new Color(0.31f, 0.35f, 0.17f, 1f);
            var roofL     = new Color(0.45f, 0.49f, 0.26f, 1f);
            // structural white body (tinted at runtime)
            var bodyHi    = new Color(1f, 1f, 1f, 1f);
            var body      = new Color(0.84f, 0.84f, 0.86f, 1f); // mid white face
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ===== SLEEPER TIES (very bottom, baked timber, run left->right) =====
                if (fy >= 0.085f && fy <= 0.150f && fx >= 0.04f && fx <= 0.96f) {
                    float tieP = Frac(fx * 9f);
                    if (tieP < 0.55f) {
                        c = woodM;
                        if (tieP < 0.16f) c = woodL;          // lit left edge of each tie
                        else if (tieP > 0.42f) c = woodD;     // shaded right edge of each tie
                        if (fy <= 0.105f) c = woodD;          // shaded underside
                    }
                }

                // ===== TWO STEEL RAILS sitting on the ties =====
                if (fx >= 0.04f && fx <= 0.96f) {
                    // near rail (front, lower on screen)
                    if (fy >= 0.150f && fy <= 0.200f) {
                        c = railM;
                        if (fy >= 0.182f) c = railL;          // top of rail catches light
                        else if (fy <= 0.165f) c = railD;     // underside shadow
                        if (Frac(fx * 30f) < 0.10f) c = railD; // length glints
                    }
                    // far rail (back, slightly higher on screen)
                    if (fy > 0.200f && fy <= 0.250f) {
                        c = railM;
                        if (fy >= 0.232f) c = railL;
                        else if (fy <= 0.215f) c = railD;
                        if (Frac(fx * 30f + 0.5f) < 0.10f) c = railD;
                    }
                }

                // ===== SHORT SUPPORT POSTS under the deck (white body) =====
                bool postA = fx >= 0.150f && fx <= 0.225f && fy >= 0.250f && fy <= 0.345f;
                bool postB = fx >= 0.465f && fx <= 0.535f && fy >= 0.250f && fy <= 0.345f;
                bool postC = fx >= 0.775f && fx <= 0.850f && fy >= 0.250f && fy <= 0.345f;
                if (postA || postB || postC) {
                    float lp = postA ? (fx - 0.150f) / 0.075f
                             : postB ? (fx - 0.465f) / 0.070f
                                     : (fx - 0.775f) / 0.075f;
                    c = (lp < 0.35f) ? bodyHi : (lp > 0.70f ? bodyShade : body);
                }

                // ===== RAISED PLANK DECK (white-body slab, wide) =====
                if (fx >= 0.04f && fx <= 0.96f && fy >= 0.345f && fy <= 0.60f) {
                    if (fy <= 0.405f) {
                        c = fascia;                            // dark fascia along the lower lip
                    } else {
                        float t = (fy - 0.405f) / 0.195f;      // 0 just above fascia .. 1 deck top
                        c = (t > 0.62f) ? bodyHi : (t < 0.22f ? bodyShade : body);
                        if (Frac(fx * 11f) < 0.07f) c = fascia; // vertical plank seams across deck
                        if (fy >= 0.575f) c = bodyHi;          // top decking lip highlight
                    }
                }

                // ===== CENTRE SHELTER: two posts + thatched gable roof =====
                bool sPostL = fx >= 0.400f && fx <= 0.445f && fy >= 0.60f && fy <= 0.78f;
                bool sPostR = fx >= 0.555f && fx <= 0.600f && fy >= 0.60f && fy <= 0.78f;
                if (sPostL || sPostR) {
                    float lp = sPostL ? (fx - 0.400f) / 0.045f : (fx - 0.555f) / 0.045f;
                    c = (lp < 0.40f) ? bodyHi : (lp > 0.75f ? bodyShade : body);
                }

                // thatched gable roof over the shelter (apex at centre, slopes to eaves)
                float roofBase = 0.78f;
                float roofLine = 0.95f - Mathf.Abs(dxc) * 0.70f; // 0.95 at centre -> 0.845 at edges
                if (fx >= 0.35f && fx <= 0.65f && fy >= roofBase && fy <= roofLine) {
                    float t = (roofLine - fy) / 0.17f;         // 0 at eave edge .. 1 at ridge
                    c = (dxc < 0f) ? roofL : roofD;            // left slope lit, right slope dark
                    if (t < 0.22f) c = roofD;                  // eave shadow band
                    else if (Mathf.Abs(dxc) > 0.015f) c = roofM; // mid field of each slope
                    if (t >= 0.80f) c = roofL;                 // ridge highlight
                    if (Frac(fx * 18f) < 0.12f) c = roofD;     // thatch streaks
                    if (Mathf.Abs(dxc) < 0.014f) c = roofD;    // ridge line
                }
                // eave shadow line just under the roof
                if (fx >= 0.35f && fx <= 0.65f && fy >= roofBase - 0.02f && fy < roofBase) c = woodD;

                // ===== HANGING SIGN BOARD between the shelter posts (baked wood) =====
                if (fx >= 0.455f && fx <= 0.545f && fy >= 0.660f && fy <= 0.745f) {
                    c = woodM;
                    if (fx <= 0.478f) c = woodL;               // lit left of board
                    else if (fx >= 0.522f) c = woodD;          // shaded right of board
                    if (fy >= 0.730f || fy <= 0.672f) c = woodD; // top/bottom board frame
                    // one carved horizontal mark across the middle
                    if (fy >= 0.698f && fy <= 0.708f) c = woodD;
                }

                // translucent ground shadow (NOT part of the silhouette)
                if (fy < 0.085f && fy >= 0.05f && fx >= 0.06f && fx <= 0.94f && c == Clear)
                    c = new Color(0.12f, 0.10f, 0.08f, 0.45f);

                px[y * s + x] = c;
            }

            // DARK OUTLINE pass: only fully-opaque pixels count as silhouette
            var outPx = new Color[s * s];
            for (int i = 0; i < px.Length; i++) outPx[i] = px[i];
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
            px = outPx;

            tex.SetPixels(px); tex.Apply();
            _stationPlatform = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _stationPlatform;
        }
    }
}
