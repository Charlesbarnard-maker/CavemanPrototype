using UnityEngine;

namespace Caveman
{
    /// <summary>Shared building status indicator (a coloured dot above the building).</summary>
    public static class Status
    {
        public static readonly Color Working = new Color(0.40f, 0.90f, 0.40f); // green
        public static readonly Color BackedUp = new Color(1.00f, 0.80f, 0.20f); // yellow — output full
        public static readonly Color Starved = new Color(0.92f, 0.30f, 0.30f); // red — no input
        public static readonly Color Idle = new Color(0.55f, 0.55f, 0.55f); // grey — no worker

        public static SpriteRenderer MakeDot(Transform parent)
        {
            var go = new GameObject("status");
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            go.transform.localScale = Vector3.one * 0.28f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Circle();
            sr.sortingOrder = 11;
            return sr;
        }
    }
}
