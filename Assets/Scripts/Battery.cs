using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A battery: stores surplus power and releases it when a network's demand outstrips its live
    /// generation. It's a wired node like a Power Pole (no fuel, up to 4 wires) — the grid maths lives
    /// in <see cref="PowerNet"/>, which calls <see cref="Absorb"/> when there's surplus and
    /// <see cref="Draw"/> when there's a deficit. Smooths brownouts and lets a small generator cover
    /// short demand spikes.
    /// </summary>
    public class Battery : MonoBehaviour
    {
        public BuildingDefinition def;
        public float capacity = 200f; // max stored energy (power × seconds)
        public float rate = 30f;      // max power it can source or sink at once
        public float Stored;          // current stored energy
        public float Flow { get; private set; } // +charging / −discharging this solve (for the UI)

        public static readonly List<Battery> All = new();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        private SpriteRenderer _sr;
        private Color _base;

        public float Fraction => capacity > 0f ? Mathf.Clamp01(Stored / capacity) : 0f;

        public static Battery Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(def.FootW, def.FootH, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteDatabase.ForBuilding(def);
            sr.color = def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>(); // clickable for select / demolish / wiring

            var bat = go.AddComponent<Battery>();
            bat.def = def;
            bat.capacity = def.batteryCapacity > 0f ? def.batteryCapacity : 200f;
            bat.rate = def.batteryRate > 0f ? def.batteryRate : 30f;
            bat._sr = sr;
            bat._base = def.color;

            var node = go.AddComponent<PowerNode>();
            node.role = PowerNode.Role.Battery;
            node.maxConnections = 4;
            node.battery = bat;
            return bat;
        }

        /// <summary>Discharge up to <paramref name="power"/> for this frame (limited by rate + stored).
        /// Returns the power actually supplied; reduces stored energy.</summary>
        public float Draw(float power, float dt)
        {
            if (power <= 0f || dt <= 0f) return 0f;
            float p = Mathf.Min(power, rate, Stored / dt);
            if (p <= 0f) return 0f;
            Stored -= p * dt;
            Flow -= p;
            return p;
        }

        /// <summary>Absorb up to <paramref name="power"/> of surplus this frame (limited by rate + room).
        /// Returns the power actually absorbed; increases stored energy.</summary>
        public float Absorb(float power, float dt)
        {
            if (power <= 0f || dt <= 0f) return 0f;
            float room = (capacity - Stored) / dt;
            float p = Mathf.Min(power, rate, room);
            if (p <= 0f) return 0f;
            Stored += p * dt;
            Flow += p;
            return p;
        }

        public void ResetFlow() => Flow = 0f; // PowerNet calls this at the start of each solve

        void Update()
        {
            // Tint by charge level: dim when empty → bright when full, a faint green when discharging.
            if (_sr == null) return;
            float f = Fraction;
            Color full = Color.Lerp(_base, new Color(0.4f, 1f, 0.6f), 0.35f);
            _sr.color = Color.Lerp(Color.Lerp(_base, Color.black, 0.5f), full, f);
        }
    }
}
