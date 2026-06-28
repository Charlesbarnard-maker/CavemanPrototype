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

        // Walk the track forward from this signal (in direction `d`), following turns, until the
        // next signal or a dead end. True if any train occupies a cell in that block.
        private bool BlockAheadOccupied(Belt.Dir d)
        {
            var prev = cell;
            var cur = cell + Belt.Step(d);
            for (int i = 0; i < 512; i++)
            {
                if (!RailNet.IsRail(cur)) return false;          // ran off the track → block open
                if (RailGraph.AnyTrainAt(cur)) return true;       // a train is in this block
                if (cur != cell && At(cur) != null) return false; // reached the next signal → block ends
                var nxt = NextAlong(prev, cur);
                if (nxt == cur) return false;                     // dead end
                prev = cur; cur = nxt;
            }
            return false;
        }

        // The next track cell continuing forward from `cur` (came from `prev`): straight on if possible,
        // else the single turning neighbour. (Assumes mostly non-branching track, as railways are.)
        private static Vector2Int NextAlong(Vector2Int prev, Vector2Int cur)
        {
            var straight = cur + (cur - prev);
            if (RailNet.IsRail(straight)) return straight;
            for (int d = 0; d < 4; d++)
            {
                var n = cur + Belt.Step((Belt.Dir)d);
                if (n != prev && RailNet.IsRail(n)) return n;
            }
            return cur;
        }
    }
}
