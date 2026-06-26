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
        public ItemDefinition meatItem, clayItem, oreItem; // expansion-target hints (shown from Tribal)
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
        private readonly HashSet<int> _pinned = new(); // pinned (favourite) buildable indices
        private string _activeCat = "Production"; // build-menu accordion: only this group is open
        private bool _showMinimap = true;
        private bool _showGuide;
        private Vector2 _guideScroll;
        private Rect _guideRect;
        private bool _showResearch;       // the research tree panel (T)
        private Vector2 _researchScroll;
        private Rect _researchRect;
        private readonly HashSet<string> _affordToasted = new(); // "research available" hint fires once per node
        private Rect _finderRect;
        private bool _localTipShown; // one-time hint when a workshop first starves
        private bool _collectorTipShown; // one-time hint when a collector first backs up
        private Texture2D _panelTex; // dark panel background for readability
        private Dictionary<ItemDefinition, int> _totals;
        private readonly Dictionary<string, int> _trendSnap = new(); // chip values ~3s ago (for ▲/▼)
        private float _lastSnap = -999f;
        private Rect _topRect, _miniRect, _objRect;
        private readonly List<(string label, int value, string detail, Color color)> _chips = new();
        private GUIStyle _toast;
        private int _lastAge = -1;
        // "What just changed" card shown on an age advance. It now PERSISTS until the player
        // dismisses it (click-to-close), so the new-buildings tips are never missed.
        private float _ageCardT;             // >0 = the card is shown (no longer a countdown)
        private string _ageCardTitle, _ageCardBody;
        private Rect _ageCardRect;           // cached each draw for the click-to-dismiss hit-test

        // Meta-groups keep the menu to a handful of headers (not one per building kind).
        private static readonly (string label, BuildingKind[] kinds)[] Cats =
        {
            ("Production", new[] { BuildingKind.Collector, BuildingKind.Workshop, BuildingKind.Power, BuildingKind.Research }),
            ("Logistics", new[] { BuildingKind.Belt, BuildingKind.Bridge, BuildingKind.Pipe, BuildingKind.Pump, BuildingKind.Depot }),
            ("Infrastructure", new[] { BuildingKind.Build, BuildingKind.Storage }),
            ("Settlement", new[] { BuildingKind.Housing }),
        };
        private static bool InGroup(BuildingKind[] kinds, BuildingKind k) => System.Array.IndexOf(kinds, k) >= 0;

        // --- Context-panel layering (Priority 1): only ONE large panel is open at a time.
        // Opening any of Build / Research / Guide / Help closes the others, so the player
        // focuses on a single system. The minimap is a world overlay, not a context panel,
        // so it's exempt. ---
        private enum Panel { Build, Research, Guide, Help }
        private void CloseAllPanels() { _showBuild = _showResearch = _showGuide = _showHelp = false; }
        private void TogglePanel(Panel p)
        {
            bool wasOpen = p == Panel.Build ? _showBuild
                         : p == Panel.Research ? _showResearch
                         : p == Panel.Guide ? _showGuide
                         : _showHelp;
            CloseAllPanels();
            if (wasOpen) return; // it was open → we just closed it
            switch (p)
            {
                case Panel.Build: _showBuild = true; break;
                case Panel.Research: _showResearch = true; break;
                case Panel.Guide: _showGuide = true; break;
                case Panel.Help: _showHelp = true; break;
            }
        }
        // A full-screen "mode" panel is up — used to dim the world and hide competing widgets.
        private bool ModalOpen => _showResearch || _showGuide || _showHelp;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame)
            {
                _paused = !_paused;
                Time.timeScale = _paused ? 0f : _speed;
            }
            if (kb.hKey.wasPressedThisFrame) TogglePanel(Panel.Help);
            if (kb.bKey.wasPressedThisFrame) TogglePanel(Panel.Build);
            if (kb.nKey.wasPressedThisFrame) _showMinimap = !_showMinimap;
            if (kb.gKey.wasPressedThisFrame) TogglePanel(Panel.Guide);
            if (kb.tKey.wasPressedThisFrame) TogglePanel(Panel.Research);

            // QoL: one-time "research available" toast when a tree node first becomes affordable.
            if (Research.Tree != null)
                foreach (var n in Research.Tree)
                    if (Research.CanBuy(n) && _affordToasted.Add(n.id))
                        Toast.Show($"<color=#9cf>🔬 Research available: {n.name} ({n.cost} pts) — press T</color>");

            // --- Sandbox / debug hotkeys ---
            if (kb.f1Key.wasPressedThisFrame && debugItems != null)
                foreach (var it in debugItems) if (it != null) gatherer.Inventory.Add(it, 500);
            if (kb.f3Key.wasPressedThisFrame && Colony.Instance != null) Colony.Instance.DebugAdvanceAge();
            if (kb.f4Key.wasPressedThisFrame) Economy.FreeBuild = !Economy.FreeBuild;
            if (kb.f7Key.wasPressedThisFrame) Economy.LocalProduction = !Economy.LocalProduction;
            if (kb.f8Key.wasPressedThisFrame && FogOfWar.Instance != null) FogOfWar.Instance.RevealAll();
            if (kb.f9Key.wasPressedThisFrame) Economy.StoredOnly = !Economy.StoredOnly;
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
            // The age-advance card no longer auto-dismisses — it stays up until the player clicks
            // "Got it" / the card (see DrawAgeCard), so there's always time to read the new tips.

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

            // One-time hint the first time a COLLECTOR backs up — teaches the core early-game
            // rule (its output is unusable until belted to a Storage) at the exact moment it bites.
            if (!_collectorTipShown && Economy.StoredOnly)
            {
                foreach (var p in ProductionBuilding.All)
                    if (p != null && p.StatusColor == Status.BackedUp)
                    {
                        _collectorTipShown = true;
                        Toast.Show("<color=#ffd24d>💡 Tip:</color> a collector's output piles up until something USES it. Easiest: place a workshop right NEXT TO it — machines pull from their neighbours, no belt needed. To stock a Storage (to build with, or feed a workshop), belt it in from the green output arrow.");
                        break;
                    }
            }

            // Celebrate reaching a new age.
            int age = Colony.Instance != null ? Colony.Instance.Age : 0;
            if (_lastAge < 0) _lastAge = age;
            // On an age advance, drop whatever panel was open (you just bought it in the research
            // mode) so the "what changed" card is seen against the world, then it fades on its own.
            else if (age > _lastAge) { _lastAge = age; CloseAllPanels(); builder?.UpgradeAllRoutes(); AnnounceAge(age); }
        }

        private void AnnounceAge(int age)
        {
            var names = new List<string>();
            if (builder != null)
                foreach (var d in builder.buildables)
                    if (d != null && d.unlockAge == age) names.Add(d.displayName);
            string unlocked = names.Count > 0 ? string.Join(", ", names) : "—";
            string an = Colony.AgeNames[Mathf.Clamp(age, 0, Colony.AgeNames.Length - 1)];

            // One-line "what this age adds to the factory" (existing systems only — no new ones).
            string rule = age switch
            {
                1 => "New: <b>Charcoal &amp; Clay</b> chains. Cluster machines so neighbours feed each other, or link them with belts.",
                2 => "New: <b>Kiln &amp; Bricks, Pottery, Conveyors</b> (research). Deeper multi-input recipes — plan your layout.",
                3 => "New: <b>Ore → Metal → Tools</b>. Ore is finite &amp; far — expand outward and run transport routes home.",
                4 => "New: <b>Power</b> — machines draw from a Coal Generator; under-supply browns out. Build the Monument to win.",
                _ => "",
            };

            _ageCardTitle = $"<color=#ffd24d><b>🎉 {an} reached!</b></color>";
            _ageCardBody = $"<b>New buildings:</b> {unlocked}"
                + (string.IsNullOrEmpty(rule) ? "" : $"\n{rule}")
                + "\n<color=#9cf>🔬 Press T to research what's next  ·  B to build.</color>";
            _ageCardT = 1f; // a "shown" flag now — the card stays up until dismissed (DrawAgeCard)
        }

        // The age-transition card: an explicit "what just changed" moment. It PERSISTS until the
        // player dismisses it (a "Got it" button OR a click anywhere on the card), so the tips never
        // vanish before they're read. Centred upper-middle so it doesn't sit on the build area.
        private void DrawAgeCard()
        {
            if (_ageCardT <= 0f || string.IsNullOrEmpty(_ageCardBody)) return;
            float w = Mathf.Min(600f, Screen.width - 40f);
            float inner = w - 36f;
            // Auto-size height to the wrapped content (so a long "New buildings:" list never clips)
            // plus a row for the dismiss button.
            string title = $"<size=20>{_ageCardTitle}</size>", body = $"<size=14>{_ageCardBody}</size>";
            float h = _s.CalcHeight(new GUIContent(title), inner)
                    + _small.CalcHeight(new GUIContent(body), inner) + 34f + 30f;
            var r = new Rect(Screen.width / 2f - w / 2f, 188f, w, h); // below the toast stack (~124+) so they don't overlap
            _ageCardRect = r;
            PanelBg(r);
            GUILayout.BeginArea(new Rect(r.x + 18, r.y + 12, inner, h - 24));
            GUILayout.Label(title, _s);
            GUILayout.Label(body, _small);
            GUILayout.Space(4f);
            if (GUILayout.Button("<b>Got it ✓</b>   <size=11>(or click this card)</size>", _btn)) _ageCardT = 0f;
            GUILayout.EndArea();

            // Click anywhere on the card (outside the button) also dismisses it. If the button was
            // pressed, GUILayout.Button already consumed the event (type==Used) so this won't double-fire.
            var e = Event.current;
            if (e.type == EventType.MouseDown && r.Contains(e.mousePosition)) { _ageCardT = 0f; e.Use(); }
        }

        void OnDisable() => Time.timeScale = 1f;

        void OnGUI()
        {
            if (gatherer == null) return;
            _s ??= new GUIStyle(GUI.skin.label) { fontSize = 20, richText = true, wordWrap = true };
            _small ??= new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, wordWrap = true };
            _big ??= new GUIStyle(GUI.skin.label) { fontSize = 40, richText = true, alignment = TextAnchor.MiddleCenter };
            _btn ??= new GUIStyle(GUI.skin.button) { richText = true, alignment = TextAnchor.MiddleLeft, fontSize = 13, wordWrap = true };
            _toast ??= new GUIStyle(GUI.skin.label) { richText = true, fontSize = 20, alignment = TextAnchor.UpperCenter, wordWrap = true };
            if (_panelTex == null)
            {
                _panelTex = new Texture2D(1, 1);
                _panelTex.SetPixel(0, 0, new Color(0.07f, 0.08f, 0.10f, 0.93f));
                _panelTex.Apply();
            }

            bool modal = ModalOpen;

            // --- Layer 1: world-level always-on HUD (top bar, vitals, alerts, objectives). ---
            DrawTopBar();
            DrawStatus();
            DrawObjective();
            DrawObjectives();

            // --- Layer 2: contextual / secondary widgets — hidden while a full "mode" panel
            //     (Research / Guide / Help) is open, so the player focuses on one system. ---
            if (!modal)
            {
                if (builder != null) DrawBuildMenu();
                DrawMinimap();
                DrawSelectedPanel();
                DrawResourceFinder();
                DrawFooter();
            }

            DrawHoverInfo();
            DrawToasts();
            if (!modal) DrawAgeCard(); // temporary "what changed" moment; yields if a mode panel is open
            if (Objectives.Instance != null && Objectives.Instance.Won) DrawWin();

            // --- Layer 3: the focused mode panel, over a dimmed world. ---
            if (modal) DimWorld();
            if (_showHelp) DrawHelp();
            if (_showGuide) DrawGuide();
            if (_showResearch) DrawResearchPanel();
            if (_paused) GUI.Label(new Rect(0, 60, Screen.width, 60), "<b>PAUSED</b>  <size=18>(space)</size>", _big);

            // Block world clicks when the cursor is over an interactive panel. A modal mode
            // dims the whole screen, so it blocks everywhere (clicking off-panel just focuses).
            if (Event.current.type == EventType.Repaint)
            {
                var m = Event.current.mousePosition;
                PointerOverUI = modal
                                || (_buildShown && _buildRect.Contains(m)) || (_selShown && _selRect.Contains(m))
                                || _topRect.Contains(m) || _miniRect.Contains(m) || _objRect.Contains(m)
                                || _finderRect.Contains(m)
                                || (_ageCardT > 0f && _ageCardRect.Contains(m));
            }
        }

        // Dim the world + Layer-1 HUD behind a focused mode panel (Satisfactory/DSP style),
        // so a single system has the player's attention. Critical alerts still draw on top.
        private void DimWorld()
        {
            var c = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = c;
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

            // Everything else the factory makes, summed into "Goods" (hover for the breakdown).
            int matSum = 0; var matDetail = new List<string>();
            if (debugItems != null)
                foreach (var it in debugItems)
                {
                    if (it == null || it == woodItem || it == stoneItem) continue;
                    int v = Get(it);
                    matSum += v; if (v > 0) matDetail.Add($"{it.displayName}: {K(v)}");
                }
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

        // ---- Status (top bar) ----
        private void DrawStatus()
        {
            GUILayout.BeginArea(new Rect(12, 34, Mathf.Max(620f, Screen.width - 310f), 52));
            var c = Colony.Instance;
            if (c != null)
            {
                // Bottleneck summary — count starved / backed-up machines. This is THE core factory
                // alert; it pairs with the minimap dots so you can find & fix the problem.
                int starved = 0, backed = 0;
                foreach (var pb in ProductionBuilding.All)
                { if (pb == null) continue; var sc = pb.StatusColor; if (sc == Status.Starved) starved++; else if (sc == Status.BackedUp) backed++; }
                foreach (var wkb in WorkshopBuilding.All)
                { if (wkb == null) continue; var sc = wkb.StatusColor; if (sc == Status.Starved) starved++; else if (sc == Status.BackedUp) backed++; }
                // Stay quiet early (Factorio focus): only show production FAILURE (starved) until the
                // base is actually going; non-critical states appear once established.
                bool established = c.Age >= 1 || c.PeakProsperity >= 100;
                string bottleneck = "";
                if (starved > 0) bottleneck += $"   <color=#f66>⚠ {starved} starved (no input)</color>";
                if (established && backed > 0) bottleneck += $"   <color=#fd4>⏸ {backed} backed-up (output full)</color>";

                // Monument progress (endgame): shown once a Monument exists or blocks are held.
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

                string research = $"   <color=#9cf>🔬 {Research.Points} pts <size=11>(T)</size></color>";

                // PRIMARY line: age, research pool, and live factory bottleneck alerts.
                GUILayout.Label($"<color=#cda><b>{c.AgeName}</b></color>{research}{bottleneck}", _s);
                // SECONDARY line (once established): automation score + power + monument.
                if (established)
                    GUILayout.Label($"<size=13><color=#dca>{c.Rank}</color>   <color=#ffcf6b>Industry {c.Prosperity}</color>{power}{monu}</size>", _small);
            }
            GUILayout.EndArea();
        }

        // ---- Objective ----
        private void DrawObjective()
        {
            GUILayout.BeginArea(new Rect(12, 82, Mathf.Min(720f, Screen.width - 330f), 30));
            GUILayout.Label($"<color=#ffd24d>▶ {CurrentObjective()}</color>", _s);
            GUILayout.EndArea();
        }

        private string CurrentObjective()
        {
            int wood = Avail(woodItem);
            var col = Colony.Instance;
            if (!HasCollector("wood") && wood < 5) return "Click trees & rocks to gather Wood and Stone by hand.";
            if (!HasCollector("wood")) return "Build a Wood Hut near the trees — it gathers Wood for you.";
            if (!HasStorage("wood")) return "Build a Woodpile next to the Wood Hut to stockpile Wood.";
            if (!HasCollector("stone")) return "Build a Stone Pit near the rocks.";
            if (WorkshopBuilding.All.Count == 0) return "Build a Sawmill — it turns Wood into Planks (your first processed good).";
            if (ResearchBuilding.All.Count == 0) return "Build an Idea Bench (Planks + Stone) and a Research Lodge beside it.";
            if (Research.TotalDelivered < 1) return "Get an Idea Tablet into the Research Lodge (belt it in, or place it adjacent).";
            if (col != null && col.Age < 1) return "Press T and research the Tribal Age to unlock the next chain.";
            return "Factory running! Scale production, automate with belts, and research the next Age.";
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
            if (wp != null) return wp.isBooster
                ? "<size=13><b>Booster Pump</b>  <color=#bbb>re-pressurises pipes</color></size>"
                : "<size=13><b>Water Pump</b>  <color=#bbb>water → pipes</color></size>";
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
                // Research: craft the current item → deliver to a Lodge → SPEND points in the tree (T).
                GUILayout.Label($"<size=12>🔬 <b>{Research.Points} research pts</b></size>", _small);
                var rTierB = Research.CurrentTier;
                if (rTierB != null)
                    GUILayout.Label($"<size=11><color=#bcd>Craft <b>{(rTierB.item != null ? rTierB.item.displayName : "?")}</b> → deliver to a Research Lodge <color=#999>({rTierB.pointsPerItem} pt)</color></color></size>", _small);
                var nextTechB = Research.NextAgeTech;
                if (nextTechB != null)
                {
                    int filled = Mathf.RoundToInt(Research.Fraction * 12f);
                    string bar = new string('█', Mathf.Clamp(filled, 0, 12)) + new string('░', Mathf.Clamp(12 - filled, 0, 12));
                    GUILayout.Label($"<size=12><color=#9cf>[{bar}]</color>  {Research.Points}/{nextTechB.cost} → {nextTechB.name}</size>", _small);
                }
                else GUILayout.Label("<size=12><color=#9f9>✔ All ages researched</color></size>", _small);
                if (GUILayout.Button("<size=12>🔬 Research Tree  <color=#bbb>(T)</color></size>", _btn)) TogglePanel(Panel.Research);
            }
            GUILayout.Space(4);
            GUILayout.Label("<b>Build</b>  <size=11>(click, then click the map · headers collapse)</size>", _small);

            int curAge = col != null ? col.Age : 0;
            _buildScroll = GUILayout.BeginScrollView(_buildScroll);

            // Draws one build entry: a pin star + the build button. Locked → greyed label.
            // Obsolete (unlocked ≥2 ages ago) is dimmed but still usable. Local fn so the
            // pinned/recent/category lists all render identically.
            void Entry(int i)
            {
                var def = builder.buildables[i];
                if (def == null) return;
                GUILayout.BeginHorizontal();
                bool pinned = _pinned.Contains(i);
                if (GUILayout.Button(pinned ? "<color=#ffd24d>★</color>" : "<color=#777>☆</color>", _btn, GUILayout.Width(22)))
                { if (pinned) _pinned.Remove(i); else _pinned.Add(i); }

                if (!builder.IsUnlocked(def))
                {
                    string reason;
                    if (!string.IsNullOrEmpty(def.requiredTech) && !Research.IsPurchased(def.requiredTech))
                    { var t = Research.Node(def.requiredTech); reason = "research " + (t != null ? t.name : def.requiredTech); }
                    else reason = def.unlockAge < Colony.AgeNames.Length ? Colony.AgeNames[def.unlockAge] : "later";
                    GUILayout.Label(new GUIContent($"<size=11><color=#888>🔒 {def.displayName} — {reason}</color></size>", Describe(def)), _small);
                }
                else
                {
                    bool obsolete = curAge - def.unlockAge >= 2; // de-emphasise old-age tech
                    string costCol = builder.CanAfford(def) ? "#9f9" : "#f99";
                    string key = i < 9 ? (i + 1).ToString() : i == 9 ? "0" : "·";
                    string nm = obsolete ? $"<color=#9a9a9a>{def.displayName}</color>" : def.displayName;
                    var label = new GUIContent($"<size=12>[{key}] {nm}  <color={costCol}>{CostText(def)}</color></size>", Describe(def));
                    if (GUILayout.Button(label, _btn)) { builder.BeginPlacement(i); RecordRecent(i); }
                }
                GUILayout.EndHorizontal();
            }

            // Pinned favourites — always one click away regardless of category/age.
            if (_pinned.Count > 0)
            {
                GUILayout.Label("<b><color=#ffd24d>★ Pinned</color></b>", _small);
                foreach (int i in _pinned)
                    if (i >= 0 && i < builder.buildables.Count && builder.IsUnlocked(builder.buildables[i])) Entry(i);
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
                    if (d != null && InGroup(cat.kinds, d.kind) && builder.IsUnlocked(d)) { hasAny = true; break; }
                }
                if (!hasAny) continue; // progressive disclosure: a category appears only once it has a buildable

                bool active = _activeCat == cat.label;
                if (GUILayout.Button($"<size=12><b><color=#d8c8a0>{(active ? "▼" : "▶")} {cat.label}</color></b></size>", _btn))
                    _activeCat = active ? "" : cat.label; // open this one (closes the rest); click again to close
                if (!active) continue;

                for (int i = 0; i < builder.buildables.Count; i++)
                {
                    var def = builder.buildables[i];
                    if (def == null || !InGroup(cat.kinds, def.kind)) continue;
                    if (!builder.IsUnlocked(def)) continue; // hide locked/age-gated until relevant (new unlocks are announced by the age-up toast + Research panel)
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
                    return $"Auto-gathers {Name(def.item)} from a nearby {Name(def.item)} patch at a fixed rate once built (no workers). Place it next to the resource; it re-targets a fresh patch when one runs dry.{age}";
                case BuildingKind.Workshop:
                    return $"Recipe: {CostList(def.inputs)} → {def.outputPerCycle} {Name(def.item)}. Runs automatically once its inputs are DELIVERED here (belt, or an adjacent storage/machine). Red dot = missing input.{age}";
                case BuildingKind.Storage:
                    return def.configurable
                        ? $"Warehouse — stores ONE resource you pick (set it in its panel); good for Planks, Charcoal, etc. Holds {def.capacity}.{age}"
                        : $"Stores {Name(def.item)} (up to {def.capacity}). Belt output here; if it fills up, production backs up.{age}";
                case BuildingKind.Housing:
                    return $"A structure (no effect in the factory build).{age}";
                case BuildingKind.Belt:
                    return $"Conveyor — carries items one cell in the way it faces ({(def.interval <= 0.6f ? "fast tier" : "slow tier")}). Click/drag to lay a line, R to rotate. Feeds storage or workshop inputs.{age}";
                case BuildingKind.Depot:
                    return $"Transport Station. Belt goods in, then SELECT it and add a route to another Station — a vehicle hauls goods across the map (load → travel → unload). Holds {def.capacity}.{age}";
                default:
                    return def.displayName;
            }
        }

        // ---- Selected building manage panel (bottom-right) ----
        private void DrawSelectedPanel()
        {
            var sel = builder != null ? builder.Selected : null;
            if (sel == null) { _selShown = false; return; }

            // Sit above the minimap when it's shown, so the two never overlap. Taller now so the
            // Station's route buttons don't crowd out the Demolish/Close row (kept bottom-anchored).
            float panelY = _showMinimap ? Screen.height - 406 : Screen.height - 222;
            var rect = new Rect(Screen.width - 290, panelY, 278, 210);
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
            var resB = sel.GetComponent<ResearchBuilding>();
            var th = sel.GetComponent<TransportHub>();
            string name = wb != null ? wb.def.displayName : pbSel != null ? pbSel.def.displayName
                : th != null && th.def != null ? th.def.displayName
                : sb != null ? sb.def.displayName : hb != null ? hb.def.displayName
                : dp != null ? dp.def.displayName : resB != null ? resB.def.displayName
                : cs != null ? cs.def.displayName : "Building";
            GUILayout.Label($"<b>{name}</b>", _s);

            // Plain-language status line for producers (the "understandable bottleneck" cue).
            if (wb != null || pbSel != null)
            {
                bool paused = wb != null ? wb.Paused : pbSel.Paused;
                bool isCollector = pbSel != null;
                Color sc = wb != null ? wb.StatusColor : pbSel.StatusColor;
                string st = paused ? "Paused"
                    : sc == Status.Working ? "Working"
                    : sc == Status.BackedUp ? (isCollector
                        ? "Output full — belt it to a Storage to use what it makes"
                        : "Output full — belt the output away (it's blocked)")
                    : sc == Status.Starved ? (isCollector
                        ? "Starved — its resource patch ran dry; move or expand"
                        : "Starved — an input isn't arriving")
                    : "Idle";
                GUILayout.Label($"<size=13><color=#{ColorUtility.ToHtmlStringRGB(sc)}>● {st}</color></size>", _small);
            }

            bool demo, close;
            if (wb != null)
            {
                GUILayout.Label($"<size=15>{RecipeText(wb)}</size>", _small);
                GUILayout.Label($"<size=11><color=#bbb>Belt inputs: {InStockText(wb)}   ·   Output: {wb.RatePerMin:0.#}/min</color></size>", _small);
                string demand = wb.InputDemandText();
                if (!string.IsNullOrEmpty(demand))
                    GUILayout.Label($"<size=11><color=#9cf>Needs {demand}  ·  1 belt lane = 60/min</color></size>", _small);
                string miss = wb.MissingText();
                if (!string.IsNullOrEmpty(miss))
                    GUILayout.Label($"<size=12><color=#f66>⚠ Waiting for: {miss}</color>  <size=10>(deliver via belt or adjacent storage)</size></size>", _small);
            }

            if (wb != null || pbSel != null || th != null)
            {
                // Buildings run automatically (no workers). Controls = pause + (transport) priority.
                if (th != null)
                {
                    if (th.mechanical) GUILayout.Label("<size=14>Automatic conveyor</size>", _small);
                    string pn = th.priorityItem != null ? th.priorityItem.displayName : "Any";
                    if (GUILayout.Button($"<size=12>Haul priority: {pn}</size>", _btn)) th.CyclePriority();
                }

                // Pause toggle — halt this machine to free a shared input for another (priorities).
                if (wb != null || pbSel != null)
                {
                    bool isPaused = wb != null ? wb.Paused : pbSel.Paused;
                    if (GUILayout.Button(isPaused ? "<size=12><color=#9f9>▶ Resume</color></size>" : "<size=12>⏸ Pause</size>", _btn))
                    { if (wb != null) wb.TogglePause(); if (pbSel != null) pbSel.TogglePause(); }
                }

                // What this collector/workshop currently holds in its buffer.
                if (pbSel != null && pbSel.produces != null)
                    GUILayout.Label($"<size=12>Holds: {pbSel.produces.displayName} {pbSel.Buffer.Count(pbSel.produces)}/{pbSel.Buffer.capacity}   ·   Rate: {pbSel.RatePerMin:0.#}/min</size>", _small);
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
                    else
                    {
                        GUILayout.Label("<size=11><color=#888>empty it to change type</color></size>", _small);
                        if (GUILayout.Button("<size=12>Empty (to hands)</size>", _btn)) EmptyStorage(sb);
                    }
                }
            }
            else if (dp != null)
            {
                string acc = dp.item != null ? dp.item.displayName : "(not set)";
                // Role badge — Supplying → / ← Receiving / Relay / Idle — colour-coded so you
                // instantly know whether this station gives or takes.
                string roleCol = dp.HasOutgoing && dp.HasIncoming ? "#9cf" : dp.HasOutgoing ? "#9f9" : dp.HasIncoming ? "#fc8" : "#999";
                GUILayout.Label($"<size=15>Station  <color={roleCol}><b>{dp.Role}</b></color></size>", _small);

                // Cargo + fill state (empty / filling / FULL) → supplying vs blocked at a glance.
                float fill = dp.FillFraction;
                string fillWord = dp.store.Total() == 0 ? "<color=#888>empty</color>" : fill >= 0.99f ? "<color=#fd4>FULL</color>" : "filling";
                GUILayout.Label($"<size=12>Holds {acc}: {dp.store.Total()}/{dp.def.capacity}  ({fillWord})</size>", _small);

                // Lightweight, non-flashing validation: only shows when a route can't move goods.
                string issue = dp.Issue();
                if (!string.IsNullOrEmpty(issue))
                    GUILayout.Label($"<size=11><color=#f99>⚠ {issue}</color></size>", _small);

                if (dp.store.Total() == 0)
                {
                    if (GUILayout.Button($"<size=12>Handle: {acc} (change)</size>", _btn)) dp.CycleItem();
                }
                else GUILayout.Label("<size=11><color=#888>empty it to change type</color></size>", _small);

                // Routes touching this station, each with explicit FROM → TO direction (OUT/IN +
                // compass arrow + distance + the resource). Capped so the panel never overflows.
                int rcount = 0, shownR = 0;
                foreach (var rv in RouteVehicle.All)
                {
                    if (rv == null || (rv.a != dp && rv.b != dp)) continue;
                    rcount++;
                    if (shownR >= 2 || rv.a == null || rv.b == null) continue;
                    shownR++;
                    bool outgoing = rv.a == dp;
                    var other = outgoing ? rv.b : rv.a;
                    Vector2 d = (Vector2)(other.transform.position - dp.transform.position);
                    string ritem = rv.a.item != null ? rv.a.item.displayName : "—";
                    string dirTag = outgoing ? "<color=#9f9>→ OUT</color>" : "<color=#fc8>← IN</color>";
                    GUILayout.Label($"<size=11>{dirTag} {ArrowFor(d)} {Mathf.RoundToInt(d.magnitude)}m  <color=#bbb>{ritem}</color></size>", _small);
                }
                if (rcount > shownR) GUILayout.Label($"<size=10><color=#888>+{rcount - shownR} more route(s)</color></size>", _small);
                if (rcount == 0) GUILayout.Label("<size=11><color=#888>No route yet. Build a 2nd Station, belt goods in, then '+ Add route' → click the other Station. A vehicle auto-shuttles — no track to lay.</color></size>", _small);

                if (builder.LinkFrom == dp)
                    GUILayout.Label("<size=11><color=#ffd24d>▶ Now click the DESTINATION Station… (Esc cancels)</color></size>", _small);
                else
                {
                    var tier = builder.BestRouteTier();
                    if (tier != null)
                    {
                        bool afford = builder.CanAfford(tier);
                        string cc = afford ? "#9f9" : "#f99";
                        if (GUILayout.Button($"<size=12>+ Add route  <color=#bbb>{tier.displayName}</color> <color={cc}>({CostList(tier.cost)})</color></size>", _btn))
                            builder.BeginStationLink(dp);
                    }
                    else GUILayout.Label("<size=11><color=#888>No transport unlocked yet</color></size>", _small);
                    if (rcount > 0 && GUILayout.Button("<size=12>✕ Remove a route</size>", _btn))
                        for (int k = RouteVehicle.All.Count - 1; k >= 0; k--)
                        {
                            var rv = RouteVehicle.All[k];
                            if (rv != null && (rv.a == dp || rv.b == dp)) { Destroy(rv.gameObject); break; }
                        }
                }
            }
            else if (resB != null)
            {
                var rt = Research.CurrentTier;
                if (rt != null)
                {
                    string itemN = rt.item != null ? rt.item.displayName : "?";
                    GUILayout.Label($"<size=14>Research Lodge</size>", _small);
                    GUILayout.Label($"<size=12>Wants: <b>{itemN}</b>  <color=#bbb>({rt.pointsPerItem} pt each)</color></size>", _small);
                    GUILayout.Label($"<size=12>Holding {resB.InBuffer.Count(rt.item)}  ·  pool {Research.Points} pts  ·  {resB.PointsPerMin:0.#} pts/min</size>", _small);
                    GUILayout.Label("<size=11><color=#bbb>Belt the research item here (or place it beside a stock). Spend points in the tree (T).</color></size>", _small);
                }
                else GUILayout.Label("<size=13><color=#9f9>✔ All research complete</color></size>", _small);
            }
            else if (hb != null)
            {
                GUILayout.Label("<size=14>Structure</size>", _small);
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

            if (demo) builder.DemolishSelected();
            if (close) builder.Deselect();
        }

        // Empty a configurable warehouse so its type can be changed. Contents move into the
        // player's carried hands (no loss — carried + storages is the "stored" pool), then the
        // type is cleared so the next belted-in item (or the change button) re-sets it.
        private void EmptyStorage(StorageBuilding sb)
        {
            if (sb == null || gatherer == null) return;
            foreach (var kv in new List<KeyValuePair<ItemDefinition, int>>(sb.Store.Items))
            {
                int moved = gatherer.Inventory.Add(kv.Key, kv.Value); // carried is unlimited
                if (moved > 0) sb.Store.RemoveUpTo(kv.Key, moved);     // remove only what transferred
            }
            if (sb.configurable && sb.Store.Total() == 0) sb.accepts = null; // ready to adopt a new type
        }

        // ---- Resource finder: arrows to the nearest raw material you haven't tapped yet.
        //      Each line disappears once you build that collector. ----
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
            var keys = new List<(ItemDefinition item, string label)>
            { (woodItem, "Wood"), (stoneItem, "Stone") };
            // From the Tribal age, also point toward the expansion materials (clay/ore) so the
            // player SEES the next supply line before feeling blocked. Each drops once its
            // collector is built (HasCollector). Subtle — reuses the existing arrows.
            int age = Colony.Instance != null ? Colony.Instance.Age : 0;
            if (age >= 1)
            {
                keys.Add((clayItem, "Clay"));
                keys.Add((oreItem, "Ore"));
            }

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
            // Constrained-width, word-wrapped, multi-line so long tips aren't cut off. Each toast's
            // height grows with its wrapped content so nothing clips and they don't overlap.
            float tw = Mathf.Min(900f, Screen.width - 40f);
            float x = (Screen.width - tw) / 2f;
            float y = 124f;
            foreach (var t in Toast.Items)
            {
                float h = Mathf.Max(30f, _toast.CalcHeight(new GUIContent(t.msg), tw) + 4f);
                GUI.Label(new Rect(x, y, tw, h), t.msg, _toast);
                y += h + 6f;
            }
        }

        // Hotkey legend + run state — lowest-priority info, pinned to the bottom-centre (out of
        // the top resource strip it used to overlap). Hidden while placing so it never fights the
        // placement instruction bar.
        private void DrawFooter()
        {
            if (builder != null && builder.PendingIndex >= 0) return;
            float w = Mathf.Min(760f, Screen.width - 24f);
            GUILayout.BeginArea(new Rect(Screen.width / 2f - w / 2f, Screen.height - 40f, w, 38f));
            GUILayout.Label($"<size=13><color=#bbb>B build · <color=#9cf>T research</color> · G guide · H help · M overview · N map {(_showMinimap ? "on" : "off")} · Space pause</color></size>", _small);
            string sandbox = Economy.FreeBuild ? "<color=#9f9>SANDBOX</color> · " : "";
            string mode = Economy.LocalProduction ? "<color=#fc8>Local logistics</color>" : "<color=#8cf>Global pool</color>";
            GUILayout.Label($"<size=11><color=#999>{sandbox}Speed x{_speed:0} · {mode} (F7) · F1–F5 sandbox</color></size>", _small);
            GUILayout.EndArea();
        }

        // ---- Victory banner (shown once the Monument is completed) ----
        // ---- Research tree (T): spend research points on age advances + building unlocks ----
        private void DrawResearchPanel()
        {
            // A dedicated MODE, not a sidebar: a large centred panel over the dimmed world.
            float w = Mathf.Min(560f, Screen.width - 40f);
            float h = Mathf.Min(Screen.height - 120f, 580f);
            var r = new Rect(Screen.width / 2f - w / 2f, 70f, w, h);
            _researchRect = r;
            PanelBg(r);
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 14, r.width - 32, r.height - 28));

            GUILayout.Label($"<size=20><b>🔬 Research</b></size>   <color=#9cf><b>{Research.Points} pts</b></color>", _s);
            GUILayout.Label("<size=12><color=#bbb>Craft research items → deliver to a Research Lodge to earn points → spend them here.  (T or Close to exit)</color></size>", _small);

            // CURRENT GOAL — the one thing to aim at, called out at the top.
            var goalTech = Research.NextAgeTech; var goalTier = Research.CurrentTier;
            GUILayout.Space(4);
            if (goalTech != null)
            {
                int filled = Mathf.RoundToInt(Research.Fraction * 16f);
                string bar = new string('█', Mathf.Clamp(filled, 0, 16)) + new string('░', Mathf.Clamp(16 - filled, 0, 16));
                GUILayout.Label($"<size=13><color=#ffd24d>⭐ Current goal: <b>{goalTech.name}</b></color>  <color=#9cf>[{bar}]</color> {Research.Points}/{goalTech.cost}</size>", _small);
                if (goalTier != null && goalTier.item != null)
                    GUILayout.Label($"<size=12><color=#bcd>→ craft <b>{goalTier.item.displayName}</b> ({goalTier.pointsPerItem} pt each) and deliver it to a Research Lodge.</color></size>", _small);
            }
            else GUILayout.Label("<size=13><color=#9f9>⭐ All ages researched — spend leftover points on upgrades below.</color></size>", _small);
            GUILayout.Space(6);

            // Render one tree node (used by both sections below).
            void RenderNode(Research.Tech n)
            {
                if (n == null) return;
                GUILayout.Space(3);
                if (n.purchased)
                    GUILayout.Label($"<size=13><color=#9f9>✔ {n.name}</color></size>", _small);
                else if (!Research.PrereqMet(n))
                {
                    var pre = Research.Node(n.prereq);
                    GUILayout.Label($"<size=13><color=#888>🔒 {n.name} — {n.cost} pts <i>(needs {(pre != null ? pre.name : n.prereq)})</i></color></size>", _small);
                }
                else if (Research.CanBuy(n))
                {
                    if (GUILayout.Button($"<size=13><b>{n.name}</b> — {n.cost} pts  <color=#9f9>✦ Research</color></size>", _btn))
                        Research.Buy(n);
                }
                else // prereq met, not enough points yet
                    GUILayout.Label($"<size=13>{n.name} — <color=#f99>{n.cost} pts</color> <color=#888>(need {n.cost - Research.Points} more)</color></size>", _small);

                if (!string.IsNullOrEmpty(n.desc))
                    GUILayout.Label($"<size=11><color=#bbb>{n.desc}</color></size>", _small);
            }

            _researchScroll = GUILayout.BeginScrollView(_researchScroll);
            if (Research.Tree != null)
            {
                // Section 1 — AGES: the progression spine (locked ones read as future ages).
                GUILayout.Label("<size=13><b><color=#cda>── Ages (progression) ──</color></b></size>", _small);
                foreach (var n in Research.Tree) if (n != null && n.advanceToAge >= 0) RenderNode(n);

                // Section 2 — UPGRADES: optional building unlocks you choose to spend on.
                GUILayout.Space(8);
                GUILayout.Label("<size=13><b><color=#cda>── Upgrades (optional unlocks) ──</color></b></size>", _small);
                foreach (var n in Research.Tree) if (n != null && n.advanceToAge < 0) RenderNode(n);
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("<size=12>Close (T)</size>", _btn)) _showResearch = false;
            GUILayout.EndArea();
        }

        private void DrawWin()
        {
            var r = new Rect(Screen.width / 2f - 240, 150f, 480, 140);
            PanelBg(r);
            int peak = Colony.Instance != null ? Colony.Instance.PeakProsperity : 0;
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 14, r.width - 32, r.height - 28));
            GUILayout.Label("<color=#ffd24d><b>🏆 YOU WIN!</b></color>", _big);
            GUILayout.Label($"<size=16>You built a self-running factory.\nPeak Industry: <b>{peak}</b>   ·   <color=#bbb>keep playing if you like</color></size>", _toast);
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
                "Grow from hand-gathering to a self-running FACTORY. Reach the Industrial Age and build the Monument (10 Blocks) to WIN.");
            Section("<color=#fc8>Logistics matter (the core)</color>",
                "The easiest way to connect things is ADJACENCY: a machine pulls inputs from buildings RIGHT NEXT TO it, so clustering a chain side-by-side feeds it automatically — no belts. Use BELTS to connect things that are far apart (or to stock a Storage). A machine never pulls from across the map, so lay your base out so every machine is fed; a shortage cascades downstream.");
            Section("<color=#9cf>Fully automatic buildings</color>",
                "Every building is a machine: it runs by itself at a FIXED rate the moment it's built and supplied — no workers, no population, nothing to staff. Construction is INSTANT (the cost is paid when you place it). Collectors auto-gather and re-target a fresh patch when one runs dry; processors run whenever their inputs arrive. You build and connect the system; it runs itself.");
            Section("<color=#ffcf6b>Industry score & Rank</color>",
                "A climbing score from your AUTOMATION (collectors, workshops, belts, routes) and tech age. Your settlement ranks up: Camp → Hamlet → Village → Town → City → Metropolis.");
            Section("<color=#cda>Research drives progress (press T)</color>",
                "You advance by RESEARCH. Craft a RESEARCH ITEM in a production chain (e.g. Planks + Stone → Idea Tablet at an Idea Bench), deliver it to a RESEARCH LODGE to earn points, then open the Research Tree (T) and SPEND points to advance the Age or unlock buildings (Splitters, Conveyors). Gathering earns no research — you must build and scale a factory.");
            Section("<color=#cda>Ages</color>",
                "Stone → Tribal → Bronze → Iron → Industrial — each unlocked from the Research Tree, each adding deeper production chains (charcoal → bricks/pottery → metal/tools → power). Reach Industrial and build the Monument to win.");
            Section("<color=#6c6>Bottlenecks</color>",
                "Status dots: green working, yellow output-full (belt it away), red missing-input, grey paused. Problems show on the minimap. Click a building to see what it's waiting for, its rate, and to Pause it (free a scarce shared input for another).");
            Section("<color=#9f9>Expansion</color>",
                "Patches deplete as you harvest; collectors auto-chase fresh ones nearby and go idle when a cluster runs dry — your cue to push outward. Ore is finite and far, so exploration + transport routes (cart → train) matter.");

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
                "• Left-click a tree/rock to gather raw materials by hand.\n" +
                "• Click a building in the Build menu (left), then click the map to place it.\n" +
                "• Construction is INSTANT — the cost is paid when you place it, and it runs itself.\n" +
                "• Buildings are fully automatic: no workers, no population, nothing to staff.\n" +
                "• <b>Logistics matter:</b> a workshop only runs on inputs that ARRIVE — belt-fed or\n" +
                "  in an ADJACENT storage/machine. Lay it out so each machine is fed.\n" +
                "• Lay Belts/Splitters — or Stations + a transport route — to move goods around.\n" +
                "• Click a building to see its status, pause it, or demolish it.\n" +
                "• <b>X</b> or <b>Delete</b> removes the building under the cursor (fast un-do). " +
                "Build menu: ★ pins a building.\n" +
                "• Hold-drag to place a row of buildings; C copies the selected building's type.\n" +
                "• <b>Goal:</b> reach the Industrial Age via RESEARCH, build the <b>Monument</b>, and make " +
                "10 Monument Blocks to <color=#ffd24d>WIN</color>.\n" +
                "• Status dots: <color=#6c6>green</color>=working, <color=#fd4>yellow</color>=output full, " +
                "<color=#f66>red</color>=no input, <color=#999>grey</color>=paused.\n" +
                "\n<b>Sandbox:</b> F1 +resources · F3 advance age · F4 free build · " +
                "F5 game speed · F8 reveal map.\n" +
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
