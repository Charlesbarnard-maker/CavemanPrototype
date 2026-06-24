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
        public bool Starving { get; private set; }
        public bool Thirsty { get; private set; }

        // --- Ages / progression ---
        public int Age { get; private set; }
        public static readonly string[] AgeNames =
            { "Stone Age", "Tribal Age", "Bronze Age", "Iron Age", "Industrial Age" };
        public string AgeName => Age >= 0 && Age < AgeNames.Length ? AgeNames[Age] : $"Age {Age}";

        [System.Serializable]
        public class AgeReq { public int pop; public List<ItemAmount> cost = new(); }
        /// <summary>Requirements to advance FROM each age (index = current age). Set by GameBootstrap.</summary>
        public List<AgeReq> ageReqs = new();

        public AgeReq NextReq => (ageReqs != null && Age < ageReqs.Count) ? ageReqs[Age] : null;
        public string NextAgeName => (Age + 1) < AgeNames.Length ? AgeNames[Age + 1] : null;

        public bool CanAdvance()
        {
            var r = NextReq;
            if (r == null) return false;
            return Population >= r.pop && Economy.CanAfford(r.cost, carried);
        }

        public void AdvanceAge()
        {
            if (!CanAdvance()) return;
            Economy.Spend(NextReq.cost, carried);
            Age++;
        }

        // --- Debug / sandbox ---
        public void DebugAdvanceAge() { if (Age + 1 < AgeNames.Length) Age++; }
        public void DebugAddPopulation(int n) { Population = Mathf.Max(0, Population + n); }

        [Header("Tuning")]
        public float foodTick = 11f;    // every N seconds each person eats 1 food (gentle)
        public float waterTick = 11f;   // every N seconds each person drinks 1 water
        public float growthTick = 15f;  // every N seconds +1 pop (needs surplus, see below)
        public float starveTick = 20f;  // every N seconds -1 pop while starving/thirsty (lots of grace)
        [Tooltip("Stored food needed before population will grow at all.")]
        public int growthFoodThreshold = 12;
        [Tooltip("Stored food consumed to raise each new citizen.")]
        public int growthFoodCost = 8;
        public float comfortTick = 9f;  // every N seconds the colony consumes its comfort goods

        // Comfort goods the colony wants (beyond survival), unlocked by age. Meeting them
        // raises Happiness, which boosts productivity and growth — the escalating demand sink.
        [System.Serializable]
        public class Comfort { public ItemDefinition item; public int unlockAge; }
        public List<Comfort> comforts = new();

        private float _foodT, _waterT, _growthT, _starveT, _comfortT, _prosperityT;
        private float _fedBonus = 1f;

        /// <summary>0..1 — fraction of currently-expected comfort goods being supplied.</summary>
        public float Happiness { get; private set; } = 1f;

        /// <summary>Global work-speed multiplier: survival × food variety × happiness.</summary>
        public float Productivity => (Starving || Thirsty) ? 0.6f : _fedBonus * (0.85f + 0.3f * Happiness);

        /// <summary>A climbing score rewarding population, happiness, tech age, and
        /// automation (systems running for you) — the long-game "how am I doing?".</summary>
        public int Prosperity { get; private set; }
        /// <summary>Highest prosperity reached this game (never drops, so it always climbs).</summary>
        public int PeakProsperity { get; private set; }

        // On-pillar weighting: automation (collectors/workshops/belts/routes) is worth
        // more than raw headcount, because the game is about systems that run themselves.
        private int ComputeProsperity()
        {
            float baseScore = Population * (5f + 5f * Happiness);  // people, weighted by quality of life
            float tech = Age * 150f;                               // progress through the ages
            int collectors = ProductionBuilding.All.Count;
            int workshops = WorkshopBuilding.All.Count;
            float automation = collectors * 15f + workshops * 20f  // systems that work for you
                               + Belt.Count * 4f + RouteVehicle.All.Count * 40f;
            return Mathf.RoundToInt(baseScore + tech + automation);
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
        //     but manually adjustable so they don't hog your gatherers. ---
        [Header("Builders")]
        public int MaxBuilders = 2;
        public int Builders { get; private set; }
        private bool _builderManual;
        private readonly List<BuilderWorker> _builderSquad = new();

        public int FreeWorkers => Mathf.Max(0, Population - AssignedTotal - Builders);

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
                while (Builders < target && FreeWorkers > 0) Builders++;
                while (Builders > target && Builders > 0) Builders--;
            }
            int maxPossible = Mathf.Max(0, Population - AssignedTotal);
            if (Builders > maxPossible) Builders = maxPossible;
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

            // --- Food consumption ---
            _foodT += dt;
            if (_foodT >= foodTick)
            {
                _foodT -= foodTick;
                if (Population > 0)
                {
                    int got = Economy.SpendFoodPoints(Population, carried); // raw + cooked
                    Starving = got < Population;
                }
                else
                {
                    Starving = false;
                }

                // Food variety -> productivity bonus (a varied, well-fed colony works faster).
                var t = Economy.Totals(carried);
                int variety = 0;
                foreach (var kv in t) if (kv.Key != null && kv.Key.foodValue > 0 && kv.Value > 0) variety++;
                _fedBonus = 1f + 0.1f * Mathf.Clamp(variety - 1, 0, 3); // up to +30% with 4 food types
            }

            // --- Water consumption ---
            _waterT += dt;
            if (_waterT >= waterTick)
            {
                _waterT -= waterTick;
                if (Population > 0 && waterItem != null)
                {
                    int got = Economy.SpendUpTo(waterItem, Population, carried);
                    Thirsty = got < Population;
                }
                else
                {
                    Thirsty = false;
                }
            }

            // --- Comfort goods (the demand sink): the colony consumes the comfort goods
            //     unlocked for its age; how many it can supply sets Happiness. ---
            _comfortT += dt;
            if (_comfortT >= comfortTick)
            {
                _comfortT -= comfortTick;
                int required = 0, met = 0;
                foreach (var c in comforts)
                {
                    if (c.item == null || c.unlockAge > Age) continue;
                    required++;
                    int want = Mathf.Max(1, Population / 2); // comfort goods used at half the eating rate
                    if (Economy.SpendUpTo(c.item, want, carried) > 0) met++;
                }
                Happiness = required == 0 ? 1f : (float)met / required;
            }

            // --- Prosperity score: recomputed once a second; the peak never drops. ---
            _prosperityT += dt;
            if (_prosperityT >= 1f)
            {
                _prosperityT -= 1f;
                Prosperity = ComputeProsperity();
                if (Prosperity > PeakProsperity) PeakProsperity = Prosperity;
            }

            // --- Growth: needs housing space AND a real food surplus; each new
            //     citizen costs stored food, so growth reflects food-economy progress
            //     (no more instant house-fill). ---
            bool canGrow = !Starving && !Thirsty
                           && Population < Capacity
                           && Economy.FoodPoints(carried) >= growthFoodThreshold;
            if (canGrow)
            {
                _growthT += dt * (0.5f + Happiness); // happier colonies grow faster
                if (_growthT >= growthTick)
                {
                    _growthT -= growthTick;
                    if (Economy.SpendFoodPoints(growthFoodCost, carried) > 0) Population++;
                }
            }
            else
            {
                _growthT = 0f;
            }

            // --- Decline (starvation or thirst) ---
            if ((Starving || Thirsty) && Population > 0)
            {
                _starveT += dt;
                if (_starveT >= starveTick)
                {
                    _starveT -= starveTick;
                    Population--;
                    EnforceAssignment();
                }
            }
            else
            {
                _starveT = 0f;
            }

            ManageBuilders();
        }

        /// <summary>If more workers are assigned than we have people, unassign the surplus.</summary>
        private void EnforceAssignment()
        {
            int over = AssignedTotal - Population;
            if (over <= 0) return;
            foreach (var p in ProductionBuilding.All)
            {
                while (over > 0 && p.AssignedWorkers > 0) { p.Unassign(); over--; }
                if (over <= 0) return;
            }
            foreach (var w in WorkshopBuilding.All)
            {
                while (over > 0 && w.AssignedWorkers > 0) { w.Unassign(); over--; }
                if (over <= 0) return;
            }
            foreach (var h in TransportHub.All)
            {
                while (over > 0 && h.AssignedWorkers > 0) { h.Unassign(); over--; }
                if (over <= 0) break;
            }
        }
    }
}
