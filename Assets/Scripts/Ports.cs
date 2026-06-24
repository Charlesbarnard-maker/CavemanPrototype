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
    }
}
