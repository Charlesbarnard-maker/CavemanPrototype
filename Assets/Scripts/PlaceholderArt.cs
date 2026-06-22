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
            var tex = new Texture2D(s, s);
            var pixels = new Color[s * s];
            float c = (s - 1) / 2f;
            float r = s / 2f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = x - c, dy = y - c;
                    pixels[y * s + x] = (dx * dx + dy * dy <= r * r) ? Color.white : new Color(0, 0, 0, 0);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _circle;
        }
    }
}
