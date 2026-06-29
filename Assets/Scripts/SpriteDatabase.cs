using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// THE central visual layer. ALL gameplay rendering resolves its sprite here: an external sprite if
    /// one exists, otherwise the procedural placeholder shape (the current code-generated art). This is
    /// the single seam for sprite-pack replacement — assign names in the type→name maps below (or drop
    /// sprites into Assets/Resources/art) and the whole game re-skins with NO other code change.
    ///
    /// Two external sources, tried in order by <see cref="LoadExternal"/>:
    ///   1. The imported Kenney **Roguelike sheet** (Assets/Resources/Roguelike/roguelikeSheet_transparent,
    ///      a multi-sprite texture). Its sub-sprites are named "roguelikeSheet_transparent_&lt;index&gt;";
    ///      use <see cref="RL"/> to address one by (col,row) on the 57-wide grid. See ApplyRoguelikeSkin().
    ///   2. A standalone sprite at Resources/art/&lt;name&gt; (the original convention — still works).
    /// A name that matches neither falls back to the procedural shape, so the game always renders.
    ///
    /// The convention for un-skinned types: filename = sanitised name (e.g. building "Basic Smelter" →
    /// "basic_smelter"). Seed() pre-fills the maps with those expected names; ApplyRoguelikeSkin() then
    /// overrides the entries we've hand-picked a real tile for.
    /// </summary>
    public static class SpriteDatabase
    {
        public const string Folder = "art/"; // Resources subfolder standalone imported sprites are expected in

        // ---- Roguelike sheet addressing -------------------------------------------------------
        // The imported pack is a sliced 16px sheet, 57 tiles wide, sub-sprites named "<prefix>_<index>"
        // with index = row*57 + col (row 0 = top). RL(col,row) builds that name so the skin table reads
        // as grid coordinates you can verify against the sheet.
        public const string SheetResourcePath = "Roguelike/roguelikeSheet_transparent"; // under Resources/
        public const string SheetPrefix = "roguelikeSheet_transparent";
        public const int SheetCols = 57;
        public static string RL(int index) => SheetPrefix + "_" + index;
        public static string RL(int col, int row) => RL(row * SheetCols + col);

        // Central type → sprite-name maps (the assignment table — the one place to wire a pack).
        public static readonly Dictionary<string, string> Building = new(); // building displayName → sprite name
        public static readonly Dictionary<string, string> Resource = new(); // resource item id     → sprite name
        public static readonly Dictionary<string, string> Belt = new();     // belt type name       → sprite name
        public static readonly Dictionary<string, string> Item = new();     // item id (icon)       → sprite name

        private static readonly Dictionary<string, Sprite> _cache = new();
        private static Dictionary<string, Sprite> _sheet; // null until first lookup loads the pack sheet

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
            var bespoke = PlaceholderArt.BespokeBuilding(def.displayName); // hand-tuned stone-age art wins over the pack
            if (bespoke != null) return bespoke;
            string name = Lookup(Building, def.displayName);
            if (name == null && def.sprite != null && !string.IsNullOrEmpty(def.sprite.name)) name = def.sprite.name;
            var ext = LoadExternal(name);
            // No external/pack tile → a procedural STRUCTURE varied per building (real-looking, distinct).
            return ext != null ? ext : PlaceholderArt.BuildingSprite(def);
        }

        /// <summary>Belt visual — central Belt map by type name; fallback conveyor tile (a matching
        /// belt-family splitter/merger junction for those, else a plain conveyor).</summary>
        public static Sprite ForBelt(string beltName, bool splitter, bool merger)
            => Resolve(Lookup(Belt, beltName), splitter ? PlaceholderShape.Splitter
                                             : merger   ? PlaceholderShape.Merger
                                                        : PlaceholderShape.Conveyor);

        /// <summary>Resource-node visual — central Resource map by item id; caller supplies the fallback shape.</summary>
        public static Sprite ForResource(ItemDefinition item, PlaceholderShape fallback)
            => Resolve(item != null ? Lookup(Resource, item.id) : null, fallback);

        /// <summary>Item visual (icon / belt dot) — central Item map by id, else the item's own
        /// SpriteDefinition (fallback Circle).</summary>
        public static Sprite ForItem(ItemDefinition item)
        {
            if (item == null) return Procedural(PlaceholderShape.Circle);
            string name = Lookup(Item, item.id);
            if (name != null)
                return Resolve(name, item.sprite != null ? item.sprite.fallback : PlaceholderShape.Circle);
            return Resolve(item.sprite);
        }

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

        // ---- The Kenney Roguelike skin: hand-picked tiles for the entities the pack can dress -------
        // Each value is RL(col,row) on the sheet (verified against the art). It's an RPG/dungeon pack,
        // not a factory set, so we only override where a tile clearly fits; everything else keeps its
        // procedural shape. Extend freely — read coordinates straight off the sheet (col across, row down).
        // Safe to call even if the sheet isn't imported: unmatched names just fall back to procedural.
        public static void ApplyRoguelikeSkin()
        {
            // Processors / machines (full-cell, static — the clearest wins).
            Building["Kiln"]             = RL(13, 0);  // hearth with fire
            Building["Campfire"]         = RL(13, 8);  // lit campfire
            Building["Toolmaker"]        = RL(15, 0);  // anvil
            Building["Potter"]           = RL(41, 16); // mortar & pestle
            Building["Basic Smelter"]    = RL(54, 8);  // arched furnace, lit
            Building["Advanced Smelter"] = RL(54, 10); // pale arched furnace, lit
            Building["Coal Generator"]   = RL(54, 9);  // dark furnace, lit
            // Storage / logistics / research.
            Building["Warehouse"]        = RL(38, 10); // treasure chest
            Building["Granary"]          = RL(24, 2);  // grain sack
            Building["Research Lodge"]   = RL(46, 12); // full bookshelf
            // (Station + Track draw their own art now — a platform with a track lane, and procedural rails.)

            // Resource nodes (the world props the player harvests).
            Resource["wood"]       = RL(12, 9);  // green tree
            Resource["stone"]      = RL(54, 21); // grey boulder pile
            Resource["clay"]       = RL(54, 19); // earthy mound
            Resource["copper_ore"] = RL(43, 11); // gold/copper nugget pile
            Resource["ore"]        = RL(55, 21); // grey ore boulders

            // A few item icons distinct enough to read on a belt dot (ingots + research goods).
            Item["copper"]       = RL(41, 10); // copper/bronze ingot
            Item["metal"]        = RL(42, 10); // iron ingot
            Item["steel"]        = RL(42, 10); // iron/steel ingot
            Item["bronze_plate"] = RL(43, 10); // gold/bronze ingot
            Item["scroll"]       = RL(44, 15); // parchment scroll  (Study Scroll)
            Item["schematic"]    = RL(48, 15); // green book        (Schematic)
            Item["blueprint"]    = RL(49, 15); // teal book         (Blueprint)

            ClearCache(); // re-resolve any sprites cached before the skin was applied
        }

        // ---- External load: pack sheet sub-sprite, else a standalone Resources/art sprite -----------
        private static Sprite LoadExternal(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_cache.TryGetValue(name, out var c)) return c;
            var s = LoadFromSheet(name);                  // a sub-sprite of the imported pack sheet, if it matches
            if (s == null) s = Resources.Load<Sprite>(Folder + name); // else a standalone Resources/art/<name>
            _cache[name] = s;                             // cache the result (incl. null → "no external, use fallback")
            return s;
        }

        // Lazily load the multi-sprite pack sheet once and index its sub-sprites by name. Returns null
        // (and never throws) if the sheet isn't present or the name doesn't match — caller falls back.
        private static Sprite LoadFromSheet(string name)
        {
            if (name == null || !name.StartsWith(SheetPrefix)) return null;
            if (_sheet == null)
            {
                _sheet = new Dictionary<string, Sprite>();
                var all = Resources.LoadAll<Sprite>(SheetResourcePath);
                foreach (var sp in all) if (sp != null) _sheet[sp.name] = sp;
            }
            return _sheet.TryGetValue(name, out var r) ? r : null;
        }

        private static Sprite Procedural(PlaceholderShape shape) => shape switch
        {
            PlaceholderShape.Circle => PlaceholderArt.Circle(),
            PlaceholderShape.Triangle => PlaceholderArt.Triangle(),
            PlaceholderShape.Hexagon => PlaceholderArt.Hexagon(),
            PlaceholderShape.Conveyor => PlaceholderArt.Conveyor(),
            PlaceholderShape.Splitter => PlaceholderArt.SplitterSprite(),
            PlaceholderShape.Merger => PlaceholderArt.MergerSprite(),
            PlaceholderShape.Tree => PlaceholderArt.Tree(),
            PlaceholderShape.Rock => PlaceholderArt.Rock(),
            PlaceholderShape.None => null,
            _ => PlaceholderArt.Square(),
        };

        /// <summary>Clear the resolved-sprite cache (e.g. after importing a pack or changing the skin).</summary>
        public static void ClearCache() => _cache.Clear();
    }
}
