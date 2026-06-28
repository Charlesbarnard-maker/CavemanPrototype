// Bespoke procedural art for "Iron Mine" (Age 1). Auto-generated partial of PlaceholderArt.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _tribalIronMine;
        public static Sprite TribalIronMine()
        {
            if (_tribalIronMine != null) return _tribalIronMine;
            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

            var outline  = new Color(0.09f, 0.07f, 0.05f, 1f);
            // grey rock hillside (3-tone, baked so the mine reads as natural stone)
            var rockD    = new Color(0.26f, 0.27f, 0.30f, 1f);
            var rockM    = new Color(0.40f, 0.42f, 0.46f, 1f);
            var rockL    = new Color(0.56f, 0.58f, 0.63f, 1f);
            // rusty-brown iron ore veins (clearly browner than copper's orange)
            var oreD     = new Color(0.44f, 0.26f, 0.21f, 1f);
            var oreM     = new Color(0.64f, 0.40f, 0.33f, 1f);
            var oreL     = new Color(0.78f, 0.52f, 0.43f, 1f);
            // dark timber support frame (heavier)
            var woodD    = new Color(0.22f, 0.14f, 0.08f, 1f);
            var woodM    = new Color(0.33f, 0.21f, 0.12f, 1f);
            var woodL    = new Color(0.47f, 0.32f, 0.18f, 1f);
            // shaft mouth darkness
            var shaftD   = new Color(0.06f, 0.05f, 0.06f, 1f);
            var shaftM   = new Color(0.12f, 0.11f, 0.13f, 1f);
            // dark iron-ore lumps at the entrance
            var lumpD    = new Color(0.20f, 0.18f, 0.20f, 1f);
            var lumpM    = new Color(0.31f, 0.29f, 0.31f, 1f);
            var lumpL    = new Color(0.44f, 0.42f, 0.44f, 1f);
            // pick metal head
            var pickD    = new Color(0.30f, 0.31f, 0.34f, 1f);
            var pickM    = new Color(0.48f, 0.50f, 0.54f, 1f);
            var pickL    = new Color(0.66f, 0.69f, 0.74f, 1f);

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                float dxc = fx - 0.5f;
                Color c = Clear;

                // ROCK HILLSIDE: a big mound filling the tile, domed top.
                float hillTop = 0.94f - dxc * dxc * 0.78f;
                if (fy <= hillTop) {
                    float t = hillTop - fy;                 // depth below the dome top
                    c = rockM;
                    // light from upper-left, shadow to lower-right
                    if (dxc < -0.04f && t < 0.32f) c = rockL;
                    else if (dxc > 0.10f || t > 0.55f) c = rockD;
                    // chunky rock facets via deterministic noise
                    float n = Frac(Mathf.Sin(fx * 13.7f + fy * 9.3f) * 43.0f);
                    if (n < 0.16f) c = rockD; else if (n > 0.88f) c = rockL;

                    // RUSTY IRON ORE VEINS (browner, snaking bands)
                    float vein = Mathf.Sin(fx * 8.0f + fy * 14.0f + Mathf.Sin(fy * 6.0f) * 1.6f);
                    if (vein > 0.74f && fy > 0.30f) {
                        c = oreM;
                        if (Frac(Mathf.Sin(fx * 21f + fy * 17f) * 31f) < 0.30f) c = oreD;
                        else if (dxc < 0f) c = oreL;
                    }
                    // a second, lower rust vein cluster on the right
                    float vx = fx - 0.76f, vy = fy - 0.50f;
                    if (Disc(vx, vy, 0.11f)) {
                        float r2 = vx * vx + vy * vy;
                        c = oreM;
                        if (r2 <= 0.05f * 0.05f) c = oreL; else if (r2 >= 0.085f * 0.085f) c = oreD;
                    }
                }

                // SHAFT MOUTH: dark arched opening cut into the hill (centred, low).
                // straight jambs fy 0.07..0.34, then a domed top fy 0.34..0.52.
                float mx = dxc, my = fy - 0.34f;
                bool archBody = Mathf.Abs(mx) <= 0.175f && fy >= 0.07f && fy <= 0.34f;
                bool archTop  = Disc(mx, my, 0.175f) && my >= 0f;
                if (archBody || archTop) {
                    c = shaftM;
                    // depth: darker toward the centre/top-back of the tunnel
                    float d = Mathf.Abs(mx) / 0.175f + Mathf.Max(0f, my) / 0.175f;
                    if (d < 0.9f) c = shaftD;
                    if (fy < 0.10f) c = shaftD; // floor of the shaft in deep shadow
                }

                // HEAVY TIMBER SUPPORT FRAME around the mouth (portal: 2 posts + lintel).
                bool leftPost  = fx >= 0.255f && fx <= 0.345f && fy >= 0.05f && fy <= 0.55f;
                bool rightPost = fx >= 0.655f && fx <= 0.745f && fy >= 0.05f && fy <= 0.55f;
                bool lintel    = fx >= 0.225f && fx <= 0.775f && fy >= 0.55f && fy <= 0.635f;
                if (leftPost || rightPost || lintel) {
                    c = woodM;
                    if (lintel) {
                        if (fy >= 0.605f) c = woodL;          // top of the beam catches light
                        else if (fy <= 0.575f) c = woodD;     // underside in shadow
                        if (Frac(fx * 9f) < 0.10f) c = woodD; // vertical grain seams
                    } else {
                        // left edge of each post is the lit face, right edge shaded
                        bool litFace = leftPost ? (fx <= 0.295f) : (fx <= 0.695f);
                        c = litFace ? woodL : woodD;
                        if (Frac(fy * 11f) < 0.10f) c = woodM; // horizontal grain seams
                    }
                }
                // diagonal cross-brace timber (upper-left post down to the mouth) for a "heavier" frame
                float brace = (fy - 0.555f) + (fx - 0.30f) * 1.15f;
                if (fx >= 0.135f && fx <= 0.345f && fy >= 0.30f && fy <= 0.555f && Mathf.Abs(brace) <= 0.045f) {
                    c = woodM;
                    if (fy > 0.43f) c = woodL; else c = woodD;
                }

                // PICK leaning against the right post: wooden handle + steel head.
                // handle (diagonal shaft from low-left to high-right)
                float ph = (fy - 0.16f) - (fx - 0.76f) * 1.9f;
                if (fx >= 0.74f && fx <= 0.92f && fy >= 0.14f && fy <= 0.52f && Mathf.Abs(ph) <= 0.026f) {
                    c = (fx < 0.82f) ? woodL : woodD;
                }
                // steel pick head (curved double-point crown at the top of the handle)
                float hx = fx - 0.84f, hy = fy - 0.50f;
                if (Disc(hx, hy, 0.105f) && !Disc(hx, hy, 0.066f) && hy > -0.02f) {
                    c = pickM;
                    if (hx + hy < -0.02f) c = pickL; else if (hx - hy > 0.03f) c = pickD;
                }

                // HEAP of dark iron-ore lumps at the entrance (foreground, low fy).
                float heapTop = 0.165f - Mathf.Abs(dxc) * 0.22f;
                if (fy <= heapTop && Mathf.Abs(dxc) <= 0.42f) {
                    c = lumpM;
                    if (fy <= 0.04f) c = lumpD; else if (dxc < -0.04f) c = lumpL;
                }
                // individual chunky lumps for a lumpy silhouette
                if (Disc(fx - 0.34f, fy - 0.085f, 0.060f)) { c = lumpM; if (fx < 0.32f) c = lumpL; else if (fx > 0.36f) c = lumpD; }
                if (Disc(fx - 0.46f, fy - 0.115f, 0.068f)) { c = lumpM; if (fx < 0.44f) c = lumpL; else if (fx > 0.49f) c = lumpD; }
                if (Disc(fx - 0.60f, fy - 0.090f, 0.062f)) { c = lumpM; if (fx < 0.58f) c = lumpL; else if (fx > 0.63f) c = lumpD; }
                // a few rusty lumps in the heap for the iron read
                if (Disc(fx - 0.52f, fy - 0.070f, 0.034f)) { c = oreM; if (fx < 0.50f) c = oreL; else c = oreD; }
                if (Disc(fx - 0.40f, fy - 0.145f, 0.030f)) { c = oreM; if (fx < 0.38f) c = oreL; else c = oreD; }

                // translucent ground shadow (NOT part of the silhouette)
                if (fy < 0.045f && fy >= 0.02f && fx >= 0.06f && fx <= 0.94f && c == Clear)
                    c = new Color(0.10f, 0.08f, 0.07f, 0.45f);

                px[y * s + x] = c;
            }

            // DARK OUTLINE pass: only fully-opaque pixels count as silhouette (translucent shadow ignored).
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
            _tribalIronMine = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _tribalIronMine;
        }
    }
}
