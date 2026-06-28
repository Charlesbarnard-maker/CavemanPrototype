using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// The WIRED power grid solver. Generators, Power Poles, Batteries and consuming machines are
    /// <see cref="PowerNode"/>s joined by player-drawn wires. Each connected component of the wire graph
    /// is one network: its live generation (+ battery discharge) is shared by the machines it feeds.
    /// A machine runs at full speed when its network can meet demand, browns out proportionally when
    /// oversubscribed, and STOPS if its network has no power (or it isn't wired to one). Surplus
    /// generation charges batteries; a deficit is covered by them. Read through
    /// WorkshopBuilding.EffectivePowerFactor. Rebuilt at most once per frame (frame-guard).
    /// </summary>
    public static class PowerNet
    {
        public const float BrownoutFloor = 0.35f; // most a network slows an oversubscribed machine
        public const int BronzeAge = 2; // electricity arrives in the Bronze age — before it, machines run free
        public const float MaxWireLength = 8f; // a single wire's reach; chain Power Poles to go further

        /// <summary>Is the power requirement in effect yet? Power is a Bronze-age mechanic; in the
        /// Stone/Tribal ages requiresPower machines run normally (no wiring needed).</summary>
        public static bool Active => Colony.Instance != null && Colony.Instance.Age >= BronzeAge;

        private static int _frame = -1;
        private static readonly Dictionary<WorkshopBuilding, float> _factor = new();
        public static float TotalGen { get; private set; }
        public static float TotalDemand { get; private set; }
        public static float TotalStored { get; private set; }   // battery charge across the whole grid
        public static float TotalCapacity { get; private set; } // battery capacity across the whole grid

        /// <summary>Power factor for a consumer: 1 connected with enough supply, &lt;1 if its network is
        /// oversubscribed, 0 if it has no power / isn't wired in. Drives the machine's run speed.</summary>
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

        // Reused scratch (cleared each rebuild) to keep the per-frame allocation small.
        private static readonly Dictionary<PowerNode, int> _comp = new();          // node → network id
        private static readonly Dictionary<WorkshopBuilding, int> _consumerComp = new();
        private static readonly Stack<PowerNode> _stack = new();

        private static void Rebuild()
        {
            _factor.Clear(); _comp.Clear(); _consumerComp.Clear();
            TotalGen = TotalDemand = TotalStored = TotalCapacity = 0f;
            bool active = Active;
            float dt = Time.deltaTime;

            var nodes = PowerNode.All;
            for (int i = 0; i < Battery.All.Count; i++) if (Battery.All[i] != null) Battery.All[i].ResetFlow();

            // 1. Label connected components (BFS over the wire links).
            int nComp = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                var seed = nodes[i];
                if (seed == null || _comp.ContainsKey(seed)) continue;
                int id = nComp++;
                _comp[seed] = id; _stack.Push(seed);
                while (_stack.Count > 0)
                {
                    var n = _stack.Pop();
                    var links = n.links;
                    for (int k = 0; k < links.Count; k++)
                    {
                        var m = links[k];
                        if (m != null && !_comp.ContainsKey(m)) { _comp[m] = id; _stack.Push(m); }
                    }
                }
            }
            if (nComp == 0) return;

            // 2. Aggregate generation / demand / batteries per component.
            var gen = new float[nComp];
            var demand = new float[nComp];
            var batteries = new List<Battery>[nComp];
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                if (n == null || !_comp.TryGetValue(n, out var c)) continue;
                switch (n.role)
                {
                    case PowerNode.Role.Generator:
                        if (n.generator != null) gen[c] += n.generator.CurrentOutput;
                        break;
                    case PowerNode.Role.Battery:
                        if (n.battery != null)
                        {
                            (batteries[c] ??= new List<Battery>()).Add(n.battery);
                            TotalStored += n.battery.Stored;
                            TotalCapacity += n.battery.capacity;
                        }
                        break;
                    case PowerNode.Role.Consumer:
                        if (n.consumer != null && active)
                        {
                            demand[c] += n.consumer.CurrentDraw;
                            _consumerComp[n.consumer] = c;
                        }
                        break;
                }
            }

            // 3. Resolve each network: gen meets demand, surplus charges batteries, deficit is drawn
            //    from them; derive a supply factor (0 if truly dead, else clamped to the brownout floor).
            var factorByComp = new float[nComp];
            for (int c = 0; c < nComp; c++)
            {
                float g = gen[c], d = demand[c];
                float supply = g;
                var bats = batteries[c];
                if (d > g + 0.0001f && bats != null)
                {
                    float deficit = d - g, got = 0f;
                    for (int b = 0; b < bats.Count && got < deficit; b++)
                        got += bats[b].Draw(deficit - got, dt);
                    supply += got;
                }
                else if (g > d + 0.0001f && bats != null)
                {
                    float surplus = g - d;
                    for (int b = 0; b < bats.Count && surplus > 0.0001f; b++)
                        surplus -= bats[b].Absorb(surplus, dt);
                }

                factorByComp[c] = d <= 0.0001f ? 1f
                    : supply <= 0.0001f ? 0f
                    : Mathf.Clamp(supply / d, BrownoutFloor, 1f);

                TotalGen += g; TotalDemand += d;
            }

            // 4. Hand each consumer its network factor.
            foreach (var kv in _consumerComp) _factor[kv.Key] = factorByComp[kv.Value];

            // 5. Tint wires by whether their network has any power source (live or stored).
            for (int i = 0; i < PowerWire.All.Count; i++)
            {
                var w = PowerWire.All[i];
                if (w == null || w.a == null || !_comp.TryGetValue(w.a, out var c)) continue;
                bool powered = gen[c] > 0.0001f || (batteries[c] != null && AnyStored(batteries[c]));
                w.Refresh();
                w.SetColor(powered ? PowerWire.Powered : PowerWire.Unpowered);
            }
        }

        private static bool AnyStored(List<Battery> bats)
        {
            for (int i = 0; i < bats.Count; i++) if (bats[i] != null && bats[i].Stored > 0.0001f) return true;
            return false;
        }
    }
}
