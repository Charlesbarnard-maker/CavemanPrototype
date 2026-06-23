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
        public Inventory carried; // player's carried inventory, for Economy spending

        public int Population { get; private set; }
        public bool Starving { get; private set; }

        [Header("Tuning")]
        public float foodTick = 7f;     // every N seconds each person eats 1 food (gentle early)
        public float growthTick = 12f;  // every N seconds +1 pop if well fed and space
        public float starveTick = 12f;  // every N seconds -1 pop while starving

        private float _foodT, _growthT, _starveT;

        public int Capacity
        {
            get
            {
                int c = 0;
                foreach (var h in FindObjectsByType<HousingBuilding>()) c += h.houseCapacity;
                return c;
            }
        }

        public int AssignedTotal
        {
            get
            {
                int a = 0;
                foreach (var p in FindObjectsByType<ProductionBuilding>()) a += p.AssignedWorkers;
                return a;
            }
        }

        // Workers temporarily claimed for construction (builders).
        private int _claimed;

        public int FreeWorkers => Mathf.Max(0, Population - AssignedTotal - _claimed);

        /// <summary>Claim a free worker (e.g. as a builder). Returns false if none free.</summary>
        public bool ClaimWorker()
        {
            if (FreeWorkers <= 0) return false;
            _claimed++;
            return true;
        }

        public void ReleaseWorker() => _claimed = Mathf.Max(0, _claimed - 1);

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
                if (Population > 0 && foodItem != null)
                {
                    int got = Economy.SpendUpTo(foodItem, Population, carried);
                    Starving = got < Population;
                }
                else
                {
                    Starving = false;
                }
            }

            // --- Growth (needs surplus food + housing space) ---
            if (!Starving && Population < Capacity)
            {
                _growthT += dt;
                if (_growthT >= growthTick) { _growthT -= growthTick; Population++; }
            }
            else
            {
                _growthT = 0f;
            }

            // --- Decline (starvation) ---
            if (Starving && Population > 0)
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
        }

        /// <summary>If more workers are assigned than we have people, unassign the surplus.</summary>
        private void EnforceAssignment()
        {
            int over = AssignedTotal - Population;
            if (over <= 0) return;
            foreach (var p in FindObjectsByType<ProductionBuilding>())
            {
                while (over > 0 && p.AssignedWorkers > 0) { p.Unassign(); over--; }
                if (over <= 0) break;
            }
        }
    }
}
