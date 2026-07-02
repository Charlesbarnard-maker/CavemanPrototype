using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Full-snapshot save/load. World generation isn't seed-reproducible (see the world-gen analysis), so a
    /// save stores the whole biome map, every live resource node, all placed buildings / belts / pipes / rails /
    /// signals / routes with their inventories, and the progress (research, age, objectives, player + camera).
    /// Content DEFINITIONS are rebuilt from code every launch (GameBootstrap), so they're referenced by a stable
    /// id (item.id / building.displayName), never serialized.
    ///
    /// LOAD tears down the current world entities — the player, HUD, Colony, Objectives, BeltSim, FogOfWar and
    /// Camera singletons persist — and reconstructs from the file. Items mid-flight on belts are kept; a train's
    /// exact position along its route is not (it resumes from a stop and re-accumulates cargo within seconds).
    /// </summary>
    public static class SaveSystem
    {
        public const int Version = 3; // v2 added the fog mask; v3 adds in-progress construction sites

        /// <summary>The most recently written save slot in [0, maxSlots), or -1 if none — drives Continue
        /// (which should prefer the freshest save, including the autosave slot).</summary>
        public static int NewestSlot(int maxSlots = 4)
        {
            int best = -1; System.DateTime bestT = System.DateTime.MinValue;
            for (int i = 0; i < maxSlots; i++)
            {
                if (!HasSave(i)) continue;
                var t = File.GetLastWriteTime(SlotPath(i));
                if (t > bestT) { bestT = t; best = i; }
            }
            return best;
        }
        private const uint Magic = 0x56535643; // 'CVSV'

        public static string SaveDir => Path.Combine(Application.persistentDataPath, "saves");
        public static string SlotPath(int slot) => Path.Combine(SaveDir, $"slot{slot}.cvsav");
        public static bool HasSave(int slot) => File.Exists(SlotPath(slot));

        // Depot ordering shared between the depot section (which defines the index) and the route section
        // (which references it) during a load.
        private static List<Depot> _loadDepots;

        /// <summary>Age name + save time for the menu, read cheaply from the header. "" if no/unreadable save.</summary>
        public static string SlotInfo(int slot)
        {
            try
            {
                if (!HasSave(slot)) return "";
                using var fs = File.OpenRead(SlotPath(slot));
                using var r = new BinaryReader(fs);
                if (r.ReadUInt32() != Magic) return "";
                r.ReadInt32(); // version
                int age = r.ReadInt32();
                string when = r.ReadString();
                string an = age >= 0 && age < Colony.AgeNames.Length ? Colony.AgeNames[age] : $"Age {age}";
                return $"{an}  ·  {when}";
            }
            catch { return ""; }
        }

        // ============================== SELF-TEST ==============================
        /// <summary>In-game validation (F10): fingerprint the live world, save to a scratch slot, load it back,
        /// and compare. PASS means the save/load round-trip is faithful on the CURRENT game state. The load does
        /// rebuild the world, so if it PASSES the game is identical; if it FAILS the console shows what differs.</summary>
        public static bool SelfTest()
        {
            string before = Fingerprint();
            if (!Save(97)) { Toast.Show("<color=#f99>Self-test: save failed.</color>"); return false; }
            if (!Load(97)) { Toast.Show("<color=#f99>Self-test: load failed.</color>"); return false; }
            string after = Fingerprint();
            bool ok = before == after;
            Debug.Log($"[SaveSystem.SelfTest] {(ok ? "PASS" : "FAIL")}\n  before: {before}\n  after:  {after}");
            Toast.Show(ok ? "<color=#9f9>✔ Save/load self-test PASSED — round-trip is faithful.</color>"
                          : "<color=#f99>✖ Save/load self-test FAILED — see console (before/after differ).</color>");
            return ok;
        }

        /// <summary>A compact fingerprint of everything a save must preserve (entity counts + all inventory
        /// totals + progress). Used by the self-test and handy for debugging.</summary>
        public static string Fingerprint()
        {
            var sb = new System.Text.StringBuilder();
            int nodes = 0, nodeAmt = 0;
            foreach (var n in ResourceNode.All) if (n != null && n.transform.parent == null) { nodes++; nodeAmt += n.Amount; }
            sb.Append($"col={ProductionBuilding.All.Count} sto={StorageBuilding.All.Count} wsp={WorkshopBuilding.All.Count} ");
            sb.Append($"res={ResearchBuilding.All.Count} dep={Depot.All.Count} pow={PowerPlant.All.Count} bat={Battery.All.Count} pole={PowerPole.All.Count} ");
            sb.Append($"pump={WaterPump.All.Count} rail={RailTile.All.Count} sig={Signal.All.Count} pipe={PipeNet.Count} belt={Belt.Count} route={RouteVehicle.All.Count} ");
            sb.Append($"node={nodes}/{nodeAmt} pts={Research.Points} tot={Research.TotalDelivered} age={(Colony.Instance != null ? Colony.Instance.Age : -1)} ");

            var totals = new SortedDictionary<string, int>();
            void Add(Inventory inv) { if (inv == null) return; foreach (var kv in inv.Items) if (kv.Key != null && kv.Value != 0) { totals.TryGetValue(kv.Key.id, out int v); totals[kv.Key.id] = v + kv.Value; } }
            var gb = GameBootstrap.Instance;
            if (gb != null && gb.Gatherer != null) Add(gb.Gatherer.Inventory);
            foreach (var p in ProductionBuilding.All) if (p != null) Add(p.Buffer);
            foreach (var s in StorageBuilding.All) if (s != null) Add(s.Store);
            foreach (var w in WorkshopBuilding.All) if (w != null) { Add(w.Buffer); Add(w.InBuffer); }
            foreach (var rb in ResearchBuilding.All) if (rb != null) Add(rb.InBuffer);
            foreach (var d in Depot.All) if (d != null) Add(d.store);
            foreach (var pp in PowerPlant.All) if (pp != null) Add(pp.Buffer);
            sb.Append("inv[");
            foreach (var kv in totals) sb.Append($"{kv.Key}:{kv.Value} ");
            sb.Append("]");
            return sb.ToString();
        }

        // ============================== SAVE ==============================
        public static bool Save(int slot)
        {
            try
            {
                Directory.CreateDirectory(SaveDir);
                string tmp = SlotPath(slot) + ".tmp";
                using (var fs = File.Create(tmp))
                using (var w = new BinaryWriter(fs))
                    WriteAll(w);
                if (File.Exists(SlotPath(slot))) File.Delete(SlotPath(slot));
                File.Move(tmp, SlotPath(slot)); // only replace the real save once it's fully written
                Debug.Log($"[SaveSystem] Saved slot {slot} → {SlotPath(slot)}");
                Toast.Show("<color=#9f9>💾 Game saved.</color>");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e}");
                Toast.Show("<color=#f99>Save failed (see console).</color>");
                return false;
            }
        }

        private static void WriteAll(BinaryWriter w)
        {
            w.Write(Magic);
            w.Write(Version);
            w.Write(Colony.Instance != null ? Colony.Instance.Age : 0);
            w.Write(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

            // progress
            WriteResearch(w);
            w.Write(Colony.Instance != null ? Colony.Instance.PeakProsperity : 0);
            WriteObjectives(w);
            WritePlayer(w);

            // world
            TerrainGrid.SerializeTo(w);
            WriteNodes(w);
            FogOfWar.SaveTo(w); // explored mask (v2+)

            // buildings — depots write an index map that routes reference
            var depotIndex = new Dictionary<Depot, int>();
            WriteCollectors(w);
            WriteStorages(w);
            WriteWorkshops(w);
            WriteResearchBuildings(w);
            WriteDepots(w, depotIndex);
            WritePowerPlants(w);
            WriteBatteries(w);
            WritePoles(w);
            WritePumps(w);
            WritePipes(w);
            WriteRails(w);
            WriteSignals(w);
            WriteBridges(w);
            WriteGarages(w);
            WriteBelts(w);
            WriteRoutes(w, depotIndex);
            WritePowerWires(w);
            WriteSites(w); // v3: buildings still under construction (they'd silently vanish otherwise)
        }

        // ============================== LOAD ==============================
        public static bool Load(int slot)
        {
            if (!HasSave(slot)) { Toast.Show("<color=#f99>No save in that slot.</color>"); return false; }
            try
            {
                using var fs = File.OpenRead(SlotPath(slot));
                using var r = new BinaryReader(fs);
                if (r.ReadUInt32() != Magic) { Debug.LogError("[SaveSystem] Not a Caveman save."); return false; }
                int ver = r.ReadInt32();
                if (ver != Version) Debug.LogWarning($"[SaveSystem] Save version {ver} != {Version} — attempting anyway.");
                int age = r.ReadInt32();
                r.ReadString(); // timestamp (display only)

                TeardownWorld();

                // progress (restore age before routes so consist sizing is right)
                ReadResearch(r);
                int peak = r.ReadInt32();
                Colony.Instance?.LoadRestore(age, peak);
                ReadObjectives(r);
                ReadPlayer(r);

                // world
                TerrainGrid.DeserializeFrom(r);
                ReadNodes(r);
                TerrainGrid.RebuildRenderer(); // AFTER nodes: the bake stains the ground under restored deposits
                if (ver >= 2) FogOfWar.LoadFrom(r); // restore exactly what was explored (v1 saves had no fog data)

                // buildings
                _loadDepots = new List<Depot>();
                ReadCollectors(r);
                ReadStorages(r);
                ReadWorkshops(r);
                ReadResearchBuildings(r);
                ReadDepots(r);
                ReadPowerPlants(r);
                ReadBatteries(r);
                ReadPoles(r);
                ReadPumps(r);
                ReadPipes(r);
                ReadRails(r);
                ReadSignals(r);
                ReadBridges(r);
                ReadGarages(r);
                ReadBelts(r);
                ReadRoutes(r);
                ReadPowerWires(r);
                if (ver >= 3) ReadSites(r); // v2 saves simply had no site data

                PlayerController.RecomputeGarageSlots();
                Debug.Log($"[SaveSystem] Loaded slot {slot}.");
                Toast.Show("<color=#9f9>✔ Game loaded.</color>");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed: {e}");
                Toast.Show("<color=#f99>Load failed (see console). Your game is unchanged.</color>");
                return false;
            }
        }

        // Destroy every world entity so a load rebuilds cleanly. DestroyImmediate so each OnDisable/OnDestroy
        // (which unregisters from the All lists + grids) runs synchronously before we respawn.
        private static void TeardownWorld()
        {
            DestroyComps(ConstructionSite.All);
            DestroyComps(RouteVehicle.All);
            DestroyComps(ProductionBuilding.All);
            DestroyComps(WorkshopBuilding.All);
            DestroyComps(StorageBuilding.All);
            DestroyComps(ResearchBuilding.All);
            DestroyComps(Depot.All);
            DestroyComps(PowerPlant.All);
            DestroyComps(Battery.All);
            DestroyComps(PowerPole.All);
            DestroyComps(WaterPump.All);
            DestroyComps(RailTile.All);
            DestroyComps(Bridge.All);
            DestroyComps(Garage.All);
            DestroyComps(ResourceNode.All);
            DestroyComps(Belt.AllBelts);
            DestroyComps(PipeNet.Pipes.Values);
            DestroyComps(Signal.All.Values);
            DestroyComps(PowerWire.All);
            // Runtime statics not tied to a single object's OnDisable.
            PipeNet.BoostCells.Clear();
            RailNet.StationLane.Clear();
            RailGraph.Reset();
        }

        private static void DestroyComps<T>(IEnumerable<T> src) where T : Component
        {
            var arr = new List<T>(src);
            foreach (var c in arr) if (c != null) Object.DestroyImmediate(c.gameObject);
        }

        // ---------------- small serialization helpers ----------------
        private static void WDir(BinaryWriter w, Belt.Dir d) => w.Write((byte)d);
        private static Belt.Dir RDir(BinaryReader r) => (Belt.Dir)r.ReadByte();
        private static void WVec(BinaryWriter w, Vector3 p) { w.Write(p.x); w.Write(p.y); }
        private static Vector3 RVec(BinaryReader r) { float x = r.ReadSingle(), y = r.ReadSingle(); return new Vector3(x, y, 0f); }
        private static void WItem(BinaryWriter w, ItemDefinition i) => w.Write(i != null ? i.id : "");
        private static ItemDefinition RItem(BinaryReader r) => SaveRegistry.Item(r.ReadString());
        private static void WColor(BinaryWriter w, Color c) { w.Write(c.r); w.Write(c.g); w.Write(c.b); }
        private static Color RColor(BinaryReader r) { float a = r.ReadSingle(), b = r.ReadSingle(), c = r.ReadSingle(); return new Color(a, b, c); }
        private static Vector2Int Cell(Vector2 p) => new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));

        private static void WInv(BinaryWriter w, Inventory inv)
        {
            var items = new List<KeyValuePair<ItemDefinition, int>>();
            if (inv != null) foreach (var kv in inv.Items) if (kv.Key != null && kv.Value > 0) items.Add(kv);
            w.Write(items.Count);
            foreach (var kv in items) { w.Write(kv.Key.id); w.Write(kv.Value); }
        }
        // Read into a temp list first (so the stream is consumed even if the owning building's def is missing).
        private static List<(string id, int amt)> RInvList(BinaryReader r)
        {
            int n = r.ReadInt32();
            var list = new List<(string, int)>(n);
            for (int i = 0; i < n; i++) { string id = r.ReadString(); int c = r.ReadInt32(); list.Add((id, c)); }
            return list;
        }
        private static void FillInv(Inventory inv, List<(string id, int amt)> list)
        {
            if (inv == null || list == null) return;
            foreach (var (id, amt) in list) { var it = SaveRegistry.Item(id); if (it != null) inv.Add(it, amt); }
        }

        // ---------------- progress ----------------
        private static void WriteResearch(BinaryWriter w)
        {
            w.Write(Research.Points);
            w.Write(Research.TotalDelivered);
            var d = Research.DeliveredByItem;
            int cnt = 0; foreach (var kv in d) if (kv.Key != null) cnt++;
            w.Write(cnt);
            foreach (var kv in d) if (kv.Key != null) { w.Write(kv.Key.id); w.Write(kv.Value); }
            var purchased = new List<string>();
            if (Research.Tree != null) foreach (var n in Research.Tree) if (n != null && n.purchased) purchased.Add(n.id);
            w.Write(purchased.Count);
            foreach (var id in purchased) w.Write(id);
        }
        private static void ReadResearch(BinaryReader r)
        {
            Research.Points = r.ReadInt32();
            Research.TotalDelivered = r.ReadInt32();
            int dn = r.ReadInt32();
            Research.DeliveredByItem.Clear();
            for (int i = 0; i < dn; i++) { var it = SaveRegistry.Item(r.ReadString()); int c = r.ReadInt32(); if (it != null) Research.DeliveredByItem[it] = c; }
            int pn = r.ReadInt32();
            var purchased = new HashSet<string>();
            for (int i = 0; i < pn; i++) purchased.Add(r.ReadString());
            if (Research.Tree != null) foreach (var n in Research.Tree) if (n != null) n.purchased = purchased.Contains(n.id);
        }

        private static void WriteObjectives(BinaryWriter w)
        {
            var obj = Objectives.Instance;
            if (obj == null || obj.quests == null) { w.Write(0); w.Write(false); return; }
            w.Write(obj.quests.Count);
            foreach (var q in obj.quests) w.Write(q.claimed);
            w.Write(obj.Won);
        }
        private static void ReadObjectives(BinaryReader r)
        {
            int n = r.ReadInt32();
            var claimed = new bool[n];
            for (int i = 0; i < n; i++) claimed[i] = r.ReadBoolean();
            bool won = r.ReadBoolean();
            Objectives.Instance?.LoadRestore(claimed, won);
        }

        private static void WritePlayer(BinaryWriter w)
        {
            var gb = GameBootstrap.Instance;
            WInv(w, gb != null && gb.Gatherer != null ? gb.Gatherer.Inventory : null);
            WVec(w, gb != null && gb.PlayerGO != null ? gb.PlayerGO.transform.position : Vector3.zero);
            w.Write(gb != null && gb.Cam != null ? gb.Cam.orthographicSize : 8f);
            w.Write(PlayerController.HasBoat);
            w.Write(PlayerController.ActiveMount);
            w.Write(PlayerController.LastRidden);
            w.Write(PlayerController.OwnedMount.Length);
            foreach (var b in PlayerController.OwnedMount) w.Write(b);
        }
        private static void ReadPlayer(BinaryReader r)
        {
            var gb = GameBootstrap.Instance;
            var carried = RInvList(r);
            if (gb != null && gb.Gatherer != null) { gb.Gatherer.Inventory.Clear(); FillInv(gb.Gatherer.Inventory, carried); }
            var pos = RVec(r);
            if (gb != null && gb.PlayerGO != null) gb.PlayerGO.transform.position = pos;
            float ortho = r.ReadSingle();
            if (gb != null && gb.Cam != null) gb.Cam.orthographicSize = ortho;
            PlayerController.HasBoat = r.ReadBoolean();
            int active = r.ReadInt32();
            int last = r.ReadInt32();
            int mn = r.ReadInt32();
            for (int i = 0; i < mn; i++) { bool b = r.ReadBoolean(); if (i < PlayerController.OwnedMount.Length) PlayerController.OwnedMount[i] = b; }
            PlayerController.LastRidden = last;
            PlayerController.SetActive(active);
        }

        // ---------------- resource nodes ----------------
        private static void WriteNodes(BinaryWriter w)
        {
            var real = new List<ResourceNode>();
            foreach (var n in ResourceNode.All)
                if (n != null && n.transform.parent == null && n.yields != null) real.Add(n); // skip invisible water sources (parented)
            w.Write(real.Count);
            foreach (var n in real)
            {
                WVec(w, n.transform.position);
                w.Write(n.BaseScaleForSave);
                WItem(w, n.yields);
                WColor(w, n.BaseColorForSave);
                w.Write(n.capacity);
                w.Write(n.regenAmount);
                w.Write(n.regenInterval);
                w.Write(n.Amount);
                w.Write(n.RegenTimerForSave);
            }
        }
        private static void ReadNodes(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                var pos = RVec(r);
                float scale = r.ReadSingle();
                var item = RItem(r);
                var col = RColor(r);
                int cap = r.ReadInt32();
                int regenAmt = r.ReadInt32();
                float regenIv = r.ReadSingle();
                int amount = r.ReadInt32();
                float regenTimer = r.ReadSingle();
                if (item == null) continue;
                CreateNode(item, col, pos, scale, cap, regenAmt, regenIv, amount, regenTimer);
            }
        }
        private static void CreateNode(ItemDefinition item, Color color, Vector3 pos, float scale,
            int cap, int regenAmt, float regenIv, int amount, float regenTimer)
        {
            var go = new GameObject(item.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * (scale > 0f ? scale : 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = NodeSprite(item.id);
            sr.color = color;
            sr.sortingOrder = 0;
            go.AddComponent<BoxCollider2D>();
            var node = go.AddComponent<ResourceNode>(); // Awake captures scale/colour, sets _amount = capacity(default)
            node.yields = item;
            node.capacity = cap;
            node.regenAmount = regenAmt;
            node.regenInterval = regenIv;
            node.SetAmountForLoad(amount, regenTimer); // override to the saved amount + regen phase
        }
        private static Sprite NodeSprite(string id) => id switch
        {
            "wood" => PlaceholderArt.Tree(),
            "stone" => PlaceholderArt.Rock(),
            "clay" => PlaceholderArt.ClayMound(),
            "copper_ore" => PlaceholderArt.OreCopper(),
            "ore" => PlaceholderArt.OreIron(),
            "oil" => PlaceholderArt.OilPatch(),
            "gems" => PlaceholderArt.Gem(),
            _ => PlaceholderArt.Rock(),
        };

        // ---------------- collectors ----------------
        private static void WriteCollectors(BinaryWriter w)
        {
            var list = ProductionBuilding.All;
            w.Write(list.Count);
            foreach (var pb in list)
            {
                if (pb == null) { w.Write(""); WVec(w, Vector3.zero); WDir(w, Belt.Dir.E); w.Write(0); w.Write(false); w.Write(0); continue; }
                w.Write(pb.def != null ? pb.def.displayName : "");
                WVec(w, pb.transform.position);
                WDir(w, pb.OutputSide);
                w.Write(pb.Tier);
                w.Write(pb.Paused);
                WInv(w, pb.Buffer);
            }
        }
        private static void ReadCollectors(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                var def = SaveRegistry.Building(r.ReadString());
                var pos = RVec(r);
                var side = RDir(r);
                int tier = r.ReadInt32();
                bool paused = r.ReadBoolean();
                var buf = RInvList(r);
                if (def == null) continue;
                var pb = ProductionBuilding.Spawn(def, pos, side);
                pb.LoadRestore(tier, paused);
                FillInv(pb.Buffer, buf);
            }
        }

        // ---------------- storage ----------------
        private static void WriteStorages(BinaryWriter w)
        {
            var list = StorageBuilding.All;
            w.Write(list.Count);
            foreach (var sb in list)
            {
                w.Write(sb != null && sb.def != null ? sb.def.displayName : "");
                WVec(w, sb != null ? sb.transform.position : Vector3.zero);
                WDir(w, sb != null ? sb.OutputSide : Belt.Dir.E);
                WItem(w, sb != null ? sb.accepts : null);
                WInv(w, sb != null ? sb.Store : null);
            }
        }
        private static void ReadStorages(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                var def = SaveRegistry.Building(r.ReadString());
                var pos = RVec(r);
                var side = RDir(r);
                var accepts = RItem(r);
                var store = RInvList(r);
                if (def == null) continue;
                var sb = StorageBuilding.Spawn(def, pos, side);
                sb.accepts = accepts;
                FillInv(sb.Store, store);
            }
        }

        // ---------------- workshops (incl. the Monument) ----------------
        private static void WriteWorkshops(BinaryWriter w)
        {
            var list = WorkshopBuilding.All;
            w.Write(list.Count);
            foreach (var wb in list)
            {
                w.Write(wb != null && wb.def != null ? wb.def.displayName : "");
                WVec(w, wb != null ? wb.transform.position : Vector3.zero);
                WDir(w, wb != null ? wb.OutputSide : Belt.Dir.E);
                w.Write(wb != null ? wb.RecipeIndexForSave : -1);
                w.Write(wb != null ? wb.Tier : 0);
                w.Write(wb != null && wb.Paused);
                WInv(w, wb != null ? wb.Buffer : null);
                WInv(w, wb != null ? wb.InBuffer : null);
            }
        }
        private static void ReadWorkshops(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                var def = SaveRegistry.Building(r.ReadString());
                var pos = RVec(r);
                var side = RDir(r);
                int recipe = r.ReadInt32();
                int tier = r.ReadInt32();
                bool paused = r.ReadBoolean();
                var buf = RInvList(r);
                var inbuf = RInvList(r);
                if (def == null) continue;
                var wb = WorkshopBuilding.Spawn(def, pos, side);
                wb.LoadRestore(tier, paused, recipe);
                FillInv(wb.Buffer, buf);
                FillInv(wb.InBuffer, inbuf);
            }
        }

        // ---------------- research buildings ----------------
        private static void WriteResearchBuildings(BinaryWriter w)
        {
            var list = ResearchBuilding.All;
            w.Write(list.Count);
            foreach (var rb in list)
            {
                w.Write(rb != null && rb.def != null ? rb.def.displayName : "");
                WVec(w, rb != null ? rb.transform.position : Vector3.zero);
                WDir(w, rb != null ? rb.OutputSide : Belt.Dir.E);
                WInv(w, rb != null ? rb.InBuffer : null);
            }
        }
        private static void ReadResearchBuildings(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                var def = SaveRegistry.Building(r.ReadString());
                var pos = RVec(r);
                var side = RDir(r);
                var inbuf = RInvList(r);
                if (def == null) continue;
                var rb = ResearchBuilding.Spawn(def, pos, side);
                FillInv(rb.InBuffer, inbuf);
            }
        }

        // ---------------- depots (stations + harbours) ----------------
        private static void WriteDepots(BinaryWriter w, Dictionary<Depot, int> index)
        {
            var list = Depot.All;
            w.Write(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i];
                if (d != null) index[d] = i;
                w.Write(d != null && d.def != null ? d.def.displayName : "");
                WVec(w, d != null ? d.transform.position : Vector3.zero);
                WDir(w, d != null ? d.FaceForSave : Belt.Dir.E);
                WItem(w, d != null ? d.item : null);
                WInv(w, d != null ? d.store : null);
            }
        }
        private static void ReadDepots(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                var def = SaveRegistry.Building(r.ReadString());
                var pos = RVec(r);
                var face = RDir(r);
                var item = RItem(r);
                var store = RInvList(r);
                Depot d = null;
                if (def != null)
                {
                    d = Depot.Spawn(def, pos, face);
                    d.item = item;
                    FillInv(d.store, store);
                }
                _loadDepots.Add(d); // keep the index aligned with the write order (null if def missing)
            }
        }

        // ---------------- power plants ----------------
        private static void WritePowerPlants(BinaryWriter w)
        {
            var list = PowerPlant.All;
            w.Write(list.Count);
            foreach (var p in list)
            {
                w.Write(p != null && p.def != null ? p.def.displayName : "");
                WVec(w, p != null ? p.transform.position : Vector3.zero);
                WDir(w, p != null ? p.InputSide : Belt.Dir.S);
                WInv(w, p != null ? p.Buffer : null);
            }
        }
        private static void ReadPowerPlants(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                var def = SaveRegistry.Building(r.ReadString());
                var pos = RVec(r);
                var side = RDir(r);
                var fuel = RInvList(r);
                if (def == null) continue;
                var p = PowerPlant.Spawn(def, pos, side);
                FillInv(p.Buffer, fuel);
            }
        }

        // ---------------- batteries ----------------
        private static void WriteBatteries(BinaryWriter w)
        {
            var list = Battery.All;
            w.Write(list.Count);
            foreach (var b in list)
            {
                w.Write(b != null && b.def != null ? b.def.displayName : "");
                WVec(w, b != null ? b.transform.position : Vector3.zero);
                w.Write(b != null ? b.Stored : 0f);
            }
        }
        private static void ReadBatteries(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                var def = SaveRegistry.Building(r.ReadString());
                var pos = RVec(r);
                float stored = r.ReadSingle();
                if (def == null) continue;
                var b = Battery.Spawn(def, pos);
                b.Stored = Mathf.Clamp(stored, 0f, b.capacity);
            }
        }

        // ---------------- power poles ----------------
        private static void WritePoles(BinaryWriter w)
        {
            var list = PowerPole.All;
            w.Write(list.Count);
            foreach (var p in list)
            {
                w.Write(p != null && p.def != null ? p.def.displayName : "");
                WVec(w, p != null ? p.transform.position : Vector3.zero);
            }
        }
        private static void ReadPoles(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                var def = SaveRegistry.Building(r.ReadString());
                var pos = RVec(r);
                if (def == null) continue;
                PowerPole.Spawn(def, pos, false); // wiring is restored explicitly (no auto-link)
            }
        }

        // ---------------- water/oil pumps ----------------
        private static void WritePumps(BinaryWriter w)
        {
            var list = WaterPump.All;
            w.Write(list.Count);
            foreach (var p in list)
            {
                w.Write(p != null && p.def != null ? p.def.displayName : "");
                WVec(w, p != null ? p.transform.position : Vector3.zero);
            }
        }
        private static void ReadPumps(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                var def = SaveRegistry.Building(r.ReadString());
                var pos = RVec(r);
                if (def == null) continue;
                WaterPump.Spawn(def, pos);
            }
        }

        // ---------------- pipes ----------------
        private static void WritePipes(BinaryWriter w)
        {
            var list = new List<Pipe>(PipeNet.Pipes.Values);
            w.Write(list.Count);
            foreach (var p in list)
            {
                w.Write(p.cell.x); w.Write(p.cell.y);
                w.Write(p.isSplitter);
                w.Write(p.isMerger);
                WItem(w, p.fluid);
            }
        }
        private static void ReadPipes(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                int x = r.ReadInt32(), y = r.ReadInt32();
                bool split = r.ReadBoolean();
                bool merge = r.ReadBoolean();
                var fluid = RItem(r);
                var p = Pipe.Spawn(null, new Vector3(x, y, 0f)); // def only sets the junction flags — set them below
                p.isSplitter = split;
                p.isMerger = merge;
                p.fluid = fluid;
                p.Refresh();
            }
        }

        // ---------------- rails ----------------
        private static void WriteRails(BinaryWriter w)
        {
            var list = RailTile.All;
            w.Write(list.Count);
            foreach (var t in list)
            {
                w.Write(t != null && t.def != null ? t.def.displayName : "");
                WVec(w, t != null ? t.transform.position : Vector3.zero);
                w.Write(t != null && t.elevated);
                w.Write(t != null ? t.links : 0);
            }
        }
        private static void ReadRails(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                var def = SaveRegistry.Building(r.ReadString());
                var pos = RVec(r);
                bool elevated = r.ReadBoolean();
                int links = r.ReadInt32();
                if (def == null) continue;
                var t = RailTile.Spawn(def, pos, elevated);
                if (t != null) t.links = links; // explicit connectivity (stations are promiscuous; belts under elevated)
            }
        }

        // ---------------- signals ----------------
        private static void WriteSignals(BinaryWriter w)
        {
            var list = new List<Signal>(Signal.All.Values);
            w.Write(list.Count);
            foreach (var s in list)
            {
                w.Write(s.cell.x); w.Write(s.cell.y);
                WDir(w, s.dir);
                w.Write(s.bothWays);
            }
        }
        private static void ReadSignals(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                int x = r.ReadInt32(), y = r.ReadInt32();
                var dir = RDir(r);
                bool both = r.ReadBoolean();
                Signal.Place(new Vector2Int(x, y), dir, both);
            }
        }

        // ---------------- bridges ----------------
        private static void WriteBridges(BinaryWriter w)
        {
            var list = Bridge.All;
            w.Write(list.Count);
            foreach (var b in list)
            {
                w.Write(b != null && b.def != null ? b.def.displayName : "");
                w.Write(b != null ? b.Cell.x : 0); w.Write(b != null ? b.Cell.y : 0);
            }
        }
        private static void ReadBridges(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                var def = SaveRegistry.Building(r.ReadString());
                int x = r.ReadInt32(), y = r.ReadInt32();
                if (def == null) continue;
                Bridge.Spawn(def, new Vector3(x, y, 0f));
            }
        }

        // ---------------- garages ----------------
        private static void WriteGarages(BinaryWriter w)
        {
            var list = Garage.All;
            w.Write(list.Count);
            foreach (var g in list)
            {
                w.Write(g != null && g.def != null ? g.def.displayName : "");
                WVec(w, g != null ? g.transform.position : Vector3.zero);
            }
        }
        private static void ReadGarages(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                var def = SaveRegistry.Building(r.ReadString());
                var pos = RVec(r);
                if (def == null) continue;
                Garage.Spawn(def, pos);
            }
        }

        // ---------------- belts ----------------
        private static void WriteBelts(BinaryWriter w)
        {
            var list = new List<Belt>(Belt.AllBelts);
            w.Write(list.Count);
            foreach (var b in list)
            {
                w.Write(b.Cell.x); w.Write(b.Cell.y);
                WDir(w, b.dir);
                w.Write(b.interval);
                w.Write(b.DisplayName ?? "Belt");
                w.Write(b.isSplitter);
                w.Write(b.isMerger);
                w.Write(b.underground);
                w.Write(b.undergroundExit);
                w.Write(b.isFilter);
                w.Write(b.isPriority);
                w.Write(b.isGate);
                WColor(w, b.BaseColorForSave);
                // filter whitelist
                w.Write(b.filterItems.Count);
                foreach (var f in b.filterItems) w.Write(f != null ? f.id : "");
                // items on the belt (lead-first)
                var items = b.ItemsForSave;
                w.Write(items.Count);
                foreach (var it in items) { w.Write(it.def != null ? it.def.id : ""); w.Write(it.p); WDir(w, it.entryEdge); }
            }
        }
        private static void ReadBelts(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                int x = r.ReadInt32(), y = r.ReadInt32();
                var dir = RDir(r);
                float interval = r.ReadSingle();
                string name = r.ReadString();
                bool split = r.ReadBoolean();
                bool merge = r.ReadBoolean();
                bool under = r.ReadBoolean();
                bool underExit = r.ReadBoolean();
                bool filter = r.ReadBoolean();
                bool priority = r.ReadBoolean();
                bool gate = r.ReadBoolean();
                var col = RColor(r);
                int fn = r.ReadInt32();
                var filters = new List<ItemDefinition>(fn);
                for (int k = 0; k < fn; k++) { var it = SaveRegistry.Item(r.ReadString()); if (it != null) filters.Add(it); }
                int itn = r.ReadInt32();
                var items = new List<BeltItem>(itn);
                for (int k = 0; k < itn; k++)
                {
                    var it = SaveRegistry.Item(r.ReadString());
                    float p = r.ReadSingle();
                    var edge = RDir(r);
                    if (it != null) items.Add(new BeltItem(it, p, edge));
                }
                var b = Belt.Spawn(new Vector2Int(x, y), dir, interval, split, merge, col, name, under, filter, priority, gate);
                if (b != null) b.LoadRestore(underExit, filters, items);
            }
        }

        // ---------------- routes (a line = a RouteVehicle + its stops) ----------------
        private static void WriteRoutes(BinaryWriter w, Dictionary<Depot, int> depotIndex)
        {
            var list = RouteVehicle.All;
            var valid = new List<RouteVehicle>();
            foreach (var rv in list) if (rv != null && rv.StopsForSave != null && rv.StopsForSave.Count >= 2) valid.Add(rv);
            w.Write(valid.Count);
            foreach (var rv in valid)
            {
                var stops = rv.StopsForSave;
                w.Write(stops.Count);
                foreach (var s in stops) w.Write(s != null && depotIndex.TryGetValue(s, out var idx) ? idx : -1);
                w.Write(rv.CapacityForSave);
                w.Write(rv.SpeedForSave);
                WColor(w, rv.ColorForSave);
                // stop modes (non-default)
                var modes = new List<(int idx, int mode)>();
                foreach (var s in stops)
                {
                    if (s == null) continue;
                    int m = rv.StopModeOf(s);
                    if (m != 0 && depotIndex.TryGetValue(s, out var idx)) modes.Add((idx, m));
                }
                w.Write(modes.Count);
                foreach (var (idx, m) in modes) { w.Write(idx); w.Write(m); }
            }
        }
        private static void ReadRoutes(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                int sc = r.ReadInt32();
                var idxs = new int[sc];
                for (int k = 0; k < sc; k++) idxs[k] = r.ReadInt32();
                int cap = r.ReadInt32();
                float speed = r.ReadSingle();
                var col = RColor(r);
                int mc = r.ReadInt32();
                var modes = new (int idx, int mode)[mc];
                for (int k = 0; k < mc; k++) { modes[k] = (r.ReadInt32(), r.ReadInt32()); }

                var stops = new List<Depot>();
                foreach (var idx in idxs)
                    if (idx >= 0 && idx < _loadDepots.Count && _loadDepots[idx] != null) stops.Add(_loadDepots[idx]);
                if (stops.Count < 2) continue;
                var rv = RouteVehicle.Spawn(stops, cap, speed, col);
                if (rv == null) continue;
                foreach (var (idx, mode) in modes)
                    if (idx >= 0 && idx < _loadDepots.Count && _loadDepots[idx] != null) rv.SetStopMode(_loadDepots[idx], mode);
            }
        }

        // ---------------- construction sites (v3) ----------------
        private static void WriteSites(BinaryWriter w)
        {
            var list = ConstructionSite.All;
            w.Write(list.Count);
            foreach (var s in list)
            {
                w.Write(s != null && s.def != null ? s.def.displayName : "");
                WVec(w, s != null ? s.transform.position : Vector3.zero);
                WDir(w, s != null ? s.outDir : Belt.Dir.E);
                w.Write(s != null ? s.buildProgress : 0f);
            }
        }
        private static void ReadSites(BinaryReader r)
        {
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                var def = SaveRegistry.Building(r.ReadString());
                var pos = RVec(r);
                var dir = RDir(r);
                float progress = r.ReadSingle();
                if (def == null) continue;
                var site = ConstructionSite.Spawn(def, pos, dir); // cost was already paid pre-save
                if (site != null) site.buildProgress = progress;  // resume where the build left off
            }
        }

        // ---------------- power wiring ----------------
        private static void WritePowerWires(BinaryWriter w)
        {
            var seen = new HashSet<(int, int, int, int)>();
            var edges = new List<(int, int, int, int)>();
            foreach (var node in PowerNode.All)
            {
                if (node == null) continue;
                var a = Cell(node.Pos);
                foreach (var lk in node.links)
                {
                    if (lk == null) continue;
                    var key = OrderEdge(a, Cell(lk.Pos));
                    if (seen.Add(key)) edges.Add(key);
                }
            }
            w.Write(edges.Count);
            foreach (var e in edges) { w.Write(e.Item1); w.Write(e.Item2); w.Write(e.Item3); w.Write(e.Item4); }
        }
        private static void ReadPowerWires(BinaryReader r)
        {
            int n = r.ReadInt32();
            var map = new Dictionary<Vector2Int, PowerNode>();
            foreach (var node in PowerNode.All) if (node != null) map[Cell(node.Pos)] = node;
            for (int i = 0; i < n; i++)
            {
                var a = new Vector2Int(r.ReadInt32(), r.ReadInt32());
                var b = new Vector2Int(r.ReadInt32(), r.ReadInt32());
                if (map.TryGetValue(a, out var na) && map.TryGetValue(b, out var nb) && na != nb) na.Connect(nb);
            }
        }
        private static (int, int, int, int) OrderEdge(Vector2Int a, Vector2Int b)
            => (a.x < b.x || (a.x == b.x && a.y <= b.y)) ? (a.x, a.y, b.x, b.y) : (b.x, b.y, a.x, a.y);
    }
}
