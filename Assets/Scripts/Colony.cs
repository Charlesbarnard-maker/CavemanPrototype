using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// The settlement manager: tracks population, housing capacity, food, and
    /// worker assignment. Population grows toward the housing cap while well fed,
    /// and declines (starvation) when food runs out. Workers are the shared labour
    /// pool that staff buildings.
    /// </summary>
    public class Colony : MonoBehaviour
    {
        public static Colony Instance { get; private set; }

        public ItemDefinition foodItem;
        public ItemDefinition waterItem;
        [System.NonSerialized] public Inventory carried; // set at runtime; not a serialized field

        public int Population { get; private set; }
        // FACTORY-FIRST: survival pressure removed. These remain as inert API (always false) so
        // existing HUD/economy references keep compiling — there is no starvation/thirst loop.
        public bool Starving => false;
        public bool Thirsty => false;

        // --- Ages / progression ---
        public int Age { get; private set; }
        public static readonly string[] AgeNames =
            { "Stone Age", "Tribal Age", "Bronze Age", "Iron Age", "Industrial Age" };
        public string AgeName => Age >= 0 && Age < AgeNames.Length ? AgeNames[Age] : $"Age {Age}";

        public string NextAgeName => (Age + 1) < AgeNames.Length ? AgeNames[Age + 1] : null;

        /// <summary>Advance the age — the ONLY path is through the Research system (crafted research
        /// items delivered to a Research Lodge). Called by <see cref="Research"/> when a tier's
        /// point cost is met; there is no resource/pop "advance" button any more.</summary>
        public void ResearchAdvance(int targetAge)
        {
            if (targetAge > Age && targetAge < AgeNames.Length) Age = targetAge;
        }

        // --- Debug / sandbox ---
        public void DebugAdvanceAge() { if (Age + 1 < AgeNames.Length) Age++; }
        public void DebugAddPopulation(int n) { Population = Mathf.Max(0, Population + n); }

        // Comfort goods (INERT in factory-first — kept as a data type so GameBootstrap still
        // compiles; the colony no longer consumes them or derives happiness from them).
        [System.Serializable]
        public class Comfort { public ItemDefinition item; public int unlockAge; }
        public List<Comfort> comforts = new();

        private float _prosperityT;

        /// <summary>Inert (factory-first): comfort/happiness removed. Always 1.</summary>
        public float Happiness => 1f;

        /// <summary>Inert: no unmet comforts in factory-first (kept for HUD compatibility).</summary>
        public readonly List<ItemDefinition> UnmetComforts = new();

        /// <summary>Global work-speed multiplier. Factory-first: constant 1 (no food/happiness
        /// modifiers). Power brownouts still scale machine speed separately (see Power.Factor).</summary>
        public float Productivity => 1f;

        /// <summary>A climbing score rewarding population, happiness, tech age, and
        /// automation (systems running for you) — the long-game "how am I doing?".</summary>
        public int Prosperity { get; private set; }
        /// <summary>Highest prosperity reached this game (never drops, so it always climbs).</summary>
        public int PeakProsperity { get; private set; }

        private static readonly int[] ProsperityMilestones = { 250, 500, 1000, 2000, 4000 };
        private int _nextMilestone;
        private string _lastRank;

        /// <summary>Settlement rank, derived from peak prosperity (only ever climbs).</summary>
        public string Rank =>
            PeakProsperity < 100 ? "Camp" :
            PeakProsperity < 300 ? "Hamlet" :
            PeakProsperity < 700 ? "Village" :
            PeakProsperity < 1500 ? "Town" :
            PeakProsperity < 3000 ? "City" : "Metropolis";

        // Factory-first score: progress is measured purely by your AUTOMATION + tech age,
        // not by headcount (no citizens to weight).
        private int ComputeProsperity()
        {
            float tech = Age * 150f;                               // progress through the ages
            int collectors = ProductionBuilding.All.Count;
            int workshops = WorkshopBuilding.All.Count;
            float automation = collectors * 15f + workshops * 20f  // systems that work for you
                               + Belt.Count * 4f + RouteVehicle.All.Count * 40f;
            return Mathf.RoundToInt(tech + automation);
        }

        public int Capacity
        {
            get
            {
                int c = 0;
                foreach (var h in HousingBuilding.All) c += h.houseCapacity;
                return c;
            }
        }

        public int AssignedTotal
        {
            get
            {
                int a = 0;
                foreach (var p in ProductionBuilding.All) a += p.AssignedWorkers;
                foreach (var w in WorkshopBuilding.All) a += w.AssignedWorkers;
                foreach (var h in TransportHub.All) a += h.AssignedWorkers;
                return a;
            }
        }

        // --- Builders: a capped HQ job, auto-filled when there's construction,
        //     but manually adjustable so they don't hog your gatherers. The cap SCALES
        //     with Construction Yards you build (infrastructure), not a fixed number. ---
        [Header("Builders")]
        public int baseBuilders = 2;
        public int MaxBuilders => baseBuilders + ConstructionYard.TotalSlots;
        public int Builders { get; private set; }
        private bool _builderManual;
        private readonly List<BuilderWorker> _builderSquad = new();

        // FACTORY-FIRST: labour is NOT a scarce survival resource. Buildings run when built;
        // "workers" are just a free per-building throughput dial (1..max) + NPC charm. This stays
        // effectively unlimited so existing TryAssign / builder gates never block on population.
        public int FreeWorkers => 9999;

        public void AddBuilder()
        {
            _builderManual = true;
            if (Builders < MaxBuilders && FreeWorkers > 0) { Builders++; SyncSquad(); }
        }

        public void RemoveBuilder()
        {
            _builderManual = true;
            if (Builders > 0) { Builders--; SyncSquad(); }
        }

        private void ManageBuilders()
        {
            if (!_builderManual)
            {
                int target = ConstructionSite.All.Count > 0 ? MaxBuilders : 0;
                while (Builders < target) Builders++;   // factory-first: labour is free, not pop-gated
                while (Builders > target && Builders > 0) Builders--;
            }
            SyncSquad();
        }

        private void SyncSquad()
        {
            _builderSquad.RemoveAll(b => b == null);
            while (_builderSquad.Count < Builders) _builderSquad.Add(BuilderWorker.Spawn());
            while (_builderSquad.Count > Builders)
            {
                var b = _builderSquad[_builderSquad.Count - 1];
                _builderSquad.RemoveAt(_builderSquad.Count - 1);
                if (b != null) Destroy(b.gameObject);
            }
        }

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void SetStartingPopulation(int n) => Population = Mathf.Max(0, n);

        void Update()
        {
            float dt = Time.deltaTime;

            // FACTORY-FIRST: no food/water/comfort/growth/starvation. Population is inert flavour;
            // labour is free (see FreeWorkers). We only keep the automation SCORE + builder squad.
            _prosperityT += dt;
            if (_prosperityT >= 1f)
            {
                _prosperityT -= 1f;
                Prosperity = ComputeProsperity();
                if (Prosperity > PeakProsperity) PeakProsperity = Prosperity;

                // Celebrate crossing automation milestones — the "one more level" hook.
                while (_nextMilestone < ProsperityMilestones.Length
                       && PeakProsperity >= ProsperityMilestones[_nextMilestone])
                {
                    Toast.Show($"<color=#ffcf6b>📈 Industry {ProsperityMilestones[_nextMilestone]}!</color>  <size=14>your factory grows</size>");
                    _nextMilestone++;
                }

                // Announce settlement rank promotions (progression hook).
                if (Rank != _lastRank)
                {
                    if (_lastRank != null) Toast.Show($"<color=#ffd24d>🏭 Your settlement is now a {Rank}!</color>");
                    _lastRank = Rank;
                }
            }

            ManageBuilders();
        }
    }
}
