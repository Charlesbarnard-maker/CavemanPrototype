using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>Registry of pipe cells — the liquid network's connectivity graph.</summary>
    public static class PipeNet
    {
        public static readonly Dictionary<Vector2Int, Pipe> Pipes = new();
        // Cells next to a Booster Pump: when a pump's pressure flood reaches one (in range),
        // pressure resets to full there, extending the network another full range onward.
        public static readonly HashSet<Vector2Int> BoostCells = new();
        public static int Count => Pipes.Count;
        public static Pipe At(Vector2Int c) => Pipes.TryGetValue(c, out var p) ? p : null;

        private static readonly Belt.Dir[] _dirs = { Belt.Dir.N, Belt.Dir.E, Belt.Dir.S, Belt.Dir.W };
        // Scratch buffers reused by NetworkFluid so a placement check doesn't allocate.
        private static readonly HashSet<Vector2Int> _seen = new();
        private static readonly Queue<Vector2Int> _q = new();

        /// <summary>The single fluid a connected pipe network is fed by, found by flooding the component from
        /// <paramref name="seed"/> and scanning for adjacent SOURCE pumps/wells (non-booster). Returns null if
        /// no source touches it yet; sets <paramref name="clash"/> if two DIFFERENT source fluids already share
        /// the network (which placement is meant to prevent). Works even when nothing is currently pumping.</summary>
        public static ItemDefinition NetworkFluid(Vector2Int seed, out bool clash)
        {
            clash = false;
            if (At(seed) == null) return null;
            _seen.Clear(); _q.Clear();
            _seen.Add(seed); _q.Enqueue(seed);
            while (_q.Count > 0)
            {
                var c = _q.Dequeue();
                foreach (var d in _dirs)
                {
                    var nb = c + Belt.Step(d);
                    if (At(nb) != null && _seen.Add(nb)) _q.Enqueue(nb);
                }
            }
            ItemDefinition found = null;
            foreach (var p in WaterPump.All)
            {
                if (p == null || p.isBooster || p.water == null) continue;
                var pc = new Vector2Int(Mathf.RoundToInt(p.transform.position.x), Mathf.RoundToInt(p.transform.position.y));
                bool touches = false;
                foreach (var d in _dirs) if (_seen.Contains(pc + Belt.Step(d))) { touches = true; break; }
                if (!touches) continue;
                if (found == null) found = p.water;
                else if (found != p.water) clash = true;
            }
            return found;
        }
    }

    /// <summary>
    /// A pipe segment. Unlike a belt, pipes carry no discrete items — they're a CONNECTIVITY network: a Water
    /// Pump pushes water through connected pipes into reachable storage (continuous, topology-dependent).
    /// Undirected; drag to lay a run. Each tile AUTO-CONNECTS to its neighbours (its sprite shows only the arms
    /// that join), tints to whatever fluid its network carries, and shows an animated droplet sliding along it
    /// in the flow direction while a source is pushing through — so direction + "is it flowing" read at a glance.
    /// </summary>
    public class Pipe : MonoBehaviour
    {
        public Vector2Int cell;
        public bool isSplitter, isMerger;     // junction pieces (distinct valve look); flow still floods through
        public ItemDefinition fluid;          // the fluid this cell currently carries (for the tint); null = empty

        private SpriteRenderer _sr;
        private SpriteRenderer _drop;          // the animated flow droplet (hidden until fluid moves)
        private int _mask = -1;                // connection mask the current sprite is drawn for
        private float _flowSeen = -1f;         // Time.time a source last pushed through here (decays the pulse)
        private Belt.Dir _flowDir = Belt.Dir.E;
        private static readonly Belt.Dir[] _dirs = { Belt.Dir.N, Belt.Dir.E, Belt.Dir.S, Belt.Dir.W };

        public static Pipe Spawn(BuildingDefinition def, Vector3 pos)
        {
            var cell = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
            var go = new GameObject("Pipe");
            go.transform.position = new Vector3(cell.x, cell.y, 0f);
            go.transform.localScale = Vector3.one; // full cell, so adjacent tubes' arms meet at the shared edge

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 0; // above terrain/bridge (-90/-80), below belts (1) & buildings (5)

            go.AddComponent<BoxCollider2D>(); // clickable to demolish

            var p = go.AddComponent<Pipe>();
            p.cell = cell;
            p._sr = sr;
            p.isSplitter = def != null && def.splitter;
            p.isMerger = def != null && def.merger;
            PipeNet.Pipes[cell] = p;

            // The flow droplet rides over the tube; faint until a source actually pushes fluid through.
            var dgo = new GameObject("flow");
            dgo.transform.SetParent(go.transform);
            dgo.transform.localScale = Vector3.one * 0.28f;
            p._drop = dgo.AddComponent<SpriteRenderer>();
            p._drop.sprite = PlaceholderArt.PipeDroplet();
            p._drop.sortingOrder = 1;
            p._drop.enabled = false;

            p.Refresh();
            // Re-shape the neighbours so their masks include this new pipe (corners/tees update live).
            foreach (var d in _dirs) PipeNet.At(cell + Belt.Step(d))?.Refresh();
            return p;
        }

        /// <summary>A source pushing fluid through this cell: marks it flowing in <paramref name="dir"/> and
        /// records the fluid (for the tint). Called by WaterPump / Depot as they flood the network.</summary>
        public void MarkFlow(ItemDefinition f, Belt.Dir dir)
        {
            fluid = f;
            _flowDir = dir;
            _flowSeen = Time.time;
        }

        private int NeighbourMask()
        {
            int m = 0;
            if (PipeNet.At(cell + Belt.Step(Belt.Dir.N)) != null) m |= 1;
            if (PipeNet.At(cell + Belt.Step(Belt.Dir.E)) != null) m |= 2;
            if (PipeNet.At(cell + Belt.Step(Belt.Dir.S)) != null) m |= 4;
            if (PipeNet.At(cell + Belt.Step(Belt.Dir.W)) != null) m |= 8;
            return m;
        }

        // Empty pipe reads as neutral metal; once it carries a fluid it tints to that fluid's colour.
        private static Color BodyColor(ItemDefinition f)
            => f == null ? new Color(0.62f, 0.66f, 0.72f) : Color.Lerp(f.color, Color.white, 0.15f);

        /// <summary>Re-pick the connection sprite (or junction valve) and re-tint to the carried fluid.</summary>
        public void Refresh()
        {
            if (_sr == null) return;
            bool junction = isSplitter || isMerger;
            int m = NeighbourMask();
            if (junction)
            {
                if (_mask != 1000) { _mask = 1000; _sr.sprite = PlaceholderArt.PipeValve(isMerger); }
            }
            else if (m != _mask) { _mask = m; _sr.sprite = PlaceholderArt.PipeTile(m); }
            _sr.color = BodyColor(fluid);
        }

        void Update()
        {
            if (_sr == null) return;
            _sr.color = Color.Lerp(_sr.color, BodyColor(fluid), 0.15f);
            bool flowing = _flowSeen >= 0f && (Time.time - _flowSeen) < 0.7f;
            if (_drop == null) return;
            _drop.enabled = flowing;
            if (!flowing) return;
            var step = Belt.Step(_flowDir);
            float t = (Time.time * 1.7f) % 1f; // 0→1 sweep entry-edge → exit-edge
            Vector3 from = new Vector3(-step.x, -step.y, 0f) * 0.5f;
            Vector3 to = new Vector3(step.x, step.y, 0f) * 0.5f;
            _drop.transform.position = transform.position + Vector3.Lerp(from, to, t);
            var fc = fluid != null ? fluid.color : new Color(0.6f, 0.85f, 1f);
            _drop.color = new Color(Mathf.Min(1f, fc.r + 0.35f), Mathf.Min(1f, fc.g + 0.35f), Mathf.Min(1f, fc.b + 0.4f), 0.95f);
        }

        void OnDisable()
        {
            if (PipeNet.Pipes.TryGetValue(cell, out var x) && x == this) PipeNet.Pipes.Remove(cell);
            foreach (var d in _dirs) PipeNet.At(cell + Belt.Step(d))?.Refresh(); // neighbours drop this connection
        }
    }
}
