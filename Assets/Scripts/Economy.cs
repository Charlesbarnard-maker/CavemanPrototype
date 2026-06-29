using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// The combined resource pool: the player's carried inventory plus every
    /// building buffer, storage, and workshop output in the world. Build costs and
    /// workshop inputs are checked/spent against this total.
    /// </summary>
    public static class Economy
    {
        /// <summary>Sandbox: building/age costs are free and instant.</summary>
        public static bool FreeBuild;

        /// <summary>
        /// When true, a workshop may only consume inputs that are PHYSICALLY present —
        /// belt-delivered (its InBuffer) or in an adjacent storage/collector/workshop —
        /// NOT the global pool. This is what makes logistics matter and bottlenecks
        /// cascade. Survival (food/water/comfort) and build costs still use the pool.
        /// Toggle with F7 to compare the old "everything teleports" behaviour.
        /// </summary>
        public static bool LocalProduction = true;

        /// <summary>
        /// "Stored, not summoned": the usable settlement pool is the player's CARRIED stock +
        /// STORAGES only — NOT raw field collector/workshop output buffers. Field production must
        /// be DELIVERED (belt / pipe / worker-carried liquids) into storage before survival,
        /// comfort, and build costs can use it. This makes logistics matter for every system, not
        /// just workshops. Toggle with F9 to compare the old "everything counts everywhere" pool.
        /// </summary>
        public static bool StoredOnly = true;

        public static int Available(ItemDefinition item, Inventory carried)
        {
            if (item == null) return 0;
            int total = carried != null ? carried.Count(item) : 0;
            foreach (var s in StorageBuilding.All) total += s.Store.Count(item);
            if (StoredOnly) return total;
            foreach (var p in ProductionBuilding.All) total += p.Buffer.Count(item);
            foreach (var w in WorkshopBuilding.All) total += w.Buffer.Count(item);
            return total;
        }

        public static bool CanAfford(List<ItemAmount> cost, Inventory carried)
        {
            if (FreeBuild || cost == null) return true;
            foreach (var c in cost)
                if (c.item == null || Available(c.item, carried) < c.amount) return false;
            return true;
        }

        public static void Spend(List<ItemAmount> cost, Inventory carried)
        {
            if (FreeBuild || cost == null) return;
            foreach (var c in cost) SpendUpTo(c.item, c.amount, carried);
        }

        /// <summary>Spends up to `amount` from the pool; returns how many were actually spent.</summary>
        public static int SpendUpTo(ItemDefinition item, int amount, Inventory carried)
        {
            if (item == null || amount <= 0) return 0;
            int remaining = amount;
            if (carried != null) remaining -= carried.RemoveUpTo(item, remaining);
            if (remaining <= 0) return amount;
            foreach (var s in StorageBuilding.All)
            {
                remaining -= s.Store.RemoveUpTo(item, remaining);
                if (remaining <= 0) return amount;
            }
            if (StoredOnly) return amount - remaining; // only carried + storages are usable
            foreach (var p in ProductionBuilding.All)
            {
                remaining -= p.Buffer.RemoveUpTo(item, remaining);
                if (remaining <= 0) return amount;
            }
            foreach (var w in WorkshopBuilding.All)
            {
                remaining -= w.Buffer.RemoveUpTo(item, remaining);
                if (remaining <= 0) return amount;
            }
            return amount - remaining;
        }

        /// <summary>Combined totals of every resource across carried + all buildings.</summary>
        public static Dictionary<ItemDefinition, int> Totals(Inventory carried)
        {
            var totals = new Dictionary<ItemDefinition, int>();

            void AddAll(Inventory inv)
            {
                if (inv == null) return;
                foreach (var kv in inv.Items)
                {
                    totals.TryGetValue(kv.Key, out int v);
                    totals[kv.Key] = v + kv.Value;
                }
            }

            AddAll(carried);
            foreach (var s in StorageBuilding.All) AddAll(s.Store);
            if (!StoredOnly)
            {
                foreach (var p in ProductionBuilding.All) AddAll(p.Buffer);
                foreach (var w in WorkshopBuilding.All) AddAll(w.Buffer);
            }
            return totals;
        }

    }
}
