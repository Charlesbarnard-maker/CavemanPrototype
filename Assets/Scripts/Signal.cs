using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A rail signal (Workers-&-Resources style). It sits on a track cell and FACES a direction: a train
    /// may pass it only when travelling that way (so signals make a track ONE-WAY — pathfinding routes the
    /// legal way round a loop instead of doubling back), and only when the BLOCK AHEAD — the run of track
    /// up to the next signal — is clear of trains. The lamp shows that block live: RED while a train is in
    /// the block ahead (so a following train waits), GREEN once it clears the next signal. Enforcement is
    /// in <see cref="RouteVehicle"/> / <see cref="RailNet"/>; this is the placeable marker + lamp.
    /// </summary>
    public class Signal : MonoBehaviour
    {
        public static readonly Dictionary<Vector2Int, Signal> All = new();
        public Vector2Int cell;
        public Belt.Dir dir = Belt.Dir.E; // the direction a train may travel to pass this signal
        public bool bothWays;             // two-way block signal: pass EITHER way along its axis (still block-gated)
        private SpriteRenderer _sr;

        private static readonly Color Green = new Color(0.3f, 0.95f, 0.4f);
        private static readonly Color Red = new Color(0.95f, 0.3f, 0.3f);

        public static Signal At(Vector2Int c) => All.TryGetValue(c, out var s) ? s : null;

        /// <summary>May a train pass this signal travelling <paramref name="travel"/>? A one-way signal
        /// allows only its own <see cref="dir"/>; a two-way signal allows its axis (dir or its opposite).</summary>
        public bool Allows(Belt.Dir travel)
            => bothWays ? (travel == dir || travel == Belt.Opposite(dir)) : travel == dir;

        public static Signal Place(Vector2Int cell, Belt.Dir dir, bool bothWays = false)
        {
            var existing = At(cell);
            if (existing != null) { existing.dir = dir; existing.bothWays = bothWays; existing.Refresh(); return existing; }

            var go = new GameObject(bothWays ? "Two-Way Signal" : "Signal");
            go.transform.position = new Vector3(cell.x, cell.y, 0f);
            go.transform.localScale = Vector3.one * 0.5f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 7; // above rail (0) and belts (1)
            sr.color = Green;

            go.AddComponent<BoxCollider2D>(); // clickable to demolish

            var s = go.AddComponent<Signal>();
            s.cell = cell; s.dir = dir; s.bothWays = bothWays; s._sr = sr;
            All[cell] = s;
            s.Refresh();
            return s;
        }

        // Re-pick the sprite (one-way arrow vs two-way double arrow) and re-aim along the axis.
        private void Refresh()
        {
            if (_sr != null) _sr.sprite = bothWays ? PlaceholderArt.SignalBidir() : PlaceholderArt.Triangle();
            Orient();
        }

        void OnDisable() { if (All.TryGetValue(cell, out var s) && s == this) All.Remove(cell); }

        public void Orient() => transform.rotation = Quaternion.Euler(0f, 0f, Belt.Angle(dir));

        // The lamp is cosmetic, so refresh it ~5×/sec rather than walking the whole block every frame.
        private float _lampTimer;
        void Update()
        {
            _lampTimer -= Time.deltaTime;
            if (_lampTimer > 0f) return;
            _lampTimer = 0.2f;
            // Two-way signals guard the block on BOTH sides, so the lamp reflects either approach.
            bool occ = BlockAheadOccupied(dir) || (bothWays && BlockAheadOccupied(Belt.Opposite(dir)));
            if (_sr != null) _sr.color = occ ? Red : Green;
        }

        // Reused BFS scratch so the cosmetic lamp doesn't allocate (Update runs ~5×/sec per signal).
        private static readonly Queue<Vector2Int> _bfs = new();
        private static readonly HashSet<Vector2Int> _seen = new();

        // Walk the track forward from this signal (in direction `d`), following ALL branches, until each
        // hits the next signal or a dead end. True if any train occupies a cell in that block. BFS over the
        // branches so a JUNCTION can't hide a train on the other leg (the old single-branch walk could show
        // a false GREEN at a fork). The lamp is cosmetic; the train's own one-way/block gating is elsewhere.
        private bool BlockAheadOccupied(Belt.Dir d)
        {
            var start = cell + Belt.Step(d);
            if (!RailNet.IsRail(start) || !RailNet.Linked(cell, start)) return false; // nothing linked ahead → block open
            _bfs.Clear(); _seen.Clear();
            _seen.Add(cell);                                      // never walk back through this signal
            _bfs.Enqueue(start); _seen.Add(start);
            for (int guard = 0; _bfs.Count > 0 && guard < 512; guard++)
            {
                var c = _bfs.Dequeue();
                if (RailGraph.AnyTrainAt(c)) return true;         // a train is in this block
                if (At(c) != null) continue;                      // reached another signal → block boundary
                for (int k = 0; k < 4; k++)
                {
                    var n = c + Belt.Step((Belt.Dir)k);
                    if (!_seen.Contains(n) && RailNet.IsRail(n) && RailNet.Linked(c, n)) { _seen.Add(n); _bfs.Enqueue(n); }
                }
            }
            return false;
        }
    }
}
