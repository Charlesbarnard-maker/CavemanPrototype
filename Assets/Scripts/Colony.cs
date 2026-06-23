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
        public Inventory carried; // player's carried inventory, for Economy spending

        public int Population { get; private set; }
        public bool Starving { get; private set; }
        public bool Thirsty { get; private set; }

        [Header("Tuning")]
        public float foodTick = 7f;     // every N seconds each person eats 1 food (gentle early)
        public float waterTick = 7f;    // every N seconds each person drinks 1 water
        public float growthTick = 15f;  // every N seconds +1 pop (needs surplus, see below)
        public float starveTick = 12f;  // every N seconds -1 pop while starving/thirsty
        [Tooltip("Stored food needed before population will grow at all.")]
        public int growthFoodThreshold = 12;
        [Tooltip("Stored food consumed to raise each new citizen.")]
        public int growthFoodCost = 8;

        private float _foodT, _waterT, _growthT, _starveT;

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

            // --- Growth: needs housing space AND a real food surplus; each new
            //     citizen costs stored food, so growth reflects food-economy progress
            //     (no more instant house-fill). ---
            bool canGrow = !Starving && !Thirsty
                           && Population < Capacity
                           && Economy.FoodPoints(carried) >= growthFoodThreshold;
            if (canGrow)
            {
                _growthT += dt;
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
