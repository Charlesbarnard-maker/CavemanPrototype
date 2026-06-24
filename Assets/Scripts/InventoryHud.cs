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
        public ItemDefinition monumentItem; // endgame win-goal tracker (10 blocks = win)
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
        private readonly List<int> _recent = new(); // recently-placed buildable indices
        private string _activeCat = "Gathering"; // build-menu accordion: only this category is open
        private bool _showMinimap = true;
        private bool _showGuide;
        private Vector2 _guideScroll;
        private Rect _guideRect;
        private Rect _finderRect;
        private bool _localTipShown; // one-time hint when a workshop first starves
        private Texture2D _panelTex; // dark panel background for readability
        private Dictionary<ItemDefinition, int> _totals;
        private readonly Dictionary<string, int> _trendSnap = new(); // chip values ~3s ago (for ▲/▼)
        private float _lastSnap = -999f;
        private Rect _topRect, _miniRect, _objRect;
        private readonly List<(string label, int value, string detail, Color color)> _chips = new();
        private GUIStyle _toast;
        private int _lastAge = -1;

        private static readonly (BuildingKind kind, string label)[] Cats =
        {
            (BuildingKind.Collector, "Gathering"),
            (BuildingKind.Workshop, "Workshops"),
            (BuildingKind.Belt, "Belts"),
            (BuildingKind.Bridge, "Bridges"),
            (BuildingKind.Pump, "Liquids"),
            (BuildingKind.Pipe, "Pipes"),
            (BuildingKind.Depot, "Depots"),
            (BuildingKind.Route, "Routes"),
            (BuildingKind.Power, "Power"),
            (BuildingKind.Build, "Construction"),
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
            if (kb.nKey.wasPressedThisFrame) _showMinimap = !_showMinimap;
            if (kb.gKey.wasPressedThisFrame) _showGuide = !_showGuide;

            // --- Sandbox / debug hotkeys ---
            if (kb.f1Key.wasPressedThisFrame && debugItems != null)
                foreach (var it in debugItems) if (it != null) gatherer.Inventory.Add(it, 500);
            if (kb.f2Key.wasPressedThisFrame && Colony.Instance != null) Colony.Instance.DebugAddPopulation(5);
            if (kb.f3Key.wasPressedThisFrame && Colony.Instance != null) Colony.Instance.DebugAdvanceAge();
            if (kb.f4Key.wasPressedThisFrame) Economy.FreeBuild = !Economy.FreeBuild;
            if (kb.f7Key.wasPressedThisFrame) Economy.LocalProduction = !Economy.LocalProduction;
            if (kb.f5Key.wasPressedThisFrame)
            {
                _speed = _speed >= 4f ? 1f : _speed * 2f;
                if (!_paused) Time.timeScale = _speed;
            }

            // Cache the resource totals once per frame (OnGUI runs twice/frame).
            if (gatherer != null) _totals = Economy.Totals(gatherer.Inventory);

            // Toasts fade out.
            for (int i = 0; i < Toast.Items.Count; i++) Toast.Items[i].t -= Time.unscaledDeltaTime;
            Toast.Items.RemoveAll(t => t.t <= 0f);

            // One-time onboarding hint the first time a workshop starves under local
            // production — so a new player learns inputs must be delivered, not pooled.
            if (!_localTipShown && Economy.LocalProduction)
            {
                foreach (var w in WorkshopBuilding.All)
                    if (w != null && w.StatusColor == Status.Starved)
                    {
                        _localTipShown = true;
                        Toast.Show("<color=#ffd24d>💡 Tip:</color> a workshop only runs on inputs that ARRIVE — put it next to its input storage/source, or belt the inputs in.");
                        break;
                    }
            }

            // Celebrate reaching a new age.
            int age = Colony.Instance != null ? Colony.Instance.Age : 0;
            if (_lastAge < 0) _lastAge = age;
            else if (age > _lastAge) { _lastAge = age; AnnounceAge(age); }
        }

        private void AnnounceAge(int age)
        {
            var names = new List<string>();
            if (builder != null)
                foreach (var d in builder.buildables)
                    if (d != null && d.unlockAge == age) names.Add(d.displayName);
            string unlocked = names.Count > 0 ? "Unlocked: " + string.Join(", ", names) : "";
            string an = Colony.AgeNames[Mathf.Clamp(age, 0, Colony.AgeNames.Length - 1)];
            Toast.Show($"<color=#ffd24d>🎉 {an}!</color>  <size=14>{unlocked}</size>");
        }

        void OnDisable() => Time.timeScale = 1f;

        void OnGUI()
        {
            if (gatherer == null) return;
            _s ??= new GUIStyle(GUI.skin.label) { fontSize = 20, richText = true };
            _small ??= new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true };
            _big ??= new GUIStyle(GUI.skin.label) { fontSize = 40, richText = true, alignment = TextAnchor.MiddleCenter };
            _btn ??= new GUIStyle(GUI.skin.button) { richText = true, alignment = TextAnchor.MiddleLeft, fontSize = 13 };
            _toast ??= new GUIStyle(GUI.skin.label) { richText = true, fontSize = 22, alignment = TextAnchor.MiddleCenter };
            if (_panelTex == null)
            {
                _panelTex = new Texture2D(1, 1);
                _panelTex.SetPixel(0, 0, new Color(0.07f, 0.08f, 0.10f, 0.93f));
                _panelTex.Apply();
            }

            DrawTopBar();
            DrawStatus();
            DrawObjective();
            if (builder != null) DrawBuildMenu();
            DrawMinimap();
            DrawSelectedPanel();
            DrawObjectives();
            DrawResourceFinder();
            DrawHoverInfo();
            DrawToasts();
            DrawFooter();
            if (Objectives.Instance != null && Objectives.Instance.Won) DrawWin();
            if (_showHelp) DrawHelp();
            if (_showGuide) DrawGuide();
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
                PointerOverUI = (_buildShown && _buildRect.Contains(m)) || (_selShown && _selRect.Contains(m))
                                || _topRect.Contains(m) || _miniRect.Contains(m) || _objRect.Contains(m)
                                || _finderRect.Contains(m) || (_showGuide && _guideRect.Contains(m));
            }
        }

        // Consistent dark panel background + border, for an organized look.
        private void PanelBg(Rect r)
        {
            if (_panelTex != null) GUI.DrawTexture(r, _panelTex);
            GUI.Box(r, GUIContent.none);
        }

        // ---- Top resource bar: grouped, k-formatted, single line, hover for detail ----
        private void DrawTopBar()
        {
            GUI.Box(new Rect(0, 0, Screen.width, 30), GUIContent.none);
            var totals = _totals ?? Economy.Totals(gatherer.Inventory);
            int Get(ItemDefinition i) { if (i == null) return 0; totals.TryGetValue(i, out int v); return v; }

            _chips.Clear();
            _chips.Add(("Wood", Get(woodItem), null, ColorOf(woodItem)));
            _chips.Add(("Stone", Get(stoneItem), null, ColorOf(stoneItem)));
            _chips.Add(("Water", Get(waterItem), null, ColorOf(waterItem)));

            int foodSum = 0; var foodDetail = new List<string>();
            int matSum = 0; var matDetail = new List<string>();
            if (debugItems != null)
                foreach (var it in debugItems)
                {
                    if (it == null) continue;
                    int v = Get(it);
                    if (it.foodValue > 0) { foodSum += v; if (v > 0) foodDetail.Add($"{it.displayName}: {K(v)}"); }
                    else if (it != woodItem && it != stoneItem && it != waterItem)
                    { matSum += v; if (v > 0) matDetail.Add($"{it.displayName}: {K(v)}"); }
                }
            _chips.Add(("Food", foodSum, foodDetail.Count > 0 ? string.Join("\n", foodDetail) : null, new Color(0.85f, 0.5f, 0.42f)));
            _chips.Add(("Goods", matSum, matDetail.Count > 0 ? string.Join("\n", matDetail) : null, new Color(0.72f, 0.7f, 0.55f)));

            var mp = Event.current.mousePosition;
            float x = 12f;
            int hover = -1; Rect hoverRect = default;
            bool snap = Time.unscaledTime - _lastSnap >= 3f; // refresh the trend baseline every ~3s
            for (int i = 0; i < _chips.Count; i++)
            {
                var c = _chips[i];
                string hex = ColorUtility.ToHtmlStringRGB(c.color);
                // Trend vs the last snapshot: ▲ growing, ▼ shrinking (deficit warning).
                string arrow = "";
                if (_trendSnap.TryGetValue(c.label, out int prev))
                    arrow = c.value > prev ? " <color=#7d7>▲</color>" : c.value < prev ? " <color=#e96>▼</color>" : "";
                if (snap) _trendSnap[c.label] = c.value;
                string text = $"<color=#{hex}>■</color> <b>{c.label}</b> {K(c.value)}{arrow}";
                float w = _small.CalcSize(new GUIContent(text)).x + 18f;
                var r = new Rect(x, 5f, w, 22f);
                GUI.Label(r, text, _small);
                if (r.Contains(mp)) { hover = i; hoverRect = r; }
                x += w;
            }
            if (snap) _lastSnap = Time.unscaledTime;
            _topRect = new Rect(0, 0, Screen.width, 30);

            if (hover >= 0 && _chips[hover].detail != null)
            {
                int lines = _chips[hover].detail.Split('\n').Length;
                var dr = new Rect(hoverRect.x, 31f, 190f, 10f + lines * 18f);
                GUI.Box(dr, GUIContent.none);
                GUI.Label(new Rect(dr.x + 8, dr.y + 5, dr.width - 16, dr.height - 10), $"<size=13>{_chips[hover].detail}</size>", _small);
            }
        }

        private static Color ColorOf(ItemDefinition i) => i != null ? i.color : Color.white;
        private static string K(int n) => n < 1000 ? n.ToString() : (n / 1000f).ToString("0.#") + "k";

        // ---- Minimap (bottom-right) ----
        private void DrawMinimap()
        {
            if (!_showMinimap) { _miniRect = default; return; }
            const float size = 168f;
            var r = new Rect(Screen.width - size - 12f, Screen.height - size - 12f, size, size);
            _miniRect = r;

            GUI.color = new Color(0.22f, 0.31f, 0.19f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var fog = FogOfWar.Instance;
            float W = fog != null ? fog.WorldSize : 240f;
            if (fog != null && fog.Tex != null) GUI.DrawTexture(r, fog.Tex); // explored = transparent → ground shows
            GUI.Box(r, GUIContent.none);

            void Dot(Vector3 wp, Color col, float s)
            {
                float u = (wp.x + W / 2f) / W, v = (wp.y + W / 2f) / W;
                if (u < 0f || u > 1f || v < 0f || v > 1f) return;
                GUI.color = col;
                GUI.DrawTexture(new Rect(r.x + u * size - s / 2f, r.yMax - v * size - s / 2f, s, s), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            // Storages tinted by fullness — a full storage (orange) is a backpressure cause.
            // Discovered resource patches — so you can see where to expand as local ones
            // deplete. Only shown where explored, to preserve the fog/exploration loop.
            foreach (var rn in ResourceNode.All)
            {
                if (rn == null || rn.yields == null) continue;
                if (fog != null && !fog.IsExplored(rn.transform.position)) continue;
                var rc = rn.yields.color;
                Dot(rn.transform.position, new Color(rc.r, rc.g, rc.b, 0.85f), 2f);
            }

            foreach (var s in StorageBuilding.All)
            {
                if (s == null) continue;
                float fill = (s.def != null && s.def.capacity > 0) ? (float)s.Store.Total() / s.def.capacity : 0f;
                Color sc = fill >= 0.99f
                    ? new Color(0.95f, 0.55f, 0.2f)
                    : Color.Lerp(new Color(0.5f, 0.5f, 0.62f), new Color(0.9f, 0.8f, 0.3f), fill);
                Dot(s.transform.position, sc, 3f);
            }
            foreach (var dp in Depot.All) if (dp != null) Dot(dp.transform.position, new Color(0.5f, 0.8f, 0.9f), 4f);
            // Collectors/workshops are coloured by live status, so starved (red) or
            // backed-up (yellow) buildings stand out across a sprawling base.
            foreach (var p in ProductionBuilding.All) if (p != null) Dot(p.transform.position, p.StatusColor, 4f);
            foreach (var wk in WorkshopBuilding.All) if (wk != null) Dot(wk.transform.position, wk.StatusColor, 4f);
            foreach (var h in HousingBuilding.All) if (h != null) Dot(h.transform.position, new Color(0.62f, 0.5f, 0.72f), 4f);
            if (gatherer != null) Dot(gatherer.transform.position, new Color(0.96f, 0.85f, 0.2f), 6f);

            // Legend so the dot colours are self-explanatory.
            GUI.Label(new Rect(r.x, r.y - 17f, r.width, 16f),
                "<size=10><color=#6c6>●</color> ok  <color=#f66>●</color> starved  <color=#fd4>●</color> full</size>", _small);
        }

        // ---- Status (population) ----
        private void DrawStatus()
        {
            GUILayout.BeginArea(new Rect(12, 36, Mathf.Max(620f, Screen.width - 320f), 28));
            var c = Colony.Instance;
            if (c != null)
            {
                string flags = "";
                if (c.Starving) flags += "   <color=#f55>STARVING</color>";
                if (c.Thirsty) flags += "   <color=#5cf>THIRSTY</color>";
                int working = c.Population - c.FreeWorkers; // assigned + builders + transporters

                // Bottleneck summary — count starved/backed-up machines (only shown when
                // there's a problem). Pairs with the minimap so you can find & fix it.
                int starved = 0, backed = 0;
                foreach (var pb in ProductionBuilding.All)
                { if (pb == null) continue; var sc = pb.StatusColor; if (sc == Status.Starved) starved++; else if (sc == Status.BackedUp) backed++; }
                foreach (var wkb in WorkshopBuilding.All)
                { if (wkb == null) continue; var sc = wkb.StatusColor; if (sc == Status.Starved) starved++; else if (sc == Status.BackedUp) backed++; }
                string bottleneck = "";
                if (starved > 0) bottleneck += $"   <color=#f66>⚠ {starved} starved</color>";
                if (backed > 0) bottleneck += $"   <color=#fd4>⏸ {backed} backed-up</color>";
                int prod = Mathf.RoundToInt(c.Productivity * 100f);
                string prodCol = prod >= 100 ? "#9cf" : "#f99";
                int happy = Mathf.RoundToInt(c.Happiness * 100f);
                string happyCol = happy >= 80 ? "#9f9" : happy >= 50 ? "#ffd24d" : "#f99";

                // Show which comfort goods are short, so low happiness is actionable.
                string needComfort = "";
                if (c.UnmetComforts != null && c.UnmetComforts.Count > 0)
                {
                    var names = new List<string>();
                    foreach (var it in c.UnmetComforts) if (it != null) names.Add(it.displayName);
                    if (names.Count > 0) needComfort = $"   <size=12><color=#f99>need: {string.Join(", ", names)}</color></size>";
                }

                // Monument progress (endgame): only shown once a Monument exists or blocks
                // are held. Uses the per-frame cached totals — no extra pool scan.
                string monu = "";
                if (monumentItem != null)
                {
                    int mb = 0; if (_totals != null) _totals.TryGetValue(monumentItem, out mb);
                    bool hasMon = false;
                    foreach (var w in WorkshopBuilding.All) if (w != null && w.output == monumentItem) { hasMon = true; break; }
                    if (hasMon || mb > 0) monu = $"   <color=#ffe08a>Monument {Mathf.Min(mb, 10)}/10</color>";
                }

                // Power (Industrial age): supply / demand, red while browning out.
                string power = "";
                Power.EnsureFresh();
                if (Power.Active || Power.Generation > 0 || Power.Demand > 0)
                {
                    bool brown = Power.Demand > Power.Generation + 0.01f;
                    string pc = brown ? "#f99" : "#9f9";
                    power = $"   <color={pc}>⚡ {Mathf.RoundToInt(Power.Generation)}/{Mathf.RoundToInt(Power.Demand)}{(brown ? " BROWNOUT" : "")}</color>";
                }

                GUILayout.Label($"<b>Population</b> {c.Population}/{c.Capacity}   <b>Working</b> {working}   <b>Free</b> {c.FreeWorkers}{bottleneck}   <color=#cda>{c.AgeName}</color>   <color=#dca>{c.Rank}</color>   <color={prodCol}>Output {prod}%</color>   <color={happyCol}>Happy {happy}%</color>{needComfort}   <color=#ffcf6b>Prosperity {c.Prosperity}</color>{power}{monu}{flags}", _s);
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
            if (HousingCount() <= 1) return "Build a House (needs Planks from a Sawmill) so your population can grow.";
            return "Settlement running! Click a building to manage its workers; expand at will.";
        }

        // ---- Build menu (left, categorised + scrollable + clickable) ----
        // Hover tooltip: name whatever's under the cursor (resource + amount, or building +
        // contents), so the placeholder shapes are readable. Skipped while placing/over a panel.
        private void DrawHoverInfo()
        {
            if (builder != null && builder.PendingIndex >= 0) return;
            if (PointerOverUI) return;
            var cam = Camera.main;
            var mouse = Mouse.current;
            if (cam == null || mouse == null) return;
            Vector2 sp = mouse.position.ReadValue();
            Vector3 w = cam.ScreenToWorldPoint(sp);
            var hit = Physics2D.OverlapPoint(w);
            if (hit == null) return;
            string info = HoverText(hit);
            if (string.IsNullOrEmpty(info)) return;

            var rect = new Rect(sp.x + 16f, Screen.height - sp.y + 8f, 234f, 30f);
            if (rect.xMax > Screen.width) rect.x = Screen.width - rect.width - 4f;
            PanelBg(rect);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 5f, rect.width - 16f, rect.height - 10f), info, _small);
        }

        private static string NameOf(BuildingDefinition d) => d != null ? d.displayName : "Building";
        private static string HoverText(Collider2D hit)
        {
            var rn = hit.GetComponent<ResourceNode>();
            if (rn != null && rn.yields != null)
            {
                string hex = ColorUtility.ToHtmlStringRGB(rn.yields.color);
                return $"<size=14><color=#{hex}>■</color> <b>{rn.yields.displayName}</b>  <color=#bbb>x{rn.Amount}</color></size>";
            }
            var pb = hit.GetComponent<ProductionBuilding>();
            if (pb != null) return $"<size=13><b>{NameOf(pb.def)}</b>  <color=#bbb>{(pb.produces != null ? pb.produces.displayName : "")}</color></size>";
            var wb = hit.GetComponent<WorkshopBuilding>();
            if (wb != null) return $"<size=13><b>{NameOf(wb.def)}</b>  <color=#bbb>{(wb.output != null ? wb.output.displayName : "")}</color></size>";
            var sb = hit.GetComponent<StorageBuilding>();
            if (sb != null) return $"<size=13><b>{NameOf(sb.def)}</b>  <color=#bbb>{(sb.accepts != null ? sb.accepts.displayName : "empty")}</color></size>";
            var dp = hit.GetComponent<Depot>();
            if (dp != null) return $"<size=13><b>{NameOf(dp.def)}</b>  <color=#bbb>{(dp.item != null ? dp.item.displayName : "empty")}</color></size>";
            var hb = hit.GetComponent<HousingBuilding>();
            if (hb != null) return $"<size=13><b>{NameOf(hb.def)}</b></size>";
            var pp = hit.GetComponent<PowerPlant>();
            if (pp != null) return "<size=13><b>Power Plant</b></size>";
            var cy = hit.GetComponent<ConstructionYard>();
            if (cy != null) return "<size=13><b>Construction Yard</b></size>";
            var wp = hit.GetComponent<WaterPump>();
            if (wp != null) return "<size=13><b>Water Pump</b>  <color=#bbb>water → pipes</color></size>";
            if (hit.GetComponent<Bridge>() != null) return "<size=13><b>Bridge</b></size>";
            if (hit.GetComponent<Pipe>() != null) return "<size=13><b>Pipe</b>  <color=#bbb>liquid network</color></size>";
            return null;
        }

        private void RecordRecent(int i)
        {
            _recent.Remove(i);
            _recent.Insert(0, i);
            while (_recent.Count > 4) _recent.RemoveAt(_recent.Count - 1);
        }

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
                else if (def.kind == BuildingKind.Route)
                {
                    GUILayout.Label(builder.RoutePickingFirst
                        ? "<b>Caravan route</b> — click the <color=#9cf>FROM</color> depot"
                        : "<b>Caravan route</b> — click the <color=#9cf>TO</color> depot", _s);
                    GUILayout.Label("<size=14>right-click to finish</size>", _small);
                }
                else
                {
                    bool isColl = def.kind == BuildingKind.Collector;
                    string bad = isColl ? $"<color=#f99>move onto a glowing {Name(def.item)} patch</color>" : "<color=#f99>move to a clear spot</color>";
                    string ok = builder.PlacementValid ? "<color=#9f9>green = click to place</color>" : bad;
                    GUILayout.Label($"<b>Placing {def.displayName}</b> — {ok}", _s);
                    string hint = isColl ? $"<color=#cda>Must sit next to a {Name(def.item)} source — the {Name(def.item)} patches are glowing.</color>  " : "";
                    bool hasPorts = def.kind == BuildingKind.Collector || def.kind == BuildingKind.Workshop;
                    string rot = hasPorts ? $"<color=#6f6>R rotates output ▸ {builder.BuildDir}</color> (belts pull from there) · " : "";
                    GUILayout.Label($"<size=14>{hint}{rot}right-click / Esc to cancel</size>", _small);
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
            PanelBg(_buildRect);

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
            GUILayout.Label("<b>Build</b>  <size=11>(click, then click the map · headers collapse)</size>", _small);

            int curAge = col != null ? col.Age : 0;
            _buildScroll = GUILayout.BeginScrollView(_buildScroll);

            // Draws one build entry (locked → label, unlocked → button). Local fn so the
            // recent row and category lists render identically.
            void Entry(int i)
            {
                var def = builder.buildables[i];
                if (def == null) return;
                if (!builder.IsUnlocked(def))
                {
                    string ageName = def.unlockAge < Colony.AgeNames.Length ? Colony.AgeNames[def.unlockAge] : "later";
                    GUILayout.Label(new GUIContent($"<size=11><color=#888>🔒 {def.displayName} — {ageName}</color></size>", Describe(def)), _small);
                    return;
                }
                string costCol = builder.CanAfford(def) ? "#9f9" : "#f99";
                string key = i < 9 ? (i + 1).ToString() : i == 9 ? "0" : "·";
                var label = new GUIContent($"<size=12>[{key}] {def.displayName}  <color={costCol}>{CostText(def)}</color></size>", Describe(def));
                if (GUILayout.Button(label, _btn)) { builder.BeginPlacement(i); RecordRecent(i); }
            }

            // Recently-placed shortcut row — fast re-access of what you're actively using.
            if (_recent.Count > 0)
            {
                GUILayout.Label("<b><color=#d8c8a0>Recent</color></b>", _small);
                foreach (int i in _recent)
                    if (i >= 0 && i < builder.buildables.Count && builder.IsUnlocked(builder.buildables[i])) Entry(i);
            }

            // Accordion categories — every header is always visible (compact), but only the
            // one you click is OPEN, so you see one category's items at a time instead of
            // scrolling the whole list. Empty/all-future categories are skipped.
            foreach (var cat in Cats)
            {
                bool hasAny = false;
                for (int i = 0; i < builder.buildables.Count; i++)
                {
                    var d = builder.buildables[i];
                    if (d != null && d.kind == cat.kind && d.unlockAge <= curAge + 1) { hasAny = true; break; }
                }
                if (!hasAny) continue;

                bool active = _activeCat == cat.label;
                if (GUILayout.Button($"<size=12><b><color=#d8c8a0>{(active ? "▼" : "▶")} {cat.label}</color></b></size>", _btn))
                    _activeCat = active ? "" : cat.label; // open this one (closes the rest); click again to close
                if (!active) continue;

                for (int i = 0; i < builder.buildables.Count; i++)
                {
                    var def = builder.buildables[i];
                    if (def == null || def.kind != cat.kind) continue;
                    if (def.unlockAge > curAge + 1) continue; // hide far-future buildings
                    Entry(i);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Tooltip describing the hovered build entry.
            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                var tr = new Rect(_buildRect.xMax + 6, _buildRect.y, 280, 172);
                PanelBg(tr);
                GUI.Label(new Rect(tr.x + 9, tr.y + 7, tr.width - 18, tr.height - 14), $"<size=13>{GUI.tooltip}</size>", _small);
            }
        }

        private static string Describe(BuildingDefinition def)
        {
            if (def == null) return "";
            // A hand-written description always wins; otherwise build a full one from data.
            if (!string.IsNullOrEmpty(def.description)) return def.description;

            string age = (def.unlockAge > 0 && def.unlockAge < Colony.AgeNames.Length)
                ? $"\n<i>Unlocks: {Colony.AgeNames[def.unlockAge]}.</i>" : "";
            switch (def.kind)
            {
                case BuildingKind.Collector:
                    return $"Gathers {Name(def.item)} from a nearby {Name(def.item)} patch. Needs workers (1 per harvester, up to {def.maxWorkers}). Place it next to the resource.{age}";
                case BuildingKind.Workshop:
                    return $"Recipe: {CostList(def.inputs)} → {def.outputPerCycle} {Name(def.item)}. Needs workers, and its inputs must be DELIVERED here (belt, or an adjacent storage/machine). Red dot = starved.{age}";
                case BuildingKind.Storage:
                    return def.configurable
                        ? $"Warehouse — stores ONE resource you pick (set it in its panel); good for Planks, Cooked Food, etc. Holds {def.capacity}.{age}"
                        : $"Stores {Name(def.item)} (up to {def.capacity}). Workers haul output here; if it fills up, production backs up.{age}";
                case BuildingKind.Housing:
                    return $"Houses {def.houseCapacity} people — raises the population cap so the colony can grow.{age}";
                case BuildingKind.Belt:
                    return $"Conveyor — carries items one cell in the way it faces ({(def.interval <= 0.6f ? "fast tier" : "slow tier")}). Click/drag to lay a line, R to rotate. Feeds storage or workshop inputs.{age}";
                case BuildingKind.Depot:
                    return $"Long-distance transfer station. Belt goods in/out and link two depots with a route vehicle. Holds {def.capacity}.{age}";
                case BuildingKind.Route:
                    return $"Sends a vehicle between two depots, hauling goods over distance (travel time scales with distance). Capacity {def.capacity}. Pick the FROM depot, then the TO depot.{age}";
                default:
                    return def.displayName;
            }
        }

        // ---- Selected building manage panel (bottom-right) ----
        private void DrawSelectedPanel()
        {
            var sel = builder != null ? builder.Selected : null;
            if (sel == null) { _selShown = false; return; }

            // Sit above the minimap when it's shown, so the two never overlap.
            float panelY = _showMinimap ? Screen.height - 384 : Screen.height - 200;
            var rect = new Rect(Screen.width - 290, panelY, 278, 188);
            _selRect = rect;
            _selShown = true;

            PanelBg(rect);
            GUILayout.BeginArea(new Rect(rect.x + 12, rect.y + 10, rect.width - 24, rect.height - 20));

            var sb = sel.GetComponent<StorageBuilding>();
            var hb = sel.GetComponent<HousingBuilding>();
            var wb = sel.GetComponent<WorkshopBuilding>();
            var cs = sel.GetComponent<ConstructionSite>();
            var dp = sel.GetComponent<Depot>();
            var pbSel = sel.GetComponent<ProductionBuilding>();
            var staff = sel.GetComponent<IStaffable>();
            string name = staff != null ? staff.StaffLabel : sb != null ? sb.def.displayName
                : hb != null ? hb.def.displayName : dp != null ? dp.def.displayName
                : cs != null ? cs.def.displayName : "Building";
            string tag = cs != null ? "  <size=14><color=#bbb>(building)</color></size>" : "";
            GUILayout.Label($"<b>{name}</b>{tag}", _s);

            // Plain-language status line for producers (the "understandable bottleneck" cue).
            if (wb != null || pbSel != null)
            {
                bool paused = wb != null ? wb.Paused : pbSel.Paused;
                Color sc = wb != null ? wb.StatusColor : pbSel.StatusColor;
                string st = paused ? "Paused"
                    : sc == Status.Working ? "Working"
                    : sc == Status.BackedUp ? "Output full — needs hauling out"
                    : sc == Status.Starved ? "Starved — inputs not arriving"
                    : "Idle — assign a worker";
                GUILayout.Label($"<size=13><color=#{ColorUtility.ToHtmlStringRGB(sc)}>● {st}</color></size>", _small);
            }

            bool add = false, rem = false, demo, close;
            if (wb != null)
            {
                GUILayout.Label($"<size=15>{RecipeText(wb)}</size>", _small);
                GUILayout.Label($"<size=11><color=#bbb>Belt inputs: {InStockText(wb)}   ·   Output: {wb.RatePerMin:0.#}/min</color></size>", _small);
                string miss = wb.MissingText();
                if (!string.IsNullOrEmpty(miss))
                    GUILayout.Label($"<size=12><color=#f66>⚠ Waiting for: {miss}</color>  <size=10>(deliver via belt or adjacent storage)</size></size>", _small);
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
                    if (staff.AssignedWorkers < staff.MaxWorkers
                        && GUILayout.Button("<size=12>Fill (assign free workers)</size>", _btn))
                        for (int k = 0; k < 64 && staff.TryAssign(); k++) { }
                }
                if (th != null)
                {
                    string pn = th.priorityItem != null ? th.priorityItem.displayName : "Any";
                    if (GUILayout.Button($"<size=12>Haul priority: {pn}</size>", _btn)) th.CyclePriority();
                }

                // Pause toggle — halt this building to free shared inputs for others (priorities).
                var pbPause = sel.GetComponent<ProductionBuilding>();
                if (wb != null || pbPause != null)
                {
                    bool isPaused = wb != null ? wb.Paused : pbPause.Paused;
                    if (GUILayout.Button(isPaused ? "<size=12><color=#9f9>▶ Resume</color></size>" : "<size=12>⏸ Pause</size>", _btn))
                    { if (wb != null) wb.TogglePause(); if (pbPause != null) pbPause.TogglePause(); }
                }

                // What this collector/workshop currently holds in its buffer.
                var pbb = sel.GetComponent<ProductionBuilding>();
                if (pbb != null && pbb.produces != null)
                    GUILayout.Label($"<size=12>Holds: {pbb.produces.displayName} {pbb.Buffer.Count(pbb.produces)}/{pbb.Buffer.capacity}   ·   Rate: {pbb.RatePerMin:0.#}/min</size>", _small);
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
            else if (dp != null)
            {
                string acc = dp.item != null ? dp.item.displayName : "(not set)";
                GUILayout.Label($"<size=15>Depot — {acc}: {dp.store.Total()}/{dp.def.capacity}</size>", _small);
                if (dp.store.Total() == 0)
                {
                    if (GUILayout.Button($"<size=12>Handle: {acc} (change)</size>", _btn)) dp.CycleItem();
                }
                else GUILayout.Label("<size=11><color=#888>empty it to change type</color></size>", _small);
                GUILayout.Label("<size=11><color=#bbb>Belt goods in; link with a Caravan route.</color></size>", _small);
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

        // ---- Resource finder: arrows to the nearest Wood/Stone/Food/Water you haven't
        //      tapped yet. Each line disappears once you build that collector. ----
        private static readonly string[] _arrows8 = { "→", "↗", "↑", "↖", "←", "↙", "↓", "↘" };
        private static string ArrowFor(Vector2 dir)
        {
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            int idx = Mathf.RoundToInt(((ang + 360f) % 360f) / 45f) % 8;
            return _arrows8[idx];
        }

        private void DrawResourceFinder()
        {
            if (gatherer == null) { _finderRect = default; return; }
            Vector2 p = gatherer.transform.position;
            var keys = new (ItemDefinition item, string label)[]
            { (woodItem, "Wood"), (stoneItem, "Stone"), (foodItem, "Food"), (waterItem, "Water") };

            var lines = new List<string>();
            foreach (var k in keys)
            {
                if (k.item == null || HasCollector(k.item.id)) continue; // already tapped → drop it
                ResourceNode best = null; float bestSq = float.MaxValue;
                foreach (var n in ResourceNode.All)
                {
                    if (n == null || n.yields != k.item) continue;
                    float sq = ((Vector2)n.transform.position - p).sqrMagnitude;
                    if (sq < bestSq) { bestSq = sq; best = n; }
                }
                if (best == null) continue;
                Vector2 dir = (Vector2)best.transform.position - p;
                string hex = ColorUtility.ToHtmlStringRGB(k.item.color);
                lines.Add($"<color=#{hex}>■</color> <b>{k.label}</b>  <size=18>{ArrowFor(dir)}</size> <size=12>{Mathf.RoundToInt(dir.magnitude)}m</size>");
            }
            if (lines.Count == 0) { _finderRect = default; return; }

            var rect = new Rect(Screen.width - 222, 182, 210, 28 + lines.Count * 22);
            _finderRect = rect;
            PanelBg(rect);
            GUILayout.BeginArea(new Rect(rect.x + 10, rect.y + 6, rect.width - 20, rect.height - 12));
            GUILayout.Label("<size=12><b>Find + build collectors:</b></size>", _small);
            foreach (var l in lines) GUILayout.Label($"<size=14>{l}</size>", _small);
            GUILayout.EndArea();
        }

        // ---- Objectives panel (top-right) ----
        private void DrawObjectives()
        {
            var o = Objectives.Instance;
            if (o == null) { _objRect = default; return; }

            var rect = new Rect(Screen.width - 300, 62, 290, 112);
            _objRect = rect;
            PanelBg(rect);
            GUILayout.BeginArea(new Rect(rect.x + 10, rect.y + 8, rect.width - 20, rect.height - 14));
            GUILayout.Label("<b>Objectives</b>", _small);
            bool any = false;
            foreach (var q in o.Active(3)) { GUILayout.Label($"<size=13>▸ {q.title}</size>", _small); any = true; }
            if (!any) GUILayout.Label("<size=13><color=#9f9>All objectives complete! 🎉</color></size>", _small);
            GUILayout.EndArea();
        }

        // ---- Toasts (centre) ----
        private void DrawToasts()
        {
            float y = 128f;
            foreach (var t in Toast.Items)
            {
                GUI.Label(new Rect(0, y, Screen.width, 30), t.msg, _toast);
                y += 30f;
            }
        }

        private void DrawFooter()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 330, 10, 320, 50));
            GUILayout.Label($"<size=15>B build · G guide · Space pause · H help · M overview · N map {(_showMinimap ? "on" : "off")}</size>", _small);
            string sandbox = Economy.FreeBuild ? "<color=#9f9>SANDBOX</color> · " : "";
            string mode = Economy.LocalProduction ? "<color=#fc8>Local logistics</color>" : "<color=#8cf>Global pool</color>";
            GUILayout.Label($"<size=12>{sandbox}Speed x{_speed:0} · {mode} (F7) · F1–F5 sandbox</size>", _small);
            GUILayout.EndArea();
        }

        // ---- Victory banner (shown once the Monument is completed) ----
        private void DrawWin()
        {
            var r = new Rect(Screen.width / 2f - 240, 150f, 480, 140);
            PanelBg(r);
            int peak = Colony.Instance != null ? Colony.Instance.PeakProsperity : 0;
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 14, r.width - 32, r.height - 28));
            GUILayout.Label("<color=#ffd24d><b>🏆 YOU WIN!</b></color>", _big);
            GUILayout.Label($"<size=16>You built a self-running civilization.\nPeak Prosperity: <b>{peak}</b>   ·   <color=#bbb>keep playing if you like</color></size>", _toast);
            GUILayout.EndArea();
        }

        // ---- In-game Guide (G): mechanics + a resource reference ----
        private void DrawGuide()
        {
            float w = 640f, h = Mathf.Min(Screen.height - 70f, 580f);
            var r = new Rect(Screen.width / 2f - w / 2f, Screen.height / 2f - h / 2f, w, h);
            _guideRect = r;
            PanelBg(r);
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 12, r.width - 32, r.height - 24));
            GUILayout.Label("<size=22><b>Guide</b></size>   <size=13><color=#bbb>(G to close)</color></size>", _s);
            _guideScroll = GUILayout.BeginScrollView(_guideScroll);

            void Section(string head, string body) =>
                GUILayout.Label($"<size=15><b>{head}</b>\n<color=#cfcfcf>{body}</color></size>\n", _small);

            Section("<color=#ffd24d>The Goal</color>",
                "Grow from a lone caveman to a self-running civilisation. Reach the Industrial Age and build the Monument (10 Blocks) to WIN.");
            Section("<color=#fc8>Logistics matter (the core)</color>",
                "A workshop only runs on inputs that ARRIVE — belt-fed, or sitting in an ADJACENT storage / collector / machine. It will NOT pull from across the map. Lay out your base so every machine is fed; a shortage cascades to everything downstream. (F7 toggles the old global-pool mode to compare.)");
            Section("<color=#9cf>People</color>",
                "Population grows while fed and housed. They eat Food and drink Water — running out causes decline. Beyond survival they want comfort goods (cooked food, bread, pottery, tools, clothes). How much you supply sets Happiness, which boosts productivity and growth. Growth raises demand — the escalating pull to keep scaling.");
            Section("<color=#ffcf6b>Prosperity & Rank</color>",
                "A climbing score from population, happiness and automation (collectors, workshops, belts, routes). Your settlement ranks up: Camp → Hamlet → Village → Town → City → Metropolis.");
            Section("<color=#cda>Ages</color>",
                "Stone → Tribal → Bronze → Iron → Industrial. Advancing costs resources + population and unlocks new buildings.");
            Section("<color=#6c6>Bottlenecks</color>",
                "Status dots: green working, yellow output-full (haul it out), red starved (no input), grey no worker. Problems pulse and show on the minimap. Click a building to see what it's waiting for, its rate, and to Pause it (free a scarce shared input for another).");
            Section("<color=#9f9>Expansion</color>",
                "Patches deplete as you harvest; collectors auto-chase fresh ones nearby, and go idle when a cluster runs dry — your cue to push outward. Ore and Gems are finite and far, so exploration + long routes (cart → train → drone) matter.");

            GUILayout.Label("<size=18><b>Resources</b></size>\n", _s);
            if (debugItems != null)
                foreach (var it in debugItems)
                {
                    if (it == null) continue;
                    string hex = ColorUtility.ToHtmlStringRGB(it.color);
                    string d = string.IsNullOrEmpty(it.description) ? "" : $"  <size=12><color=#bbb>{it.description}</color></size>";
                    GUILayout.Label($"<size=14><color=#{hex}>■</color> <b>{it.displayName}</b></size>{d}", _small);
                    GUILayout.Space(3);
                }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawHelp()
        {
            var r = new Rect(Screen.width / 2f - 240, Screen.height / 2f - 225, 500, 460);
            PanelBg(r);
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 12, r.width - 32, r.height - 24));
            GUILayout.Label("<b>How to play</b>", _s);
            GUILayout.Label("<size=15>" +
                "• WASD / arrows to move.  Mouse wheel = zoom, M = map.\n" +
                "• Left-click a tree/rock/bush to gather by hand.\n" +
                "• Click a building in the Build menu (left), then click the map.\n" +
                "• Builders (from the HQ) haul materials there, then build it.\n" +
                "• Collectors/workshops need WORKERS — each is one person.\n" +
                "• <b>Logistics matter:</b> a workshop only runs on inputs that ARRIVE — belt-fed or\n" +
                "  in an ADJACENT storage/machine. Lay it out so each machine is fed. (F7 = old mode.)\n" +
                "• Lay Belts (Bronze) — or Depots + a Caravan route — to move goods to storage.\n" +
                "• Click a building to manage it (workers, demolish).\n" +
                "• People need Food + Housing. Space = pause, Esc = cancel, X = demolish.\n" +
                "• Hold-drag to place a row of buildings; C copies the selected building's type.\n" +
                "• <b>Prosperity</b> (status bar) climbs with population, happiness & automation.\n" +
                "• <b>Goal:</b> reach the Industrial Age, build the <b>Monument</b>, and make " +
                "10 Monument Blocks to <color=#ffd24d>WIN</color>.\n" +
                "• Status dots: <color=#6c6>green</color>=working, <color=#fd4>yellow</color>=output full, " +
                "<color=#f66>red</color>=no input, <color=#999>grey</color>=no worker.\n" +
                "\n<b>Sandbox:</b> F1 +resources · F2 +5 people · F3 advance age · " +
                "F4 free build · F5 game speed.\n" +
                "</size>", _small);
            GUILayout.Label("<size=15>Press <b>H</b> to close  ·  Press <b>G</b> for the full Guide (mechanics + every resource)  ·  <b>N</b> hides the map.</size>", _small);
            GUILayout.EndArea();
        }

        // ---- helpers ----
        private int Avail(ItemDefinition i) => Economy.Available(i, gatherer.Inventory);

        private static bool HasCollector(string itemId)
        {
            foreach (var p in ProductionBuilding.All)
                if (p != null && p.produces != null && p.produces.id == itemId) return true;
            // Count one that's been PLACED but is still building, so the finder arrow
            // disappears the moment you commit a collector — not when it finishes.
            foreach (var cs in ConstructionSite.All)
                if (cs != null && cs.def != null && cs.def.kind == BuildingKind.Collector
                    && cs.def.item != null && cs.def.item.id == itemId) return true;
            return false;
        }

        private static bool HasStorage(string itemId)
        {
            foreach (var s in StorageBuilding.All)
                if (s.accepts != null && s.accepts.id == itemId) return true;
            return false;
        }

        private static int HousingCount() => HousingBuilding.All.Count;

        private static string Name(ItemDefinition item) => item != null ? item.displayName : "?";

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
