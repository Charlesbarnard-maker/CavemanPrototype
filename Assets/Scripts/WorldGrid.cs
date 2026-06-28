using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Maps grid cells to the building occupying them, so belts (and placement) can
    /// look up "what's at this cell?" in O(1) instead of scanning every building each
    /// tick. Buildings register/unregister themselves in OnEnable/OnDisable.
    /// </summary>
    public static class WorldGrid
    {
        public static readonly Dictionary<Vector2Int, ProductionBuilding> Collectors = new();
        public static readonly Dictionary<Vector2Int, WorkshopBuilding> Workshops = new();
        public static readonly Dictionary<Vector2Int, StorageBuilding> Storages = new();
        public static readonly Dictionary<Vector2Int, Depot> Depots = new();
        public static readonly Dictionary<Vector2Int, ResearchBuilding> Research = new();
        public static readonly Dictionary<Vector2Int, PowerPlant> Generators = new(); // belt-fed fuel intake

        // Transport corridors that RESERVE their tiles (block belts/buildings on top, and vice-versa).
        // Tiles (RoadTile/RailTile) self-register here when that phase lands; empty until then, so
        // IsReserved is false everywhere for now (zero gameplay change). Typed as MonoBehaviour so this
        // plumbing compiles before the tile classes exist.
        public static readonly Dictionary<Vector2Int, MonoBehaviour> Roads = new();
        public static readonly Dictionary<Vector2Int, MonoBehaviour> Rails = new();

        /// <summary>Is this cell occupied by a transport corridor (road or rail)? The single check
        /// every placement gate routes through so reserved tiles block uniformly.</summary>
        public static bool IsReserved(Vector2Int cell) => Roads.ContainsKey(cell) || Rails.ContainsKey(cell);

        public static void Remove<T>(Dictionary<Vector2Int, T> map, Vector2Int cell, T who) where T : class
        {
            if (map.TryGetValue(cell, out var cur) && ReferenceEquals(cur, who)) map.Remove(cell);
        }
    }
}
