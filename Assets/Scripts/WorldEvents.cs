using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Periodic random events — bonuses and setbacks that add variety and a
    /// "what happens next?" pull. Fires every ~minGap..maxGap seconds. Effects use
    /// the existing Colony / Economy / Toast systems; item refs are wired by
    /// GameBootstrap so events can grant or remove resources.
    /// </summary>
    public class WorldEvents : MonoBehaviour
    {
        [System.NonSerialized] public Inventory carried; // player's carried inventory
        public ItemDefinition food, wood, stone;
        public float minGap = 55f;
        public float maxGap = 105f;

        private float _t;
        private float _next;

        void Start() => _next = Random.Range(minGap, maxGap);

        void Update()
        {
            _t += Time.deltaTime;
            if (_t < _next) return;
            _t = 0f;
            _next = Random.Range(minGap, maxGap);
            Fire();
        }

        private void Fire()
        {
            var col = Colony.Instance;
            switch (Random.Range(0, 5))
            {
                case 0: // bountiful harvest
                    if (carried != null && food != null)
                    { carried.Add(food, 40); Toast.Show("<color=#9f9>🌾 Bountiful Harvest!</color>  <size=14>+40 Food</size>"); }
                    break;

                case 1: // wandering nomads
                    if (col != null && col.Population < col.Capacity)
                    { col.DebugAddPopulation(3); Toast.Show("<color=#9cf>🚶 Wandering nomads joined you!</color>  <size=14>+3 people</size>"); }
                    else
                        Toast.Show("<color=#bbb>🚶 Nomads passed by — no housing to take them in.</color>");
                    break;

                case 2: // rockslide
                    if (carried != null && stone != null)
                    { carried.Add(stone, 30); Toast.Show("<color=#9f9>⛏ A rockslide exposed stone!</color>  <size=14>+30 Stone</size>"); }
                    break;

                case 3: // windfall timber
                    if (carried != null && wood != null)
                    { carried.Add(wood, 30); Toast.Show("<color=#9f9>🌲 A storm felled trees — free timber!</color>  <size=14>+30 Wood</size>"); }
                    break;

                default: // hard times — lose some food
                    if (food != null)
                    { int lost = Economy.SpendUpTo(food, 20, carried); Toast.Show($"<color=#f99>❄ Hard times set in.</color>  <size=14>-{lost} Food</size>"); }
                    break;
            }
        }
    }
}
