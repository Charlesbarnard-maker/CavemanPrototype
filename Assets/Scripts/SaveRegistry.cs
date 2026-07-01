using System.Collections.Generic;

namespace Caveman
{
    /// <summary>
    /// Stable id → definition lookups so the save system can reference content by a durable key instead
    /// of an object reference (definitions are rebuilt from code every launch — see GameBootstrap — so a
    /// save only stores ids). Items key by their <see cref="ItemDefinition.id"/>; buildings by
    /// <see cref="BuildingDefinition.displayName"/> (unique per building). Populated by GameBootstrap as
    /// it creates the content.
    /// </summary>
    public static class SaveRegistry
    {
        public static readonly Dictionary<string, ItemDefinition> Items = new();
        public static readonly Dictionary<string, BuildingDefinition> Buildings = new();

        public static void RegisterItem(ItemDefinition item)
        {
            if (item == null || string.IsNullOrEmpty(item.id)) return;
            Items[item.id] = item;
        }

        public static void RegisterBuildings(IEnumerable<BuildingDefinition> defs)
        {
            if (defs == null) return;
            foreach (var d in defs)
                if (d != null && !string.IsNullOrEmpty(d.displayName)) Buildings[d.displayName] = d;
        }

        public static ItemDefinition Item(string id)
            => !string.IsNullOrEmpty(id) && Items.TryGetValue(id, out var i) ? i : null;

        public static BuildingDefinition Building(string name)
            => !string.IsNullOrEmpty(name) && Buildings.TryGetValue(name, out var d) ? d : null;

        /// <summary>Wipe on a fresh game so a reload can't carry stale content refs (statics persist across
        /// Play sessions when domain-reload-on-play is disabled).</summary>
        public static void Reset() { Items.Clear(); Buildings.Clear(); }
    }
}
