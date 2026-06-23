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

        public static int Available(ItemDefinition item, Inventory carried)
        {
            if (item == null) return 0;
            int total = carried != null ? carried.Count(item) : 0;
            foreach (var s in StorageBuilding.All) total += s.Store.Count(item);
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
            foreach (var p in ProductionBuilding.All) AddAll(p.Buffer);
            foreach (var w in WorkshopBuilding.All) AddAll(w.Buffer);
            return totals;
        }

        // ---- Food (supports multiple food types; cooked food is worth more) ----

        private static int FoodPointsIn(Inventory inv)
        {
            if (inv == null) return 0;
            int pts = 0;
            foreach (var kv in inv.Items)
                if (kv.Key != null && kv.Key.foodValue > 0) pts += kv.Value * kv.Key.foodValue;
            return pts;
        }

        /// <summary>Total nourishment available across the pool (units × foodValue).</summary>
        public static int FoodPoints(Inventory carried)
        {
            int pts = FoodPointsIn(carried);
            foreach (var s in StorageBuilding.All) pts += FoodPointsIn(s.Store);
            foreach (var p in ProductionBuilding.All) pts += FoodPointsIn(p.Buffer);
            foreach (var w in WorkshopBuilding.All) pts += FoodPointsIn(w.Buffer);
            return pts;
        }

        /// <summary>Eat up to `points` of nourishment, spending best (highest-value) food first.
        /// Returns the nourishment actually consumed.</summary>
        public static int SpendFoodPoints(int points, Inventory carried)
        {
            if (points <= 0) return 0;

            var foods = new List<ItemDefinition>();
            foreach (var kv in Totals(carried))
                if (kv.Key != null && kv.Key.foodValue > 0) foods.Add(kv.Key);
            foods.Sort((a, b) => b.foodValue.CompareTo(a.foodValue));

            int covered = 0;
            foreach (var f in foods)
            {
                if (covered >= points) break;
                int unitsNeeded = Mathf.CeilToInt((points - covered) / (float)f.foodValue);
                int spent = SpendUpTo(f, unitsNeeded, carried);
                covered += spent * f.foodValue;
            }
            return covered;
        }
    }
}
