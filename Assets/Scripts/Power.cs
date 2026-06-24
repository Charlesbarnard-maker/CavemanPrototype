using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Global electrical power — the Industrial age's defining constraint. From the
    /// Industrial age on, machines (workshops) DRAW power and Power Plants SUPPLY it.
    /// If demand outruns supply the whole grid browns out, scaling every machine's
    /// speed down toward <see cref="BrownoutFloor"/>. So "just add another machine"
    /// stops being free — you must grow generation (and its fuel supply) to match.
    ///
    /// Computed once per frame, lazily, guarded by frame count so update order between
    /// the HUD, workshops and plants doesn't matter.
    /// </summary>
    public static class Power
    {
        public const int IndustrialAge = 4; // index into Colony.AgeNames (Stone..Industrial)
        public const float BrownoutFloor = 0.35f; // worst-case machine slowdown at zero supply

        /// <summary>True once the colony has reached the Industrial age (power matters).</summary>
        public static bool Active { get; private set; }
        public static float Generation { get; private set; }
        public static float Demand { get; private set; }
        /// <summary>0.35..1 — multiplier applied to machine speed (1 = fully powered).</summary>
        public static float Factor { get; private set; } = 1f;

        private static int _frame = -1;

        public static void EnsureFresh()
        {
            if (_frame == Time.frameCount) return;
            _frame = Time.frameCount;

            Active = Colony.Instance != null && Colony.Instance.Age >= IndustrialAge;

            float gen = 0f;
            foreach (var p in PowerPlant.All) if (p != null) gen += p.CurrentOutput;

            float dem = 0f;
            foreach (var w in WorkshopBuilding.All) if (w != null && w.DrawsPower) dem += w.PowerDraw;

            Generation = gen;
            Demand = dem;
            Factor = (!Active || dem <= 0f) ? 1f : Mathf.Clamp(gen / dem, BrownoutFloor, 1f);
        }
    }
}
