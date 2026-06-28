using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A power pole — relays power across distance. Poles link to other poles and to generators within
    /// their ConnectRange (forming a network), and SUPPLY power to consuming buildings within their
    /// SupplyRange. Chain poles from a generator out to distant machines. Pure infrastructure: no fuel,
    /// no production — the network maths lives in <see cref="PowerNet"/>. A faint ring shows its supply
    /// reach so you can see coverage.
    /// </summary>
    public class PowerPole : MonoBehaviour
    {
        public BuildingDefinition def;
        public float ConnectRange = 7f;
        public float SupplyRange = 4f;

        public static readonly List<PowerPole> All = new();
        void OnEnable() => All.Add(this);
        void OnDisable() { All.Remove(this); if (_ring != null) Destroy(_ring.gameObject); }

        private LineRenderer _ring;

        public static PowerPole Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * 0.45f; // a small post

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>(); // clickable for select / demolish

            var p = go.AddComponent<PowerPole>();
            p.def = def;
            p.ConnectRange = def.connectRange > 0f ? def.connectRange : 7f;
            p.SupplyRange = def.supplyRange > 0f ? def.supplyRange : 4f;
            return p;
        }

        void Update() => DrawRing();

        // Faint supply ring (world-space so the small post scale doesn't distort it) — see coverage.
        private void DrawRing()
        {
            if (_ring == null)
            {
                var go = new GameObject("SupplyRing");
                _ring = go.AddComponent<LineRenderer>();
                _ring.useWorldSpace = true;
                _ring.loop = true;
                _ring.widthMultiplier = 0.08f;
                _ring.material = new Material(Shader.Find("Sprites/Default"));
                _ring.sortingOrder = 3;
                _ring.startColor = _ring.endColor = new Color(0.4f, 0.8f, 1f, 0.18f);
                const int seg = 40;
                _ring.positionCount = seg;
                Vector3 c = transform.position;
                for (int i = 0; i < seg; i++)
                {
                    float a = (i / (float)seg) * Mathf.PI * 2f;
                    _ring.SetPosition(i, new Vector3(c.x + Mathf.Cos(a) * SupplyRange, c.y + Mathf.Sin(a) * SupplyRange, 0f));
                }
            }
        }
    }
}
