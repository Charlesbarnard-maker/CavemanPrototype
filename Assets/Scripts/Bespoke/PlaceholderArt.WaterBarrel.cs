// Bespoke procedural art for "Water Barrel" (Age 0). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _waterBarrel;
        public static Sprite WaterBarrel()
        {
            if (_waterBarrel != null) return _waterBarrel;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline = new Color(0.09f, 0.07f, 0.05f, 1f);
            var staveD  = new Color(0.27f, 0.16f, 0.08f, 1f); // dark stave seam
            var hoopD   = new Color(0.18f, 0.19f, 0.21f, 1f);
            var hoopM   = new Color(0.30f, 0.32f, 0.35f, 1f);
            var hoopL   = new Color(0.48f, 0.51f, 0.55f, 1f);
            var waterD  = new Color(0.10f, 0.27f, 0.45f, 1f);
            var waterM  = new Color(0.16f, 0.40f, 0.62f, 1f);
            var waterL  = new Color(0.34f, 0.62f, 0.82f, 1f);
            var rimD    = new Color(0.24f, 0.14f, 0.07f, 1f); // dark inner rim of the open top
            var rimM    = new Color(0.38f, 0.24f, 0.12f, 1f); // mid rim
            var rimL    = new Color(0.52f, 0.36f, 0.20f, 1f); // lit rim of the open top
            var bodyHi  = new Color(1f, 1f, 1f, 1f);
            var bodyMid = new Color(0.86f, 0.86f, 0.88f, 1f);
            var bodyShade = new Color(0.66f, 0.66f, 0.68f, 1f);

            // Barrel geometry: centred column, bulged silhouette (widest at mid-height).
            // fy = 0 is the BOTTOM, fy = 1 the TOP. Barrel sits on the ground (low fy).
            const float cxB    = 0.5f;   // centre column
            const float topY   = 0.86f;  // top of the barrel body
            const float botY   = 0.10f;  // bottom of the barrel body (on the ground)
            const float midY   = (topY + botY) * 0.5f; // 0.48
            const float baseHalf = 0.20f; // half-width at top/bottom rims
            const float bulge    = 0.10f; // extra half-width at mid-height
            const float topRX  = 0.22f;  // open-top ellipse radius (x), matches top rim width
            const float topRY  = 0.075f; // open-top ellipse radius (y), shallow squash

            // 3 metal hoop band centres (within the body, evenly spread).
            const float hoopHalf = 0.028f; // half-thickness of a hoop band
            const float hoop0 = 0.225f;    // lower hoop
            const float hoop1 = 0.480f;    // middle hoop
            const float hoop2 = 0.735f;    // upper hoop

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - cxB;
                Color c = Clear;

                // Half-width of the barrel at this height (parabolic bulge, widest at mid).
                // vt = 0 at mid, +/-1 at top/bottom -> halfW peaks at mid-height.
                float vt = (fy - midY) / (midY - botY);
                float halfW = baseHalf + bulge * (1f - vt * vt);

                bool inBarrelBody = fy >= botY && fy <= topY && Mathf.Abs(dxc) <= halfW;

                if (inBarrelBody) {
                    // WHITE structural body: lit on upper-LEFT, shaded on lower-RIGHT.
                    float curve = dxc / Mathf.Max(halfW, 0.0001f); // -1 left .. +1 right
                    c = bodyMid;
                    if (curve < -0.20f) c = bodyHi;        // lit left face
                    else if (curve > 0.34f) c = bodyShade; // right face in shadow

                    // Vertical stave seams (baked thin dark lines following the curve).
                    float staves = Frac((curve * 0.5f + 0.5f) * 7f);
                    if (staves < 0.10f) c = staveD;

                    // 3 metal HOOP bands across the barrel.
                    bool hoop = Mathf.Abs(fy - hoop0) <= hoopHalf
                             || Mathf.Abs(fy - hoop1) <= hoopHalf
                             || Mathf.Abs(fy - hoop2) <= hoopHalf;
                    if (hoop) {
                        c = hoopM;
                        if (curve < -0.20f) c = hoopL;       // lit left
                        else if (curve > 0.34f) c = hoopD;   // shaded right
                        // rivet ticks repeated across the band
                        if (Frac((curve * 0.5f + 0.5f) * 5f) < 0.10f) c = hoopD;
                    }

                    // Darken the very left/right silhouette edges so the body reads as round.
                    if (Mathf.Abs(dxc) >= halfW - 0.018f && c != staveD) {
                        if (curve > 0f) c = bodyShade;
                    }
                }

                // OPEN TOP: shallow ellipse of blue water ringed by a wooden rim.
                // Centre of the mouth sits at the top of the body.
                float ex = dxc / topRX;
                float ey = (fy - topY) / topRY;
                float er = Mathf.Sqrt(ex * ex + ey * ey); // 1.0 at the ellipse edge
                if (er <= 1f) {
                    if (er >= 0.78f) {
                        // Wooden rim of the cask mouth: lit on the upper-left arc.
                        c = rimM;
                        if (dxc < -0.02f && ey > -0.2f) c = rimL; // upper-left lit
                        else if (dxc > 0.04f || ey < -0.4f) c = rimD; // far/lower darker
                    } else {
                        // Blue water surface, lit toward the upper-left.
                        c = waterM;
                        if (dxc < -0.03f && ey > -0.1f) c = waterL; // upper-left sheen
                        else if (dxc > 0.06f) c = waterD;           // far side darker
                        // Small bright highlight glint (upper-left of the disc).
                        if (Disc(dxc + 0.055f, fy - (topY + 0.022f), 0.035f)) c = waterL;
                    }
                }

                // Spilled-water sheen pooled on the ground at the base (translucent).
                // Widest at centre, tapering out; only on empty pixels so it doesn't
                // overwrite the barrel body.
                float pool = 0.085f - Mathf.Abs(dxc) * 0.26f;
                if (c == Clear && fy >= 0.035f && fy <= pool && Mathf.Abs(dxc) <= 0.32f) {
                    c = new Color(0.22f, 0.46f, 0.64f, 0.6f);
                    if (dxc < -0.05f) c = new Color(0.34f, 0.60f, 0.78f, 0.6f);
                }

                px[y * s + x] = c;
            }

            // DARK OUTLINE pass: only fully-opaque pixels count as silhouette.
            // Skip translucent pool pixels (a <= 0.9) when testing neighbours so the
            // ground sheen is not outlined.
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
            _waterBarrel = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _waterBarrel;
        }
    }
}
