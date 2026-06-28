using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A fuel-burning HEARTH — the Stone-age energy source. It auto-burns its fuel (Wood) at a fixed
    /// rate (no workers, like a PowerPlant) and projects a HEAT RADIUS; processing machines that
    /// requirePower (smelters/kilns) must sit inside a LIT hearth to run, so you cluster production
    /// around a fuelled hearth. No grid, no wires — proximity only (see <see cref="HeatField"/>). A
    /// faint ring shows the coverage so you can plan the layout.
    /// </summary>
    public class Hearth : MonoBehaviour
    {
        public BuildingDefinition def;
        public ItemDefinition fuel;
        public int fuelPerCycle = 1;
        public float interval = 2.5f;
        public float Radius = 7f;

        private float _timer;
        private bool _lit;
        public bool Lit => _lit;

        private SpriteRenderer _sr;
        private Color _baseColor;
        private LineRenderer _ring;

        public static Hearth Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(def.FootW, def.FootH, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Circle(); // a round hearth/fire
            sr.color = def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>(); // clickable for select / demolish

            var h = go.AddComponent<Hearth>();
            h.def = def;
            h.interval = Mathf.Max(0.2f, def.interval);
            h.Radius = def.heatRadius > 0f ? def.heatRadius : 7f;
            bool hasFuel = def.inputs != null && def.inputs.Count > 0 && def.inputs[0] != null;
            h.fuel = hasFuel ? def.inputs[0].item : null;
            h.fuelPerCycle = hasFuel ? Mathf.Max(1, def.inputs[0].amount) : 1;
            h._sr = sr;
            h._baseColor = def.color;
            return h;
        }

        void OnEnable() => HeatField.Register(this);
        void OnDisable() { HeatField.Unregister(this); if (_ring != null) Destroy(_ring.gameObject); }

        void Update()
        {
            var carried = Colony.Instance != null ? Colony.Instance.carried : null;
            _timer += Time.deltaTime;
            if (_timer >= interval)
            {
                _timer -= interval;
                // Fuel-free hearths (no inputs) always burn; otherwise consume fuel each cycle.
                _lit = fuel == null || Economy.SpendUpTo(fuel, fuelPerCycle, carried) >= fuelPerCycle;
            }
            if (_sr != null) _sr.color = _lit ? _baseColor : Color.Lerp(_baseColor, Color.black, 0.55f);
            DrawRing();
        }

        // Faint coverage ring so the heat zone is visible to plan around (warm when lit, dim when out of fuel).
        private void DrawRing()
        {
            if (_ring == null)
            {
                var go = new GameObject("HeatRing");
                go.transform.SetParent(transform, false);
                _ring = go.AddComponent<LineRenderer>();
                _ring.useWorldSpace = false;
                _ring.loop = true;
                _ring.widthMultiplier = 0.12f;
                _ring.material = new Material(Shader.Find("Sprites/Default"));
                _ring.sortingOrder = 3;
                const int seg = 48;
                _ring.positionCount = seg;
                for (int i = 0; i < seg; i++)
                {
                    float a = (i / (float)seg) * Mathf.PI * 2f;
                    _ring.SetPosition(i, new Vector3(Mathf.Cos(a) * Radius, Mathf.Sin(a) * Radius, 0f));
                }
            }
            var c = _lit ? new Color(1f, 0.55f, 0.20f, 0.30f) : new Color(0.5f, 0.30f, 0.20f, 0.16f);
            _ring.startColor = _ring.endColor = c;
        }
    }
}
