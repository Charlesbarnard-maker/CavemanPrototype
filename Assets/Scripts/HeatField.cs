using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Stone-age RADIUS power: lit Hearths project a heat field, and a machine that requiresPower runs
    /// only when inside one. NO grid, NO wires — pure proximity (mirrors the collector's distance test).
    /// This is the pre-Industrial branch of <see cref="WorkshopBuilding.EffectivePowerFactor"/>; at the
    /// Industrial age the existing <see cref="Power"/> (electricity) supersedes it through that SAME
    /// accessor, so energy evolves radius → steam → electricity with no machine-code change.
    /// </summary>
    public static class HeatField
    {
        public static readonly List<Hearth> All = new();
        public static void Register(Hearth h) { if (h != null && !All.Contains(h)) All.Add(h); }
        public static void Unregister(Hearth h) { if (h != null) All.Remove(h); }

        /// <summary>Hard-stop model (player's choice): 1 inside ANY lit hearth's radius, else 0 (the
        /// machine stalls). Cheap O(hearths) scan — there are only a handful on a map.</summary>
        public static float FactorAt(Vector3 pos)
        {
            for (int i = 0; i < All.Count; i++)
            {
                var h = All[i];
                if (h == null || !h.Lit) continue;
                float r = h.Radius;
                if (((Vector2)(h.transform.position - pos)).sqrMagnitude <= r * r) return 1f;
            }
            return 0f;
        }

        public static bool Covered(Vector3 pos) => FactorAt(pos) >= 0.5f;
    }
}
