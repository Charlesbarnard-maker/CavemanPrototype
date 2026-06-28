using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A node on the WIRED power grid. Every power participant carries one — Generators, Power Poles,
    /// Batteries, and the machines that consume power. Nodes are joined by WIRES the player draws (see
    /// BuildController / InventoryHud); there is no proximity radius any more. Two nodes may link when
    /// both are under their connection cap and within <see cref="PowerNet.MaxWireLength"/>. A "network"
    /// is a connected component of the wire graph, which <see cref="PowerNet"/> solves each frame.
    /// </summary>
    public class PowerNode : MonoBehaviour
    {
        public enum Role { Generator, Pole, Battery, Consumer }
        public Role role = Role.Pole;
        public int maxConnections = 4; // poles/generators/batteries 4; a consuming machine 1

        // Back-refs (set by the owner's Spawn) so the solver can read output / demand / storage.
        public PowerPlant generator;
        public Battery battery;
        public WorkshopBuilding consumer;

        public static readonly List<PowerNode> All = new();
        // Adjacency — the nodes this one is wired to (kept symmetric with the other end).
        public readonly List<PowerNode> links = new();

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable()
        {
            All.Remove(this);
            // Tear down every wire touching this node (also removes us from neighbours' link lists).
            for (int i = links.Count - 1; i >= 0; i--) Disconnect(links[i]);
        }

        // Drive the grid solve once per frame (frame-guarded inside PowerNet) so batteries charge and
        // wire tints update even when no machine is currently querying its power factor.
        void Update() => PowerNet.EnsureFresh();

        public Vector2 Pos => transform.position;
        public bool CanLinkMore => links.Count < maxConnections;
        public string RoleLabel => role switch
        {
            Role.Generator => "Generator",
            Role.Battery => "Battery",
            Role.Consumer => "Machine",
            _ => "Power Pole",
        };

        /// <summary>Reason a wire from this node to <paramref name="other"/> is disallowed (null = OK).</summary>
        public string LinkBlockedReason(PowerNode other)
        {
            if (other == null || other == this) return "pick another power building";
            if (links.Contains(other)) return "already wired together";
            if (!CanLinkMore) return $"this {RoleLabel} already has {maxConnections} wires";
            if (!other.CanLinkMore) return $"that {other.RoleLabel} already has {other.maxConnections} wires";
            float max = PowerNet.MaxWireLength;
            if ((Pos - other.Pos).sqrMagnitude > max * max) return "too far — place a Power Pole between them";
            return null;
        }

        public bool CanLink(PowerNode other) => LinkBlockedReason(other) == null;

        /// <summary>Draw a wire to <paramref name="other"/> (symmetric). Returns false if not allowed.</summary>
        public bool Connect(PowerNode other)
        {
            if (!CanLink(other)) return false;
            links.Add(other);
            other.links.Add(this);
            PowerWire.Create(this, other);
            return true;
        }

        public void Disconnect(PowerNode other)
        {
            if (other == null) return;
            links.Remove(other);
            other.links.Remove(this);
            PowerWire.Destroy(this, other);
        }
    }

    /// <summary>The visible cable for one wire (a thin LineRenderer between two nodes). Created/destroyed
    /// with the link; <see cref="PowerNet"/> tints it each solve to show whether its network is powered.</summary>
    public class PowerWire : MonoBehaviour
    {
        public PowerNode a, b;
        public static readonly List<PowerWire> All = new();
        private LineRenderer _lr;

        public static readonly Color Powered = new Color(0.45f, 0.85f, 1f, 0.95f);   // live cable — cyan
        public static readonly Color Unpowered = new Color(0.55f, 0.55f, 0.60f, 0.7f); // no generation — grey

        public static void Create(PowerNode a, PowerNode b)
        {
            var go = new GameObject("PowerWire");
            var w = go.AddComponent<PowerWire>();
            w.a = a; w.b = b;
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.widthMultiplier = 0.09f;
            lr.numCapVertices = 2;
            lr.material = PlaceholderArt.LineMaterial(); // shared (no per-wire Material leak)
            lr.sortingOrder = 3; // above ground, below buildings (5)
            lr.positionCount = 2;
            w._lr = lr;
            w.Refresh();
            w.SetColor(Powered);
            All.Add(w);
        }

        public static void Destroy(PowerNode a, PowerNode b)
        {
            for (int i = All.Count - 1; i >= 0; i--)
            {
                var w = All[i];
                if (w == null) { All.RemoveAt(i); continue; }
                if ((w.a == a && w.b == b) || (w.a == b && w.b == a))
                {
                    All.RemoveAt(i);
                    Object.Destroy(w.gameObject);
                }
            }
        }

        void OnDisable() => All.Remove(this);

        public void SetColor(Color c) { if (_lr != null) { _lr.startColor = c; _lr.endColor = c; } }

        // Buildings don't move, so positions are set once on create; kept as a method in case a node moves.
        public void Refresh()
        {
            if (_lr == null || a == null || b == null) return;
            _lr.SetPosition(0, new Vector3(a.Pos.x, a.Pos.y, 0f));
            _lr.SetPosition(1, new Vector3(b.Pos.x, b.Pos.y, 0f));
        }
    }
}
