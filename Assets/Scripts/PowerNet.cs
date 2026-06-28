using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Spatial POWER NETWORK: generators + power poles are nodes; two nodes link when within range, so
    /// they form connected networks. Each network's total generation (fuelled generators on it) is
    /// shared by the consumers it supplies (machines within a node's SupplyRange). A consumer runs at
    /// full speed if its network has enough generation, browns out proportionally if oversubscribed,
    /// and STOPS if it isn't connected to any powered network. This is the single energy model across
    /// ages (better generators/poles later) and is read through WorkshopBuilding.EffectivePowerFactor.
    ///
    /// Rebuilt at most once per frame (frame-guard) — cheap union-find over the handful of nodes.
    /// </summary>
    public static class PowerNet
    {
        public const float BrownoutFloor = 0.35f; // most a network slows an oversubscribed machine

        private static int _frame = -1;
        private static readonly Dictionary<WorkshopBuilding, float> _factor = new();
        public static float TotalGen { get; private set; }
        public static float TotalDemand { get; private set; }

        /// <summary>Power factor for a consumer: 1 connected+supplied, &lt;1 if its network is
        /// oversubscribed, 0 if not connected to a powered network. Drives the machine's run speed.</summary>
        public static float FactorOf(WorkshopBuilding w)
        {
            EnsureFresh();
            return (w != null && _factor.TryGetValue(w, out var f)) ? f : 0f;
        }

        public static void EnsureFresh()
        {
            if (_frame == Time.frameCount) return;
            _frame = Time.frameCount;
            Rebuild();
        }

        // Node arrays (reused each rebuild). Generators first, then poles.
        private static readonly List<Vector2> _pos = new();
        private static readonly List<float> _connect = new();
        private static readonly List<float> _supply = new();
        private static readonly List<float> _gen = new();
        private static int[] _parent = System.Array.Empty<int>();

        private static int Find(int x) { while (_parent[x] != x) { _parent[x] = _parent[_parent[x]]; x = _parent[x]; } return x; }
        private static void Union(int a, int b) { int ra = Find(a), rb = Find(b); if (ra != rb) _parent[ra] = rb; }

        private static void Rebuild()
        {
            _pos.Clear(); _connect.Clear(); _supply.Clear(); _gen.Clear();

            // Generators are nodes that ALSO produce power.
            var gens = PowerPlant.All;
            for (int i = 0; i < gens.Count; i++)
            {
                var p = gens[i]; if (p == null) continue;
                _pos.Add(p.transform.position); _connect.Add(p.ConnectRange); _supply.Add(p.SupplyRange); _gen.Add(p.CurrentOutput);
            }
            // Poles are relay-only nodes.
            var poles = PowerPole.All;
            for (int i = 0; i < poles.Count; i++)
            {
                var p = poles[i]; if (p == null) continue;
                _pos.Add(p.transform.position); _connect.Add(p.ConnectRange); _supply.Add(p.SupplyRange); _gen.Add(0f);
            }

            int n = _pos.Count;
            _factor.Clear(); TotalGen = 0f; TotalDemand = 0f;
            if (n == 0) return;

            if (_parent.Length < n) _parent = new int[n];
            for (int i = 0; i < n; i++) _parent[i] = i;
            // Link nodes within the smaller of their two connect ranges (both must reach).
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    float r = Mathf.Min(_connect[i], _connect[j]);
                    if ((_pos[i] - _pos[j]).sqrMagnitude <= r * r) Union(i, j);
                }

            // Per-network generation.
            var compGen = new Dictionary<int, float>();
            for (int i = 0; i < n; i++) { int r = Find(i); compGen.TryGetValue(r, out var g); compGen[r] = g + _gen[i]; }

            // Assign each powered-machine to the network of the nearest node that can supply it; tally demand.
            var compDem = new Dictionary<int, float>();
            var consumerComp = new Dictionary<WorkshopBuilding, int>();
            var ws = WorkshopBuilding.All;
            for (int c = 0; c < ws.Count; c++)
            {
                var w = ws[c]; if (w == null || !w.RequiresPower) continue;
                Vector2 wp = w.transform.position;
                int best = -1; float bestSq = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    float s = _supply[i]; float sq = (_pos[i] - wp).sqrMagnitude;
                    if (sq <= s * s && sq < bestSq) { bestSq = sq; best = i; }
                }
                if (best >= 0)
                {
                    int r = Find(best);
                    consumerComp[w] = r;
                    compDem.TryGetValue(r, out var d); compDem[r] = d + w.PowerDraw;
                }
            }

            foreach (var kv in compGen) TotalGen += kv.Value;
            foreach (var kv in compDem) TotalDemand += kv.Value;

            for (int c = 0; c < ws.Count; c++)
            {
                var w = ws[c]; if (w == null || !w.RequiresPower) continue;
                float f = 0f;
                if (consumerComp.TryGetValue(w, out var r))
                {
                    compGen.TryGetValue(r, out var g);
                    compDem.TryGetValue(r, out var d);
                    f = g <= 0f ? 0f : (d <= g ? 1f : Mathf.Clamp(g / d, BrownoutFloor, 1f));
                }
                _factor[w] = f;
            }
        }
    }
}
