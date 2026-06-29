using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Factory-first settlement state: tracks the current Age and a climbing automation "Industry"
    /// score. NO population, NO workers, NO survival — buildings run themselves and construction is instant.
    /// </summary>
    public class Colony : MonoBehaviour
    {
        public static Colony Instance { get; private set; }

        [System.NonSerialized] public Inventory carried; // set at runtime; not a serialized field

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

        private float _prosperityT;

        /// <summary>A climbing score rewarding tech age + automation (systems running for you) —
        /// the long-game "how am I doing?".</summary>
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

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            float dt = Time.deltaTime;

            // FACTORY-FIRST: no survival/comfort — we only keep the automation SCORE + rank.
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
        }
    }
}
