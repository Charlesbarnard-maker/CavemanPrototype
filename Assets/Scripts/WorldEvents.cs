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
        public ItemDefinition wood, stone;
        public float minGap = 140f;
        public float maxGap = 280f;

        private float _t;
        private float _next;
        private int _last = -1; // don't fire the same event twice in a row

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
            // Factory-first: events nudge your raw-material stockpiles (no food/population events).
            int kind = Random.Range(0, 4);
            if (kind == _last) kind = (kind + 1) % 4; // avoid back-to-back repeats
            _last = kind;
            switch (kind)
            {
                case 0: // windfall timber
                    if (carried != null && wood != null)
                    { carried.Add(wood, 30); Toast.Show("<color=#9f9>🌲 A storm felled trees — free timber!</color>  <size=14>+30 Wood</size>"); }
                    break;

                case 1: // rockslide
                    if (carried != null && stone != null)
                    { carried.Add(stone, 30); Toast.Show("<color=#9f9>⛏ A rockslide exposed stone!</color>  <size=14>+30 Stone</size>"); }
                    break;

                case 2: // prospectors' cache
                    if (carried != null && wood != null && stone != null)
                    { carried.Add(wood, 15); carried.Add(stone, 15); Toast.Show("<color=#9f9>🧭 Prospectors shared a cache!</color>  <size=14>+15 Wood, +15 Stone</size>"); }
                    break;

                default: // a cart broke down — minor setback
                    if (carried != null && wood != null)
                    { int lost = Economy.SpendUpTo(wood, 10, carried); Toast.Show($"<color=#f99>🛠 A cart broke down.</color>  <size=14>-{lost} Wood</size>"); }
                    break;
            }
        }
    }
}
