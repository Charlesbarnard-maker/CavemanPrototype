using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Caveman
{
    /// <summary>
    /// The game's UI controller (OnGUI). Keeps the always-on display minimal:
    /// a compact status line, a guiding objective, and a compact build bar.
    /// Detailed controls hide behind H; clicking a building opens a manage panel.
    /// Spacebar pauses. Sets PointerOverUI so world clicks don't fire through panels.
    /// </summary>
    public class InventoryHud : MonoBehaviour
    {
        public PlayerGatherer gatherer;
        public BuildController builder;
        public ItemDefinition woodItem, stoneItem, foodItem, waterItem;
        public List<ItemDefinition> debugItems; // all resources, for the sandbox resource dump
        private float _speed = 1f;

        /// <summary>True when the cursor is over an interactive HUD panel.</summary>
        public static bool PointerOverUI { get; private set; }

        private GUIStyle _s, _small, _big, _btn;
        private bool _paused;
        private bool _showHelp;
        private Vector2 _buildScroll;
        private Rect _buildRect, _selRect;
        private bool _buildShown, _selShown;
        private bool _showBuild;
        private Dictionary<ItemDefinition, int> _totals;

        private static readonly (BuildingKind kind, string label)[] Cats =
        {
            (BuildingKind.Collector, "Gathering"),
            (BuildingKind.Workshop, "Workshops"),
            (BuildingKind.Logistics, "Logistics"),
            (BuildingKind.Belt, "Belts"),
            (BuildingKind.Storage, "Storage"),
            (BuildingKind.Housing, "Housing"),
        };

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame)
            {
                _paused = !_paused;
                Time.timeScale = _paused ? 0f : _speed;
            }
            if (kb.hKey.wasPressedThisFrame) _showHelp = !_showHelp;
            if (kb.bKey.wasPressedThisFrame) _showBuild = !_showBuild;

            // --- Sandbox / debug hotkeys ---
            if (kb.f1Key.wasPressedThisFrame && debugItems != null)
                foreach (var it in debugItems) if (it != null) gatherer.Inventory.Add(it, 500);
            if (kb.f2Key.wasPressedThisFrame && Colony.Instance != null) Colony.Instance.DebugAddPopulation(5);
            if (kb.f3Key.wasPressedThisFrame && Colony.Instance != null) Colony.Instance.DebugAdvanceAge();
            if (kb.f4Key.wasPressedThisFrame) Economy.FreeBuild = !Economy.FreeBuild;
            if (kb.f5Key.wasPressedThisFrame)
            {
                _speed = _speed >= 4f ? 1f : _speed * 2f;
                if (!_paused) Time.timeScale = _speed;
            }

            // Cache the resource totals once per frame (OnGUI runs twice/frame).
            if (gatherer != null) _totals = Economy.Totals(gatherer.Inventory);
        }

        void OnDisable() => Time.timeScale = 1f;

        void OnGUI()
        {
            if (gatherer == null) return;
            _s ??= new GUIStyle(GUI.skin.label) { fontSize = 20, richText = true };
            _small ??= new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true };
            _big ??= new GUIStyle(GUI.skin.label) { fontSize = 40, richText = true, alignment = TextAnchor.MiddleCenter };
            _btn ??= new GUIStyle(GUI.skin.button) { richText = true, alignment = TextAnchor.MiddleLeft, fontSize = 13 };

            DrawTopBar();
            DrawStatus();
            DrawObjective();
            if (builder != null) DrawBuildMenu();
            DrawSelectedPanel();
            DrawFooter();
            if (_showHelp) DrawHelp();
            if (_paused) GUI.Label(new Rect(0, 60, Screen.width, 60), "<b>PAUSED</b>  <size=18>(space)</size>", _big);

            var col = Colony.Instance;
            if (col != null && !_paused && (col.Starving || col.Thirsty))
            {
                string need = col.Starving && col.Thirsty ? "NO FOOD OR WATER"
                            : col.Thirsty ? "OUT OF WATER" : "OUT OF FOOD";
                GUI.Label(new Rect(0, 96, Screen.width, 44),
                    $"<size=26><color=#ff5555><b>⚠ {need} — your people are dying!</b></color></size>", _big);
            }

            // Block world clicks when the cursor is over an interactive panel.
            if (Event.current.type == EventType.Repaint)
            {
                var m = Event.current.mousePosition;
                PointerOverUI = (_buildShown && _buildRect.Contains(m)) || (_selShown && _selRect.Contains(m));
            }
        }

        // ---- Top resource bar (full width) ----
        private void DrawTopBar()
        {
            GUI.Box(new Rect(0, 0, Screen.width, 30), GUIContent.none);
            GUILayout.BeginArea(new Rect(10, 5, Screen.width - 20, 22));
            GUILayout.BeginHorizontal();

            var totals = _totals ?? Economy.Totals(gatherer.Inventory);
            int Get(ItemDefinition i) { if (i == null) return 0; totals.TryGetValue(i, out int v); return v; }

            // Core resources always shown, then any others.
            var core = new[] { woodItem, stoneItem, foodItem, waterItem };
            foreach (var it in core) if (it != null) ResChip(it, Get(it));

            var extras = new List<ItemDefinition>();
            foreach (var k in totals.Keys)
                if (k != null && System.Array.IndexOf(core, k) < 0) extras.Add(k);
            extras.Sort((a, b) => string.Compare(a.displayName, b.displayName, System.StringComparison.Ordinal));
            foreach (var it in extras) ResChip(it, Get(it));

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void ResChip(ItemDefinition item, int count)
        {
            string hex = ColorUtility.ToHtmlStringRGB(item.color);
            GUILayout.Label($"<color=#{hex}>■</color> <b>{item.displayName}</b> {count}", _small);
            GUILayout.Space(16);
        }

        // ---- Status (population) ----
        private void DrawStatus()
        {
            GUILayout.BeginArea(new Rect(12, 36, 620, 28));
            var c = Colony.Instance;
            if (c != null)
            {
                string flags = "";
                if (c.Starving) flags += "   <color=#f55>STARVING</color>";
                if (c.Thirsty) flags += "   <color=#5cf>THIRSTY</color>";
                int working = c.Population - c.FreeWorkers; // assigned + builders + transporters
                int prod = Mathf.RoundToInt(c.Productivity * 100f);
                string prodCol = prod >= 100 ? "#9cf" : "#f99";
                GUILayout.Label($"<b>Population</b> {c.Population}/{c.Capacity}   <b>Working</b> {working}   <b>Free</b> {c.FreeWorkers}   <color=#cda>{c.AgeName}</color>   <color={prodCol}>Output {prod}%</color>{flags}", _s);
            }
            GUILayout.EndArea();
        }

        // ---- Objective ----
        private void DrawObjective()
        {
            GUILayout.BeginArea(new Rect(12, 64, 720, 30));
            GUILayout.Label($"<color=#ffd24d>▶ {CurrentObjective()}</color>", _s);
            GUILayout.EndArea();
        }

        private string CurrentObjective()
        {
            int wood = Avail(woodItem);
            if (!HasCollector("food") && wood < 4) return "Click the green trees/bushes to gather by hand.";
            if (!HasCollector("food")) return "Build a Forager Hut near the bushes (bottom) — food keeps people alive.";
            if (!HasCollector("wood")) return "Build a Wood Hut near the trees.";
            if (!HasStorage("wood")) return "Build a Wood Warehouse to stockpile wood.";
            if (TransportHub.All.Count == 0) return "Build a Mammoth Shack — its transporters carry goods from huts to storage.";
            if (HousingCount() <= 1) return "Build a House (needs Planks from a Sawmill) so your population can grow.";
            return "Settlement running! Click a building to manage its workers; expand at will.";
        }

        // ---- Build menu (left, categorised + scrollable + clickable) ----
        private void DrawBuildMenu()
        {
            if (builder.PendingIndex >= 0)
            {
                _buildShown = false;
                var def = builder.buildables[builder.PendingIndex];
                GUILayout.BeginArea(new Rect(12, Screen.height - 58, 520, 50));
                if (def.kind == BuildingKind.Belt)
                {
                    GUILayout.Label($"<b>Laying Belt</b> — facing <color=#9cf>{builder.BeltDir}</color>  <size=14>(R to rotate)</size>", _s);
                    GUILayout.Label("<size=14>click or drag to lay · right-click to finish</size>", _small);
                }
                else
                {
                    string ok = builder.PlacementValid ? "<color=#9f9>click to place</color>" : "<color=#f99>move to a valid spot</color>";
                    GUILayout.Label($"<b>Placing {def.displayName}</b> — {ok}", _s);
                    GUILayout.Label("<size=14>right-click / Esc to cancel</size>", _small);
                }
                GUILayout.EndArea();
                return;
            }

            if (!_showBuild)
            {
                _buildShown = false;
                GUILayout.BeginArea(new Rect(12, Screen.height - 34, 320, 28));
                GUILayout.Label("<size=15>Press <b>B</b> to open the build menu</size>", _small);
                GUILayout.EndArea();
                return;
            }

            const float top = 100f;
            float height = Mathf.Min(Screen.height - top - 16f, 470f);
            _buildRect = new Rect(12, top, 268, height);
            _buildShown = true;
            GUI.Box(_buildRect, GUIContent.none);

            GUILayout.BeginArea(new Rect(_buildRect.x + 8, _buildRect.y + 8, _buildRect.width - 16, _buildRect.height - 16));

            // --- Age + advance ---
            var col = Colony.Instance;
            if (col != null)
            {
                GUILayout.Label($"<b><color=#cda>{col.AgeName}</color></b>", _small);
                if (col.NextReq != null) // guard: last age has no requirement (was crashing the menu)
                {
                    var r = col.NextReq;
                    string c = col.CanAdvance() ? "#9f9" : "#f99";
                    if (GUILayout.Button($"<size=12>▲ Advance to {col.NextAgeName}\n<color={c}>(pop {r.pop}, {CostList(r.cost)})</color></size>", _btn))
                        col.AdvanceAge();
                }
            }
            GUILayout.Space(4);
            GUILayout.Label("<b>Build</b>  <size=11>(click, then click the map)</size>", _small);
            _buildScroll = GUILayout.BeginScrollView(_buildScroll);

            int curAge = col != null ? col.Age : 0;
            foreach (var cat in Cats)
            {
                bool header = false;
                for (int i = 0; i < builder.buildables.Count; i++)
                {
                    var def = builder.buildables[i];
                    if (def == null || def.kind != cat.kind) continue;
                    if (def.unlockAge > curAge + 1) continue; // hide far-future buildings
                    if (!header) { GUILayout.Label($"<b><color=#d8c8a0>{cat.label}</color></b>", _small); header = true; }

                    if (!builder.IsUnlocked(def))
                    {
                        string ageName = def.unlockAge < Colony.AgeNames.Length ? Colony.AgeNames[def.unlockAge] : "later";
                        GUILayout.Label($"<size=11><color=#888>🔒 {def.displayName} — {ageName}</color></size>", _small);
                        continue;
                    }

                    string costCol = builder.CanAfford(def) ? "#9f9" : "#f99";
                    string key = i < 9 ? (i + 1).ToString() : i == 9 ? "0" : "·";
                    var label = new GUIContent($"<size=12>[{key}] {def.displayName}  <color={costCol}>{CostText(def)}</color></size>", Describe(def));
                    if (GUILayout.Button(label, _btn))
                        builder.BeginPlacement(i);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Tooltip describing the hovered build entry.
            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                var tr = new Rect(_buildRect.xMax + 6, _buildRect.y, 250, 120);
                GUI.Box(tr, GUIContent.none);
                GUI.Label(new Rect(tr.x + 8, tr.y + 6, tr.width - 16, tr.height - 12), $"<size=13>{GUI.tooltip}</size>", _small);
            }
        }

        private static string Describe(BuildingDefinition def)
        {
            switch (def.kind)
            {
                case BuildingKind.Collector:
                    return $"Collector — produces {Name(def.item)}. Place near a {Name(def.item)} source; needs workers.";
                case BuildingKind.Workshop:
                    return $"Workshop — {CostList(def.inputs)} → {def.outputPerCycle} {Name(def.item)}. Needs workers; belts can feed its inputs.";
                case BuildingKind.Storage:
                    return def.configurable
                        ? "Warehouse — stores ONE resource you choose (set it in the building's panel). Good for Planks, Cooked Food, etc."
                        : $"Stores {Name(def.item)} only.";
                case BuildingKind.Housing:
                    return $"Housing — raises population cap by {def.houseCapacity}.";
                case BuildingKind.Logistics:
                    return def.mechanical
                        ? "Conveyor hub — auto-hauls nearby goods to storage, no worker."
                        : "Hauler hut — assign workers to carry nearby goods to storage (short range).";
                case BuildingKind.Belt:
                    return $"Belt — carries items in its direction ({(def.interval <= 0.6f ? "fast" : "slow")}). Drag to route; feeds storage or workshop inputs.";
                default:
                    return def.displayName;
            }
        }

        // ---- Selected building manage panel (bottom-right) ----
        private void DrawSelectedPanel()
        {
            var sel = builder != null ? builder.Selected : null;
            if (sel == null) { _selShown = false; return; }

            var rect = new Rect(Screen.width - 290, Screen.height - 200, 278, 188);
            _selRect = rect;
            _selShown = true;

            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 12, rect.y + 10, rect.width - 24, rect.height - 20));

            var sb = sel.GetComponent<StorageBuilding>();
            var hb = sel.GetComponent<HousingBuilding>();
            var wb = sel.GetComponent<WorkshopBuilding>();
            var cs = sel.GetComponent<ConstructionSite>();
            var staff = sel.GetComponent<IStaffable>();
            string name = staff != null ? staff.StaffLabel : sb != null ? sb.def.displayName
                : hb != null ? hb.def.displayName : cs != null ? cs.def.displayName : "Building";
            string tag = cs != null ? "  <size=14><color=#bbb>(building)</color></size>" : "";
            GUILayout.Label($"<b>{name}</b>{tag}", _s);

            bool add = false, rem = false, demo, close;
            if (wb != null)
            {
                GUILayout.Label($"<size=15>{RecipeText(wb)}</size>", _small);
                GUILayout.Label($"<size=11><color=#bbb>Belt inputs: {InStockText(wb)}</color></size>", _small);
            }

            if (staff != null)
            {
                var th = sel.GetComponent<TransportHub>();
                if (th != null && th.mechanical)
                {
                    GUILayout.Label("<size=14>Automatic conveyor (no worker)</size>", _small);
                }
                else
                {
                    GUILayout.Label($"Workers: {staff.AssignedWorkers}/{staff.MaxWorkers}    <size=14>(free: {(Colony.Instance != null ? Colony.Instance.FreeWorkers : 0)})</size>", _small);
                    GUILayout.BeginHorizontal();
                    rem = GUILayout.Button("- worker", GUILayout.Height(28));
                    add = GUILayout.Button("+ worker", GUILayout.Height(28));
                    GUILayout.EndHorizontal();
                }
                if (th != null)
                {
                    string pn = th.priorityItem != null ? th.priorityItem.displayName : "Any";
                    if (GUILayout.Button($"<size=12>Haul priority: {pn}</size>", _btn)) th.CyclePriority();
                }

                // What this collector/workshop currently holds in its buffer.
                var pbb = sel.GetComponent<ProductionBuilding>();
                if (pbb != null && pbb.produces != null)
                    GUILayout.Label($"<size=12>Holds: {pbb.produces.displayName} {pbb.Buffer.Count(pbb.produces)}/{pbb.Buffer.capacity}</size>", _small);
                else if (wb != null && wb.output != null)
                    GUILayout.Label($"<size=12>Holds: {wb.output.displayName} {wb.Buffer.Count(wb.output)}/{wb.Buffer.capacity}</size>", _small);
            }
            else if (sb != null)
            {
                string acc = sb.accepts != null ? sb.accepts.displayName : "(not set)";
                GUILayout.Label($"<size=15>Stores {acc}: {sb.Store.Total()}/{sb.def.capacity}</size>", _small);
                if (sb.configurable)
                {
                    if (sb.Store.Total() == 0)
                    {
                        if (GUILayout.Button($"<size=12>Store: {acc} (change)</size>", _btn)) sb.CycleAccepts();
                    }
                    else GUILayout.Label("<size=11><color=#888>empty it to change type</color></size>", _small);
                }
            }
            else if (hb != null)
            {
                GUILayout.Label($"<size=15>Houses {hb.houseCapacity} people</size>", _small);
                if (hb.isHQ && Colony.Instance != null)
                {
                    var col = Colony.Instance;
                    GUILayout.Label($"<b>Builders: {col.Builders}/{col.MaxBuilders}</b>   <size=14>(free: {col.FreeWorkers})</size>", _small);
                    GUILayout.BeginHorizontal();
                    rem = GUILayout.Button("- builder", GUILayout.Height(30));
                    add = GUILayout.Button("+ builder", GUILayout.Height(30));
                    GUILayout.EndHorizontal();
                }
            }
            else if (cs != null)
            {
                if (!cs.MaterialsDone)
                    GUILayout.Label($"<size=15>Materials: {cs.deliveredUnits}/{cs.totalUnits}</size>", _small);
                else
                    GUILayout.Label($"<size=15>Building… {(int)(cs.BuildFraction * 100)}%</size>", _small);
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            demo = GUILayout.Button("Demolish", GUILayout.Height(28));
            close = GUILayout.Button("Close", GUILayout.Height(28));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            bool hq = hb != null && hb.isHQ;
            if (add) { if (hq) Colony.Instance?.AddBuilder(); else builder.AssignSelected(); }
            if (rem) { if (hq) Colony.Instance?.RemoveBuilder(); else builder.UnassignSelected(); }
            if (demo) builder.DemolishSelected();
            if (close) builder.Deselect();
        }

        private void DrawFooter()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 330, 10, 320, 50));
            GUILayout.Label("<size=15>B build · Space pause · H help</size>", _small);
            string sandbox = Economy.FreeBuild ? "<color=#9f9>SANDBOX</color> · " : "";
            GUILayout.Label($"<size=12>{sandbox}Speed x{_speed:0} · F1–F5 sandbox</size>", _small);
            GUILayout.EndArea();
        }

        private void DrawHelp()
        {
            var r = new Rect(Screen.width / 2f - 240, Screen.height / 2f - 175, 480, 360);
            GUI.Box(r, GUIContent.none);
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 12, r.width - 32, r.height - 24));
            GUILayout.Label("<b>How to play</b>", _s);
            GUILayout.Label("<size=15>" +
                "• WASD / arrows to move.  Mouse wheel = zoom, M = map.\n" +
                "• Left-click a tree/rock/bush to gather by hand.\n" +
                "• Click a building in the Build menu (left), then click the map.\n" +
                "• Builders (from the HQ) haul materials there, then build it.\n" +
                "• Collectors/workshops need WORKERS — each is one person.\n" +
                "• A Mammoth Shack's transporters carry goods from huts to storage.\n" +
                "• Click a building to manage it (workers, demolish).\n" +
                "• People need Food + Housing. Space = pause, Esc = cancel, X = demolish.\n" +
                "\n<b>Sandbox:</b> F1 +resources · F2 +5 people · F3 advance age · " +
                "F4 free build · F5 game speed.\n" +
                "</size>", _small);
            GUILayout.Label("<size=15>Press H to close.</size>", _small);
            GUILayout.EndArea();
        }

        // ---- helpers ----
        private int Avail(ItemDefinition i) => Economy.Available(i, gatherer.Inventory);

        private static bool HasCollector(string itemId)
        {
            foreach (var p in ProductionBuilding.All)
                if (p.produces != null && p.produces.id == itemId) return true;
            return false;
        }

        private static bool HasStorage(string itemId)
        {
            foreach (var s in StorageBuilding.All)
                if (s.accepts != null && s.accepts.id == itemId) return true;
            return false;
        }

        private static int HousingCount() => HousingBuilding.All.Count;

        private static string CostText(BuildingDefinition def) => CostList(def.cost);

        private static string CostList(List<ItemAmount> cost)
        {
            var parts = new List<string>();
            if (cost != null)
                foreach (var c in cost)
                    if (c.item != null) parts.Add($"{c.amount} {c.item.displayName}");
            return parts.Count > 0 ? string.Join(", ", parts) : "free";
        }

        private static string InStockText(WorkshopBuilding w)
        {
            var parts = new List<string>();
            foreach (var c in w.inputs)
                if (c.item != null) parts.Add($"{c.item.displayName} {w.InBuffer.Count(c.item)}");
            return parts.Count > 0 ? string.Join(" · ", parts) : "—";
        }

        private static string RecipeText(WorkshopBuilding w)
        {
            var parts = new List<string>();
            foreach (var i in w.inputs)
                if (i.item != null) parts.Add($"{i.amount} {i.item.displayName}");
            string ins = parts.Count > 0 ? string.Join(" + ", parts) : "—";
            string outName = w.output != null ? w.output.displayName : "?";
            return $"{ins} → {w.outputPerCycle} {outName}";
        }
    }
}
