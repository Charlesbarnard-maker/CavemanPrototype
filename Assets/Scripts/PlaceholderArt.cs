using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Generates simple placeholder sprites in code so we need zero art assets
    /// during prototyping. Shared by everything that needs a quick shape.
    /// </summary>
    public static partial class PlaceholderArt
    {
        private static Sprite _square;
        private static Sprite _circle;
        private static Sprite _triangle;
        private static Sprite _hexagon;
        private static Sprite _ground;
        private static Sprite _conveyor;
        private static Sprite _rail;
        private static Sprite _signalBidir;
        private static Sprite _pole;
        private static Sprite[] _caveman;         // player avatar per age × walk-frame
        private static Sprite _tree, _rock, _boat, _clay, _oreCopper, _oreIron, _oilPatch, _pipe;
        private static Material _lineMat;

        /// <summary>One SHARED material for every LineRenderer (wires, route lines, reach rings, previews).
        /// They set their colour via the renderer's start/endColor, not the material, so a single shared
        /// material is safe and avoids leaking a `new Material` per object (Unity never GCs those).</summary>
        public static Material LineMaterial()
        {
            if (_lineMat == null)
            {
                var sh = Shader.Find("Sprites/Default");
                if (sh != null) _lineMat = new Material(sh);
            }
            return _lineMat;
        }

        /// <summary>A soft Perlin-noise ground sprite (organic tonal variation, baked colour)
        /// so the world doesn't read as one flat slab. Deterministic — no Random.</summary>
        public static Sprite Ground(Color baseCol)
        {
            if (_ground != null) return _ground;
            const int s = 256;
            var tex = new Texture2D(s, s) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float n = Mathf.PerlinNoise(x / (float)s * 4f, y / (float)s * 4f);
                    float n2 = Mathf.PerlinNoise(x / (float)s * 12f + 10f, y / (float)s * 12f + 5f);
                    float v = 0.85f + 0.22f * n + 0.06f * n2; // ~0.85 .. 1.13
                    px[y * s + x] = new Color(baseCol.r * v, baseCol.g * v, baseCol.b * v, 1f);
                }
            tex.SetPixels(px);
            tex.Apply();
            _ground = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _ground;
        }

        public static Sprite Square()
        {
            if (_square != null) return _square;
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _square;
        }

        public static Sprite Circle()
        {
            if (_circle != null) return _circle;
            const int s = 64;
            var tex = NewTex(s);
            var pixels = new Color[s * s];
            float c = (s - 1) / 2f;
            float r = s / 2f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = x - c, dy = y - c;
                    pixels[y * s + x] = (dx * dx + dy * dy <= r * r) ? Color.white : Clear;
                }
            tex.SetPixels(pixels);
            tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _circle;
        }

        /// <summary>An upward-pointing triangle (reads as a pine tree).</summary>
        public static Sprite Triangle()
        {
            if (_triangle != null) return _triangle;
            _triangle = Polygon(new[]
            {
                new Vector2(0.5f, 0.97f),
                new Vector2(0.03f, 0.05f),
                new Vector2(0.97f, 0.05f),
            });
            return _triangle;
        }

        /// <summary>A flat-top hexagon (reads as a rock/boulder).</summary>
        public static Sprite Hexagon()
        {
            if (_hexagon != null) return _hexagon;
            var v = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float a = Mathf.Deg2Rad * (60f * i + 30f);
                v[i] = new Vector2(0.5f + 0.46f * Mathf.Cos(a), 0.5f + 0.46f * Mathf.Sin(a));
            }
            _hexagon = Polygon(v);
            return _hexagon;
        }

        /// <summary>A conveyor-belt tile: a rounded rectangle with two forward chevrons (baked
        /// darker so they read on any colour tint). Local +Y is "forward", so rotating the belt by
        /// Belt.Angle(dir) aims the chevrons along travel. Replaces the old triangle so belts read
        /// as conveyors rather than pine trees.</summary>
        public static Sprite Conveyor()
        {
            if (_conveyor != null) return _conveyor;
            const int s = 64;
            var tex = NewTex(s);
            var px = new Color[s * s];
            float cx = (s - 1) * 0.5f;
            var belt = Color.white;
            var chev = new Color(0.42f, 0.42f, 0.42f, 1f); // baked dark → visible after any tint
            float[] apex = { 24f, 44f };                   // two stacked chevrons
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    if (!InRoundedRect(x, y, s, 3f, 12f)) { px[y * s + x] = Clear; continue; }
                    Color c = belt;
                    for (int k = 0; k < apex.Length; k++)
                    {
                        float dy = apex[k] - y; // rows below the apex form the "^" legs
                        if (dy >= 0f && dy <= 12f && Mathf.Abs(Mathf.Abs(x - cx) - dy) <= 2.5f) { c = chev; break; }
                    }
                    px[y * s + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            _conveyor = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _conveyor;
        }

        /// <summary>A railway-track tile: two steel rails running vertically over wooden sleepers (with a
        /// faint ballast bed), baked in colour. Local +Y is "along the track", so RailTile rotates this 90°
        /// for an east–west run. Reads as proper train track instead of the old grey block.</summary>
        public static Sprite Rail()
        {
            if (_rail != null) return _rail;
            const int s = 64;
            var tex = NewTex(s);
            var px = new Color[s * s];
            var bed = new Color(0.34f, 0.30f, 0.27f, 0.85f);  // ballast
            var tie = new Color(0.46f, 0.32f, 0.20f, 1f);     // wooden sleeper
            var rail = new Color(0.70f, 0.72f, 0.78f, 1f);    // steel rail
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1);
                    Color c = (fx > 0.12f && fx < 0.88f) ? bed : Clear; // ballast strip down the middle
                    // sleepers: horizontal bands across the bed, every 16 px
                    if (y % 16 < 6 && fx > 0.06f && fx < 0.94f) c = tie;
                    // two rails: vertical lines
                    if (Mathf.Abs(fx - 0.33f) < 0.05f || Mathf.Abs(fx - 0.67f) < 0.05f) c = rail;
                    px[y * s + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            _rail = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _rail;
        }

        // ---- Track tiles shaped to their NEIGHBOURS (so corners curve + junctions meet) ----------
        // mask bits: N=1, E=2, S=4, W=8 (1<<Belt.Dir). Cached per mask (16). A perpendicular pair draws a
        // CURVED corner; everything else draws straight rail "arms" from the centre toward each neighbour
        // (so opposite-pair = straight, 3 = T, 4 = cross, 1 = stub). Local space — no rotation needed.
        // ---- Conveyor tiles per TIER (look) and SHAPE (straight / corner) ------------------------
        // tier: 0 Wooden=rollers, 1 Conveyor=chevrons, 2 Geared=chevrons+teeth, 3 Steel=arrows.
        // shape: 0 straight, 1 corner from local-E (right), 2 corner from local-W (left). Output is local
        // +Y (the Belt rotates the tile to its travel dir). Drawn white+dark so the per-tier tint shows.
        private static Sprite[] _beltSprites;
        public static Sprite BeltSprite(int tier, int shape)
        {
            tier = Mathf.Clamp(tier, 0, 3); shape = Mathf.Clamp(shape, 0, 2);
            int key = tier * 3 + shape;
            if (_beltSprites == null) _beltSprites = new Sprite[12];
            if (_beltSprites[key] != null) return _beltSprites[key];
            const int s = 64;
            var tex = NewTex(s);
            var px = new Color[s * s];
            var belt = Color.white;
            var dark = new Color(0.38f, 0.38f, 0.40f, 1f);
            var edge = tier == 3 ? new Color(0.85f, 0.88f, 0.95f, 1f) : new Color(0.30f, 0.30f, 0.32f, 1f);
            if (shape == 0) BeltStraight(px, s, tier, belt, dark, edge);
            else BeltCorner(px, s, shape == 1, belt, dark, edge);
            tex.SetPixels(px); tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            _beltSprites[key] = sp;
            return sp;
        }

        // A STRAIGHT belt that fills the full cell along travel (local +Y), so consecutive belts read as
        // ONE continuous conveyor with no gap. The roller/chevron/arrow pattern uses a 16px period that
        // divides the 64px tile, so the moving detail tiles seamlessly across cell boundaries too.
        private static void BeltStraight(Color[] px, int s, int tier, Color belt, Color dark, Color edge)
        {
            float cx = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1);
                    Color c = Clear;
                    if (fx >= 0.05f && fx <= 0.95f) // belt surface spans the FULL height (top↔bottom edges)
                    {
                        c = belt;
                        if (fx < 0.17f || fx > 0.83f) c = edge; // side rails / frame
                        int p = y & 15;                          // seamless 16px period (64 = 4 × 16)
                        if (tier == 0) { if (p < 4) c = dark; }  // rollers: a dark crossbar each period
                        else
                        {
                            float dy = 13f - p;                  // apex near the top of each period; legs run down
                            if (tier == 3) { if (dy >= 0f && dy <= 13f && Mathf.Abs(x - cx) <= dy * 0.95f) c = dark; } // filled arrow
                            else { if (dy >= 0f && dy <= 13f && Mathf.Abs(Mathf.Abs(x - cx) - dy) <= 2.4f) c = dark; }   // chevron
                            if (tier == 2 && (fx < 0.17f || fx > 0.83f) && (y & 7) < 4) c = dark; // gear teeth on the rails
                        }
                    }
                    px[y * s + x] = c;
                }
        }

        // A quarter-turn belt band from the input edge (E if fromRight, else W) to the +Y output edge. The
        // band radius matches the straight body's width (rails at the same 0.17 inset) AND reaches both
        // shared edges, so a corner joins its straight neighbours edge-to-edge instead of looking cut off.
        private static void BeltCorner(Color[] px, int s, bool fromRight, Color belt, Color dark, Color edge)
        {
            float cx = fromRight ? 1f : 0f, cy = 1f; // arc centre on the cell corner between input + output
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    float dx = fx - cx, dy = fy - cy, d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color c = Clear;
                    if (d >= 0.05f && d <= 0.95f) // full-width band → meets the straight body at both ends
                    {
                        c = belt;
                        if (d < 0.17f || d > 0.83f) c = edge; // inner/outer rails (match the straight's inset)
                        float ang = Mathf.Atan2(Mathf.Abs(dy), Mathf.Abs(dx)); // 0..π/2 along the curve
                        if (Frac(ang / (Mathf.PI * 0.5f) * 3f) < 0.28f && d >= 0.24f && d <= 0.76f) c = dark; // chevron ticks
                    }
                    px[y * s + x] = c;
                }
        }

        /// <summary>A small power pole — a wooden post with a crossarm + insulators, baked-colour so it
        /// reads as a pole on any tint. Kept small on the grid; it's a pure wired relay (see PowerPole).</summary>
        public static Sprite Pole()
        {
            if (_pole != null) return _pole;
            const int s = 64;
            var tex = NewTex(s);
            var px = new Color[s * s];
            var wood = new Color(0.45f, 0.32f, 0.20f, 1f);
            var metal = new Color(0.78f, 0.80f, 0.85f, 1f);
            float cx = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fy = y / (float)(s - 1);
                    float dx = Mathf.Abs(x - cx);
                    Color c = Clear;
                    if (dx <= 3.5f && fy > 0.06f && fy < 0.94f) c = wood;                 // vertical post
                    if (fy >= 0.70f && fy <= 0.77f && dx <= 20f) c = wood;                // crossarm near the top
                    if (fy > 0.77f && fy < 0.87f && Mathf.Abs(dx - 18f) <= 3f) c = metal; // insulators at the arm ends
                    if (fy >= 0.90f && dx <= 3.5f) c = metal;                             // metal top cap
                    px[y * s + x] = c;
                }
            tex.SetPixels(px); tex.Apply();
            _pole = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _pole;
        }

        /// <summary>A double-headed arrow (points both ways along local +Y) for a TWO-WAY rail signal —
        /// visibly distinct from the one-way triangle. Symmetric, so it reads the same whichever way it aims.</summary>
        public static Sprite SignalBidir()
        {
            if (_signalBidir != null) return _signalBidir;
            const int s = 64;
            var tex = NewTex(s);
            var px = new Color[s * s];
            float cx = (s - 1) * 0.5f;
            var col = Color.white;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fy = y / (float)(s - 1);
                    float dxp = Mathf.Abs(x - cx) / (float)(s - 1); // 0..0.5 from the centreline
                    Color c = Clear;
                    if (dxp <= 0.06f && fy > 0.22f && fy < 0.78f) c = col;          // central shaft
                    if (fy >= 0.62f && dxp <= (0.95f - fy) * 1.3f) c = col;         // top head (points up)
                    if (fy <= 0.38f && dxp <= (fy - 0.05f) * 1.3f) c = col;         // bottom head (points down)
                    px[y * s + x] = c;
                }
            tex.SetPixels(px); tex.Apply();
            _signalBidir = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _signalBidir;
        }

        private static Sprite[] _railMasks;
        public static Sprite RailMask(int mask)
        {
            mask &= 15;
            if (_railMasks == null) _railMasks = new Sprite[16];
            if (_railMasks[mask] != null) return _railMasks[mask];
            const int s = 64;
            var tex = NewTex(s);
            var px = new Color[s * s];
            bool n = (mask & 1) != 0, e = (mask & 2) != 0, so = (mask & 4) != 0, w = (mask & 8) != 0;
            int cnt = (n ? 1 : 0) + (e ? 1 : 0) + (so ? 1 : 0) + (w ? 1 : 0);
            bool corner = cnt == 2 && !((n && so) || (e && w)); // 2 neighbours, not an opposite pair
            var bed = new Color(0.34f, 0.30f, 0.27f, 0.85f);
            var tie = new Color(0.46f, 0.32f, 0.20f, 1f);
            var rl = new Color(0.70f, 0.72f, 0.78f, 1f);
            if (corner) RailDrawCorner(px, s, n, e, bed, tie, rl);
            else RailDrawArms(px, s, mask == 0 ? 5 : mask, bed, tie, rl); // isolated → a straight N–S piece
            tex.SetPixels(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            _railMasks[mask] = sp;
            return sp;
        }

        private static float Frac(float v) => v - Mathf.Floor(v);

        // Straight rail arms from centre toward each set edge (vertical for N/S, horizontal for E/W).
        private static void RailDrawArms(Color[] px, int s, int mask, Color bed, Color tie, Color rl)
        {
            bool n = (mask & 1) != 0, e = (mask & 2) != 0, so = (mask & 4) != 0, w = (mask & 8) != 0;
            bool vert = n || so, horz = e || w;
            float vyLo = so ? 0f : 0.5f, vyHi = n ? 1f : 0.5f; // vertical-rail extent (S→bottom, N→top)
            float hxLo = w ? 0f : 0.5f, hxHi = e ? 1f : 0.5f;  // horizontal-rail extent
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;
                    if (((n && fy >= 0.5f) || (so && fy <= 0.5f)) && Mathf.Abs(fx - 0.5f) < 0.42f) c = bed;
                    if (((e && fx >= 0.5f) || (w && fx <= 0.5f)) && Mathf.Abs(fy - 0.5f) < 0.42f) c = bed;
                    if (vert && Mathf.Abs(fx - 0.5f) < 0.40f && fy >= vyLo && fy <= vyHi && Frac(fy / 0.22f) < 0.42f) c = tie;
                    if (horz && Mathf.Abs(fy - 0.5f) < 0.40f && fx >= hxLo && fx <= hxHi && Frac(fx / 0.22f) < 0.42f) c = tie;
                    if (vert && (Mathf.Abs(fx - 0.33f) < 0.05f || Mathf.Abs(fx - 0.67f) < 0.05f) && fy >= vyLo && fy <= vyHi) c = rl;
                    if (horz && (Mathf.Abs(fy - 0.33f) < 0.05f || Mathf.Abs(fy - 0.67f) < 0.05f) && fx >= hxLo && fx <= hxHi) c = rl;
                    px[y * s + x] = c;
                }
        }

        // A curved quarter-turn through the corner between the two set edges. The ballast band is as WIDE as
        // a straight tile's (|f-0.5|<0.42 → d∈[0.08,0.92]) and the rails sit at d=0.33/0.67, so the curve
        // meets the neighbouring straight track at the exact same width + rail positions (no pinched / cut-off
        // corner). Curved sleepers cross the bed at angular intervals to match the straight ties.
        private static void RailDrawCorner(Color[] px, int s, bool n, bool e, Color bed, Color tie, Color rl)
        {
            float cx = e ? 1f : 0f, cy = n ? 1f : 0f; // arc centred on the cell corner between the two edges
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    bool xside = e ? fx >= 0.5f : fx <= 0.5f;
                    bool yside = n ? fy >= 0.5f : fy <= 0.5f;
                    Color c = Clear;
                    if (xside && yside)
                    {
                        float dx = fx - cx, dy = fy - cy, d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d >= 0.08f && d <= 0.92f) c = bed;
                        float ang = Mathf.Atan2(Mathf.Abs(dy), Mathf.Abs(dx)); // 0..π/2 round the curve
                        if (d >= 0.14f && d <= 0.86f && Frac(ang / (Mathf.PI * 0.5f) * 4f) < 0.34f) c = tie; // curved sleepers
                        if (Mathf.Abs(d - 0.33f) < 0.05f || Mathf.Abs(d - 0.67f) < 0.05f) c = rl;
                    }
                    px[y * s + x] = c;
                }
        }

        // ====================================================================================
        //  Procedural BUILDING + CHARACTER + RESOURCE art — a "real" look with no asset pack.
        //  Building bodies are WHITE (tinted by each building's def.color); roofs/details are baked
        //  darker so they read on any tint. Characters + resources are baked full-colour.
        // ====================================================================================

        private enum BK { House, Workshop, Crate, Lab, Generator, Platform }

        /// <summary>A procedural building STRUCTURE keyed by kind (hut / workshop / crate / lab / generator /
        /// platform) — the fallback when no external pack tile is assigned, so un-skinned buildings read as
        /// real structures instead of flat squares. White body (tinted by def.color) + baked detail.</summary>
        /// <summary>Hand-tuned bespoke art for EVERY buildable structure, age by age — wins over the generic
        /// archetype AND any asset-pack tile. Each method lives in its own partial-class file under Bespoke/.
        /// Returns null only for infrastructure tiles (belts/track/pipe/pole/signals) that have their own
        /// dedicated procedural art, so those fall through to it.</summary>
        public static Sprite BespokeBuilding(string displayName) => displayName switch
        {
            // --- Age 0 (Stone) ---
            "Wood Hut" => StoneHut(),
            "Stone Pit" => StoneQuarry(),
            "Sawmill" => StoneSawmill(),
            "Idea Bench" => StoneIdeaBench(),
            "Research Lodge" => StoneLodge(),
            "Woodpile" => StoneWoodpile(),
            "Station" => StationPlatform(),
            "Harbour" => HarbourDock(),
            "Warehouse" => WarehouseShed(),
            "Water Barrel" => WaterBarrel(),
            // --- Age 1 (Tribal) ---
            "Clay Pit" => TribalClayPit(),
            "Copper Mine" => TribalCopperMine(),
            "Iron Mine" => TribalIronMine(),
            "Charcoal Burner" => TribalCharcoalBurner(),
            "Basic Smelter" => TribalBasicSmelter(),
            "Scroll Maker" => TribalScrollMaker(),
            "Clay Pile" => TribalClayPile(),
            // --- Age 2 (Bronze) ---
            "Kiln" => BronzeKiln(),
            "Potter" => BronzePotter(),
            "Advanced Smelter" => BronzeAdvSmelter(),
            "Refinery" => BronzeRefinery(),
            "Drafting Table" => BronzeDraftingTable(),
            "Brick Yard" => BronzeBrickYard(),
            "Oil Tank" => BronzeOilTank(),
            "Wood Generator" => BronzeWoodGenerator(),
            "Battery" => BronzeBattery(),
            "Water Pump" => BronzeWaterPump(),
            "Booster Pump" => BronzeBoosterPump(),
            "Oil Well" => BronzeOilWell(),
            // --- Age 3 (Iron) ---
            "Toolmaker" => IronToolmaker(),
            "Engineering Lab" => IronEngineeringLab(),
            "Coal Generator" => IronCoalGenerator(),
            "Oil Generator" => IronOilGenerator(),
            // --- Age 4 (Industrial) ---
            "Monument" => IndustrialMonument(),
            _ => null,
        };

        private static System.Collections.Generic.Dictionary<string, Sprite> _bldgByName;
        public static Sprite BuildingSprite(BuildingDefinition def)
        {
            if (def == null) return Square();
            _bldgByName ??= new System.Collections.Generic.Dictionary<string, Sprite>();
            string nm = def.displayName ?? "?";
            var bespoke = BespokeBuilding(nm);
            if (bespoke != null) return bespoke;
            if (_bldgByName.TryGetValue(nm, out var sp) && sp != null) return sp;
            BK bk = def.kind switch
            {
                BuildingKind.Workshop => BK.Workshop,
                BuildingKind.Storage => BK.Crate,
                BuildingKind.Research => BK.Lab,
                BuildingKind.Power => BK.Generator,
                BuildingKind.Pump => BK.Generator, // a pump is a machine, not a hut
                BuildingKind.Depot => BK.Platform,
                _ => BK.House,
            };
            int hash = 17; foreach (char ch in nm) hash = hash * 31 + ch; hash &= 0x7fffffff;
            var made = BakeBuilding(bk, hash);
            _bldgByName[nm] = made;
            return made;
        }

        // `hash` (from the building's name) varies the roof tone + stamps a distinguishing EMBLEM so two
        // buildings of the same kind (e.g. Sawmill vs Kiln) don't look identical.
        private static Sprite BakeBuilding(BK kind, int hash)
        {
            const int s = 64;
            var tex = NewTex(s);
            var px = new Color[s * s];
            var body = Color.white;                          // tinted by def.color
            var roofs = new[] { new Color(0.32f, 0.24f, 0.17f, 1f), new Color(0.28f, 0.20f, 0.24f, 1f), new Color(0.22f, 0.26f, 0.30f, 1f), new Color(0.36f, 0.28f, 0.16f, 1f) };
            var roof = roofs[hash % roofs.Length];           // per-building roof tone
            var dark = new Color(0.20f, 0.18f, 0.18f, 1f);   // door / vents / emblem
            var glass = new Color(0.55f, 0.78f, 0.92f, 1f);  // window
            var metal = new Color(0.72f, 0.74f, 0.80f, 1f);
            var smoke = new Color(0.82f, 0.82f, 0.84f, 0.85f);
            int emblem = (hash / 7) % 5;                      // 0 circle 1 triangle 2 square 3 diamond 4 cross
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    Color c = Clear;
                    switch (kind)
                    {
                        case BK.House:
                            if (fx >= 0.16f && fx <= 0.84f && fy >= 0.06f && fy <= 0.58f) c = body;
                            if (fy > 0.56f && fy <= 0.92f) { float h = Mathf.Lerp(0.42f, 0.02f, (fy - 0.56f) / 0.36f); if (Mathf.Abs(fx - 0.5f) <= h) c = roof; }
                            if (fx >= 0.44f && fx <= 0.56f && fy >= 0.06f && fy <= 0.30f) c = dark;            // door
                            if (fx >= 0.22f && fx <= 0.34f && fy >= 0.34f && fy <= 0.48f) c = glass;           // window
                            break;
                        case BK.Workshop:
                            if (fx >= 0.12f && fx <= 0.88f && fy >= 0.06f && fy <= 0.66f) c = body;
                            if (fy > 0.64f && fy <= 0.76f && fx >= 0.10f && fx <= 0.90f) c = roof;             // flat roof band
                            if (fx >= 0.66f && fx <= 0.78f && fy >= 0.66f && fy <= 0.94f) c = dark;            // chimney
                            if (fx >= 0.64f && fx <= 0.82f && fy > 0.92f) c = smoke;                           // smoke
                            if (fx >= 0.42f && fx <= 0.58f && fy >= 0.06f && fy <= 0.30f) c = dark;            // door
                            if (fx >= 0.18f && fx <= 0.36f && fy >= 0.34f && fy <= 0.52f) c = glass;           // window
                            break;
                        case BK.Crate:
                            if (fx >= 0.10f && fx <= 0.90f && fy >= 0.10f && fy <= 0.90f)
                            {
                                c = body;
                                bool border = fx < 0.16f || fx > 0.84f || fy < 0.16f || fy > 0.84f;
                                if (border || Mathf.Abs(fx - 0.5f) < 0.045f || Mathf.Abs(fy - 0.5f) < 0.045f) c = roof; // frame + cross
                            }
                            break;
                        case BK.Lab:
                            if (fx >= 0.16f && fx <= 0.84f && fy >= 0.06f && fy <= 0.58f) c = body;
                            if (fy > 0.56f && fy <= 0.90f) { float h = Mathf.Lerp(0.40f, 0.04f, (fy - 0.56f) / 0.34f); if (Mathf.Abs(fx - 0.5f) <= h) c = roof; }
                            if (fx >= 0.36f && fx <= 0.64f && fy >= 0.18f && fy <= 0.46f) c = glass;           // a "scroll/book"
                            if (fx >= 0.36f && fx <= 0.64f && Mathf.Abs(fy - 0.32f) < 0.02f) c = dark;
                            break;
                        case BK.Generator:
                            if (fx >= 0.12f && fx <= 0.80f && fy >= 0.06f && fy <= 0.64f) c = body;
                            if (fx >= 0.18f && fx <= 0.74f && (Mathf.Abs(fy - 0.22f) < 0.025f || Mathf.Abs(fy - 0.34f) < 0.025f || Mathf.Abs(fy - 0.46f) < 0.025f)) c = dark; // vents
                            if (fx >= 0.70f && fx <= 0.84f && fy >= 0.58f && fy <= 0.92f) c = metal;           // smokestack
                            if (fx >= 0.68f && fx <= 0.88f && fy > 0.90f) c = smoke;                           // smoke
                            break;
                        case BK.Platform:
                            if (fx >= 0.04f && fx <= 0.96f && fy >= 0.22f && fy <= 0.78f)
                            {
                                c = body;
                                if (fx < 0.09f || fx > 0.91f || fy < 0.27f || fy > 0.73f) c = roof;            // deck edge
                            }
                            break;
                    }
                    // Distinguishing emblem stamped on the building face (so same-kind buildings differ).
                    if (c == body)
                    {
                        float ex = fx - 0.5f, ey = fy - 0.42f;
                        bool em =
                            emblem == 0 ? (ex * ex + ey * ey <= 0.075f * 0.075f) :                                  // circle
                            emblem == 1 ? (ey >= -0.07f && ey <= 0.07f && Mathf.Abs(ex) <= (0.07f - ey) * 0.7f) :   // triangle
                            emblem == 2 ? (Mathf.Abs(ex) <= 0.06f && Mathf.Abs(ey) <= 0.06f) :                       // square
                            emblem == 3 ? (Mathf.Abs(ex) + Mathf.Abs(ey) <= 0.085f) :                               // diamond
                                          (Mathf.Abs(ex) <= 0.025f && Mathf.Abs(ey) <= 0.075f) || (Mathf.Abs(ey) <= 0.025f && Mathf.Abs(ex) <= 0.075f); // cross
                        if (em) c = dark;
                    }
                    px[y * s + x] = c;
                }
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        private static bool Disc(float a, float b, float r) => a * a + b * b <= r * r;

        /// <summary>The player avatar — a little caveman that re-skins as the colony advances ages:
        /// fur + club (Stone) → headband + spear (Tribal) → bronze → iron → hard-hat + wrench (Industrial).
        /// Baked full-colour (the renderer uses white tint).</summary>
        public static Sprite Caveman(int age, int frame = 0)
{
    age = Mathf.Clamp(age, 0, 4); frame = Mathf.Clamp(frame, 0, 2);
    if (_caveman == null) _caveman = new Sprite[15];
    int key = age * 3 + frame;
    if (_caveman[key] != null) return _caveman[key];
    const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

    // ===== SKIN SHADES (3) =====
    var skinL = new Color(0.93f, 0.76f, 0.59f, 1f);
    var skin  = new Color(0.84f, 0.65f, 0.49f, 1f);
    var skinD = new Color(0.66f, 0.47f, 0.34f, 1f);
    var dark  = new Color(0.09f, 0.07f, 0.07f, 1f); // outline / dark detail
    var eyeW  = new Color(0.97f, 0.96f, 0.93f, 1f);

    // ===== PER-AGE PALETTES: cloth / hair / tool (3 shades each) =====
    Color clothL, clothM, clothD, hairL, hairM, hairD, toolL, toolM, toolD;
    switch (age) {
        case 0:
            clothL = new Color(0.60f,0.45f,0.30f,1f); clothM = new Color(0.46f,0.33f,0.21f,1f); clothD = new Color(0.31f,0.21f,0.12f,1f);
            hairL  = new Color(0.35f,0.26f,0.19f,1f); hairM  = new Color(0.23f,0.16f,0.11f,1f); hairD  = new Color(0.12f,0.08f,0.06f,1f);
            toolL  = new Color(0.66f,0.49f,0.31f,1f); toolM  = new Color(0.51f,0.37f,0.23f,1f); toolD  = new Color(0.36f,0.25f,0.15f,1f);
            break;
        case 1:
            clothL = new Color(0.74f,0.54f,0.38f,1f); clothM = new Color(0.59f,0.41f,0.28f,1f); clothD = new Color(0.42f,0.28f,0.18f,1f);
            hairL  = new Color(0.88f,0.40f,0.31f,1f); hairM  = new Color(0.72f,0.27f,0.22f,1f); hairD  = new Color(0.51f,0.17f,0.14f,1f);
            toolL  = new Color(0.70f,0.53f,0.35f,1f); toolM  = new Color(0.55f,0.40f,0.26f,1f); toolD  = new Color(0.39f,0.27f,0.17f,1f);
            break;
        case 2:
            clothL = new Color(0.82f,0.72f,0.47f,1f); clothM = new Color(0.66f,0.56f,0.34f,1f); clothD = new Color(0.49f,0.41f,0.23f,1f);
            hairL  = new Color(0.93f,0.69f,0.43f,1f); hairM  = new Color(0.80f,0.55f,0.30f,1f); hairD  = new Color(0.59f,0.39f,0.19f,1f);
            toolL  = new Color(0.93f,0.69f,0.41f,1f); toolM  = new Color(0.82f,0.56f,0.30f,1f); toolD  = new Color(0.61f,0.41f,0.21f,1f);
            break;
        case 3:
            clothL = new Color(0.66f,0.70f,0.76f,1f); clothM = new Color(0.50f,0.54f,0.60f,1f); clothD = new Color(0.35f,0.39f,0.45f,1f);
            hairL  = new Color(0.82f,0.86f,0.92f,1f); hairM  = new Color(0.66f,0.70f,0.76f,1f); hairD  = new Color(0.47f,0.51f,0.57f,1f);
            toolL  = new Color(0.90f,0.93f,0.98f,1f); toolM  = new Color(0.72f,0.75f,0.82f,1f); toolD  = new Color(0.51f,0.54f,0.61f,1f);
            break;
        default:
            clothL = new Color(0.40f,0.56f,0.76f,1f); clothM = new Color(0.26f,0.42f,0.62f,1f); clothD = new Color(0.16f,0.29f,0.46f,1f);
            hairL  = new Color(0.99f,0.91f,0.42f,1f); hairM  = new Color(0.92f,0.80f,0.25f,1f); hairD  = new Color(0.69f,0.57f,0.15f,1f);
            toolL  = new Color(0.92f,0.94f,0.98f,1f); toolM  = new Color(0.75f,0.77f,0.82f,1f); toolD  = new Color(0.53f,0.55f,0.61f,1f);
            break;
    }

    // ===== WALK CYCLE: 3 frames, legs step (offset each leg vertically/forward) =====
    // frame 0 = stand/pass, frame 1 = left leg forward, frame 2 = right leg forward.
    float lLeg = frame == 1 ? 0.050f : frame == 2 ? -0.025f : 0f;
    float rLeg = frame == 2 ? 0.050f : frame == 1 ? -0.025f : 0f;
    // arms swing opposite to legs
    float armSwing = frame == 1 ? 0.035f : frame == 2 ? -0.035f : 0f;

    for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
        float fx = x/(float)(s-1), fy = y/(float)(s-1); float dxc = fx - 0.5f; Color c = Clear;

        // ===== LEGS (with feet) ===== (fy=0 is the BOTTOM, so feet are low)
        // left leg (screen-left)
        if (dxc >= -0.20f && dxc <= -0.05f && fy >= 0.06f + lLeg && fy <= 0.32f) {
            c = clothM;
            if (dxc <= -0.16f) c = clothD;                 // lower-right shadow edge
            else if (dxc >= -0.09f) c = clothL;            // upper-left lit edge
            if (fy <= 0.15f + lLeg) { c = skin; if (dxc <= -0.16f) c = skinD; else if (dxc >= -0.09f) c = skinL; }
        }
        // left foot
        if (dxc >= -0.22f && dxc <= -0.04f && fy >= 0.045f + lLeg && fy <= 0.085f + lLeg) { c = skinD; }
        // right leg (screen-right)
        if (dxc >= 0.05f && dxc <= 0.20f && fy >= 0.06f + rLeg && fy <= 0.32f) {
            c = clothM;
            if (dxc >= 0.16f) c = clothD;
            else if (dxc <= 0.09f) c = clothL;
            if (fy <= 0.15f + rLeg) { c = skin; if (dxc >= 0.16f) c = skinD; else if (dxc <= 0.09f) c = skinL; }
        }
        // right foot
        if (dxc >= 0.04f && dxc <= 0.22f && fy >= 0.045f + rLeg && fy <= 0.085f + rLeg) { c = skinD; }

        // ===== TORSO / FUR TUNIC =====
        if (Mathf.Abs(dxc) <= 0.19f && fy >= 0.30f && fy <= 0.58f) {
            c = clothM;
            if (dxc > 0.10f || fy < 0.34f) c = clothD;          // lower-right shadow
            else if (dxc < -0.06f && fy > 0.46f) c = clothL;    // upper-left highlight
            else if (fy > 0.52f) c = clothL;
        }
        // jagged fur hem at bottom of tunic (triangular notches)
        if (Mathf.Abs(dxc) <= 0.19f && fy >= 0.27f && fy < 0.32f) {
            float tooth = Frac(fx * 7.0f);
            float hemEdge = 0.285f + 0.030f * Mathf.Abs(tooth - 0.5f) * 2.0f;
            if (fy >= hemEdge) c = clothD;                       // keep notches, shade them
            else c = Clear;
        }
        // shoulder strap (diagonal band, upper-left to lower-right)
        {
            float strap = dxc + (fy - 0.50f) * 0.9f;
            if (Mathf.Abs(strap) <= 0.045f && fy >= 0.40f && fy <= 0.59f && Mathf.Abs(dxc) <= 0.20f) {
                c = clothD;
                if (strap < -0.01f) c = clothM;                  // slight highlight upper side
            }
        }

        // ===== ARMS + HANDS =====
        // left arm (screen-left) hangs by side; swings with armSwing
        if (dxc >= -0.29f && dxc <= -0.18f && fy >= 0.34f + armSwing && fy <= 0.55f) {
            c = skin; if (dxc <= -0.25f) c = skinD; else if (dxc >= -0.21f) c = skinL;
        }
        // left hand
        if (Disc(dxc + 0.235f, fy - (0.33f + armSwing), 0.048f)) { c = skin; if (dxc + 0.235f > 0.015f) c = skinD; else c = skinL; }
        // right arm (screen-right) raised to grip tool; swings opposite
        if (dxc >= 0.18f && dxc <= 0.29f && fy >= 0.38f - armSwing && fy <= 0.56f) {
            c = skin; if (dxc >= 0.25f) c = skinD; else if (dxc <= 0.21f) c = skinL;
        }
        // right hand (grips tool handle)
        if (Disc(dxc - 0.235f, fy - (0.40f - armSwing), 0.052f)) { c = skin; if (dxc - 0.235f > 0.015f) c = skinD; else c = skinL; }

        // ===== HEAD ===== (top of body, since fy=0 is bottom)
        float hcy = 0.74f; // head centre y
        bool head = Disc(dxc, fy - hcy, 0.135f);
        if (head) {
            c = skin;                                            // neutral base
            if (dxc < -0.03f && fy > hcy + 0.01f) c = skinL;     // upper-left highlight
            else if (dxc > 0.05f || fy < hcy - 0.05f) c = skinD; // lower-right shadow
        }
        // neck
        if (Mathf.Abs(dxc) <= 0.06f && fy >= 0.585f && fy <= 0.63f) { c = skin; if (dxc > 0.02f) c = skinD; }

        // ===== FACE FEATURES ===== (eyes near centre, nose/mouth below = smaller fy)
        if (head) {
            // brow line (dark) just above the eyes
            if (fy >= hcy + 0.040f && fy <= hcy + 0.058f && Mathf.Abs(dxc) <= 0.085f) c = skinD;
            // left eye white + pupil
            if (Disc(dxc + 0.055f, fy - (hcy + 0.012f), 0.027f)) c = eyeW;
            if (Disc(dxc + 0.052f, fy - (hcy + 0.008f), 0.013f)) c = dark;
            // right eye white + pupil
            if (Disc(dxc - 0.055f, fy - (hcy + 0.012f), 0.027f)) c = eyeW;
            if (Disc(dxc - 0.058f, fy - (hcy + 0.008f), 0.013f)) c = dark;
            // small nose (below eyes)
            if (Mathf.Abs(dxc) <= 0.014f && fy >= hcy - 0.048f && fy <= hcy - 0.006f) c = skinD;
            // neutral mouth (below nose)
            if (Mathf.Abs(dxc) <= 0.045f && fy >= hcy - 0.088f && fy <= hcy - 0.072f) c = skinD;
        }

        // ===== HAIR (tufty) over top + sides of head =====
        // top cap (only over the upper half of the head)
        if (Disc(dxc, fy - (hcy + 0.075f), 0.140f) && fy >= hcy + 0.035f) {
            c = hairM;
            if (dxc < -0.02f) c = hairL;
            else if (dxc > 0.07f) c = hairD;
        }
        // tufts: jagged crown
        if (fy >= hcy + 0.10f && fy <= hcy + 0.21f && Mathf.Abs(dxc) <= 0.15f) {
            float tuft = Frac(fx * 6.0f);
            float top = hcy + 0.12f + 0.06f * (0.5f + 0.5f * Mathf.Sin(fx * 30f));
            if (fy <= top + 0.04f * Mathf.Abs(tuft - 0.5f)) {
                c = hairM; if (dxc < -0.02f) c = hairL; else if (dxc > 0.06f) c = hairD;
            }
        }
        // sideburns
        if ((Mathf.Abs(dxc) >= 0.10f && Mathf.Abs(dxc) <= 0.15f) && fy >= hcy - 0.02f && fy <= hcy + 0.08f && Disc(dxc, fy - hcy, 0.155f)) {
            c = hairM; if (dxc < 0f) c = hairL; else c = hairD;
        }

        // ===== TOOL in right hand (age-specific) =====
        float hx = 0.79f; // handle centre x (over the right hand)
        if (age == 0) {
            // wooden CLUB: tapered shaft + knobbly head at top
            if (fx >= hx - 0.035f && fx <= hx + 0.035f && fy >= 0.30f && fy <= 0.66f) {
                c = toolM; if (fx >= hx + 0.012f) c = toolD; else if (fx <= hx - 0.012f) c = toolL;
            }
            if (Disc(fx - hx, fy - 0.70f, 0.075f) || Disc(fx - (hx-0.04f), fy - 0.66f, 0.045f) || Disc(fx - (hx+0.04f), fy - 0.68f, 0.050f)) {
                c = toolM; if (fx - hx > 0.01f) c = toolD; else c = toolL;
            }
        } else if (age == 1) {
            // SPEAR: long thin shaft + point at top
            if (fx >= hx - 0.018f && fx <= hx + 0.018f && fy >= 0.16f && fy <= 0.80f) {
                c = toolM; if (fx >= hx + 0.005f) c = toolD; else c = toolL;
            }
            if (fy >= 0.80f && fy <= 0.90f) {
                float w = (0.90f - fy) * 0.45f;
                if (Mathf.Abs(fx - hx) <= w) { c = toolL; if (fx > hx) c = toolM; }
            }
            if (fx >= hx - 0.022f && fx <= hx + 0.022f && fy >= 0.78f && fy <= 0.80f) c = clothD; // binding
        } else if (age == 2 || age == 3) {
            // metal HAMMER: wooden-look handle (toolD) + metal head (toolM/L/D)
            if (fx >= hx - 0.022f && fx <= hx + 0.022f && fy >= 0.30f && fy <= 0.72f) {
                c = toolD; if (fx >= hx + 0.006f) c = toolD; else c = toolM;
            }
            if (fx >= hx - 0.085f && fx <= hx + 0.085f && fy >= 0.70f && fy <= 0.82f) {
                c = toolM;
                if (fy < 0.74f || fx > hx + 0.04f) c = toolD;
                else if (fy > 0.78f && fx < hx) c = toolL;
            }
        } else {
            // WRENCH: shaft + open-end jaw at top
            if (fx >= hx - 0.020f && fx <= hx + 0.020f && fy >= 0.30f && fy <= 0.70f) {
                c = toolM; if (fx >= hx + 0.006f) c = toolD; else c = toolL;
            }
            bool jaw = Disc(fx - hx, fy - 0.74f, 0.075f);
            if (jaw) { c = toolM; if (fx - hx > 0.01f) c = toolD; else c = toolL; }
            // clear the open notch in the jaw
            if (jaw && fx >= hx - 0.03f && fy >= 0.745f) c = Clear;
        }

        px[y*s+x] = c;
    }

    // ===== OUTLINE PASS: thin dark edge around the whole silhouette =====
    var outPx = new Color[s * s];
    System.Array.Copy(px, outPx, px.Length);
    for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
        if (px[y*s+x].a > 0.01f) continue;                       // only fill transparent pixels
        bool adj = false;
        for (int oy = -1; oy <= 1 && !adj; oy++) for (int ox = -1; ox <= 1; ox++) {
            if (ox == 0 && oy == 0) continue;
            int nx = x + ox, ny = y + oy;
            if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
            if (px[ny*s+nx].a > 0.01f) { adj = true; break; }
        }
        if (adj) outPx[y*s+x] = dark;
    }

    tex.SetPixels(outPx); tex.Apply();
    _caveman[key] = Sprite.Create(tex, new Rect(0,0,s,s), new Vector2(0.5f,0.5f), s);
    return _caveman[key];
}

        /// <summary>A leafy tree (trunk + layered canopy) for wood resource nodes.</summary>
        public static Sprite Tree()
{
    if (_tree != null) return _tree;
    const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

    // Bark shades (highlight / mid / shadow) + dark outline
    var barkL = new Color(0.60f, 0.44f, 0.26f, 1f);
    var barkM = new Color(0.45f, 0.31f, 0.17f, 1f);
    var barkD = new Color(0.30f, 0.19f, 0.10f, 1f);
    var barkO = new Color(0.16f, 0.10f, 0.05f, 1f);

    // Canopy shades (dark underside / mid / bright highlight) + dark outline
    var leafO = new Color(0.07f, 0.20f, 0.09f, 1f);
    var leafD = new Color(0.13f, 0.34f, 0.16f, 1f);
    var leafM = new Color(0.22f, 0.52f, 0.24f, 1f);
    var leafL = new Color(0.40f, 0.70f, 0.34f, 1f);
    var leafH = new Color(0.58f, 0.84f, 0.47f, 1f);

    for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
        // fy = 0 is the BOTTOM of the sprite
        float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
        float dxc = fx - 0.5f;
        Color c = Clear;

        // ---------- TRUNK ----------
        // Centred trunk that flares slightly toward the base for a planted look.
        float trunkHalf = 0.058f + (0.34f - fy) * 0.085f;
        if (fy >= 0.03f && fy <= 0.40f && Mathf.Abs(dxc) <= trunkHalf) {
            // cylinder shading: lit on the upper-LEFT, shadowed on the lower-RIGHT
            float t = (dxc + trunkHalf) / (2f * trunkHalf); // 0 = left .. 1 = right
            c = t < 0.34f ? barkL : (t < 0.68f ? barkM : barkD);

            // vertical bark grain
            float grain = Frac(fx * 7.3f + fy * 1.1f);
            if (grain < 0.16f) c = barkD;
            else if (grain > 0.86f && t < 0.6f) c = barkL;

            // a couple of horizontal knot lines
            float knot = Frac(fy * 9.0f + fx * 0.5f);
            if (knot < 0.10f && t > 0.22f && t < 0.82f) c = barkD;

            // crisp outline on the trunk silhouette edges
            if (Mathf.Abs(dxc) >= trunkHalf - 0.02f) c = barkO;
        }

        // ---------- BRANCH STUBS ----------
        // Short stubs angling up-left and up-right where trunk meets canopy.
        if (fy >= 0.30f && fy <= 0.46f) {
            // left stub (lit)
            if (Mathf.Abs((fy - 0.37f) + (dxc + 0.05f) * 0.55f) < 0.024f
                && dxc > -0.22f && dxc < -0.02f) c = barkL;
            // right stub (shadowed)
            if (Mathf.Abs((fy - 0.37f) - (dxc - 0.05f) * 0.55f) < 0.024f
                && dxc > 0.02f && dxc < 0.22f) c = barkD;
        }

        // ---------- CANOPY ----------
        // Bumpy rounded mass from overlapping discs; silhouette (outline) is one
        // ring larger than the solid body so we get a crisp dark edge.
        bool canopyOut =
            Disc(dxc + 0.00f, fy - 0.60f, 0.330f) ||
            Disc(dxc + 0.22f, fy - 0.56f, 0.220f) ||
            Disc(dxc - 0.23f, fy - 0.55f, 0.210f) ||
            Disc(dxc + 0.10f, fy - 0.76f, 0.205f) ||
            Disc(dxc - 0.12f, fy - 0.74f, 0.195f) ||
            Disc(dxc - 0.02f, fy - 0.86f, 0.165f) ||
            Disc(dxc + 0.18f, fy - 0.68f, 0.165f) ||
            Disc(dxc - 0.19f, fy - 0.67f, 0.160f);

        bool canopyBody =
            Disc(dxc + 0.00f, fy - 0.60f, 0.308f) ||
            Disc(dxc + 0.22f, fy - 0.56f, 0.198f) ||
            Disc(dxc - 0.23f, fy - 0.55f, 0.188f) ||
            Disc(dxc + 0.10f, fy - 0.76f, 0.183f) ||
            Disc(dxc - 0.12f, fy - 0.74f, 0.173f) ||
            Disc(dxc - 0.02f, fy - 0.86f, 0.143f) ||
            Disc(dxc + 0.18f, fy - 0.68f, 0.143f) ||
            Disc(dxc - 0.19f, fy - 0.67f, 0.138f);

        if (canopyOut && fy > 0.30f) {
            // Default to the dark underside green.
            c = leafD;

            // Mid green fills most of the interior.
            if (Disc(dxc + 0.00f, fy - 0.62f, 0.262f) ||
                Disc(dxc + 0.16f, fy - 0.60f, 0.150f) ||
                Disc(dxc - 0.16f, fy - 0.59f, 0.150f) ||
                Disc(dxc + 0.06f, fy - 0.76f, 0.150f) ||
                Disc(dxc - 0.10f, fy - 0.74f, 0.140f))
                c = leafM;

            // Bright highlights biased toward the upper-LEFT (discs centred left/high).
            if (Disc(dxc - 0.10f, fy - 0.68f, 0.150f) ||
                Disc(dxc - 0.02f, fy - 0.80f, 0.110f) ||
                Disc(dxc - 0.16f, fy - 0.62f, 0.090f))
                c = leafL;

            // Tiny hot highlights only on the very upper-left bumps.
            if (Disc(dxc - 0.13f, fy - 0.72f, 0.072f) ||
                Disc(dxc - 0.07f, fy - 0.82f, 0.058f))
                c = leafH;

            // Mottled leaf speckle so the canopy isn't flat (only in the solid body).
            float n = Frac(fx * 5.1f + fy * 6.7f) + Frac(fx * 11.3f - fy * 3.9f) * 0.5f;
            if (canopyBody) {
                if (n > 1.08f && c.r > leafD.r) c = leafM;
                else if (n < 0.26f && c.r > leafM.r) c = leafL;
            }

            // Lower-RIGHT underside stays dark for a grounded, 3D shadow.
            if (dxc > 0.08f && fy < 0.58f) c = leafD;
        }

        // Crisp bumpy dark outline: inside the outer ring but outside the solid body.
        if (canopyOut && !canopyBody && fy > 0.30f) c = leafO;

        px[y * s + x] = c;
    }

    tex.SetPixels(px); tex.Apply();
    _tree = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
    return _tree;
}

        /// <summary>A liquid PIPE tile — a metal tube cross (connects any way). White body (tinted by the
        /// pipe's colour: blue water / dark oil), with baked darker rims so it reads as a pipe.</summary>
        public static Sprite PipeTile()
        {
            if (_pipe != null) return _pipe;
            const int s = 64;
            var tex = NewTex(s);
            var px = new Color[s * s];
            var body = Color.white;
            var rim = new Color(0.45f, 0.47f, 0.52f, 1f);
            var hi = new Color(1f, 1f, 1f, 1f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    float ax = Mathf.Abs(fx - 0.5f), ay = Mathf.Abs(fy - 0.5f);
                    bool horiz = ay < 0.20f, vert = ax < 0.20f;
                    Color c = Clear;
                    if (horiz || vert)
                    {
                        c = body;
                        if ((horiz && ay > 0.15f) || (vert && ax > 0.15f)) c = rim;      // tube rims
                        if ((horiz && ay < 0.04f) || (vert && ax < 0.04f)) c = hi;        // centre highlight
                    }
                    px[y * s + x] = c;
                }
            tex.SetPixels(px); tex.Apply();
            _pipe = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _pipe;
        }

        /// <summary>A dark oil pool with a purple sheen — for Oil deposit nodes.</summary>
        public static Sprite OilPatch()
        {
            if (_oilPatch != null) return _oilPatch;
            const int s = 64;
            var tex = NewTex(s);
            var px = new Color[s * s];
            var dark = new Color(0.12f, 0.10f, 0.14f, 1f);
            var sheen = new Color(0.34f, 0.28f, 0.46f, 1f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    float dxc = fx - 0.5f, dyc = fy - 0.42f;
                    Color c = Clear;
                    if ((dxc * dxc) / (0.34f * 0.34f) + (dyc * dyc) / (0.22f * 0.22f) <= 1f)
                    {
                        c = dark;
                        if (Disc(dxc + 0.10f, dyc + 0.05f, 0.075f) || Disc(dxc - 0.13f, dyc - 0.03f, 0.045f)) c = sheen; // sheen
                    }
                    px[y * s + x] = c;
                }
            tex.SetPixels(px); tex.Apply();
            _oilPatch = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _oilPatch;
        }

        /// <summary>A little sailing/cargo boat (hull + mast + sail) for cargo ships on harbour lines.
        /// Drawn light so the route's tier colour tints it.</summary>
        public static Sprite Boat()
        {
            if (_boat != null) return _boat;
            const int s = 64;
            var tex = NewTex(s);
            var px = new Color[s * s];
            var hull = new Color(0.85f, 0.85f, 0.88f, 1f);
            var sail = new Color(0.96f, 0.96f, 0.98f, 1f);
            var mast = new Color(0.45f, 0.32f, 0.20f, 1f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    float dxc = fx - 0.5f;
                    Color c = Clear;
                    if (fy >= 0.12f && fy <= 0.34f) { float hw = Mathf.Lerp(0.18f, 0.40f, (fy - 0.12f) / 0.22f); if (Mathf.Abs(dxc) <= hw) c = hull; } // hull
                    if (Mathf.Abs(dxc) <= 0.03f && fy >= 0.34f && fy <= 0.86f) c = mast;                                                              // mast
                    if (fy >= 0.40f && fy <= 0.82f && dxc >= 0f) { float h = (0.82f - fy) / 0.42f; if (dxc <= 0.02f + 0.34f * h) c = sail; }           // sail
                    px[y * s + x] = c;
                }
            tex.SetPixels(px); tex.Apply();
            _boat = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _boat;
        }

        /// <summary>A shaded boulder cluster for stone/ore/clay resource nodes.</summary>
        public static Sprite Rock()
{
    if (_rock != null) return _rock;
    const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

    // grey stone palette (3 shades) + outline + crack + moss
    var edge   = new Color(0.15f, 0.16f, 0.18f, 1f); // dark outline
    var shadow = new Color(0.33f, 0.35f, 0.38f, 1f); // dark-grey shadow side (lower-right)
    var mid    = new Color(0.50f, 0.52f, 0.55f, 1f); // mid grey body
    var lit    = new Color(0.70f, 0.72f, 0.75f, 1f); // light-grey lit top (upper-left)
    var crack  = new Color(0.23f, 0.24f, 0.26f, 1f); // crack lines
    var mossD  = new Color(0.19f, 0.39f, 0.17f, 1f); // moss shadow
    var mossL  = new Color(0.38f, 0.64f, 0.31f, 1f); // moss highlight

    for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
        float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
        float dxc = fx - 0.5f; Color c = Clear;

        // three overlapping boulder bodies (back-left, back-right, front-centre)
        // each a flattened ellipse sitting on the ground (low fy). fy=0 is BOTTOM.
        float bx = dxc + 0.22f, by = fy - 0.42f;          // back-left boulder
        bool bL = (bx * bx) / (0.25f * 0.25f) + (by * by) / (0.28f * 0.28f) <= 1f && fy >= 0.10f;
        float rx = dxc - 0.25f, ry = fy - 0.38f;          // back-right boulder
        bool bR = (rx * rx) / (0.23f * 0.23f) + (ry * ry) / (0.26f * 0.26f) <= 1f && fy >= 0.10f;
        float gx = dxc + 0.01f, gy = fy - 0.30f;          // front-centre boulder (largest)
        bool bF = (gx * gx) / (0.34f * 0.34f) + (gy * gy) / (0.28f * 0.28f) <= 1f && fy >= 0.07f;

        bool inside = bL || bR || bF;
        if (inside) {
            c = mid;

            // per-boulder shading: light upper-left, shadow lower-right + base contact.
            // front boulder is drawn last so it dominates overlaps.
            if (bR && !bF) {
                if ((rx + 0.07f) * (rx + 0.07f) + (ry - 0.08f) * (ry - 0.08f) <= 0.12f * 0.12f) c = lit;
                else if (rx >= 0.01f && ry <= 0.01f) c = shadow;
                if (ry < -0.16f) c = shadow;               // base contact shadow
            }
            if (bL && !bF) {
                if ((bx + 0.08f) * (bx + 0.08f) + (by - 0.09f) * (by - 0.09f) <= 0.13f * 0.13f) c = lit;
                else if (bx >= 0.01f && by <= 0.01f) c = shadow;
                if (by < -0.18f) c = shadow;
            }
            if (bF) {
                if ((gx + 0.10f) * (gx + 0.10f) + (gy - 0.10f) * (gy - 0.10f) <= 0.17f * 0.17f) c = lit;
                else if (gx >= 0.03f && gy <= 0.02f) c = shadow;
                if (gy < -0.18f) c = shadow;
            }

            // crack lines (darker than shadow) on the front and back-left boulders
            if (bF) {
                if (Mathf.Abs((gx * 1.4f) - (gy + 0.04f)) < 0.020f && gy > -0.15f && gy < 0.14f) c = crack;
                if (Mathf.Abs((gx + 0.11f) + (gy * 0.6f)) < 0.017f && gy > -0.11f && gy < 0.07f) c = crack;
            }
            if (bL && !bF) {
                if (Mathf.Abs((bx * 1.2f) + (by + 0.02f)) < 0.016f && by > -0.11f && by < 0.11f) c = crack;
            }

            // moss specks (life) - small green clusters tucked in shaded/damp spots,
            // each a dark base with a lighter highlight on its upper-left.
            if (Disc(dxc - 0.06f, fy - 0.16f, 0.040f)) c = mossD;
            if (Disc(dxc - 0.075f, fy - 0.15f, 0.020f)) c = mossL;
            if (Disc(dxc + 0.20f, fy - 0.20f, 0.036f)) c = mossD;
            if (Disc(dxc + 0.185f, fy - 0.19f, 0.018f)) c = mossL;
            if (Disc(dxc - 0.24f, fy - 0.24f, 0.030f)) c = mossD;
            if (Disc(dxc - 0.25f, fy - 0.23f, 0.015f)) c = mossL;
        }

        // dark outline: this pixel is empty but a neighbour lies inside the silhouette
        if (c.a == 0f) {
            float o = 1.5f / (s - 1);
            bool nb = false;
            for (int oy = -1; oy <= 1 && !nb; oy++) for (int ox = -1; ox <= 1 && !nb; ox++) {
                if (ox == 0 && oy == 0) continue;
                float nx = dxc + ox * o, ny = fy + oy * o;
                float nbx = nx + 0.22f, nby = ny - 0.42f;
                bool nbL = (nbx * nbx) / (0.25f * 0.25f) + (nby * nby) / (0.28f * 0.28f) <= 1f && ny >= 0.10f;
                float nrx = nx - 0.25f, nry = ny - 0.38f;
                bool nbR = (nrx * nrx) / (0.23f * 0.23f) + (nry * nry) / (0.26f * 0.26f) <= 1f && ny >= 0.10f;
                float nfx = nx + 0.01f, nfy = ny - 0.30f;
                bool nbF = (nfx * nfx) / (0.34f * 0.34f) + (nfy * nfy) / (0.28f * 0.28f) <= 1f && ny >= 0.07f;
                nb = nbL || nbR || nbF;
            }
            if (nb) c = edge;
        }

        px[y * s + x] = c;
    }

    tex.SetPixels(px); tex.Apply();
    _rock = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
    return _rock;
}

        /// <summary>A reddish, layered clay mound (a dome with dug bands) — clearly NOT stone.</summary>
        public static Sprite ClayMound()
        {
            if (_clay != null) return _clay;
            const int s = 64;
            var tex = NewTex(s);
            var px = new Color[s * s];
            var d = new Color(0.52f, 0.33f, 0.23f, 1f);
            var m = new Color(0.68f, 0.45f, 0.33f, 1f);
            var l = new Color(0.80f, 0.57f, 0.43f, 1f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    float dxc = fx - 0.5f;
                    Color c = Clear;
                    if (fy >= 0.08f && Disc(dxc, fy - 0.10f, 0.40f))
                    {
                        c = m;
                        if (((int)(fy * 22f)) % 3 == 0) c = d;             // dug clay bands
                        if (Disc(dxc - 0.08f, fy - 0.30f, 0.14f)) c = l;   // highlight
                    }
                    px[y * s + x] = c;
                }
            tex.SetPixels(px); tex.Apply();
            _clay = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _clay;
        }

        /// <summary>Copper ore — grey boulders shot with ORANGE mineral veins.</summary>
        public static Sprite OreCopper() { if (_oreCopper == null) _oreCopper = BakeOre(new Color(0.92f, 0.56f, 0.20f)); return _oreCopper; }
        /// <summary>Iron ore — grey boulders shot with RUSTY veins.</summary>
        public static Sprite OreIron() { if (_oreIron == null) _oreIron = BakeOre(new Color(0.64f, 0.40f, 0.33f)); return _oreIron; }

        // Grey boulders with coloured mineral veins (the vein colour distinguishes copper vs iron ore).
        private static Sprite BakeOre(Color vein)
        {
            const int s = 64;
            var tex = NewTex(s);
            var px = new Color[s * s];
            var d = new Color(0.38f, 0.38f, 0.43f, 1f);
            var m = new Color(0.54f, 0.54f, 0.59f, 1f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
                    float dxc = fx - 0.5f;
                    Color c = Clear;
                    bool body = Disc(dxc + 0.04f, fy - 0.36f, 0.30f) || Disc(dxc - 0.20f, fy - 0.30f, 0.18f) || Disc(dxc + 0.22f, fy - 0.40f, 0.20f);
                    if (body)
                    {
                        c = d;
                        if (Disc(dxc + 0.02f, fy - 0.40f, 0.22f)) c = m;                       // lit face
                        if (Frac((fx + fy) * 4f) < 0.16f) c = vein;                            // diagonal mineral veins
                        if (Disc(dxc - 0.10f, fy - 0.42f, 0.05f) || Disc(dxc + 0.14f, fy - 0.32f, 0.045f)) c = vein; // bright specks
                    }
                    px[y * s + x] = c;
                }
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

private static Sprite _stoneHut;
public static Sprite StoneHut()
{
    if (_stoneHut != null) return _stoneHut;
    const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

    // Baked detail colours (lean DARK so they survive runtime tinting).
    var outline   = new Color(0.10f, 0.07f, 0.05f, 1f);
    var roofD     = new Color(0.18f, 0.22f, 0.11f, 1f); // dark muddy green-brown thatch (lower-right)
    var roofM     = new Color(0.29f, 0.33f, 0.16f, 1f); // mid thatch
    var roofL     = new Color(0.42f, 0.46f, 0.24f, 1f); // highlight thatch (upper-left)
    var thatch    = new Color(0.13f, 0.15f, 0.08f, 1f); // darker thatch contour lines
    var logLine   = new Color(0.27f, 0.17f, 0.09f, 1f); // dark-brown stacked-log seams
    var logShade  = new Color(0.18f, 0.11f, 0.06f, 1f); // shadow under each log
    var doorD     = new Color(0.06f, 0.05f, 0.04f, 1f); // dark arched doorway
    var hideM     = new Color(0.52f, 0.37f, 0.22f, 1f); // tan animal-hide flap (mid)
    var hideD     = new Color(0.37f, 0.25f, 0.14f, 1f); // hide shadow / wrinkle
    var hideL     = new Color(0.64f, 0.47f, 0.30f, 1f); // hide highlight (upper-left edge)
    var body      = Color.white;                         // structural walls; tinted at runtime
    var bodyShade = new Color(0.74f, 0.74f, 0.74f, 1f);  // baked wall shadow (lower-right)
    var bodyHi    = new Color(1f, 1f, 1f, 1f);           // baked wall highlight (upper-left)

    // -------- geometry helpers (shared by fill + outline pass) --------
    // Roof: dome whose top sits high in the grid; overhangs the walls.
    System.Func<float, float, bool> isRoof = (dx, fy) =>
        Disc(dx, fy - 0.585f, 0.40f) && fy >= 0.50f;

    // Wall: rounded-corner stacked-log box, fy ~0.08..0.55.
    System.Func<float, float, bool> isWall = (dx, fy) => {
        bool core = Mathf.Abs(dx) <= 0.34f && fy >= 0.08f && fy <= 0.55f;
        if (!core) return false;
        // round the two TOP corners (fy near 0.55) so the box tucks under the dome
        if (Disc(dx - 0.31f, fy - 0.51f, 0.05f) && dx > 0.29f && fy > 0.49f) return false;
        if (Disc(dx + 0.31f, fy - 0.51f, 0.05f) && dx < -0.29f && fy > 0.49f) return false;
        return true;
    };

    // Doorway opening (arched): column + half-disc cap at bottom-centre.
    System.Func<float, float, bool> isDoor = (dx, fy) => {
        bool col  = Mathf.Abs(dx) <= 0.115f && fy >= 0.08f && fy <= 0.345f;
        bool arch = Disc(dx, fy - 0.345f, 0.115f) && fy > 0.345f && fy <= 0.40f;
        return col || arch;
    };

    for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
        // fy = 0 is the BOTTOM of the sprite.
        float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
        float dxc = fx - 0.5f;
        Color c = Clear;

        bool wall = isWall(dxc, fy);
        bool roof = isRoof(dxc, fy);
        bool door = isDoor(dxc, fy);

        // --- WALL BODY: stacked white logs (structural; stays white-ish) ---
        if (wall) {
            c = body;
            // baked directional shading: highlight upper-left, shadow lower-right
            if (dxc > 0.20f) c = bodyShade;
            if (dxc < -0.22f) c = bodyHi;
            // stacked-log horizontal seams (2-3 visible bands across the wall)
            float wy = Frac(fy * 4.2f);
            if (wy < 0.10f) c = logLine;
            else if (wy < 0.18f) c = logShade;
        }

        // --- DOME / THATCH ROOF (drawn over the wall tops) ---
        if (roof) {
            c = roofM;
            // light from upper-left, shadow toward lower-right
            float diag = dxc + (0.585f - fy); // negative = upper-left, positive = lower-right
            if (diag < -0.14f) c = roofL;
            if (diag > 0.16f) c = roofD;
            // darker thatch contour lines following the dome
            float ry = Frac((0.95f - fy) * 6.5f + dxc * 1.5f);
            if (ry < 0.13f) c = thatch;
        }

        // --- ARCHED DOORWAY (cut into the wall) ---
        if (door && fy <= 0.55f) c = doorD;

        // --- TAN ANIMAL-HIDE FLAP half across the doorway (covers left half) ---
        bool flap = door && dxc <= 0.012f && fy >= 0.085f;
        if (flap) {
            c = hideM;
            if (dxc < -0.07f) c = hideD;   // shadowed outer fold (left)
            if (dxc > -0.02f) c = hideL;   // lit inner edge (toward centre)
            float vy = Frac(fy * 9f);       // hide wrinkles / seams
            if (vy < 0.16f) c = hideD;
        }

        // --- DARK OUTLINE: trace the silhouette of roof + walls ---
        if (!roof && !wall) {
            bool nb = false;
            for (int oy = -1; oy <= 1 && !nb; oy++)
            for (int ox = -1; ox <= 1 && !nb; ox++) {
                if (ox == 0 && oy == 0) continue;
                int nx = x + ox, ny = y + oy;
                if (nx < 0 || nx > s - 1 || ny < 0 || ny > s - 1) continue;
                float nfx = nx / (float)(s - 1), nfy = ny / (float)(s - 1);
                float ndx = nfx - 0.5f;
                if (isRoof(ndx, nfy) || isWall(ndx, nfy)) nb = true;
            }
            if (nb) c = outline;
        }

        // --- inner outline around the doorway opening so it reads against the wall ---
        if (c == body || c == bodyShade || c == bodyHi) {
            bool ring = false;
            for (int oy = -1; oy <= 1 && !ring; oy++)
            for (int ox = -1; ox <= 1 && !ring; ox++) {
                if (ox == 0 && oy == 0) continue;
                int nx = x + ox, ny = y + oy;
                if (nx < 0 || nx > s - 1 || ny < 0 || ny > s - 1) continue;
                float nfx = nx / (float)(s - 1), nfy = ny / (float)(s - 1);
                float ndx = nfx - 0.5f;
                if (isDoor(ndx, nfy)) ring = true;
            }
            if (ring) c = outline;
        }

        px[y * s + x] = c;
    }

    tex.SetPixels(px); tex.Apply();
    _stoneHut = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
    return _stoneHut;
}

private static Sprite _stoneQuarry;
public static Sprite StoneQuarry()
{
    if (_stoneQuarry != null) return _stoneQuarry;
    const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

    // baked greys for the boulder pile (dark -> mid -> light: 3 shades, leaning dark to survive tinting)
    var rockD = new Color(0.22f, 0.22f, 0.25f, 1f);   // shadow grey  (lower-right)
    var rockM = new Color(0.37f, 0.38f, 0.42f, 1f);   // mid grey
    var rockL = new Color(0.55f, 0.57f, 0.62f, 1f);   // highlight grey (upper-left)
    var crack = new Color(0.12f, 0.12f, 0.14f, 1f);   // crack / fissure line

    // dark silhouette outline (shared)
    var outline = new Color(0.07f, 0.07f, 0.08f, 1f);

    // baked browns for the A-frame hoist / scaffold (3 shades)
    var woodD = new Color(0.24f, 0.15f, 0.07f, 1f);   // shadow side (right leg)
    var woodM = new Color(0.40f, 0.26f, 0.13f, 1f);   // mid (left leg)
    var woodL = new Color(0.55f, 0.39f, 0.22f, 1f);   // lit edge (crossbar)
    var rope  = new Color(0.62f, 0.54f, 0.34f, 1f);   // hoist rope

    // white-body cut stone blocks (tinted at runtime); 3 tones via body + lit + shade
    var body       = Color.white;
    var blockLit   = new Color(0.88f, 0.88f, 0.88f, 1f); // baked top light (still bright -> tints well)
    var blockShade = new Color(0.58f, 0.58f, 0.60f, 1f); // baked side shade
    var blockEdge  = new Color(0.09f, 0.09f, 0.10f, 1f); // dark mortar / edge

    // A-frame geometry (fy = 0 is the BOTTOM of the tile)
    const float apexY = 0.92f, baseY = 0.30f;

    for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
        float fx = x / (float)(s - 1), fy = y / (float)(s - 1); float dxc = fx - 0.5f; Color c = Clear;

        // ---------- WOODEN A-FRAME HOIST (drawn first; the boulder pile overlaps its feet) ----------
        // two legs splay from near the ground up to a high apex, plus a crossbar making the A.
        float legLT = Mathf.Abs((fy - baseY) * 0.34f - (apexY - baseY) * (dxc + 0.34f));
        bool legL = legLT < 0.022f && fy >= baseY && fy <= apexY && dxc <= 0.02f;   // left leg
        float legRT = Mathf.Abs((fy - baseY) * 0.34f + (apexY - baseY) * (dxc - 0.34f));
        bool legR = legRT < 0.022f && fy >= baseY && fy <= apexY && dxc >= -0.02f;  // right leg
        bool cross = Mathf.Abs(fy - 0.62f) < 0.022f && Mathf.Abs(dxc) < 0.15f;      // crossbar
        if (legL || legR || cross) {
            if (legL) c = woodM;        // left leg catches the light
            else if (cross) c = woodL;  // crossbar lit edge
            else c = woodD;             // right leg in shadow
        }
        // rope + hook block hanging from the apex toward the pit
        if (Mathf.Abs(dxc) < 0.012f && fy > 0.50f && fy < 0.90f) c = rope;
        if (Disc(dxc, fy - 0.50f, 0.028f)) c = woodD;

        // ---------- BOULDER PILE (compact, centred, lower tile; lifted off the floor) ----------
        bool b1 = Disc(dxc + 0.24f, fy - 0.22f, 0.145f);
        bool b2 = Disc(dxc + 0.01f, fy - 0.19f, 0.170f);
        bool b3 = Disc(dxc - 0.23f, fy - 0.21f, 0.145f);
        bool b4 = Disc(dxc - 0.05f, fy - 0.34f, 0.145f);
        bool b5 = Disc(dxc + 0.21f, fy - 0.35f, 0.115f);
        bool b6 = Disc(dxc - 0.22f, fy - 0.34f, 0.105f);
        bool boulders = b1 || b2 || b3 || b4 || b5 || b6;
        bool baseFill = fy >= 0.09f && fy <= 0.22f && fx > 0.16f && fx < 0.84f; // solid ground at the base
        if (boulders || baseFill) {
            c = rockM;
            // directional light: upper-left lit, lower-right in shadow
            float lit = (fy - 0.09f) * 0.85f - dxc * 0.65f;
            if (lit < 0.09f) c = rockD;
            else if (lit > 0.28f) c = rockL;
            // per-boulder upper-left highlight caps
            if (Disc(dxc + 0.21f, fy - 0.27f, 0.06f)) c = rockL;
            if (Disc(dxc - 0.02f, fy - 0.25f, 0.07f)) c = rockL;
            if (Disc(dxc - 0.26f, fy - 0.26f, 0.05f)) c = rockL;
            if (Disc(dxc - 0.08f, fy - 0.39f, 0.05f)) c = rockL;
            // a couple of baked crack lines
            float ck1 = Mathf.Abs((fy - 0.20f) - (-0.55f * (dxc - 0.02f)));
            if (ck1 < 0.016f && fx > 0.33f && fx < 0.65f && fy < 0.34f) c = crack;
            float ck2 = Mathf.Abs((fy - 0.32f) - (0.70f * (dxc + 0.10f)));
            if (ck2 < 0.015f && fx > 0.20f && fx < 0.52f && fy > 0.22f && fy < 0.46f) c = crack;
        }

        // ---------- STACK OF CUT STONE BLOCKS (white-body, lower-right, clear of the pile) ----------
        bool blkA = fx > 0.66f && fx < 0.80f && fy > 0.10f && fy < 0.22f; // bottom-left block
        bool blkB = fx > 0.80f && fx < 0.92f && fy > 0.10f && fy < 0.22f; // bottom-right block (edge margin)
        bool blkC = fx > 0.72f && fx < 0.86f && fy > 0.22f && fy < 0.33f; // top block
        if (blkA || blkB || blkC) {
            c = body;
            // baked top light (upper band) + side shade (right band) for the 3-tone read
            if (blkA) { if (fy > 0.185f) c = blockLit; else if (fx > 0.775f) c = blockShade; }
            if (blkB) { if (fy > 0.185f) c = blockLit; else if (fx > 0.895f) c = blockShade; }
            if (blkC) { if (fy > 0.295f) c = blockLit; else if (fx > 0.835f) c = blockShade; }
            // dark edges / mortar seams
            bool edge = false;
            if (blkA && (fx < 0.675f || fx > 0.785f || fy < 0.115f || fy > 0.205f)) edge = true;
            if (blkB && (fx < 0.815f || fx > 0.905f || fy < 0.115f || fy > 0.205f)) edge = true;
            if (blkC && (fx < 0.735f || fx > 0.845f || fy < 0.235f || fy > 0.315f)) edge = true;
            if (edge) c = blockEdge;
        }

        // ---------- DARK OUTLINE around the whole silhouette ----------
        if (c == Clear) {
            bool nearBoulder =
                Disc(dxc + 0.24f, fy - 0.22f, 0.160f) || Disc(dxc + 0.01f, fy - 0.19f, 0.185f) ||
                Disc(dxc - 0.23f, fy - 0.21f, 0.160f) || Disc(dxc - 0.05f, fy - 0.34f, 0.160f) ||
                Disc(dxc + 0.21f, fy - 0.35f, 0.130f) || Disc(dxc - 0.22f, fy - 0.34f, 0.120f);
            bool nearBase = fy >= 0.075f && fy <= 0.225f && fx > 0.145f && fx < 0.855f;
            bool nearLeg =
                (Mathf.Abs((fy - baseY) * 0.34f - (apexY - baseY) * (dxc + 0.34f)) < 0.036f && fy >= baseY && fy <= apexY && dxc <= 0.045f) ||
                (Mathf.Abs((fy - baseY) * 0.34f + (apexY - baseY) * (dxc - 0.34f)) < 0.036f && fy >= baseY && fy <= apexY && dxc >= -0.045f) ||
                (Mathf.Abs(fy - 0.62f) < 0.036f && Mathf.Abs(dxc) < 0.17f);
            bool nearBlock =
                (fx > 0.645f && fx < 0.925f && fy > 0.085f && fy < 0.225f) ||
                (fx > 0.705f && fx < 0.865f && fy > 0.205f && fy < 0.335f);
            if (nearBoulder || nearBase || nearLeg || nearBlock)
                c = outline;
        }

        px[y * s + x] = c;
    }
    tex.SetPixels(px); tex.Apply(); _stoneQuarry = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s); return _stoneQuarry;
}

private static Sprite _stoneSawmill;
public static Sprite StoneSawmill()
{
    if (_stoneSawmill != null) return _stoneSawmill;
    const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

    // Baked detail colours (lean dark so they survive tinting)
    var outline = new Color(0.10f, 0.07f, 0.05f, 1f);
    var roofD   = new Color(0.30f, 0.18f, 0.09f, 1f);
    var roofM   = new Color(0.42f, 0.26f, 0.13f, 1f);
    var roofL   = new Color(0.55f, 0.36f, 0.19f, 1f);
    var woodD   = new Color(0.28f, 0.17f, 0.09f, 1f);
    var woodM   = new Color(0.40f, 0.25f, 0.13f, 1f);
    var woodL   = new Color(0.56f, 0.38f, 0.21f, 1f);
    var bladeD  = new Color(0.22f, 0.23f, 0.25f, 1f);
    var bladeM  = new Color(0.38f, 0.40f, 0.43f, 1f);
    var bladeL  = new Color(0.58f, 0.61f, 0.66f, 1f);
    var dustD   = new Color(0.50f, 0.40f, 0.22f, 1f);
    var dustM   = new Color(0.66f, 0.55f, 0.33f, 1f);
    var dustL   = new Color(0.82f, 0.72f, 0.48f, 1f);
    var body    = Color.white; // structural body, tinted at runtime (kept pure white)

    for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
        float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
        float dxc = fx - 0.5f;
        Color c = Clear;

        // ---- WHITE-BODY corner posts (open shelter frame) ----
        // Kept PURE white so they tint correctly at runtime; form comes from the outline.
        if (fx >= 0.10f && fx <= 0.18f && fy >= 0.05f && fy <= 0.70f) c = body; // left post
        if (fx >= 0.82f && fx <= 0.90f && fy >= 0.05f && fy <= 0.70f) c = body; // right post
        // Top cross-beam (white body) spanning posts under the roof
        if (fx >= 0.10f && fx <= 0.90f && fy >= 0.64f && fy <= 0.70f) c = body;

        // ---- ROOF (slanted baked-brown planks) ----
        // Roof slab sits ABOVE the posts; higher on the left, lower on the right.
        float roofLine = 0.88f - dxc * 0.10f;
        if (fy <= roofLine && fy >= roofLine - 0.12f && fx >= 0.04f && fx <= 0.96f) {
            float t = (roofLine - fy) / 0.12f; // 0 = sunlit top edge .. 1 = shadowed underside
            c = roofM;
            if (t < 0.34f) c = roofL;        // sunlit top
            else if (t > 0.72f) c = roofD;   // shadowed underside
            // plank seams running down the slope
            if (Frac(fx * 7f) < 0.10f) c = roofD;
        }

        // ---- LOG STACK (left side): log ends as brown circles ----
        float lx = fx - 0.235f;
        bool logA = Disc(lx, fy - 0.20f, 0.075f);
        bool logB = Disc(lx + 0.085f, fy - 0.20f, 0.075f);
        bool logC = Disc(lx + 0.043f, fy - 0.275f, 0.075f);
        if (logA || logB || logC) {
            // pick the nearest log centre so rings are computed correctly where discs overlap
            float cx = logA ? lx : (logB ? lx + 0.085f : lx + 0.043f);
            float cy = (logA || logB) ? fy - 0.20f : fy - 0.275f;
            float r2 = cx * cx + cy * cy;
            c = woodM;
            if (r2 <= 0.040f * 0.040f) c = woodL;       // bright core
            else if (r2 <= 0.055f * 0.055f) c = woodM;  // growth ring
            else if (r2 <= 0.067f * 0.067f) c = woodD;  // outer bark ring (lower-right shadow band)
            // upper-left highlight on the bark rim
            if (cx < -0.03f && cy > 0.03f) c = woodL;
        }

        // ---- SAW BLADE (right side): grey circle with teeth, cutting a log ----
        float bx = fx - 0.66f, by = fy - 0.34f;
        float br = Mathf.Sqrt(bx * bx + by * by);
        float ang = Mathf.Atan2(by, bx);
        float toothEdge = 0.135f + 0.018f * Mathf.Cos(ang * 12f); // serrated rim
        if (br <= toothEdge) {
            c = bladeM;
            if (br <= 0.045f) c = bladeD;                 // hub
            else if (br <= 0.060f) c = bladeL;            // hub ring shine
            else if (br <= 0.110f) {
                c = (bx - by < 0f) ? bladeL : bladeM;     // upper-left shine on disc
            } else {
                c = bladeD;                               // outer teeth band (dark)
                if (Frac(ang * 1.91f) < 0.5f && (bx - by) < -0.02f) c = bladeM; // lit teeth UL
            }
        }
        // log being cut: short horizontal log under the blade
        if (fx >= 0.50f && fx <= 0.82f && fy >= 0.155f && fy <= 0.235f) {
            c = woodM;
            if (fy >= 0.205f) c = woodD;       // lower-right shadow
            else if (fy <= 0.175f) c = woodL;  // upper highlight
            if (Frac(fx * 9f) < 0.12f) c = woodD; // bark grain
        }

        // ---- SAWDUST pile at base under the blade ----
        float sdx = fx - 0.66f;
        float duneTop = 0.10f - Mathf.Abs(sdx) * 0.55f; // little mound
        if (fy <= duneTop && fy >= 0.025f && Mathf.Abs(sdx) <= 0.16f) {
            c = dustM;
            if (fy <= 0.05f) c = dustD;                                  // bottom shadow
            else if (sdx < -0.02f && fy >= duneTop - 0.03f) c = dustL;   // upper-left lit crest
            if (Frac((fx + fy) * 23f) < 0.18f) c = dustL;               // speckle texture
        }

        // ---- ground shadow strip (translucent; NOT part of the silhouette) ----
        if (fy < 0.045f && fy >= 0.02f && fx >= 0.10f && fx <= 0.90f && c == Clear)
            c = new Color(0.12f, 0.10f, 0.08f, 0.45f);

        px[y * s + x] = c;
    }

    // ---- DARK OUTLINE pass (1px around any solid pixel) ----
    // Only fully-opaque pixels count as the silhouette, so the translucent ground
    // shadow neither gets outlined nor gets overwritten.
    var outPx = new Color[s * s];
    for (int i = 0; i < px.Length; i++) outPx[i] = px[i];
    for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
        if (px[y * s + x].a > 0.05f) continue; // skip anything already painted (incl. shadow)
        bool near = false;
        for (int dy = -1; dy <= 1 && !near; dy++) for (int dx = -1; dx <= 1; dx++) {
            int nx = x + dx, ny = y + dy;
            if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
            if (px[ny * s + nx].a > 0.9f) { near = true; break; }
        }
        if (near) outPx[y * s + x] = outline;
    }
    px = outPx;

    tex.SetPixels(px); tex.Apply();
    _stoneSawmill = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
    return _stoneSawmill;
}

private static Sprite _stoneIdeaBench;
public static Sprite StoneIdeaBench()
{
    if (_stoneIdeaBench != null) return _stoneIdeaBench;
    const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

    // outline
    var outline   = new Color(0.08f, 0.06f, 0.04f, 1f);
    // legs / wood (3 shades, baked dark brown)
    var woodD     = new Color(0.24f, 0.15f, 0.07f, 1f);
    var woodM     = new Color(0.36f, 0.23f, 0.11f, 1f);
    var woodL     = new Color(0.50f, 0.33f, 0.18f, 1f);
    // table top slab (structural body = white; 2 baked form shades)
    var slabHi    = new Color(0.86f, 0.86f, 0.86f, 1f);
    var slabShade = new Color(0.70f, 0.70f, 0.70f, 1f);
    // stone tablet (3 shades, baked grey)
    var stoneD    = new Color(0.30f, 0.30f, 0.34f, 1f);
    var stoneM    = new Color(0.45f, 0.45f, 0.50f, 1f);
    var stoneL    = new Color(0.60f, 0.60f, 0.65f, 1f);
    var scratch   = new Color(0.14f, 0.14f, 0.18f, 1f);
    // idea spark (3 shades, baked warm yellow)
    var sparkD    = new Color(0.72f, 0.50f, 0.07f, 1f);
    var sparkM    = new Color(0.98f, 0.80f, 0.18f, 1f);
    var sparkL    = new Color(1f, 0.97f, 0.62f, 1f);
    var dot       = new Color(0.96f, 0.80f, 0.25f, 1f);

    // Shape membership for a given pixel centre (used for fill AND for a crisp outline halo).
    // Returns true if (fx,fy) is inside the solid silhouette of the subject.
    System.Func<float, float, bool> inShape = (fx, fy) =>
    {
        float dxc = fx - 0.5f;
        // legs
        if ((Mathf.Abs(fx - 0.30f) <= 0.045f || Mathf.Abs(fx - 0.70f) <= 0.045f) && fy >= 0.06f && fy <= 0.40f) return true;
        // cross stretcher between legs
        if (Mathf.Abs(dxc) <= 0.24f && fy >= 0.18f && fy <= 0.235f) return true;
        // table top slab
        if (Mathf.Abs(dxc) <= 0.42f && fy >= 0.40f && fy <= 0.50f) return true;
        // wood trim under slab front edge
        if (Mathf.Abs(dxc) <= 0.42f && fy >= 0.385f && fy < 0.40f) return true;
        // stone tablet (centred)
        if (Mathf.Abs(dxc) <= 0.20f && fy >= 0.50f && fy <= 0.66f) return true;
        // floating idea spark: round body + 4-point star arms
        float sx = dxc, syA = fy - 0.84f;
        if (Disc(sx, syA, 0.075f)) return true;
        if (Mathf.Abs(sx) <= 0.018f && Mathf.Abs(syA) <= 0.115f) return true;
        if (Mathf.Abs(syA) <= 0.018f && Mathf.Abs(sx) <= 0.115f) return true;
        return false;
    };

    for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
        float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
        float dxc = fx - 0.5f;
        Color c = Clear;

        // ---- Legs (baked dark brown, 3 shades) ----
        bool legL = Mathf.Abs(fx - 0.30f) <= 0.045f && fy >= 0.06f && fy <= 0.40f;
        bool legR = Mathf.Abs(fx - 0.70f) <= 0.045f && fy >= 0.06f && fy <= 0.40f;
        if (legL || legR) {
            c = woodM;
            float legCx = legL ? 0.30f : 0.70f;
            if (fx < legCx - 0.018f) c = woodL; // upper-left lit face
            if (fx > legCx + 0.018f) c = woodD; // lower-right shaded face
        }
        // cross stretcher between legs
        if (Mathf.Abs(dxc) <= 0.24f && fy >= 0.18f && fy <= 0.235f) {
            c = (fy > 0.21f) ? woodD : woodL;
        }

        // ---- Table top slab (STRUCTURAL BODY = white) ----
        bool topSlab = Mathf.Abs(dxc) <= 0.42f && fy >= 0.40f && fy <= 0.50f;
        if (topSlab) {
            c = Color.white;                       // body
            if (fy < 0.435f) c = slabShade;        // front lip in shadow (lower edge)
            else if (fy < 0.47f) c = slabHi;       // mid band, soft form
            if (dxc > 0.30f && fy < 0.47f) c = slabShade; // lower-right corner shadow
        }
        // thin baked wood trim under the slab front edge
        if (Mathf.Abs(dxc) <= 0.42f && fy >= 0.385f && fy < 0.40f) c = woodD;

        // ---- Stone tablet on the table (centred, 3 shades) ----
        bool tablet = Mathf.Abs(dxc) <= 0.20f && fy >= 0.50f && fy <= 0.66f;
        if (tablet) {
            c = stoneM;
            if (dxc < -0.08f || fy > 0.62f) c = stoneL; // upper-left highlight
            if (dxc > 0.10f || fy < 0.525f) c = stoneD; // lower-right shadow
            // scratched idea marks (baked dark lines)
            if (fy >= 0.535f && fy <= 0.545f && dxc > -0.14f && dxc < 0.08f) c = scratch;
            if (fy >= 0.565f && fy <= 0.575f && dxc > -0.16f && dxc < 0.12f) c = scratch;
            if (fy >= 0.595f && fy <= 0.605f && dxc > -0.12f && dxc < 0.04f) c = scratch;
            if (Mathf.Abs(dxc + 0.06f) <= 0.008f && fy >= 0.535f && fy <= 0.61f) c = scratch; // vertical tick
            if (Disc(dxc - 0.10f, fy - 0.58f, 0.022f) && !Disc(dxc - 0.10f, fy - 0.58f, 0.011f)) c = scratch; // little ring mark
        }

        // ---- Floating idea spark (star/bulb, centred above tablet, 3 shades) ----
        float sx = dxc, syA = fy - 0.84f;
        bool starBody = Disc(sx, syA, 0.075f);
        bool arms = (Mathf.Abs(sx) <= 0.018f && Mathf.Abs(syA) <= 0.115f) ||
                    (Mathf.Abs(syA) <= 0.018f && Mathf.Abs(sx) <= 0.115f);
        if (starBody || arms) {
            c = sparkM;
            if (arms && !starBody) c = sparkD;
            if (starBody && sx < 0.0f && syA > 0.0f) c = sparkL; // upper-left glow
            if (starBody && sx > 0.03f && syA < -0.02f) c = sparkD; // lower-right shadow
        }
        if (Disc(sx + 0.018f, syA + 0.018f, 0.028f)) c = sparkL; // bright core (upper-left)

        // thought dots trailing down from spark toward tablet
        if (Disc(dxc - 0.02f, fy - 0.72f, 0.016f)) c = dot;
        if (Disc(dxc + 0.04f, fy - 0.685f, 0.011f)) c = dot;

        // ---- Crisp dark outline: a Clear pixel that touches the silhouette ----
        if (c == Clear) {
            float u = 1f / (s - 1);
            if (inShape(fx - u, fy) || inShape(fx + u, fy) ||
                inShape(fx, fy - u) || inShape(fx, fy + u) ||
                inShape(fx - u, fy - u) || inShape(fx + u, fy - u) ||
                inShape(fx - u, fy + u) || inShape(fx + u, fy + u)) {
                c = outline;
            }
        }

        px[y * s + x] = c;
    }

    tex.SetPixels(px); tex.Apply();
    _stoneIdeaBench = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
    return _stoneIdeaBench;
}

private static Sprite _stoneLodge;
public static Sprite StoneLodge()
{
    if (_stoneLodge != null) return _stoneLodge;
    const int s = 64; var tex = NewTex(s); var px = new Color[s * s];

    // White structural body (tinted at runtime) - keep these pure white.
    var body   = Color.white;
    var bodyHi = Color.white;
    var bodyShade = new Color(0.62f, 0.62f, 0.64f, 1f);   // baked lower-right wall shadow

    // Baked detail colours (lean dark so they survive runtime tint).
    var outline    = new Color(0.09f, 0.06f, 0.05f, 1f);

    var thatchD    = new Color(0.20f, 0.24f, 0.12f, 1f);  // muddy green-brown shadow
    var thatchM    = new Color(0.31f, 0.35f, 0.17f, 1f);  // mid
    var thatchL    = new Color(0.45f, 0.49f, 0.26f, 1f);  // highlight
    var thatchLine = new Color(0.14f, 0.17f, 0.09f, 1f);  // thatch combing lines

    var woodD   = new Color(0.22f, 0.14f, 0.07f, 1f);     // post / beam shadow
    var woodM   = new Color(0.34f, 0.22f, 0.11f, 1f);     // post / beam mid
    var doorDark= new Color(0.07f, 0.05f, 0.04f, 1f);

    var stoneD  = new Color(0.30f, 0.27f, 0.24f, 1f);     // totem shadow
    var stoneM  = new Color(0.44f, 0.40f, 0.35f, 1f);     // totem mid
    var stoneL  = new Color(0.58f, 0.53f, 0.47f, 1f);     // totem highlight
    var carve   = new Color(0.15f, 0.12f, 0.10f, 1f);     // carved face lines

    var fireC   = new Color(1f, 0.55f, 0.12f, 1f);        // fire core
    var fireL   = new Color(1f, 0.84f, 0.36f, 1f);        // fire bright
    var glow    = new Color(1f, 0.50f, 0.14f, 0.40f);     // warm glow halo

    // -------- PASS 1: paint all shapes --------
    for (int y = 0; y < s; y++)
    for (int x = 0; x < s; x++)
    {
        float fx = x / (float)(s - 1), fy = y / (float)(s - 1);
        float dxc = fx - 0.5f;       // -0.5 .. +0.5, 0 = centre column
        Color c = Clear;

        // ---- WALLS (white body): broad lodge block, shifted slightly left so
        //      the totem on the right keeps the whole composition centred. ----
        float wcx = dxc + 0.06f;                              // wall centre a touch left
        bool inWall = Mathf.Abs(wcx) <= 0.26f && fy >= 0.07f && fy <= 0.50f;
        if (inWall)
        {
            c = body;
            // upper-left highlight, lower-right shadow (baked)
            float shade = wcx * 0.9f - (0.30f - fy);
            if (shade > 0.10f) c = bodyShade;
            else if (wcx < -0.10f && fy > 0.30f) c = bodyHi;

            // vertical wood corner posts
            if (Mathf.Abs(Mathf.Abs(wcx) - 0.235f) <= 0.018f) c = woodM;
            if (Mathf.Abs(Mathf.Abs(wcx) - 0.255f) <= 0.012f) c = woodD;
            // horizontal lintel beam under the roof line
            if (fy >= 0.345f && fy <= 0.378f) c = (wcx > 0.04f) ? woodD : woodM;
        }

        // ---- DOORWAY (dark arched opening, centred on the wall) ----
        float ddx = dxc + 0.06f;                              // align to wall centre
        bool doorBody = Mathf.Abs(ddx) <= 0.075f && fy >= 0.08f && fy <= 0.28f;
        bool doorArch = Disc(ddx, fy - 0.28f, 0.075f) && fy >= 0.28f;
        if (doorBody || doorArch)
        {
            c = doorDark;
            // warm rim-light on the left jamb cast by the fire
            if (ddx < -0.04f && fy < 0.20f) c = new Color(0.26f, 0.13f, 0.06f, 1f);
        }

        // ---- BIG TALL THATCH ROOF (triangular, overhangs the eaves) ----
        float roofBase = 0.48f, roofApex = 0.93f;
        if (fy >= roofBase && fy <= roofApex)
        {
            float t = (fy - roofBase) / (roofApex - roofBase);   // 0 at eaves -> 1 at peak
            float half = Mathf.Lerp(0.46f, 0.02f, t);            // wide eaves -> narrow peak
            float rdx = dxc + 0.06f * (1f - t);                  // lean roof over the wall centre
            if (Mathf.Abs(rdx) <= half)
            {
                c = thatchM;
                if (rdx > 0.05f) c = thatchD;                    // right face in shadow
                if (rdx < -0.08f && t < 0.75f) c = thatchL;      // upper-left catches light
                // thatch combing: stacked horizontal-ish strands
                float line = Frac(fy * 22f + rdx * 4f);
                if (line < 0.32f) c = thatchLine;
                else if (line > 0.80f && rdx < 0f) c = thatchL;
                // darker eave underside lip
                if (fy <= roofBase + 0.035f) c = thatchD;
            }
        }

        // ---- TOTEM POLE / STANDING STONE (right of the lodge) ----
        float tx = dxc - 0.385f;                 // totem centre column
        bool totemBody = Mathf.Abs(tx) <= 0.055f && fy >= 0.06f && fy <= 0.64f;
        bool totemCap  = Disc(tx, fy - 0.64f, 0.055f) && fy >= 0.64f;   // domed top
        if (totemBody || totemCap)
        {
            c = stoneM;
            if (tx > 0.015f) c = stoneD;         // right side shadow
            else if (tx < -0.025f) c = stoneL;   // left side highlight

            // carved faces (two stacked), only on the body
            if (totemBody)
            {
                // upper face
                if (Disc(tx + 0.024f, fy - 0.53f, 0.013f) || Disc(tx - 0.024f, fy - 0.53f, 0.013f)) c = carve;
                if (Mathf.Abs(tx) <= 0.030f && fy >= 0.475f && fy <= 0.492f) c = carve;   // upper mouth
                // lower face
                if (Disc(tx + 0.024f, fy - 0.33f, 0.013f) || Disc(tx - 0.024f, fy - 0.33f, 0.013f)) c = carve;
                if (Mathf.Abs(tx) <= 0.030f && fy >= 0.270f && fy <= 0.287f) c = carve;   // lower mouth
                // dividing groove between the two faces
                if (fy >= 0.405f && fy <= 0.420f) c = carve;
            }
        }

        // ---- FIRE GLOW at the entrance (front, bottom centre on the door) ----
        float fdx = dxc + 0.06f;
        if (c == Clear && Disc(fdx, fy - 0.10f, 0.13f) && fy <= 0.20f) c = glow;
        if (Disc(fdx, fy - 0.085f, 0.058f) && fy >= 0.05f)
        {
            c = fireC;
            if (Disc(fdx + 0.008f, fy - 0.078f, 0.030f)) c = fireL;
        }
        // little flame flick rising off the core
        if (Disc(fdx, fy - 0.135f, 0.022f) && fy >= 0.115f) c = fireL;

        px[y * s + x] = c;
    }

    // -------- PASS 2: crisp dark outline on the silhouette --------
    // Any transparent pixel that touches a solid (non-fire, non-glow) pixel
    // becomes outline. This traces the whole shape cleanly with no gaps.
    var outPx = new Color[s * s];
    for (int y = 0; y < s; y++)
    for (int x = 0; x < s; x++)
    {
        Color c = px[y * s + x];
        bool isClear = (c == Clear);
        if (isClear)
        {
            bool touches = false;
            for (int oy = -1; oy <= 1 && !touches; oy++)
            for (int ox = -1; ox <= 1; ox++)
            {
                if (ox == 0 && oy == 0) continue;
                int nx = x + ox, ny = y + oy;
                if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                Color n = px[ny * s + nx];
                if (n == Clear) continue;
                if (n == fireC || n == fireL || n == glow) continue;  // don't outline the flame
                touches = true; break;
            }
            outPx[y * s + x] = touches ? outline : Clear;
        }
        else
        {
            outPx[y * s + x] = c;
        }
    }

    tex.SetPixels(outPx); tex.Apply();
    _stoneLodge = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
    return _stoneLodge;
}

private static Sprite _stoneWoodpile;
public static Sprite StoneWoodpile()
{
    if (_stoneWoodpile != null) return _stoneWoodpile;
    const int s = 64; var tex = NewTex(s); var px = new Color[s * s];
    Color body = Color.white;                               // tinted structural fill (log ends)
    var outline = new Color(0.10f, 0.06f, 0.03f, 1f);       // dark silhouette outline
    var barkD  = new Color(0.18f, 0.10f, 0.04f, 1f);        // dark bark ring shadow (lower-right)
    var barkM  = new Color(0.30f, 0.18f, 0.08f, 1f);        // bark ring mid
    var barkH  = new Color(0.42f, 0.26f, 0.13f, 1f);        // bark ring highlight (upper-left)
    var woodD  = new Color(0.50f, 0.33f, 0.18f, 1f);        // inner wood shadow
    var woodM  = new Color(0.66f, 0.48f, 0.29f, 1f);        // inner wood mid
    var woodH  = new Color(0.84f, 0.68f, 0.47f, 1f);        // inner wood highlight
    var ropeD  = new Color(0.30f, 0.24f, 0.12f, 1f);        // tie-rope shadow
    var ropeM  = new Color(0.52f, 0.43f, 0.22f, 1f);        // tie-rope mid
    var ropeH  = new Color(0.72f, 0.62f, 0.36f, 1f);        // tie-rope highlight
    var logBark= new Color(0.26f, 0.15f, 0.06f, 1f);        // top lying-log bark
    var logD   = new Color(0.40f, 0.26f, 0.13f, 1f);        // top log side shadow (lower)
    var logM   = new Color(0.58f, 0.41f, 0.23f, 1f);        // top log side mid
    var logH   = new Color(0.78f, 0.62f, 0.42f, 1f);        // top log side highlight (upper)
    var groundD= new Color(0.16f, 0.12f, 0.07f, 1f);        // ground shadow under pile

    // log-end cross-sections (cx, cy in fx/fy space), radius rr, pyramid 3 + 2 + 1.
    // fy = 0 is the BOTTOM, so the wide row sits low and the single log sits high.
    float rr = 0.135f;                                      // log outer radius
    float[,] logs = new float[6, 2] {
        { 0.255f, 0.250f }, { 0.500f, 0.250f }, { 0.745f, 0.250f }, // bottom row (3)
        { 0.377f, 0.470f }, { 0.623f, 0.470f },                     // middle row (2)
        { 0.500f, 0.690f }                                          // top row (1)
    };

    for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) {
        float fx = x/(float)(s-1), fy = y/(float)(s-1); float dxc = fx - 0.5f; Color c = Clear;

        // ---- ground shadow ellipse beneath the stack ----
        if (Disc(dxc, (fy - 0.090f) * 2.4f, 0.42f) && fy < 0.135f) c = groundD;

        // ---- stacked log ends (pyramid) ----
        for (int i = 0; i < 6; i++) {
            float cx = logs[i, 0], cy = logs[i, 1];
            float ax = fx - cx, ay = fy - cy;
            // light from UPPER-LEFT: low fx (left) and high fy (up) => +ve lit
            float lit = (-ax + ay) * 0.62f;
            if (Disc(ax, ay, rr + 0.014f)) c = outline;      // crisp dark rim outline
            if (Disc(ax, ay, rr)) {
                if (Disc(ax, ay, rr * 0.66f)) {              // inner wood face (3 shades)
                    Color w = woodM;
                    if (lit > 0.030f) w = woodH; else if (lit < -0.030f) w = woodD;
                    // a couple of baked growth rings
                    float rd = Mathf.Sqrt(ax * ax + ay * ay);
                    if (rd > rr * 0.40f && rd < rr * 0.47f) w = woodD;
                    if (rd > rr * 0.18f && rd < rr * 0.24f) w = woodH;
                    c = w;
                } else {                                     // dark-brown bark ring (3 shades)
                    Color b = barkM;
                    if (lit > 0.025f) b = barkH; else if (lit < -0.025f) b = barkD;
                    c = b;
                }
            }
        }

        // ---- tie-rope band wrapping vertically around the stack centre ----
        if (Mathf.Abs(dxc) <= 0.052f && fy >= 0.11f && fy <= 0.80f) {
            // only paint rope where the stack body exists (not on Clear or the ground)
            if (c.a > 0f && c != groundD) {
                float band = Frac(fy * 11.0f);               // twisted-cord look
                Color rp = (band < 0.5f) ? ropeM : ropeD;
                if (band > 0.18f && band < 0.40f) rp = ropeH;
                // darken the right edge of the rope band (lower-right shadow)
                if (dxc > 0.044f) rp = ropeD;
                else if (dxc < -0.044f && rp == ropeM) rp = ropeH;
                c = rp;
            }
        }

        // ---- one log lying across the very top ----
        float topCy = 0.880f;
        float tdx = dxc, tdy = fy - topCy;
        float halfH = 0.066f;
        bool inTopBody = Mathf.Abs(tdx) <= 0.40f && Mathf.Abs(tdy) <= halfH;
        bool inTopOut  = Mathf.Abs(tdx) <= 0.42f && Mathf.Abs(tdy) <= halfH + 0.012f;
        if (inTopOut && !inTopBody) c = outline;             // crisp top-log outline
        if (inTopBody) {
            float v = tdy / halfH;                            // -1 bottom .. +1 top
            // cylinder side lit from above: top catches light, underside shadowed (3 shades)
            Color lg = logM;
            if (v > 0.20f) lg = logH;                        // upper face highlight
            else if (v < -0.35f) lg = logD;                  // underside shadow
            c = lg;
            // bark seam along the very top edge
            if (v > 0.80f) c = logBark;
            // rounded LEFT end cap: small bark ring + lit wood face
            if (tdx < -0.32f) {
                float ex = (tdx + 0.40f) / 0.08f;            // 0..1 across cap
                if (Mathf.Abs(v) < 0.62f) c = (ex < 0.45f) ? woodH : logBark;
            }
            // right end cap edge shadow
            if (tdx > 0.355f) c = logBark;
        }

        px[y*s+x] = c;
    }

    tex.SetPixels(px); tex.Apply(); _stoneWoodpile = Sprite.Create(tex, new Rect(0,0,s,s), new Vector2(0.5f,0.5f), s); return _stoneWoodpile;
}

        // Signed-distance test for a rounded square inset by `margin` (px) with corner radius `rad`.
        private static bool InRoundedRect(int x, int y, int s, float margin, float rad)
        {
            float lo = margin, hi = (s - 1) - margin;
            float c = (lo + hi) * 0.5f, half = (hi - lo) * 0.5f;
            float qx = Mathf.Abs(x - c) - (half - rad);
            float qy = Mathf.Abs(y - c) - (half - rad);
            float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
            float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
            return outside + inside - rad <= 0f;
        }

        private static readonly Color Clear = new Color(0, 0, 0, 0);

        private static Texture2D NewTex(int s)
        {
            return new Texture2D(s, s) { filterMode = FilterMode.Bilinear };
        }

        // Rasterizes a convex polygon (vertices normalized to [0,1]) into a sprite.
        private static Sprite Polygon(Vector2[] norm)
        {
            const int s = 64;
            var tex = NewTex(s);
            var pts = new Vector2[norm.Length];
            for (int i = 0; i < norm.Length; i++)
                pts[i] = new Vector2(norm[i].x * (s - 1), norm[i].y * (s - 1));

            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    px[y * s + x] = PointInConvex(new Vector2(x, y), pts) ? Color.white : Clear;

            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        private static bool PointInConvex(Vector2 p, Vector2[] poly)
        {
            bool pos = false, neg = false;
            int n = poly.Length;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % n];
                float cross = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
                if (cross > 0) pos = true;
                else if (cross < 0) neg = true;
                if (pos && neg) return false;
            }
            return true;
        }
    }
}
