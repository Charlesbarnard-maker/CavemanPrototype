using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A power pole — relays power across distance. It's a pure wired node: the player draws wires from
    /// it to generators, batteries, machines, or other poles (up to 4 wires each, max
    /// <see cref="PowerNet.MaxWireLength"/> apart). Chain poles to span big gaps. No fuel, no production
    /// — the network maths lives in <see cref="PowerNet"/>.
    /// </summary>
    public class PowerPole : MonoBehaviour
    {
        public BuildingDefinition def;

        public static readonly List<PowerPole> All = new();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static PowerPole Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * 0.55f; // a small post (the crossarm reads at this size)

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Pole(); // a proper little pole (post + crossarm + insulators)
            sr.color = Color.white;            // baked wood/metal colours
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>(); // clickable for select / demolish / wiring

            var p = go.AddComponent<PowerPole>();
            p.def = def;

            var node = go.AddComponent<PowerNode>();
            node.role = PowerNode.Role.Pole;
            node.maxConnections = 4;
            return p;
        }
    }
}
