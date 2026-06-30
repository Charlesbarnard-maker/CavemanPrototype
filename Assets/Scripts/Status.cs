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

        /// <summary>Set the dot colour, and make PROBLEM states (starved/backed-up) clearly
        /// bigger AND pulsing so bottlenecks jump out at a glance — not a subtle tint.</summary>
        public static void Apply(SpriteRenderer dot, Color col)
        {
            if (dot == null) return;
            dot.color = col;
            bool problem = col == Starved || col == BackedUp;
            float baseScale = problem ? 0.44f : 0.30f;                       // problems noticeably larger
            float pulse = problem ? 1f + 0.30f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
            dot.transform.localScale = Vector3.one * (baseScale * pulse);
        }

        // ---- "Can upgrade now" badge: a green ⬆ that bobs over a building you can afford to upgrade. ----
        public static SpriteRenderer MakeUpgradeBadge(Transform parent)
        {
            var go = new GameObject("upgradeBadge");
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(0.46f, 0.72f, 0f); // upper-right, beside the status dot
            go.transform.localScale = Vector3.one * 0.4f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.UpgradeArrow();
            sr.sortingOrder = 12; // above the status dot (11)
            sr.enabled = false;
            return sr;
        }

        /// <summary>Show/hide the upgrade badge; while shown it bobs + pulses to draw the eye.</summary>
        public static void ApplyUpgradeBadge(SpriteRenderer badge, bool show)
        {
            if (badge == null) return;
            badge.enabled = show;
            if (!show) return;
            float t = Time.unscaledTime * 4f;
            badge.transform.localScale = Vector3.one * (0.4f * (1f + 0.14f * Mathf.Sin(t)));
            var c = badge.color; c.a = 0.8f + 0.2f * Mathf.Sin(t); badge.color = c;
        }
    }
}
