using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Production-driven progression with a SPENDABLE research tree.
    ///
    /// Crafted RESEARCH ITEMS (never raw gathering) delivered to a Research Lodge add to a research
    /// POINT pool. The player then SPENDS points in the research tree (press T) on Tech nodes that
    /// advance the age and/or unlock buildings. Nothing auto-unlocks — you choose what to research.
    ///
    /// Two data lists, set in GameBootstrap:
    ///  • Tiers — which research item the Lodge consumes at each age, and the points each is worth.
    ///  • Tree  — the Tech nodes you spend points on (age advances + building unlocks), with prereqs.
    /// Both are data, so future ages/branches are just more entries.
    /// </summary>
    public static class Research
    {
        // What the Lodge CONSUMES per age (the item you must be crafting now) and its point value.
        public class Tier { public int targetAge; public ItemDefinition item; public int pointsPerItem = 1; }
        public static List<Tier> Tiers = new();

        // What you SPEND points on.
        public class Tech
        {
            public string id, name, desc;
            public int cost;
            public string prereq;                // id of a required tech, or null/"" for none
            public int advanceToAge = -1;        // if ≥0, buying advances the colony to this age
            public List<BuildingDefinition> unlocks = new(); // buildings this tech enables
            public bool purchased;
            // STRUCTURAL GATE: buildings that must be BUILT in the world before this node can be bought
            // (so advancing an age REQUIRES building that age's new processing chain, not just points).
            public List<BuildingDefinition> requiredBuildings = new();
            // ...and a SPECIAL ITEM that must actually be CRAFTED + delivered N times (so you can't fund
            // an age with stockpiled cheap items — you must run that age's new chain). 0 = no item gate.
            public ItemDefinition gateItem;
            public int gateItemCount;
        }
        public static List<Tech> Tree = new();

        public static int Points;          // spendable pool
        public static int TotalDelivered;  // lifetime research items delivered (stat/objective)
        // Lifetime count delivered PER item — drives the per-age "deliver N of the special item" gate.
        public static Dictionary<ItemDefinition, int> DeliveredByItem = new();
        public static int DeliveredOf(ItemDefinition i) => i != null && DeliveredByItem.TryGetValue(i, out var c) ? c : 0;

        private static int Age => Colony.Instance != null ? Colony.Instance.Age : 0;

        // --- What the Lodge wants right now (the item for the NEXT age) ---
        public static Tier CurrentTier
        {
            get { int want = Age + 1; foreach (var t in Tiers) if (t.targetAge == want) return t; return null; }
        }
        public static ItemDefinition CurrentItem => CurrentTier != null ? CurrentTier.item : null;
        public static int PointsPerItem => CurrentTier != null ? CurrentTier.pointsPerItem : 0;

        // --- The next age node (what advancing costs) — for the headline progress readout ---
        public static Tech NextAgeTech
        {
            get { int want = Age + 1; foreach (var n in Tree) if (n.advanceToAge == want) return n; return null; }
        }
        public static int Cost => NextAgeTech != null ? NextAgeTech.cost : 0;
        public static float Fraction => Cost > 0 ? Mathf.Clamp01((float)Points / Cost) : 1f;
        public static bool Complete => NextAgeTech == null; // no further age to research

        /// <summary>Research items still needed to AFFORD the next age node, at the current value.</summary>
        public static int ItemsRemaining
        {
            get
            {
                var t = CurrentTier; var n = NextAgeTech;
                if (t == null || n == null) return 0;
                int ppi = Mathf.Max(1, t.pointsPerItem);
                return Mathf.Max(0, Mathf.CeilToInt((n.cost - Points) / (float)ppi));
            }
        }

        // --- Delivery (points IN) ---
        /// <summary>Is this an item the Lodge converts to points (ANY tier's item, not just the
        /// current age's)? Lets you keep earning points for building-unlock nodes and at the final
        /// age — fixes the soft-lock where only the next age's item was accepted.</summary>
        public static bool IsResearchItem(ItemDefinition item)
        {
            if (item == null) return false;
            foreach (var t in Tiers) if (t.item == item) return true;
            return false;
        }

        /// <summary>Points awarded per unit of this research item (0 if it isn't a research item).</summary>
        public static int PointsFor(ItemDefinition item)
        {
            if (item != null) foreach (var t in Tiers) if (t.item == item) return Mathf.Max(1, t.pointsPerItem);
            return 0;
        }

        /// <summary>Every tech node purchased — nothing left to research.</summary>
        public static bool AllResearched { get { foreach (var n in Tree) if (!n.purchased) return false; return true; } }

        /// <summary>Deliver `count` research items — ANY tier's item earns its point value, so you can
        /// always fund building-unlock nodes (and keep earning at the final age).</summary>
        public static int Deliver(ItemDefinition item, int count = 1)
        {
            int ppi = PointsFor(item);
            if (ppi <= 0 || count <= 0) return 0;
            Points += ppi * count;
            TotalDelivered += count;
            DeliveredByItem.TryGetValue(item, out var have);
            DeliveredByItem[item] = have + count; // track per-item for the age gate
            return count;
        }

        // --- Tree (points OUT) ---
        public static Tech Node(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var n in Tree) if (n.id == id) return n;
            return null;
        }
        public static bool IsPurchased(string id)
        {
            if (string.IsNullOrEmpty(id)) return true; // no requirement = always satisfied
            var n = Node(id);
            return n != null && n.purchased;
        }
        public static bool PrereqMet(Tech n) => n != null && (string.IsNullOrEmpty(n.prereq) || IsPurchased(n.prereq));

        /// <summary>Is at least one instance of `def` built in the world? (the structural-gate check)</summary>
        public static bool HasBuilding(BuildingDefinition def)
        {
            if (def == null) return true;
            foreach (var w in WorkshopBuilding.All) if (w != null && w.def == def) return true;
            foreach (var p in ProductionBuilding.All) if (p != null && p.def == def) return true;
            foreach (var r in ResearchBuilding.All) if (r != null && r.def == def) return true;
            foreach (var s in StorageBuilding.All) if (s != null && s.def == def) return true;
            return false;
        }

        /// <summary>All of this tech's structural requirements met: every required building exists AND
        /// the gate item has been delivered enough times (true if none required).</summary>
        public static bool RequirementsMet(Tech n)
        {
            if (n == null) return false;
            if (n.requiredBuildings != null)
                foreach (var d in n.requiredBuildings) if (!HasBuilding(d)) return false;
            if (n.gateItem != null && DeliveredOf(n.gateItem) < n.gateItemCount) return false;
            return true;
        }

        /// <summary>The unmet structural requirements as readable text (buildings to build + the special
        /// item still owed), for the locked-node UI. Empty when everything's satisfied.</summary>
        public static string MissingRequirementsText(Tech n)
        {
            if (n == null) return "";
            var parts = new List<string>();
            if (n.requiredBuildings != null)
                foreach (var d in n.requiredBuildings)
                    if (d != null && !HasBuilding(d)) parts.Add($"build {d.displayName}");
            if (n.gateItem != null && DeliveredOf(n.gateItem) < n.gateItemCount)
                parts.Add($"deliver {n.gateItem.displayName} {DeliveredOf(n.gateItem)}/{n.gateItemCount}");
            return string.Join(" · ", parts);
        }

        public static bool CanBuy(Tech n) => n != null && !n.purchased && PrereqMet(n) && RequirementsMet(n) && Points >= n.cost;

        public static bool Buy(Tech n)
        {
            if (!CanBuy(n)) return false;
            Points -= n.cost;
            n.purchased = true;
            if (n.advanceToAge >= 0) Colony.Instance?.ResearchAdvance(n.advanceToAge);
            return true;
        }

        /// <summary>Reset on a new game (statics persist across Play sessions in the editor).</summary>
        public static void Reset()
        {
            Tiers = new List<Tier>();
            Tree = new List<Tech>();
            Points = 0;
            TotalDelivered = 0;
            DeliveredByItem = new Dictionary<ItemDefinition, int>();
        }
    }
}
