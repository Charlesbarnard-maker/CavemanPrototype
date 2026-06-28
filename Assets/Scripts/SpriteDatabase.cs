using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// THE central visual layer. ALL gameplay rendering resolves its sprite here: an external sprite if
    /// one exists (loaded once from Resources/art/&lt;name&gt;), otherwise the procedural placeholder shape
    /// (the current code-generated art). This is the single seam for future sprite-pack replacement —
    /// drop sprites into Assets/Resources/art and/or set names in the type→name maps below, and the whole
    /// game re-skins with NO other code change. No external assets are referenced yet, so every Resolve
    /// currently returns the procedural fallback → visuals are identical to before.
    ///
    /// The convention: a type's sprite filename = its sanitised name (e.g. building "Basic Smelter" →
    /// Resources/art/basic_smelter). Seed() pre-fills the maps with those expected names.
    /// </summary>
    public static class SpriteDatabase
    {
        public const string Folder = "art/"; // Resources subfolder imported sprites are expected in

        // Central type → sprite-name maps (the assignment table — the one place to wire a pack).
        public static readonly Dictionary<string, string> Building = new(); // building displayName → sprite name
        public static readonly Dictionary<string, string> Resource = new(); // resource item id     → sprite name
        public static readonly Dictionary<string, string> Belt = new();     // belt type name       → sprite name

        private static readonly Dictionary<string, Sprite> _cache = new();

        // ---- Resolution (every renderer calls one of these) ------------------------------------
        public static Sprite Resolve(SpriteDefinition def)
        {
            if (def == null) return Procedural(PlaceholderShape.Square);
            var ext = LoadExternal(def.name);
            return ext != null ? ext : Procedural(def.fallback);
        }

        public static Sprite Resolve(string name, PlaceholderShape fallback)
        {
            var ext = LoadExternal(name);
            return ext != null ? ext : Procedural(fallback);
        }

        /// <summary>Building visual — central map name (else the def's own SpriteDefinition), else fallback.</summary>
        public static Sprite ForBuilding(BuildingDefinition def)
        {
            if (def == null) return Procedural(PlaceholderShape.Square);
            string name = Lookup(Building, def.displayName);
            if (name == null && def.sprite != null) name = def.sprite.name;
            var fb = def.sprite != null ? def.sprite.fallback : PlaceholderShape.Square;
            return Resolve(name, fb);
        }

        /// <summary>Belt visual — central Belt map by type name; fallback conveyor tile (hex for junctions).</summary>
        public static Sprite ForBelt(string beltName, bool splitter, bool merger)
            => Resolve(Lookup(Belt, beltName), (splitter || merger) ? PlaceholderShape.Hexagon : PlaceholderShape.Conveyor);

        /// <summary>Resource-node visual — central Resource map by item id; caller supplies the fallback shape.</summary>
        public static Sprite ForResource(ItemDefinition item, PlaceholderShape fallback)
            => Resolve(item != null ? Lookup(Resource, item.id) : null, fallback);

        /// <summary>Item visual (icon / belt dot) — the item's own SpriteDefinition (fallback Circle).</summary>
        public static Sprite ForItem(ItemDefinition item)
            => item != null ? Resolve(item.sprite) : Procedural(PlaceholderShape.Circle);

        private static string Lookup(Dictionary<string, string> map, string key)
            => (!string.IsNullOrEmpty(key) && map.TryGetValue(key, out var n) && !string.IsNullOrEmpty(n)) ? n : null;

        // ---- Pre-fill the assignment table with expected sprite names (filename = sanitised name) ----
        public static void Seed(IEnumerable<BuildingDefinition> buildables, IEnumerable<ItemDefinition> resourceItems)
        {
            if (buildables != null)
                foreach (var b in buildables)
                {
                    if (b == null || string.IsNullOrEmpty(b.displayName)) continue;
                    if (b.kind == BuildingKind.Belt) Belt[b.displayName] = Sanitize(b.displayName);
                    else Building[b.displayName] = Sanitize(b.displayName);
                }
            if (resourceItems != null)
                foreach (var r in resourceItems)
                    if (r != null && !string.IsNullOrEmpty(r.id)) Resource[r.id] = Sanitize(r.id);
        }

        private static string Sanitize(string s) => s.ToLowerInvariant().Replace(' ', '_').Replace("'", "");

        // ---- External load (null until a pack is imported) + procedural fallback ----------------
        private static Sprite LoadExternal(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_cache.TryGetValue(name, out var c)) return c;
            var s = Resources.Load<Sprite>(Folder + name); // null until matching assets exist
            _cache[name] = s;
            return s;
        }

        private static Sprite Procedural(PlaceholderShape shape) => shape switch
        {
            PlaceholderShape.Circle => PlaceholderArt.Circle(),
            PlaceholderShape.Triangle => PlaceholderArt.Triangle(),
            PlaceholderShape.Hexagon => PlaceholderArt.Hexagon(),
            PlaceholderShape.Conveyor => PlaceholderArt.Conveyor(),
            PlaceholderShape.None => null,
            _ => PlaceholderArt.Square(),
        };

        /// <summary>Clear the resolved-sprite cache (e.g. after importing a pack at runtime).</summary>
        public static void ClearCache() => _cache.Clear();
    }
}
