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
    }

    /// <summary>
    /// A pipe segment. Unlike a belt, pipes carry no discrete items — they're a CONNECTIVITY
    /// network: a Water Pump pushes water through connected pipes into reachable storage
    /// (continuous flow, topology-dependent). Undirected; drag to lay a run.
    /// </summary>
    public class Pipe : MonoBehaviour
    {
        private Vector2Int _cell;

        public static Pipe Spawn(BuildingDefinition def, Vector3 pos)
        {
            var cell = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
            var go = new GameObject("Pipe");
            go.transform.position = new Vector3(cell.x, cell.y, 0f);
            go.transform.localScale = Vector3.one * 0.55f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = def != null ? def.color : new Color(0.40f, 0.62f, 0.85f);
            sr.sortingOrder = 0; // above terrain/bridge (-90/-80), below belts (1) & buildings (5)

            go.AddComponent<BoxCollider2D>(); // clickable to demolish

            var p = go.AddComponent<Pipe>();
            p._cell = cell;
            PipeNet.Pipes[cell] = p;
            return p;
        }

        void OnDisable()
        {
            if (PipeNet.Pipes.TryGetValue(_cell, out var x) && x == this) PipeNet.Pipes.Remove(_cell);
        }
    }
}
