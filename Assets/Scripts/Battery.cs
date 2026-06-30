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
        private SpriteRenderer _barFill; // the coloured fill of the charge bar (tracks Fraction)

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

            // Charge BAR above the battery — a dark frame with a coloured fill that tracks how full it is
            // (red when low → green when full), so you can read the state at a glance without selecting it.
            MakeBarSprite(go.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.74f, 0.16f, 1f),
                new Color(0.08f, 0.09f, 0.11f, 0.9f), 11);
            bat._barFill = MakeBarSprite(go.transform, new Vector3(-0.35f, 0.6f, 0f), new Vector3(0.0001f, 0.12f, 1f),
                new Color(0.4f, 0.95f, 0.55f), 12);

            var node = go.AddComponent<PowerNode>();
            node.role = PowerNode.Role.Battery;
            node.maxConnections = 4;
            node.battery = bat;
            return bat;
        }

        // A unit-square sprite used as a bar segment (the dark frame, or the coloured fill).
        private static SpriteRenderer MakeBarSprite(Transform parent, Vector3 localPos, Vector3 localScale, Color col, int order)
        {
            var go = new GameObject("chargebar");
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = col;
            sr.sortingOrder = order;
            return sr;
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

            // Drive the charge bar: a LEFT-anchored fill that grows with charge and shifts red→green.
            if (_barFill != null)
            {
                float w = 0.70f * f; // inner width (frame is 0.74 wide)
                _barFill.enabled = w > 0.001f;
                _barFill.transform.localScale = new Vector3(Mathf.Max(0.0001f, w), 0.12f, 1f);
                _barFill.transform.localPosition = new Vector3(-0.35f + 0.5f * w, 0.6f, 0f); // keep the left edge pinned
                _barFill.color = Color.Lerp(new Color(0.92f, 0.42f, 0.34f), new Color(0.40f, 0.95f, 0.55f), f);
            }
        }
    }
}
