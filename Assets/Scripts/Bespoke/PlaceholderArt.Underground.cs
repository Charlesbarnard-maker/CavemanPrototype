// Procedural art for the UNDERGROUND BELT ends. The belt's local forward is +Y (the GameObject rotates by
// Belt.Angle(dir)), so the sprite is drawn facing UP. An ENTRANCE has the dark tunnel mouth at the FORWARD
// (top) edge — items dive underground there; an EXIT has the mouth at the BACK (bottom) edge — items emerge.
// A bright forward arrow shows flow direction either way. 64x64, baked full colour.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        private static Sprite _ugEntry, _ugExit;

        /// <summary>The underground-belt end sprite — entrance (mouth ahead) or exit (mouth behind).</summary>
        public static Sprite UndergroundBelt(bool exit)
        {
            if (exit && _ugExit != null) return _ugExit;
            if (!exit && _ugEntry != null) return _ugEntry;

            const int s = 64; var px = new Color[s * s];
            var deck = new Color(0.30f, 0.28f, 0.34f, 1f);     // slate deck (distinct from the brown surface belt)
            var roller = new Color(0.42f, 0.40f, 0.48f, 1f);
            var mouth = new Color(0.05f, 0.05f, 0.07f, 1f);     // black tunnel opening
            var rim = new Color(0.62f, 0.58f, 0.40f, 1f);       // lip around the mouth
            var arrow = new Color(0.96f, 0.82f, 0.30f, 1f);     // flow arrow

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = deck;
                    if (Frac(fy * 8f) < 0.22f) c = roller; // roller stripes across the lane

                    // Tunnel mouth band at the forward edge (entrance) or back edge (exit).
                    float md = exit ? fy : 1f - fy;          // distance INTO the cell from the mouth edge
                    if (md < 0.26f)
                    {
                        c = mouth;
                        if (md > 0.20f) c = rim;             // a lighter lip just inside the cell
                        if (Mathf.Abs(fx - 0.5f) > 0.40f) c = deck; // leave the corners as deck
                    }

                    // Forward (up) arrow in the deck area away from the mouth.
                    float ay = exit ? fy - 0.34f : 0.66f - fy; // arrow sits toward the non-mouth half
                    if (ay >= 0f && ay <= 0.22f)
                    {
                        float hw = 0.18f * (1f - ay / 0.22f);
                        if (Mathf.Abs(fx - 0.5f) <= hw && Mathf.Abs(fx - 0.5f) >= hw - 0.06f) c = arrow;
                    }

                    px[y * s + x] = c;
                }

            var sp = Finish(px, s);
            if (exit) _ugExit = sp; else _ugEntry = sp;
            return sp;
        }
    }
}
