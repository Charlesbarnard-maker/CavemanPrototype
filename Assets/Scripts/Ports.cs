using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Building I/O ports. Step A: a single OUTPUT side per producer/processor — belts can
    /// only pull a building's output from that side, so you must orient buildings to route
    /// goods (R rotates during placement). Visualised as a green arrow on the output edge.
    /// (Per-input ports come in a later step.)
    /// </summary>
    public static class Ports
    {
        /// <summary>Place PER-CELL port markers along the building's edges so they line up with the
        /// belt grid: one marker per edge cell (a 2×2 warehouse → 2 inputs + 2 outputs), aligned to
        /// each cell's outer face. Output = green arrow on `outputSide`; input = cyan notch opposite.</summary>
        public static void PlacePorts(Transform t, int w, int h, Belt.Dir outputSide, bool hasIn, bool hasOut)
        {
            if (hasOut) PlaceSide(t, w, h, outputSide, true);
            if (hasIn) PlaceSide(t, w, h, Belt.Opposite(outputSide), false);
        }

        private static void PlaceSide(Transform t, int w, int h, Belt.Dir side, bool isOutput)
        {
            float ps = Mathf.Max(0.01f, t.localScale.x); // building scale (square footprints → uniform)
            var step = Belt.Step(side);
            Vector3 c = t.position;
            float ax = c.x - (w - 1) * 0.5f, ay = c.y - (h - 1) * 0.5f; // bottom-left cell centre
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                {
                    bool edge = side == Belt.Dir.E ? i == w - 1 : side == Belt.Dir.W ? i == 0
                              : side == Belt.Dir.N ? j == h - 1 : j == 0;
                    if (!edge) continue;
                    var go = new GameObject(isOutput ? "outport" : "inport");
                    go.transform.SetParent(t);
                    go.transform.position = new Vector3(ax + i + step.x * 0.55f, ay + j + step.y * 0.55f, 0f);
                    go.transform.localScale = Vector3.one * (0.3f / ps);
                    if (isOutput) go.transform.localRotation = Quaternion.Euler(0f, 0f, Belt.Angle(side));
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = isOutput ? PlaceholderArt.Triangle() : PlaceholderArt.Square();
                    sr.color = isOutput ? new Color(0.25f, 0.95f, 0.35f, 0.95f) : new Color(0.35f, 0.70f, 1f, 0.95f);
                    sr.sortingOrder = 11;
                }
        }

        /// <summary>A small green arrow on the building's output edge, pointing outward.</summary>
        public static SpriteRenderer MakeOutputArrow(Transform parent, Belt.Dir side)
        {
            var go = new GameObject("outport");
            go.transform.SetParent(parent);
            var s = Belt.Step(side);
            // localPosition is in parent space (parent scale = footprint), so 0.5 sits on the edge.
            go.transform.localPosition = new Vector3(s.x, s.y, 0f) * 0.5f;
            go.transform.localScale = Vector3.one * 0.3f;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, Belt.Angle(side));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Triangle();
            sr.color = new Color(0.25f, 0.95f, 0.35f, 0.95f);
            sr.sortingOrder = 11;
            return sr;
        }

        /// <summary>A small cyan square on the building's input edge (belts deliver here).</summary>
        public static SpriteRenderer MakeInputNotch(Transform parent, Belt.Dir side)
        {
            var go = new GameObject("inport");
            go.transform.SetParent(parent);
            var s = Belt.Step(side);
            go.transform.localPosition = new Vector3(s.x, s.y, 0f) * 0.5f;
            go.transform.localScale = Vector3.one * 0.26f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = new Color(0.35f, 0.70f, 1f, 0.95f); // cyan = input
            sr.sortingOrder = 11;
            return sr;
        }
    }
}
