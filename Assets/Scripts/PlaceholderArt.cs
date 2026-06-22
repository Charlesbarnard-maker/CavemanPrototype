using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Generates simple placeholder sprites in code so we need zero art assets
    /// during prototyping. Shared by everything that needs a quick shape.
    /// </summary>
    public static class PlaceholderArt
    {
        private static Sprite _square;
        private static Sprite _circle;
        private static Sprite _triangle;
        private static Sprite _hexagon;

        public static Sprite Square()
        {
            if (_square != null) return _square;
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _square;
        }

        public static Sprite Circle()
        {
            if (_circle != null) return _circle;
            const int s = 64;
            var tex = NewTex(s);
            var pixels = new Color[s * s];
            float c = (s - 1) / 2f;
            float r = s / 2f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = x - c, dy = y - c;
                    pixels[y * s + x] = (dx * dx + dy * dy <= r * r) ? Color.white : Clear;
                }
            tex.SetPixels(pixels);
            tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _circle;
        }

        /// <summary>An upward-pointing triangle (reads as a pine tree).</summary>
        public static Sprite Triangle()
        {
            if (_triangle != null) return _triangle;
            _triangle = Polygon(new[]
            {
                new Vector2(0.5f, 0.97f),
                new Vector2(0.03f, 0.05f),
                new Vector2(0.97f, 0.05f),
            });
            return _triangle;
        }

        /// <summary>A flat-top hexagon (reads as a rock/boulder).</summary>
        public static Sprite Hexagon()
        {
            if (_hexagon != null) return _hexagon;
            var v = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float a = Mathf.Deg2Rad * (60f * i + 30f);
                v[i] = new Vector2(0.5f + 0.46f * Mathf.Cos(a), 0.5f + 0.46f * Mathf.Sin(a));
            }
            _hexagon = Polygon(v);
            return _hexagon;
        }

        private static readonly Color Clear = new Color(0, 0, 0, 0);

        private static Texture2D NewTex(int s)
        {
            return new Texture2D(s, s) { filterMode = FilterMode.Bilinear };
        }

        // Rasterizes a convex polygon (vertices normalized to [0,1]) into a sprite.
        private static Sprite Polygon(Vector2[] norm)
        {
            const int s = 64;
            var tex = NewTex(s);
            var pts = new Vector2[norm.Length];
            for (int i = 0; i < norm.Length; i++)
                pts[i] = new Vector2(norm[i].x * (s - 1), norm[i].y * (s - 1));

            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    px[y * s + x] = PointInConvex(new Vector2(x, y), pts) ? Color.white : Clear;

            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        private static bool PointInConvex(Vector2 p, Vector2[] poly)
        {
            bool pos = false, neg = false;
            int n = poly.Length;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % n];
                float cross = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
                if (cross > 0) pos = true;
                else if (cross < 0) neg = true;
                if (pos && neg) return false;
            }
            return true;
        }
    }
}
