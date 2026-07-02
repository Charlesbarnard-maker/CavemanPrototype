using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Live per-item CONSUMPTION rates for the production stats panel (P). Producers already measure
    /// themselves (ProductionBuilding/WorkshopBuilding.RatePerMin); consumption had no meter, so the
    /// consumers report here — WorkshopBuilding.ConsumeInputs, ResearchBuilding's lodge burn, and
    /// PowerPlant fuel — and the counts roll up into a per-minute rate over a ~4s window (matching
    /// the producers' smoothing). InventoryHud ticks the window once per frame.
    /// </summary>
    public static class ProductionStats
    {
        private static readonly Dictionary<ItemDefinition, int> _window = new();
        private static readonly Dictionary<ItemDefinition, float> _perMin = new();
        private static float _t;

        public static IReadOnlyDictionary<ItemDefinition, float> ConsumedPerMin => _perMin;

        public static void RecordConsumed(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return;
            _window.TryGetValue(item, out int have);
            _window[item] = have + amount;
        }

        /// <summary>Roll the window (call once per frame). Uses scaled time so a paused game reads 0.</summary>
        public static void Tick()
        {
            _t += Time.deltaTime;
            if (_t < 4f) return;
            _perMin.Clear();
            foreach (var kv in _window) _perMin[kv.Key] = kv.Value * (60f / _t);
            _window.Clear();
            _t = 0f;
        }

        public static void Reset() { _window.Clear(); _perMin.Clear(); _t = 0f; }
    }
}
