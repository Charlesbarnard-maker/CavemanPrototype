using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Central, deterministic, fixed-timestep simulation for ALL belts. Replaces the old
    /// per-belt Update() tick so item motion is provably independent of GameObject update
    /// order (the root of the historic "items stuck on a belt" throughput bug).
    ///
    /// Belts are processed DOWNSTREAM-FIRST (nearest-to-sink first; see Belt.DistToSink) so an
    /// upstream cell reads its downstream's already-advanced position, keeping throughput exact.
    /// Each fixed step runs a MULTI-PASS over every registered belt in that order:
    ///   1. Snapshot   — refresh each cell's sink-reachability (gates pulls + colour state).
    ///   2. Advance    — move each cell's items along their lane by speed*dt, the lead capped a
    ///                   MinGap behind the LIVE downstream tail (deterministic spacing, no overlaps,
    ///                   cross-tile aware — no-overlap holds in any order; order affects only rate).
    ///   3. Handoff    — push a matured lead (reached the exit edge) into the next belt cell
    ///                   or a building sink.
    ///   4. Pull       — draw a new item from an adjacent feeder building into a cleared entry.
    ///
    /// Rendering (the per-item dots) stays on each Belt's own Update so it runs every frame for
    /// smoothness; only the SIMULATION is centralised + fixed-step here.
    /// </summary>
    public class BeltSim : MonoBehaviour
    {
        public static BeltSim Instance { get; private set; }

        public const float H = 1f / 60f;        // fixed simulation timestep (seconds)
        private const int MaxStepsPerFrame = 12; // catch-up cap (covers 4× game-speed at ~20fps before dropping backlog)

        private static readonly List<Belt> _belts = new();
        private static bool _needsSort;
        private float _acc;

        public static int BeltCount => _belts.Count;
        public static int ItemCount
        {
            get { int n = 0; for (int i = 0; i < _belts.Count; i++) if (_belts[i] != null) n += _belts[i].ItemCount; return n; }
        }

        public static void Register(Belt b)
        {
            if (b == null || _belts.Contains(b)) return;
            _belts.Add(b);
            _needsSort = true;
        }

        public static void Unregister(Belt b)
        {
            if (b != null) _belts.Remove(b);
        }

        /// <summary>Create the singleton once (call from GameBootstrap). Idempotent.</summary>
        public static BeltSim Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("BeltSim");
            Instance = go.AddComponent<BeltSim>();
            return Instance;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            if (_needsSort) { SortBelts(); _needsSort = false; }
            // Scaled time → the sim respects pause (timeScale 0) and the F5 game-speed multiplier.
            _acc += Time.deltaTime;
            int steps = 0;
            while (_acc >= H && steps < MaxStepsPerFrame) { Step(H); _acc -= H; steps++; }
            if (steps >= MaxStepsPerFrame) _acc = 0f; // drop the backlog after a long stall
        }

        // DOWNSTREAM-FIRST order: nearest-to-sink belts processed first, so an upstream cell reads
        // its downstream's already-advanced position each step → throughput stays exact (= 1/interval).
        // Ties broken by cell (row-major) for a stable, deterministic order. Recomputed only when the
        // belt set changes (placement/demolish — rare); a stale order after a SINK change merely costs
        // a few % throughput on affected lines until the next belt edit (never a correctness issue —
        // no-overlap holds in any order). P2's connectivity dirty-set will refresh this on sink edits.
        private static void SortBelts()
        {
            _belts.RemoveAll(b => b == null);
            var dist = new Dictionary<Belt, int>(_belts.Count);
            for (int i = 0; i < _belts.Count; i++) dist[_belts[i]] = _belts[i].DistToSink();
            _belts.Sort((a, b) =>
            {
                int da = dist[a], db = dist[b];
                if (da != db) return da.CompareTo(db);
                var ca = a.Cell; var cb = b.Cell;
                if (ca.y != cb.y) return ca.y.CompareTo(cb.y);
                return ca.x.CompareTo(cb.x);
            });
        }

        // _belts is kept in downstream-first order (see SortBelts), so each pass below visits
        // nearest-to-sink cells first.
        private static void Step(float h)
        {
            for (int i = 0; i < _belts.Count; i++) _belts[i]?.SimSnapshot();
            for (int i = 0; i < _belts.Count; i++) _belts[i]?.SimAdvance(h);
            for (int i = 0; i < _belts.Count; i++) _belts[i]?.SimHandoff();
            for (int i = 0; i < _belts.Count; i++) _belts[i]?.SimPull();
        }
    }
}
