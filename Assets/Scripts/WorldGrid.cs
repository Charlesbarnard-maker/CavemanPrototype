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

        public static void Remove<T>(Dictionary<Vector2Int, T> map, Vector2Int cell, T who) where T : class
        {
            if (map.TryGetValue(cell, out var cur) && ReferenceEquals(cur, who)) map.Remove(cell);
        }
    }
}
