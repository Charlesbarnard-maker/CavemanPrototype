using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// The combined resource pool: the player's carried inventory plus every
    /// building buffer and storage in the world. Build costs are checked and
    /// spent against this total, so anything you've gathered (wherever it sits)
    /// counts toward what you can build.
    /// </summary>
    public static class Economy
    {
        public static int Available(ItemDefinition item, Inventory carried)
        {
            if (item == null) return 0;
            int total = carried != null ? carried.Count(item) : 0;
            foreach (var s in Object.FindObjectsByType<StorageBuilding>())
                total += s.Store.Count(item);
            foreach (var p in Object.FindObjectsByType<ProductionBuilding>())
                total += p.Buffer.Count(item);
            return total;
        }

        public static bool CanAfford(List<ItemAmount> cost, Inventory carried)
        {
            if (cost == null) return true;
            foreach (var c in cost)
                if (c.item == null || Available(c.item, carried) < c.amount) return false;
            return true;
        }

        public static void Spend(List<ItemAmount> cost, Inventory carried)
        {
            if (cost == null) return;
            foreach (var c in cost) SpendItem(c.item, c.amount, carried);
        }

        private static void SpendItem(ItemDefinition item, int amount, Inventory carried)
        {
            if (item == null || amount <= 0) return;

            int remaining = amount;
            if (carried != null) remaining -= carried.RemoveUpTo(item, remaining);
            if (remaining <= 0) return;

            foreach (var s in Object.FindObjectsByType<StorageBuilding>())
            {
                remaining -= s.Store.RemoveUpTo(item, remaining);
                if (remaining <= 0) return;
            }
            foreach (var p in Object.FindObjectsByType<ProductionBuilding>())
            {
                remaining -= p.Buffer.RemoveUpTo(item, remaining);
                if (remaining <= 0) return;
            }
        }

        /// <summary>Spends up to `amount` from the pool; returns how many were actually spent.</summary>
        public static int SpendUpTo(ItemDefinition item, int amount, Inventory carried)
        {
            if (item == null || amount <= 0) return 0;
            int remaining = amount;
            if (carried != null) remaining -= carried.RemoveUpTo(item, remaining);
            if (remaining <= 0) return amount;
            foreach (var s in Object.FindObjectsByType<StorageBuilding>())
            {
                remaining -= s.Store.RemoveUpTo(item, remaining);
                if (remaining <= 0) return amount;
            }
            foreach (var p in Object.FindObjectsByType<ProductionBuilding>())
            {
                remaining -= p.Buffer.RemoveUpTo(item, remaining);
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
            foreach (var s in Object.FindObjectsByType<StorageBuilding>()) AddAll(s.Store);
            foreach (var p in Object.FindObjectsByType<ProductionBuilding>()) AddAll(p.Buffer);
            return totals;
        }
    }
}
