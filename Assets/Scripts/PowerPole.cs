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

        public static PowerPole Spawn(BuildingDefinition def, Vector3 pos, bool autoLink = true)
        {
            var go = new GameObject(def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            // A small post so poles fit easily between buildings; a high-slot Power Hub renders a touch bigger.
            float scale = 0.40f * (def != null && def.maxConnections > 4 ? 1.35f : 1f);
            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Pole(); // a proper little pole (post + crossarm + insulators)
            // The plain pole keeps its baked white art; specialised poles (Hub / Pylon) tint by def colour to read apart.
            bool plain = def == null || (def.maxConnections == 0 && def.wireReach == 0f);
            sr.color = plain ? Color.white : def.color;
            sr.sortingOrder = 5;

            go.AddComponent<BoxCollider2D>(); // clickable for select / demolish / wiring

            var p = go.AddComponent<PowerPole>();
            p.def = def;

            var node = go.AddComponent<PowerNode>();
            node.role = PowerNode.Role.Pole;
            node.maxConnections = def != null && def.maxConnections > 0 ? def.maxConnections : 4; // def-driven (Power Hub = 8)
            node.wireReach = def != null ? def.wireReach : 0f;                                    // def-driven (Tall Pylon = long)
            // No auto-connect: a placed pole starts UNWIRED — the player draws every wire deliberately
            // (the `autoLink` parameter is kept for call-site compatibility but no longer auto-wires).
            return p;
        }
    }
}
