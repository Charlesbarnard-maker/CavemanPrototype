using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Production-driven progression. The ONLY way to advance an age is to craft a special
    /// RESEARCH ITEM (a multi-step factory product — never raw gathering) and deliver it to a
    /// Research Lodge, which converts items into research POINTS. Reaching a tier's point cost
    /// advances the age (which unlocks that age's buildings via the usual `unlockAge` gate).
    ///
    /// One tier per age transition; the list is data (set in GameBootstrap), so adding future
    /// ages is just appending a Tier. Gathering generates ZERO research — only delivered items do.
    /// </summary>
    public static class Research
    {
        public class Tier
        {
            public int targetAge;          // the age this tier unlocks (1 = Tribal, 2 = Bronze, …)
            public ItemDefinition item;    // the research item consumed at this tier
            public int pointsPerItem = 1;  // points each delivered item is worth (rises with tier)
            public int cost;               // total points needed to advance (20 → 50 → 100 → 200)
        }

        public static List<Tier> Tiers = new();

        /// <summary>Points accumulated toward the CURRENT tier (resets to 0 each advance).</summary>
        public static int Points;
        /// <summary>Lifetime research items delivered (a stat / objective hook).</summary>
        public static int TotalDelivered;

        private static int Age => Colony.Instance != null ? Colony.Instance.Age : 0;

        /// <summary>The tier the player is working on now (null once everything is researched).</summary>
        public static Tier Current
        {
            get
            {
                int want = Age + 1;
                foreach (var t in Tiers) if (t.targetAge == want) return t;
                return null;
            }
        }

        public static ItemDefinition CurrentItem => Current != null ? Current.item : null;
        public static int Cost => Current != null ? Current.cost : 0;
        public static int PointsPerItem => Current != null ? Current.pointsPerItem : 0;
        public static bool Complete => Current == null; // all ages researched

        /// <summary>0..1 progress toward the current unlock.</summary>
        public static float Fraction =>
            Current != null && Current.cost > 0 ? Mathf.Clamp01((float)Points / Current.cost) : 1f;

        /// <summary>Items still required at the current per-item value to finish this tier.</summary>
        public static int ItemsRemaining
        {
            get
            {
                var t = Current;
                if (t == null) return 0;
                int ppi = Mathf.Max(1, t.pointsPerItem);
                return Mathf.Max(0, Mathf.CeilToInt((t.cost - Points) / (float)ppi));
            }
        }

        /// <summary>Deliver `count` research items. Only the CURRENT tier's item earns points;
        /// anything else is ignored (returns 0). Returns the count actually accepted.</summary>
        public static int Deliver(ItemDefinition item, int count = 1)
        {
            var t = Current;
            if (t == null || item == null || item != t.item || count <= 0) return 0;
            Points += t.pointsPerItem * count;
            TotalDelivered += count;
            if (Points >= t.cost) Advance(t);
            return count;
        }

        private static void Advance(Tier t)
        {
            Points = 0; // fresh bar for the next tier (overflow is forgiven — keeps it legible)
            Colony.Instance?.ResearchAdvance(t.targetAge);
        }

        /// <summary>Reset on a new game (statics persist across Play sessions in the editor).</summary>
        public static void Reset()
        {
            Tiers = new List<Tier>();
            Points = 0;
            TotalDelivered = 0;
        }
    }
}
