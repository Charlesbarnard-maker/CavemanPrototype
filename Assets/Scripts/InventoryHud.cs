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
        public ItemDefinition woodItem, stoneItem;
        public ItemDefinition clayItem, oreItem; // expansion-target hints (shown from Tribal)
        public ItemDefinition monumentItem; // endgame win-goal tracker (10 blocks = win)
        // Responsive GUI: the HUD is laid out in a LOGICAL canvas (_vw × _vh) scaled to the real screen, so it
        // fits — and panels don't overlap — on smaller monitors. _uiScale = 1 at/above the reference (no change).
        private float _uiScale = 1f, _vw, _vh;
        public List<ItemDefinition> debugItems; // all resources, for the sandbox resource dump
        private float _speed = 1f;

        /// <summary>True when the cursor is over an interactive HUD panel.</summary>
        public static bool PointerOverUI { get; private set; }

        /// <summary>Singleton so other systems (Objectives) can hand the HUD a centre-screen popup.</summary>
        public static InventoryHud Instance { get; private set; }
        void Awake() => Instance = this;

        private GUIStyle _s, _small, _big, _btn;
        private GUIStyle _techNode; // research-tree node style (centred rich-text button)
        private bool _paused;
        private bool _showHelp;
        private Vector2 _buildScroll;
        private Vector2 _flyoutScroll;     // scroll for the category flyout panel (right of the build menu)
        private Rect _buildRect, _selRect, _flyoutRect;
        private bool _buildShown, _selShown, _flyoutShown;
        private Vector2 _selScroll; // selected-panel scroll so long content (warnings + buttons) never clips
        private bool _showBuild;
        // (Removed the "recent" build-menu shortcuts — the menu just shows Pinned + Categories now.)
        private readonly HashSet<int> _pinned = new(); // pinned (favourite) buildable indices
        private string _activeCat = "Gathering"; // build-menu accordion: only this group is open
        private bool _showMinimap = true;
        private bool _showMap;            // full-screen pan/zoom world map (M)
        private float _mapZoom = 1f;      // 1 = whole world fits the map area; higher = zoomed in
        private Vector2 _mapPan;          // map content offset (pixels) within the map area
        private bool _mapDragging;
        private Vector2 _mapDragLast;
        private bool _showGuide;
        private Vector2 _guideScroll;
        private Rect _guideRect;
        private bool _showResearch;       // the research tree panel (T)
        private bool _showLines;          // the global transport-line overview (L)
        private Vector2 _linesScroll;
        private Rect _linesRect;
        private Vector2 _researchScroll;
        private Rect _researchRect;
        private readonly HashSet<string> _affordToasted = new(); // "research available" hint fires once per node
        // --- Objectives overhaul: a queued centre-screen reveal popup + the quest journal panel (J) ---
        private bool _showObjectives;                                          // the quest journal panel (J)
        private Vector2 _objPanelScroll;
        private Rect _objPanelRect;
        private readonly List<Quest> _revealQueue = new(); // next-step objectives, shown centre-screen one at a time
        private Rect _objRevealRect;
        private bool _revealConsume;                                           // set by the popup's OK; applied in Update (no mid-frame queue mutation)
        private bool _upgradeHintShown; // one-time hint when an upgrade first becomes affordable
        private Texture2D _panelTex; // dark panel background for readability
        private Texture2D _accentTex, _btnTex, _btnHoverTex; // top accent bar + flat button skins
        private static Texture2D Solid(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }
        private Dictionary<ItemDefinition, int> _totals;
        private int _starvedCount, _waitingCount, _backedCount; // bottleneck counts, cached once/frame in Update
        private bool _hasMonument;
        private readonly Dictionary<string, int> _trendSnap = new(); // chip values ~3s ago (for ▲/▼)
        private float _lastSnap = -999f;
        private Rect _topRect, _miniRect, _objRect, _powerRect;
        private readonly List<(string label, int value, string detail, Color color)> _chips = new();
        private int _topBarFrame = -1;              // frame the chip set + widths were last built (cache key)
        private readonly List<float> _chipWidths = new();
        private float _barH = 30f;
        private GUIStyle _toast;
        private GUIStyle _gatherStyle; // bold "+N" floating hand-gather popup
        private GUIStyle _legend;      // right-aligned minimap legend (so it never clips at the screen edge)
        private int _lastAge = -1;
        // "What just changed" card shown on an age advance. It now PERSISTS until the player
        // dismisses it (click-to-close), so the new-buildings tips are never missed.
        private float _ageCardT;             // >0 = the card is shown (no longer a countdown)
        private string _ageCardTitle, _ageCardBody;
        private Rect _ageCardRect;           // cached each draw for the click-to-dismiss hit-test

        // Build-menu categories. A building lands here by its kind (or a menuCategory override). ENERGY
        // (generators/poles/batteries) is now its own tab — separate from Production; the water Bridge sits
        // under INFRASTRUCTURE (it's a crossing, not a belt); rail/stations/signals + elevated track live
        // under Trains; storage is its own tab.
        private static readonly (string label, BuildingKind[] kinds)[] Cats =
        {
            ("Gathering",      new[] { BuildingKind.Collector }), // resource collectors (Wood/Stone/Clay/Ore/Oil…)
            ("Fabrication",    new[] { BuildingKind.Workshop }),  // workshops that process raws into goods
            ("Research",       new[] { BuildingKind.Research }), // Research Lodge + the research-item crafters (tagged via menuCategory)
            ("Energy",         new[] { BuildingKind.Power, BuildingKind.Pole, BuildingKind.Battery }),
            ("Belts",          new[] { BuildingKind.Belt }),
            ("Infrastructure", new[] { BuildingKind.Bridge }),
            ("Liquids",        new[] { BuildingKind.Pipe, BuildingKind.Pump }),
            ("Trains",         new[] { BuildingKind.Depot, BuildingKind.Rail, BuildingKind.Signal }),
            ("Boats",          new BuildingKind[0]),  // tag-only (Harbour sets menuCategory="Boats")
            ("Planes",         new BuildingKind[0]),  // reserved — appears once it has a buildable
            // (Mounts merged into Infrastructure — the Garage sets menuCategory="Infrastructure" to save a tab.)
            ("Storage",        new[] { BuildingKind.Storage }),
        };
        private static bool InGroup(BuildingKind[] kinds, BuildingKind k) => System.Array.IndexOf(kinds, k) >= 0;
        // A building belongs to a category by its menuCategory tag when set (e.g. a Harbour → "Boats"), else by kind.
        private static bool Belongs(BuildingDefinition d, (string label, BuildingKind[] kinds) cat)
            => d != null && (!string.IsNullOrEmpty(d.menuCategory) ? d.menuCategory == cat.label : InGroup(cat.kinds, d.kind));

        // --- Context-panel layering (Priority 1): only ONE large panel is open at a time.
        // Opening any of Build / Research / Guide / Help closes the others, so the player
        // focuses on a single system. The minimap is a world overlay, not a context panel,
        // so it's exempt. ---
        private enum Panel { Build, Research, Guide, Help, Map, Lines, Objectives, Stats, Alerts }
        private bool _showStats;   // per-item production/consumption rates (P)
        private bool _showAlerts;  // every building with a sustained problem (K)
        private Vector2 _statsScroll, _alertsScroll;
        public void CloseAllPanels() { _showBuild = _showResearch = _showGuide = _showHelp = _showMap = _showLines = _showObjectives = _showStats = _showAlerts = false; }
        /// <summary>True while any full-screen HUD panel is open — GameMenu checks this to route the Esc key.</summary>
        public bool AnyPanelOpen => ModalOpen;
        private void TogglePanel(Panel p)
        {
            bool wasOpen = p == Panel.Build ? _showBuild
                         : p == Panel.Research ? _showResearch
                         : p == Panel.Guide ? _showGuide
                         : p == Panel.Help ? _showHelp
                         : p == Panel.Lines ? _showLines
                         : p == Panel.Objectives ? _showObjectives
                         : _showMap;
            CloseAllPanels();
            if (wasOpen) return; // it was open → we just closed it
            switch (p)
            {
                case Panel.Build: _showBuild = true; break;
                case Panel.Research: _showResearch = true; break;
                case Panel.Guide: _showGuide = true; break;
                case Panel.Help: _showHelp = true; break;
                case Panel.Map: _showMap = true; _mapZoom = 1f; _mapPan = Vector2.zero; break; // open fitting the whole world
                case Panel.Lines: _showLines = true; break;
                case Panel.Objectives: _showObjectives = true; break;
                case Panel.Stats: _showStats = true; break;
                case Panel.Alerts: _showAlerts = true; break;
            }
        }
        // A full-screen "mode" panel is up — used to dim the world and hide competing widgets.
        private bool ModalOpen => _showResearch || _showGuide || _showHelp || _showMap || _showLines || _showObjectives || _showStats || _showAlerts;

        void Update()
        {
            // Advance the centre-screen objective-reveal popup queue when the player clicked "OK" last frame.
            // Done here (once/frame, before any early-out) so the queue never mutates between OnGUI's
            // Layout + Repaint passes, and so it works even with no keyboard device.
            if (_revealConsume) { if (_revealQueue.Count > 0) _revealQueue.RemoveAt(0); _revealConsume = false; }

            var kb = Keyboard.current;
            if (kb == null) return;
            if (GameMenu.MenuOpen) return; // a menu screen is up — it owns input while the game is paused
            if (kb.spaceKey.wasPressedThisFrame)
            {
                _paused = !_paused;
                Time.timeScale = _paused ? 0f : _speed;
            }
            if (kb.hKey.wasPressedThisFrame) TogglePanel(Panel.Help);
            if (kb.bKey.wasPressedThisFrame) TogglePanel(Panel.Build);
            if (kb.nKey.wasPressedThisFrame) _showMinimap = !_showMinimap;
            if (kb.mKey.wasPressedThisFrame) TogglePanel(Panel.Map);   // full-screen pan/zoom world map
            if (kb.gKey.wasPressedThisFrame) TogglePanel(Panel.Guide);
            if (kb.tKey.wasPressedThisFrame) TogglePanel(Panel.Research);
            if (kb.lKey.wasPressedThisFrame) TogglePanel(Panel.Lines); // global transport-line overview
            if (kb.jKey.wasPressedThisFrame) TogglePanel(Panel.Objectives); // the quest journal (multiple goals at once)
            if (kb.pKey.wasPressedThisFrame) TogglePanel(Panel.Stats);      // per-item production/consumption rates
            if (kb.kKey.wasPressedThisFrame) TogglePanel(Panel.Alerts);     // every problem building, sorted by distance
            // Esc-to-close panels is now handled centrally by GameMenu (so Esc can also open the pause menu).

            // QoL: one-time "research available" toast when a tree node first becomes affordable.
            if (Research.Tree != null)
                foreach (var n in Research.Tree)
                    if (Research.CanBuy(n) && _affordToasted.Add(n.id))
                        Toast.Show($"<color=#9cf>🔬 Research available: {n.name} ({n.cost} pts) — press T</color>");

            // --- Sandbox / debug hotkeys ---
            if (kb.f1Key.wasPressedThisFrame && debugItems != null)
                foreach (var it in debugItems) if (it != null) gatherer.Inventory.Add(it, 500);
            if (kb.f3Key.wasPressedThisFrame && Colony.Instance != null)
            {
                Colony.Instance.DebugAdvanceAge();
                Research.DebugUnlockTo(Colony.Instance.Age); // also unlock that age's tech so it's testable
            }
            if (kb.f4Key.wasPressedThisFrame) Economy.FreeBuild = !Economy.FreeBuild;
            if (kb.f7Key.wasPressedThisFrame) Economy.LocalProduction = !Economy.LocalProduction;
            if (kb.f8Key.wasPressedThisFrame && FogOfWar.Instance != null) FogOfWar.Instance.RevealAll();
            if (kb.f9Key.wasPressedThisFrame) Economy.StoredOnly = !Economy.StoredOnly;
            if (kb.f10Key.wasPressedThisFrame) SaveSystem.SelfTest(); // dev: save→load→compare the live game
            if (kb.f5Key.wasPressedThisFrame)
            {
                _speed = _speed >= 4f ? 1f : _speed * 2f;
                if (!_paused) Time.timeScale = _speed;
            }

            // Cache the resource totals once per frame (OnGUI runs twice/frame).
            if (gatherer != null) _totals = Economy.Totals(gatherer.Inventory);
            ProductionStats.Tick(); // roll the consumption-rate window for the P stats panel

            // Cache the bottleneck/monument scan once per frame too (DrawStatus runs every OnGUI pass).
            _starvedCount = 0; _waitingCount = 0; _backedCount = 0; _hasMonument = false;
            foreach (var pb in ProductionBuilding.All)
            { if (pb == null) continue; var sc = pb.StatusColor; if (sc == Status.Starved) _starvedCount++; else if (sc == Status.Waiting) _waitingCount++; else if (sc == Status.BackedUp) _backedCount++; }
            foreach (var wkb in WorkshopBuilding.All)
            {
                if (wkb == null) continue;
                var sc = wkb.StatusColor; if (sc == Status.Starved) _starvedCount++; else if (sc == Status.Waiting) _waitingCount++; else if (sc == Status.BackedUp) _backedCount++;
                if (monumentItem != null && wkb.output == monumentItem) _hasMonument = true;
            }

            // Toasts fade out.
            for (int i = 0; i < Toast.Items.Count; i++) Toast.Items[i].t -= Time.unscaledDeltaTime;
            Toast.Items.RemoveAll(t => t.t <= 0f);
            // "+N" hand-gather popups rise + fade (unscaled, so they read even while paused).
            for (int i = 0; i < GatherPopup.Items.Count; i++) GatherPopup.Items[i].t -= Time.unscaledDeltaTime;
            GatherPopup.Items.RemoveAll(g => g.t <= 0f);
            // The age-advance card no longer auto-dismisses — it stays up until the player clicks
            // "Got it" / the card (see DrawAgeCard), so there's always time to read the new tips.

            // (Removed: the one-time "workshop starved" and "collector backed up" tip toasts — they popped up
            // and were more annoying than useful. The status dots + the bottleneck counter still convey the state.)

            // One-time hint the first time ANY building can be upgraded + afforded — teaches the upgrade
            // mechanic at the moment it becomes actionable (and points at the new green ⬆ badge).
            if (!_upgradeHintShown)
            {
                bool any = false;
                foreach (var w in WorkshopBuilding.All) if (w != null && w.CanUpgradeNow) { any = true; break; }
                if (!any) foreach (var p in ProductionBuilding.All) if (p != null && p.CanUpgradeNow) { any = true; break; }
                if (any)
                {
                    _upgradeHintShown = true;
                    Toast.Show("<color=#9f9>⬆ You can UPGRADE buildings!</color> Any building wearing the green <b>⬆</b> badge can be upgraded right now — <b>select it</b> and click the Upgrade button to make it faster (and tougher-looking). Each has a few tiers across the ages.");
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
            AudioManager.Chime(); // a positive flourish when a new age is reached
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
            float w = Mathf.Min(600f, _vw - 40f);
            float inner = w - 36f;
            // Auto-size height to the wrapped content (so a long "New buildings:" list never clips)
            // plus a row for the dismiss button.
            string title = $"<size=20>{_ageCardTitle}</size>", body = $"<size=14>{_ageCardBody}</size>";
            float h = _s.CalcHeight(new GUIContent(title), inner)
                    + _small.CalcHeight(new GUIContent(body), inner) + 34f + 30f;
            var r = new Rect(_vw / 2f - w / 2f, 188f, w, h); // below the toast stack (~124+) so they don't overlap
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

        // A warm screen VIGNETTE (amber centre glow → cool dark corners) drawn UNDER the HUD each frame, to tie
        // the world into a cozy, sun-warmed frame. Cached 256² texture stretched to the screen (zoom-independent).
        private Texture2D _vignetteTex;
        private Texture2D Vignette()
        {
            if (_vignetteTex != null) return _vignetteTex;
            const int s = 256;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[s * s];
            float c = (s - 1) * 0.5f, maxR = Mathf.Sqrt(2f) * c;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / maxR;
                    px[y * s + x] = d < 0.5f
                        ? new Color(1f, 0.72f, 0.34f, 0.09f * (1f - d / 0.5f))                                                   // warm centre glow
                        : new Color(0.05f, 0.06f, 0.11f, 0.42f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.55f) / 0.45f)));  // cool dark corners
                }
            t.SetPixels(px); t.Apply();
            _vignetteTex = t;
            return _vignetteTex;
        }

        void OnGUI()
        {
            if (gatherer == null) return;
            // Warm vignette over the world, drawn first (in true screen space) so the HUD panels stay on top of it.
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Vignette());
                // DAY/NIGHT ambient: the world cools and darkens as the sun sets (Colony.Daylight already
                // drives Solar panels — now it's visible). Subtle by design (max ~22% moonlit blue at deep
                // night) so the factory stays readable; dawn/dusk get a brief warm blush at the transition.
                float dl = Colony.Daylight;
                float night = 1f - Mathf.SmoothStep(0f, 1f, dl);            // 0 midday → 1 deep night
                if (night > 0.01f)
                {
                    GUI.color = new Color(0.07f, 0.10f, 0.22f, 0.22f * night); // cool moonlight wash
                    GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                }
                float dusk = 4f * dl * (1f - dl);                            // peaks at the day/night crossover
                if (dusk > 0.5f)
                {
                    GUI.color = new Color(0.95f, 0.55f, 0.25f, 0.06f * dusk); // golden-hour blush
                    GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                }
                GUI.color = Color.white;
            }
            // Scale the whole HUD to a 1080-tall reference canvas so it fits + stays un-overlapped on smaller
            // monitors. At/above 1080 the matrix is identity → no change on normal/large screens. Mouse hit-tests
            // (Event.current.mousePosition + logical rects) are transformed by the matrix, so clicks stay correct.
            _uiScale = Mathf.Clamp(Screen.height / 1080f, 0.5f, 1.0f);
            _vw = Screen.width / _uiScale; _vh = Screen.height / _uiScale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_uiScale, _uiScale, 1f));
            _s ??= new GUIStyle(GUI.skin.label) { fontSize = 20, richText = true, wordWrap = true };
            _small ??= new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, wordWrap = true };
            _big ??= new GUIStyle(GUI.skin.label) { fontSize = 40, richText = true, alignment = TextAnchor.MiddleCenter };
            _btn ??= new GUIStyle(GUI.skin.button) { richText = true, alignment = TextAnchor.MiddleLeft, fontSize = 13, wordWrap = true };
            _toast ??= new GUIStyle(GUI.skin.label) { richText = true, fontSize = 20, alignment = TextAnchor.UpperCenter, wordWrap = true };
            if (_panelTex == null)
            {
                _panelTex = Solid(new Color(0.09f, 0.11f, 0.14f, 0.95f));        // deep blue-grey panel
                _accentTex = Solid(new Color(1f, 0.82f, 0.30f, 0.9f));           // amber top accent
                _btnTex = Solid(new Color(0.16f, 0.18f, 0.23f, 0.96f));          // flat button
                _btnHoverTex = Solid(new Color(0.25f, 0.30f, 0.38f, 0.98f));     // lighter on hover
                // Flat, modern buttons (replaces the default grey 3D skin) — reads cleaner across the UI.
                _btn.normal.background = _btnTex; _btn.normal.textColor = new Color(0.92f, 0.93f, 0.96f);
                _btn.hover.background = _btnHoverTex; _btn.hover.textColor = Color.white;
                _btn.active.background = _btnHoverTex; _btn.active.textColor = Color.white;
                _btn.border = new RectOffset(0, 0, 0, 0);
                _btn.padding = new RectOffset(9, 9, 6, 6);
                _btn.margin = new RectOffset(2, 2, 3, 3);
            }

            bool modal = ModalOpen;

            // --- Layer 1: world-level always-on HUD (top bar, vitals, alerts, objectives). ---
            DrawTopBar();
            DrawStatus();
            DrawObjectives();

            // --- Layer 2: contextual / secondary widgets — hidden while a full "mode" panel
            //     (Research / Guide / Help) is open, so the player focuses on one system. ---
            if (!modal)
            {
                if (builder != null) DrawBuildMenu();
                DrawMinimap();
                DrawSelectedPanel();
                if (!_showBuild) DrawPowerGridOverview(); // grid readout (bottom-left); yields to the build menu
                DrawFooter();
            }

            DrawHoverInfo();
            DrawToasts();
            DrawDragCost();
            DrawGatherPopups();
            if (!modal) DrawAgeCard();         // temporary "what changed" moment; yields if a mode panel is open
            if (!modal) DrawObjectiveReveal();  // centre-screen "new objectives" popup → OK → docks top-right
            if (Objectives.Instance != null && Objectives.Instance.Won) DrawWin();

            // --- Layer 3: the focused mode panel, over a dimmed world. ---
            if (modal) DimWorld();
            if (_showHelp) DrawHelp();
            if (_showGuide) DrawGuide();
            if (_showResearch) DrawResearchPanel();
            if (_showMap) DrawMapScreen();
            if (_showLines) DrawLinesPanel();
            if (_showObjectives) DrawObjectivesPanel();
            if (_showStats) DrawStatsPanel();
            if (_showAlerts) DrawAlertsPanel();
            if (_paused) GUI.Label(new Rect(0, 60, _vw, 60), "<b>PAUSED</b>  <size=18>(space)</size>", _big);

            // Block world clicks when the cursor is over an interactive panel. A modal mode
            // dims the whole screen, so it blocks everywhere (clicking off-panel just focuses).
            if (Event.current.type == EventType.Repaint)
            {
                var m = Event.current.mousePosition;
                PointerOverUI = modal
                                || (_buildShown && _buildRect.Contains(m)) || (_selShown && _selRect.Contains(m))
                                || (_flyoutShown && _flyoutRect.Contains(m))
                                || _topRect.Contains(m) || _miniRect.Contains(m) || _objRect.Contains(m)
                                || _powerRect.Contains(m)
                                || (_ageCardT > 0f && _ageCardRect.Contains(m))
                                || (_revealQueue.Count > 0 && _objRevealRect.Contains(m));
            }
        }

        // Dim the world + Layer-1 HUD behind a focused mode panel (Satisfactory/DSP style),
        // so a single system has the player's attention. Critical alerts still draw on top.
        private void DimWorld()
        {
            var c = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, _vw, _vh), Texture2D.whiteTexture);
            GUI.color = c;
        }

        // Consistent dark panel background + border, for an organized look.
        private void PanelBg(Rect r)
        {
            if (_panelTex != null) GUI.DrawTexture(r, _panelTex);
            if (_accentTex != null) GUI.DrawTexture(new Rect(r.x, r.y, r.width, 3f), _accentTex); // amber top accent bar
            GUI.Box(r, GUIContent.none);
        }

        // ---- Top resource bar: grouped, k-formatted, single line, hover for detail ----
        private void DrawTopBar()
        {
            // The chip set + their measured widths change only when the once/frame-cached totals do, but OnGUI
            // runs TWICE per frame — so build + CalcSize-measure the chips at most once per frame, then reuse
            // them for both the Layout and Repaint passes (CalcSize per chip was the cost being doubled).
            if (Time.frameCount != _topBarFrame) { _topBarFrame = Time.frameCount; BuildTopBarChips(); }

            float maxX = _vw - 8f;
            GUI.Box(new Rect(0, 0, _vw, _barH), GUIContent.none);
            _topRect = new Rect(0, 0, _vw, _barH);

            var mp = Event.current.mousePosition;
            bool snap = Time.unscaledTime - _lastSnap >= 3f; // refresh the trend baseline every ~3s
            float x = 12f, y = 5f;
            int hover = -1; Rect hoverRect = default;
            for (int i = 0; i < _chips.Count && i < _chipWidths.Count; i++)
            {
                if (x + _chipWidths[i] > maxX) { x = 12f; y += 24f; } // wrap to the next row
                var c = _chips[i];
                string hex = ColorUtility.ToHtmlStringRGB(c.color);
                // Trend vs the last snapshot: ▲ growing, ▼ shrinking (deficit warning).
                string arrow = "";
                if (_trendSnap.TryGetValue(c.label, out int prev))
                    arrow = c.value > prev ? " <color=#7d7>▲</color>" : c.value < prev ? " <color=#e96>▼</color>" : "";
                if (snap) _trendSnap[c.label] = c.value;
                var r = new Rect(x, y, _chipWidths[i], 22f);
                GUI.Label(r, $"<color=#{hex}>■</color> <b>{c.label}</b> {K(c.value)}{arrow}", _small);
                if (r.Contains(mp)) { hover = i; hoverRect = r; }
                x += _chipWidths[i];
            }
            if (snap) _lastSnap = Time.unscaledTime;

            if (hover >= 0 && _chips[hover].detail != null)
            {
                int lines = _chips[hover].detail.Split('\n').Length;
                var dr = new Rect(hoverRect.x, hoverRect.yMax + 2f, 190f, 10f + lines * 18f);
                GUI.Box(dr, GUIContent.none);
                GUI.Label(new Rect(dr.x + 8, dr.y + 5, dr.width - 16, dr.height - 10), $"<size=13>{_chips[hover].detail}</size>", _small);
            }
        }

        // Build the chip list + measure widths + bar height — once per frame (CalcSize is the cached cost).
        private void BuildTopBarChips()
        {
            var totals = _totals ?? Economy.Totals(gatherer.Inventory);
            int Get(ItemDefinition i) { if (i == null) return 0; totals.TryGetValue(i, out int v); return v; }

            _chips.Clear();
            // Each resource is its OWN chip now (no more grouped "Goods") — Wood/Stone always shown, and
            // every other tracked resource appears once you have some, so the bar fills out as you progress.
            _chips.Add((woodItem != null ? woodItem.displayName : "Wood", Get(woodItem), null, ColorOf(woodItem)));
            _chips.Add((stoneItem != null ? stoneItem.displayName : "Stone", Get(stoneItem), null, ColorOf(stoneItem)));
            if (debugItems != null)
                foreach (var it in debugItems)
                {
                    if (it == null || it == woodItem || it == stoneItem) continue;
                    int v = Get(it);
                    if (v > 0) _chips.Add((it.displayName, v, null, ColorOf(it)));
                }

            // Pre-measure so the bar can WRAP onto extra rows (many individual resources won't fit one line)
            // and the background box can size to however many rows we end up using.
            _chipWidths.Clear();
            float maxX = _vw - 8f, mx = 12f; int rows = 1;
            for (int i = 0; i < _chips.Count; i++)
            {
                var c = _chips[i];
                string hex = ColorUtility.ToHtmlStringRGB(c.color);
                float w = _small.CalcSize(new GUIContent($"<color=#{hex}>■</color> <b>{c.label}</b> {K(c.value)} ▲")).x + 16f;
                _chipWidths.Add(w);
                if (mx + w > maxX) { mx = 12f; rows++; }
                mx += w;
            }
            _barH = rows * 24f + 6f;
        }

        private static Color ColorOf(ItemDefinition i) => i != null ? i.color : Color.white;
        private static string K(int n) => n < 1000 ? n.ToString() : (n / 1000f).ToString("0.#") + "k";

        // ---- Minimap (bottom-right) ----
        private void DrawMinimap()
        {
            if (!_showMinimap) { _miniRect = default; return; }
            const float size = 168f;
            var r = new Rect(_vw - size - 12f, _vh - size - 12f, size, size);
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
            if (gatherer != null) Dot(gatherer.transform.position, new Color(0.96f, 0.85f, 0.2f), 6f);

            // Legend so the dot colours are self-explanatory. Right-aligned in a rect that extends
            // LEFT of the map (which hugs the screen's right edge), so a legible-size label can't be
            // clipped off-screen. A bit higher + bigger than before (was an illegible size-10).
            _legend ??= new GUIStyle(_small) { alignment = TextAnchor.LowerRight };
            GUI.Label(new Rect(r.x - 90f, r.y - 25f, r.width + 90f, 20f),
                "<size=13><color=#6c6>●</color> ok   <color=#f66>●</color> starved   <color=#fd4>●</color> full</size>", _legend);
        }

        // ---- Full world map screen (M): pan + zoom the discovered world ----
        // A proper map "mode" (replaces the old M = zoom-camera-all-the-way): a large square view
        // of the explored fog with live building/resource/player dots. Drag to pan, wheel to zoom
        // (toward the cursor); M or Esc closes. Modal, so the game world is dimmed + click-blocked
        // underneath (handled by ModalOpen) — this is purely a HUD overlay, the game camera is untouched.
        private void DrawMapScreen()
        {
            var fog = FogOfWar.Instance;
            float W = fog != null ? fog.WorldSize : 240f;

            // Square map area, centred, leaving headroom for the title + legend.
            float side = Mathf.Min(_vw - 120f, _vh - 150f);
            var area = new Rect((_vw - side) / 2f, (_vh - side) / 2f + 6f, side, side);

            GUI.Label(new Rect(area.x, area.y - 38f, area.width, 30f),
                "<b><color=#e8e0c8>World Map</color></b>   <size=13><color=#aaa>drag to pan · wheel to zoom · M or Esc to close</color></size>", _s);

            // Pan + zoom from events over the area — handled BEFORE BeginGroup so coords are screen-space.
            var e = Event.current;
            if (e != null && area.Contains(e.mousePosition))
            {
                if (e.type == EventType.ScrollWheel)
                {
                    float old = _mapZoom;
                    _mapZoom = Mathf.Clamp(_mapZoom * (e.delta.y > 0f ? 0.9f : 1.1f), 1f, 8f); // wheel up = zoom in
                    Vector2 rel = e.mousePosition - new Vector2(area.x, area.y);               // keep the point under the cursor fixed
                    _mapPan = (_mapPan - rel) * (_mapZoom / old) + rel;
                    e.Use();
                }
                else if (e.type == EventType.MouseDown && e.button == 0) { _mapDragging = true; _mapDragLast = e.mousePosition; e.Use(); }
            }
            if (_mapDragging && e != null)
            {
                if (e.type == EventType.MouseDrag) { _mapPan += e.mousePosition - _mapDragLast; _mapDragLast = e.mousePosition; e.Use(); }
                else if (e.type == EventType.MouseUp) { _mapDragging = false; e.Use(); }
            }

            float content = side * _mapZoom;
            _mapPan.x = Mathf.Clamp(_mapPan.x, side - content, 0f); // content always covers the area (no empty gaps)
            _mapPan.y = Mathf.Clamp(_mapPan.y, side - content, 0f);

            GUI.BeginGroup(area); // clips everything below to the map area
            var cr = new Rect(_mapPan.x, _mapPan.y, content, content);

            // Base: dark ground, then the TERRAIN biome map (aligned to world coords), then fog over it
            // (explored = transparent → terrain shows; unexplored = dark fog).
            GUI.color = new Color(0.16f, 0.20f, 0.15f);
            GUI.DrawTexture(cr, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (TerrainGrid.MapTex != null)
            {
                float uL = (W * 0.5f - TerrainGrid.Half) / W, span = (2f * TerrainGrid.Half) / W;
                GUI.DrawTexture(new Rect(cr.x + uL * content, cr.y + uL * content, span * content, span * content), TerrainGrid.MapTex);
            }
            if (fog != null && fog.Tex != null) GUI.DrawTexture(cr, fog.Tex);

            Vector2 gm = e != null ? e.mousePosition : new Vector2(-999f, -999f); // group-relative cursor (for hover)
            string hoverLabel = null; Vector2 hoverScreen = default;

            void Dot(Vector3 wp, Color col, float s)
            {
                float u = (wp.x + W / 2f) / W, v = (wp.y + W / 2f) / W;
                if (u < 0f || u > 1f || v < 0f || v > 1f) return;
                GUI.color = col;
                GUI.DrawTexture(new Rect(cr.x + u * content - s / 2f, cr.y + (1f - v) * content - s / 2f, s, s), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            void Hover(Vector3 wp, string label)
            {
                float u = (wp.x + W / 2f) / W, v = (wp.y + W / 2f) / W;
                if (u < 0f || u > 1f || v < 0f || v > 1f) return;
                float gx = cr.x + u * content, gy = cr.y + (1f - v) * content;
                if (Mathf.Abs(gm.x - gx) <= 9f && Mathf.Abs(gm.y - gy) <= 9f) { hoverLabel = label; hoverScreen = new Vector2(area.x + gx, area.y + gy); }
            }

            float ds = Mathf.Clamp(4f * Mathf.Sqrt(_mapZoom), 4f, 14f); // dots grow a bit as you zoom in
            foreach (var rn in ResourceNode.All)
            {
                if (rn == null || rn.yields == null) continue;
                if (fog != null && !fog.IsExplored(rn.transform.position)) continue; // only show explored patches
                var rc = rn.yields.color;
                Dot(rn.transform.position, new Color(rc.r, rc.g, rc.b, 0.95f), ds * 0.8f);
                Hover(rn.transform.position, rn.yields.displayName);
            }
            foreach (var st in StorageBuilding.All)
            {
                if (st == null) continue;
                float fill = (st.def != null && st.def.capacity > 0) ? (float)st.Store.Total() / st.def.capacity : 0f;
                Color sc = fill >= 0.99f ? new Color(0.95f, 0.55f, 0.2f)
                         : Color.Lerp(new Color(0.5f, 0.5f, 0.62f), new Color(0.9f, 0.8f, 0.3f), fill);
                Dot(st.transform.position, sc, ds);
                Hover(st.transform.position, (st.accepts != null ? st.accepts.displayName + " store" : "Storage") + $"  {Mathf.RoundToInt(fill * 100f)}%");
            }
            foreach (var dp in Depot.All) { if (dp == null) continue; Dot(dp.transform.position, new Color(0.5f, 0.8f, 0.9f), ds); Hover(dp.transform.position, dp.item != null ? dp.item.displayName + " station" : "Station"); }
            foreach (var p in ProductionBuilding.All) { if (p == null) continue; Dot(p.transform.position, p.StatusColor, ds); Hover(p.transform.position, p.produces != null ? p.produces.displayName + " collector" : "Collector"); }
            foreach (var wk in WorkshopBuilding.All) { if (wk == null) continue; Dot(wk.transform.position, wk.StatusColor, ds); Hover(wk.transform.position, wk.output != null ? wk.output.displayName + " workshop" : "Workshop"); }
            if (gatherer != null) Dot(gatherer.transform.position, new Color(0.96f, 0.85f, 0.2f), ds * 1.7f); // you

            GUI.EndGroup();
            GUI.Box(area, GUIContent.none); // frame

            // Hover tooltip — drawn in screen space, after the clip group, so it isn't clipped.
            if (hoverLabel != null)
            {
                var tip = new GUIContent($"<size=13>{hoverLabel}</size>");
                Vector2 sz = _small.CalcSize(tip);
                float tw = sz.x + 16f, th = 24f;
                float tx = hoverScreen.x + 14f; if (tx + tw > _vw) tx = hoverScreen.x - tw - 14f;
                float ty = Mathf.Clamp(hoverScreen.y - 12f, 4f, _vh - th - 4f);
                var tr = new Rect(tx, ty, tw, th);
                PanelBg(tr);
                GUI.Label(new Rect(tr.x + 8f, tr.y + 3f, tr.width - 16f, tr.height - 6f), tip, _small);
            }

            GUI.Label(new Rect(area.x, area.yMax + 4f, area.width, 22f),
                "<size=12><color=#fd2>●</color> you  <color=#6c6>●</color> ok  <color=#f66>●</color> starved  <color=#fd4>●</color> full  <color=#8cf>●</color> resource   ·   hover a dot to identify it    <color=#4f6840>▮</color>plains <color=#29542c>▮</color>forest <color=#786e5c>▮</color>hills <color=#56524f>▮</color>mtn <color=#33639e>▮</color>water</size>", _small);
        }

        // ---- "+N" hand-gather popups: small bold numbers that float up from the node and fade ----
        private void DrawGatherPopups()
        {
            if (GatherPopup.Items.Count == 0) return;
            var cam = Camera.main;
            if (cam == null) return;
            _gatherStyle ??= new GUIStyle(GUI.skin.label) { richText = true, fontSize = 17, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            foreach (var g in GatherPopup.Items)
            {
                float life = Mathf.Clamp01(g.t / GatherPopup.Life); // 1 at spawn → 0 at expiry
                Vector3 sp = cam.WorldToScreenPoint(g.world);
                if (sp.z < 0f) continue; // behind the camera (shouldn't occur in 2D)
                float x = sp.x / _uiScale, y = _vh - sp.y / _uiScale - (1f - life) * 42f; // screen px → logical; GUI y top-down
                string hex = ColorUtility.ToHtmlStringRGB(g.color);
                var prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(life * 1.5f)); // hold, then fade near the end
                // Size the box to the TEXT (the old fixed 120px clipped anything longer than "+1 Wood",
                // e.g. the "Too far — move closer" warning).
                var content = new GUIContent($"<color=#{hex}>{g.text}</color>");
                float bw = Mathf.Max(120f, _gatherStyle.CalcSize(content).x + 14f);
                GUI.Label(new Rect(x - bw / 2f, y - 38f, bw, 22f), content, _gatherStyle);
                GUI.color = prev;
            }
        }

        // ---- Status (top bar) ----
        private void DrawStatus()
        {
            GUILayout.BeginArea(new Rect(12, 34, Mathf.Max(620f, _vw - 310f), 52));
            var c = Colony.Instance;
            if (c != null)
            {
                // Bottleneck summary — starved / backed-up machine counts (cached once/frame in Update;
                // this pairs with the minimap dots so you can find & fix the problem).
                int starved = _starvedCount, waiting = _waitingCount, backed = _backedCount;
                // Stay quiet early (Factorio focus): only show production FAILURE (starved) until the
                // base is actually going; non-critical states appear once established.
                bool established = c.Age >= 1 || c.PeakProsperity >= 100;
                string bottleneck = "";
                if (starved > 0) bottleneck += $"   <color=#f66>⚠ {starved} out of resource</color>";
                if (established && waiting > 0) bottleneck += $"   <color=#fc3>◷ {waiting} waiting on inputs</color>";
                if (established && backed > 0) bottleneck += $"   <color=#fd4>⏸ {backed} backed-up (output full)</color>";

                // Monument progress (endgame): shown once a Monument exists or blocks are held.
                string monu = "";
                if (monumentItem != null)
                {
                    int mb = 0; if (_totals != null) _totals.TryGetValue(monumentItem, out mb);
                    if (_hasMonument || mb > 0) monu = $"   <color=#ffe08a>Monument {Mathf.Min(mb, 10)}/10</color>";
                }

                // Power network: total generation / demand across all networks, red while oversubscribed,
                // plus battery charge across the grid.
                string power = "";
                PowerNet.EnsureFresh();
                if (PowerNet.TotalGen > 0 || PowerNet.TotalDemand > 0 || PowerNet.TotalCapacity > 0)
                {
                    bool brown = PowerNet.TotalDemand > PowerNet.TotalGen + 0.01f;
                    string pc = brown ? "#f99" : "#9f9";
                    string batt = PowerNet.TotalCapacity > 0
                        ? $" <color=#8df>🔋 {Mathf.RoundToInt(PowerNet.TotalStored)}/{Mathf.RoundToInt(PowerNet.TotalCapacity)}</color>" : "";
                    power = $"   <color={pc}>⚡ {Mathf.RoundToInt(PowerNet.TotalGen)} made · {Mathf.RoundToInt(PowerNet.TotalDemand)}/{Mathf.RoundToInt(PowerNet.TotalMaxDemand)} used{(brown ? " — OVERLOADED" : "")}</color>{batt}";
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

        // (The old top-left single-objective line + the on-screen ore-finder arrows were removed in the
        //  objectives overhaul — guidance now lives in the centre reveal popup, the top-right box, and the
        //  J journal. See DrawObjectives / DrawObjectiveReveal / DrawObjectivesPanel below.)

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

            var rect = new Rect(sp.x / _uiScale + 16f, _vh - sp.y / _uiScale + 8f, 234f, 30f); // screen px → logical
            if (rect.xMax > _vw) rect.x = _vw - rect.width - 4f;
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
            var pp = hit.GetComponent<PowerPlant>();
            if (pp != null) return $"<size=13><b>{NameOf(pp.def)}</b>  <color=#bbb>generator</color></size>";
            var pole = hit.GetComponent<PowerPole>();
            if (pole != null) return $"<size=13><b>{NameOf(pole.def)}</b>  <color=#bbb>power network</color></size>";
            var blt = hit.GetComponent<Belt>();
            if (blt != null) return $"<size=13><b>{blt.DisplayName}</b>  <color=#bbb>{(blt.item != null ? blt.item.displayName : "")}</color></size>";
            var wp = hit.GetComponent<WaterPump>();
            if (wp != null) return wp.isBooster
                ? "<size=13><b>Booster Pump</b>  <color=#bbb>re-pressurises pipes</color></size>"
                : "<size=13><b>Water Pump</b>  <color=#bbb>water → pipes</color></size>";
            if (hit.GetComponent<Bridge>() != null) return "<size=13><b>Bridge</b></size>";
            if (hit.GetComponent<Pipe>() != null) return "<size=13><b>Pipe</b>  <color=#bbb>liquid network</color></size>";
            var rt = hit.GetComponent<RailTile>();
            if (rt != null) return $"<size=13><b>{(rt.def != null ? NameOf(rt.def) : "Track")}</b>  <color=#bbb>rail</color></size>";
            var sg = hit.GetComponent<Signal>();
            if (sg != null) return $"<size=13><b>{(sg.bothWays ? "Two-Way Signal" : "Signal")}</b>  <color=#bbb>rail block</color></size>";
            var bat = hit.GetComponent<Battery>();
            if (bat != null) return $"<size=13><b>{(bat.def != null ? NameOf(bat.def) : "Battery")}</b>  <color=#bbb>power storage</color></size>";
            var gar = hit.GetComponent<Garage>();
            if (gar != null) return $"<size=13><b>{(gar.def != null ? NameOf(gar.def) : "Garage")}</b>  <color=#bbb>mounts</color></size>";
            var rv = hit.GetComponent<RouteVehicle>();
            if (rv != null) return $"<size=13><b>{rv.VehicleName()}</b>  <color=#bbb>{rv.CargoSummary()}</color></size>";
            return null;
        }

        private void DrawBuildMenu()
        {
            if (builder.PendingIndex >= 0)
            {
                _buildShown = false; _flyoutShown = false;
                var def = builder.buildables[builder.PendingIndex];
                GUILayout.BeginArea(new Rect(12, _vh - 58, 520, 50));
                if (def.kind == BuildingKind.Belt && (def.splitter || def.merger))
                {
                    GUILayout.Label($"<b>Placing {def.displayName}</b> — <color=#9cf>{builder.BeltDir}</color>  <size=14>(R to rotate)</size>", _s);
                    GUILayout.Label("<size=14><color=#9f9>green ▸ = out</color> · <color=#6cf>cyan = in</color> · click to place · right-click to finish</size>", _small);
                }
                else if (def.kind == BuildingKind.Belt && def.underground)
                {
                    GUILayout.Label($"<b>Placing Underground Belt</b> — flow <color=#9cf>{builder.BeltDir}</color>  <size=14>(R rotates)</size>", _s);
                    GUILayout.Label("<size=14>click the ENTRANCE, then click up to 4 tiles ahead (same way) for the EXIT — belts/track cross the gap · right-click to finish</size>", _small);
                }
                else if (def.kind == BuildingKind.Belt && (def.filter || def.gate))
                {
                    GUILayout.Label($"<b>Placing {def.displayName}</b> — <color=#9cf>{builder.BeltDir}</color>  <size=14>(R rotates)</size>", _s);
                    GUILayout.Label("<size=14>click an empty cell or a plain belt to convert it · right-click to finish</size>", _small);
                }
                else if (def.kind == BuildingKind.Belt)
                {
                    GUILayout.Label("<b>Belt blueprint</b> — <color=#9cf>drag to plan</color>, <color=#9f9>click to build</color>  <size=14>(R rotates)</size>", _s);
                    GUILayout.Label("<size=14>plan a run, then click to build it · right-click to cancel</size>", _small);
                }
                else if (def.kind == BuildingKind.Rail && def.elevated)
                {
                    GUILayout.Label("<b>Elevated Track</b> — <color=#9cf>drag to plan</color> (90°), <color=#9f9>click to build</color>", _s);
                    GUILayout.Label("<size=14>lay it OVER belts to cross them; join ground track at each end · right-click to cancel</size>", _small);
                }
                else if (def.kind == BuildingKind.Rail)
                {
                    GUILayout.Label("<b>Track blueprint</b> — <color=#9cf>drag to plan</color> (90°), <color=#9f9>click to build</color>", _s);
                    GUILayout.Label("<size=14>plan a run between two Stations, then click to build · right-click to cancel</size>", _small);
                }
                else if (def.kind == BuildingKind.Signal && def.bothWaySignal)
                {
                    GUILayout.Label($"<b>Placing Two-Way Signal</b> — axis <color=#9cf>{builder.BeltDir}</color>  <size=14>(R rotates)</size>", _s);
                    GUILayout.Label("<size=14>click a track cell · trains pass EITHER way, one train per block · right-click to finish</size>", _small);
                }
                else if (def.kind == BuildingKind.Signal)
                {
                    GUILayout.Label($"<b>Placing Signal</b> — pass way <color=#9cf>{builder.BeltDir}</color>  <size=14>(R rotates)</size>", _s);
                    GUILayout.Label("<size=14>click a track cell · trains pass only this way + when the block ahead is clear · right-click to finish</size>", _small);
                }
                else
                {
                    bool isColl = def.kind == BuildingKind.Collector;
                    string bad = isColl ? $"<color=#f99>{(def.drill ? $"place it ON the {Name(def.item)} deposit" : $"move onto a glowing {Name(def.item)} patch")}</color>" : "<color=#f99>move to a clear spot</color>";
                    string ok = builder.PlacementValid ? "<color=#9f9>green = click to place</color>" : bad;
                    GUILayout.Label($"<b>Placing {def.displayName}</b> — {ok}", _s);
                    string hint = isColl
                        ? (def.drill
                            ? $"<color=#cda>A DRILL mines only what its footprint covers — more {Name(def.item)} pockets under it = faster.</color>  "
                            : $"<color=#cda>Must sit next to a {Name(def.item)} source — the {Name(def.item)} patches are glowing.</color>  ")
                        : "";
                    bool hasPorts = def.kind == BuildingKind.Collector || def.kind == BuildingKind.Workshop;
                    string rot = hasPorts ? $"<color=#6f6>R rotates output ▸ {builder.BuildDir}</color> (belts pull from there) · "
                               : def.kind == BuildingKind.Depot ? "<color=#6f6>R rotates the platform + track lane</color> · "
                               : def.kind == BuildingKind.Power ? $"<color=#6f6>R aims the fuel input ▸ {builder.BuildDir}</color> · "
                               : "";
                    GUILayout.Label($"<size=14>{hint}{rot}right-click / Esc to cancel</size>", _small);
                }
                GUILayout.EndArea();
                return;
            }

            if (!_showBuild)
            {
                _buildShown = false; _flyoutShown = false;
                GUILayout.BeginArea(new Rect(12, _vh - 34, 320, 28));
                GUILayout.Label("<size=15>Press <b>B</b> to open the build menu</size>", _small);
                GUILayout.EndArea();
                return;
            }

            const float top = 118f; // sit below the top status + objective/tips bars (which end ~112) so it doesn't cover them
            float height = Mathf.Min(_vh - top - 16f, 470f);
            _buildRect = new Rect(12, top, 268, height);
            _buildShown = true;
            _flyoutShown = false; // set true below if a category flyout is open
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
            GUILayout.Label("<b>Build</b>  <size=11>(open a category ▸ pick · then click the map)</size>", _small);

            int curAge = col != null ? col.Age : 0;
            // The current objective's target building drives the guided highlight: flash its CATEGORY until you
            // open it, then flash the BUILDING itself. Follows the LIVE objective, so it never sticks on a building
            // you've already built (the old bug: it keyed off age + a static flag, not the active goal).
            var guideQ = Objectives.Instance != null ? Objectives.Instance.CurrentStep() : null;
            var guideTarget = guideQ != null ? guideQ.highlightBuilding : null;
            string guideCat = null;
            if (guideTarget != null) foreach (var gc in Cats) if (Belongs(guideTarget, gc)) { guideCat = gc.label; break; }

            // Draws one build entry: a pin star + the build button. Locked → greyed label.
            // Obsolete (unlocked ≥2 ages ago) is dimmed but still usable. Local fn so the
            // pinned/recent/flyout lists all render identically.
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
                    bool guide = guideTarget != null && def == guideTarget; // flash the CURRENT objective's target building
                    bool blink = guide && Mathf.Sin(Time.unscaledTime * 5f) > 0f;
                    string costCol = builder.CanAfford(def) ? "#9f9" : "#f99";
                    string key = i < 9 ? (i + 1).ToString() : i == 9 ? "0" : "·";
                    string nm = guide ? $"<color=#ffd24d>{(blink ? "★" : "☆")} {def.displayName}</color>" : obsolete ? $"<color=#9a9a9a>{def.displayName}</color>" : def.displayName;
                    var label = new GUIContent($"<size=12>[{key}] {nm}  <color={costCol}>{CostText(def)}</color></size>", Describe(def));
                    if (GUILayout.Button(label, _btn)) builder.BeginPlacement(i);
                }
                GUILayout.EndHorizontal();
            }

            // Does this category have at least one unlocked buildable right now? (progressive disclosure)
            bool CatHasAny((string label, BuildingKind[] kinds) cat)
            {
                for (int i = 0; i < builder.buildables.Count; i++)
                {
                    var d = builder.buildables[i];
                    if (d != null && Belongs(d, cat) && builder.IsUnlocked(d)) return true;
                }
                return false;
            }

            // ---- LEFT panel: Pinned shortcuts, then the category list. Clicking a category
            //      OPENS it as a flyout to the RIGHT (one category at a time); click it again to close. ----
            _buildScroll = GUILayout.BeginScrollView(_buildScroll);

            if (_pinned.Count > 0)
            {
                GUILayout.Label("<b><color=#ffd24d>★ Pinned</color></b>", _small);
                foreach (int i in _pinned)
                    if (i >= 0 && i < builder.buildables.Count && builder.IsUnlocked(builder.buildables[i])) Entry(i);
                GUILayout.Space(4);
            }

            GUILayout.Label("<b><color=#cda>Categories</color></b>  <size=11>(click to open ▸)</size>", _small);
            foreach (var cat in Cats)
            {
                if (!CatHasAny(cat)) continue; // a category appears only once it has a buildable
                bool active = _activeCat == cat.label;
                bool catGuide = guideCat == cat.label && !active; // flash the category holding the objective's building
                bool catBlink = catGuide && Mathf.Sin(Time.unscaledTime * 5f) > 0f;
                string arrow = active ? "<color=#ffd24d>▸</color> " : catGuide ? (catBlink ? "<color=#ffd24d>★</color> " : "<color=#ffd24d>☆</color> ") : "";
                string catCol = catBlink ? "#ffd24d" : "#d8c8a0";
                if (GUILayout.Button($"<size=12><b><color={catCol}>{arrow}{cat.label}</color></b></size>", _btn))
                    _activeCat = active ? "" : cat.label; // open this one (closes the rest); click again to close
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // ---- RIGHT flyout: the active category's items pop out beside the menu. ----
            Rect tipAnchor = _buildRect;
            var activeCat = System.Array.Find(Cats, c => c.label == _activeCat);
            if (activeCat.label != null && CatHasAny(activeCat))
            {
                var fr = new Rect(_buildRect.xMax + 6, _buildRect.y, 232, _buildRect.height);
                _flyoutRect = fr; _flyoutShown = true; tipAnchor = fr;
                PanelBg(fr);
                GUILayout.BeginArea(new Rect(fr.x + 8, fr.y + 8, fr.width - 16, fr.height - 16));
                GUILayout.Label($"<b><color=#d8c8a0>{activeCat.label}</color></b>  <size=11>(click, then click the map)</size>", _small);
                GUILayout.Space(2);
                _flyoutScroll = GUILayout.BeginScrollView(_flyoutScroll);
                for (int i = 0; i < builder.buildables.Count; i++)
                {
                    var def = builder.buildables[i];
                    if (def == null || !Belongs(def, activeCat)) continue;
                    if (!builder.IsUnlocked(def)) continue; // hide locked/age-gated until relevant
                    Entry(i);
                }
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }

            // Tooltip describing the hovered build entry — to the right of whichever panel is rightmost.
            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                const float tw = 304f;
                var tip = new GUIContent($"<size=13>{GUI.tooltip}</size>");
                float th = _small.CalcHeight(tip, tw - 18) + 14f;            // size to content — never clip the stat block
                float ty = Mathf.Max(8f, Mathf.Min(tipAnchor.y, _vh - 8f - th)); // and keep the whole box on-screen
                var tr = new Rect(tipAnchor.xMax + 6, ty, tw, th);
                PanelBg(tr);
                GUI.Label(new Rect(tr.x + 9, tr.y + 7, tr.width - 18, th - 14), tip, _small);
            }
        }

        // A Satisfactory-style entry: a prose body (hand-written if present, else synthesised from the kind)
        // followed by a structured stat block — recipe, power, capacity, build cost, footprint, unlock age.
        private static string Describe(BuildingDefinition def)
        {
            if (def == null) return "";
            string body = !string.IsNullOrEmpty(def.description) ? def.description : KindBody(def);
            return body + StatBlock(def);
        }

        // The data-driven prose for kinds without a hand-written description.
        private static string KindBody(BuildingDefinition def)
        {
            switch (def.kind)
            {
                case BuildingKind.Collector:
                    return $"Auto-gathers {Name(def.item)} from a nearby patch at a fixed rate once built (no workers). Place it next to the resource; it re-targets a fresh patch when one runs dry.";
                case BuildingKind.Workshop:
                    return "Runs automatically once its inputs are DELIVERED here (belt, or an adjacent storage/machine). Red dot = a missing input.";
                case BuildingKind.Storage:
                    return def.configurable
                        ? "Stores ONE resource you pick (set it in its panel) — Planks, Charcoal, etc. Belt output here."
                        : $"Stores {Name(def.item)}. Belt output here; if it fills up, production backs up.";
                case BuildingKind.Belt:
                    return $"Carries items one cell in the way it faces ({(def.interval <= 0.6f ? "fast tier" : "slow tier")}). Click/drag to lay a line, R to rotate.";
                case BuildingKind.Depot:
                    return "Transport Station. Belt goods in, then SELECT it and route to another Station — a vehicle hauls goods across the map (load → travel → unload).";
                case BuildingKind.Power:
                    return "Generates electricity for the grid. Wire it to poles or machines — you draw the wires, there is no radius.";
                case BuildingKind.Pole:
                    return "Relays power across the map. Wire poles together to extend the grid to distant machines.";
                case BuildingKind.Battery:
                    return "Stores surplus electricity and releases it when generation drops, smoothing the grid through quiet spells.";
                case BuildingKind.Pump:
                    return $"Draws {Name(def.item)} and feeds it into pipes/belts on its output side.";
                case BuildingKind.Garage:
                    return "Houses and maintains your vehicles.";
                default:
                    return def.displayName;
            }
        }

        // The structured footer shown under every build-menu tooltip.
        private static string StatBlock(BuildingDefinition def)
        {
            var sb = new System.Text.StringBuilder();
            if (def.kind == BuildingKind.Workshop && def.inputs != null && def.inputs.Count > 0)
                sb.Append($"\n\n<color=#cfe>Recipe:</color> {CostList(def.inputs)} → {def.outputPerCycle} {Name(def.item)}");
            else if (def.kind == BuildingKind.Collector && def.item != null)
                sb.Append($"\n\n<color=#cfe>Output:</color> {Name(def.item)}");
            else sb.Append("\n");

            if (def.powerOutput > 0) sb.Append($"\n<color=#9f9>⚡ Generates {def.powerOutput} power</color>");
            else if (def.powerDraw > 0) sb.Append($"\n<color=#fd6>⚡ Uses {def.powerDraw} power</color>");
            else if (def.requiresPower) sb.Append("\n<color=#fd6>⚡ Needs power</color>");

            if (def.kind == BuildingKind.Storage && def.capacity > 0) sb.Append($"\n<color=#bcd>📦 Holds {def.capacity}</color>");
            if (def.cost != null && def.cost.Count > 0) sb.Append($"\n<color=#cb9>🔨 {AmountsText(def.cost)}</color>");
            if (def.FootW * def.FootH > 1) sb.Append($"\n<color=#999>📐 {def.FootW}×{def.FootH}</color>");
            if (def.unlockAge > 0 && def.unlockAge < Colony.AgeNames.Length)
                sb.Append($"\n<i><color=#999>Unlocks: {Colony.AgeNames[def.unlockAge]}</color></i>");
            return sb.ToString();
        }

        // ---- Selected building manage panel (bottom-right) ----
        // Format a resource cost list, e.g. "6 Planks, 3 Copper" (for the upgrade button).
        private static string AmountsText(List<ItemAmount> cost)
        {
            if (cost == null || cost.Count == 0) return "free";
            var parts = new List<string>();
            foreach (var c in cost) if (c != null && c.item != null) parts.Add($"{c.amount} {c.item.displayName}");
            return string.Join(", ", parts);
        }

        private void DrawSelectedPanel()
        {
            var sel = builder != null ? builder.Selected : null;
            if (sel == null) { _selShown = false; return; }

            // Bottom-anchored so it clears the minimap when shown; taller + scrollable so a smelter's
            // full readout (recipe + needs + ⚠ warnings + buttons) never gets clipped off the bottom.
            float bottom = _showMinimap ? _vh - 196f : _vh - 12f;
            float top = Mathf.Max(120f, bottom - 330f);
            var rect = new Rect(_vw - 290, top, 278, bottom - top);
            _selRect = rect;
            _selShown = true;

            PanelBg(rect);
            GUILayout.BeginArea(new Rect(rect.x + 12, rect.y + 10, rect.width - 24, rect.height - 20));
            _selScroll = GUILayout.BeginScrollView(_selScroll, GUILayout.Height(rect.height - 58)); // leave room for the button row

            var sb = sel.GetComponent<StorageBuilding>();
            var wb = sel.GetComponent<WorkshopBuilding>();
            var cs = sel.GetComponent<ConstructionSite>();
            var dp = sel.GetComponent<Depot>();
            var pbSel = sel.GetComponent<ProductionBuilding>();
            var resB = sel.GetComponent<ResearchBuilding>();
            var pwr = sel.GetComponent<PowerPlant>();
            var pole = sel.GetComponent<PowerPole>();
            var bat = sel.GetComponent<Battery>();
            var pnode = sel.GetComponent<PowerNode>();
            var garage = sel.GetComponent<Garage>();
            var blt = sel.GetComponent<Belt>();
            var armC = sel.GetComponent<CraneArm>();
            string name = wb != null ? wb.def.displayName : pbSel != null ? pbSel.def.displayName
                : sb != null ? sb.def.displayName
                : dp != null ? dp.def.displayName : resB != null ? resB.def.displayName
                : pwr != null ? pwr.def.displayName : pole != null ? pole.def.displayName
                : bat != null ? bat.def.displayName
                : garage != null ? garage.def.displayName
                : armC != null ? armC.def.displayName
                : cs != null ? cs.def.displayName : blt != null ? blt.DisplayName : "Building";
            GUILayout.Label($"<b>{name}</b>", _s);

            // GARAGE — buy age-gated mounts + pick which to ride (the limited mount garage).
            if (garage != null) DrawGaragePanel();

            // FILTER BELT — a visible whitelist picker (the old behaviour, silently adopting the first
            // item to arrive, was an invisible rule nobody could discover or change).
            if (blt != null && blt.isFilter) DrawFilterPicker(blt);

            // CRANE ARM — swing stats, what's in the claw, the Gantry filter, and its power state.
            if (armC != null)
            {
                GUILayout.Label($"<size=13>Swings <b>{armC.grabSize}</b> items every <b>{armC.cycleTime:0.0}s</b> · reach <b>{armC.reach}</b></size>", _small);
                GUILayout.Label($"<size=12><color=#bbb>Grabs from BEHIND → drops in FRONT (R aims while placing). Holding: {(armC.held != null && armC.heldCount > 0 ? $"{armC.heldCount}× {armC.held.displayName}" : "nothing")}</color></size>", _small);
                if (armC.HasFilterUI) DrawFilterList(armC.filterItems, "FILTER — the claw grabs ONLY the ticked items:");
                if (armC.PowerDraw > 0 && PowerNet.Active)
                {
                    var anode = armC.GetComponent<PowerNode>();
                    if (anode != null && anode.links.Count == 0)
                    {
                        GUILayout.Label("<size=12><color=#fc6>⚡ Running at HALF speed — wire it to the grid for full power.</color></size>", _small);
                        if (GUILayout.Button("<size=12>⚡ Connect to nearest pole/generator</size>", _btn))
                        {
                            PowerNode best = null; float bestSq = float.MaxValue;
                            foreach (var n in PowerNode.All)
                            {
                                if (n == null || n == anode || n.role == PowerNode.Role.Consumer || !n.CanLinkMore) continue;
                                float sq = (n.Pos - anode.Pos).sqrMagnitude;
                                if (sq < bestSq) { bestSq = sq; best = n; }
                            }
                            if (best != null && anode.Connect(best)) AudioManager.Click();
                            else Toast.Show("<color=#ffb24d>No pole/generator within wire reach — place a Power Pole closer, then try again.</color>");
                        }
                    }
                }
            }

            // Plain-language status line for producers (the "understandable bottleneck" cue).
            if (wb != null || pbSel != null)
            {
                bool paused = wb != null ? wb.Paused : pbSel.Paused;
                bool isCollector = pbSel != null;
                Color sc = wb != null ? wb.StatusColor : pbSel.StatusColor;
                string st = paused ? "Paused"
                    : (wb != null && wb.PowerStatus == WorkshopBuilding.PowerState.Unwired) ? "No power — not wired to the electricity grid"
                    : (wb != null && wb.PowerStatus == WorkshopBuilding.PowerState.GridDead) ? "No power — the grid has no generation"
                    : (wb != null && wb.PowerStatus == WorkshopBuilding.PowerState.Brownout) ? "Browning out — running slow (demand exceeds generation)"
                    : sc == Status.Working ? "Working"
                    : sc == Status.BackedUp ? (isCollector
                        ? "Output full — belt it to a Storage to use what it makes"
                        : "Output full — belt the output away (it's blocked)")
                    : sc == Status.Waiting ? "Waiting — an input isn't arriving yet (belt it in)"
                    : sc == Status.Starved ? (isCollector
                        ? "Out of resource — its patch ran dry; move or expand"
                        : "Starved — an input isn't arriving")
                    : "Idle";
                GUILayout.Label($"<size=13><color=#{ColorUtility.ToHtmlStringRGB(sc)}>● {st}</color></size>", _small);
                // Drill: show WHAT it's standing on — coverage drives its rate, so this is its whole story.
                if (pbSel != null && pbSel.IsDrill)
                {
                    int pk = pbSel.CoveredPockets;
                    GUILayout.Label($"<size=12><color=#9cf>Deposit: {pk} pocket{(pk == 1 ? "" : "s")} covered · ×{pbSel.Richness:0.0} rate · ~{pbSel.CoveredRemaining} {(pbSel.produces != null ? pbSel.produces.displayName : "")} left</color></size>", _small);
                    if (pbSel.PowerDraw > 0 && PowerNet.Active && pbSel.DrillPowerFactor < 0.999f)
                    {
                        GUILayout.Label("<size=12><color=#fc6>⚡ Running at HALF speed — wire it to the grid for full power.</color></size>", _small);
                        var dnode = pbSel.GetComponent<PowerNode>();
                        if (dnode != null && dnode.links.Count == 0 && GUILayout.Button("<size=12>⚡ Connect to nearest pole/generator</size>", _btn))
                        {
                            PowerNode best = null; float bestSq = float.MaxValue;
                            foreach (var n in PowerNode.All)
                            {
                                if (n == null || n == dnode || n.role == PowerNode.Role.Consumer || !n.CanLinkMore) continue;
                                float sq = (n.Pos - dnode.Pos).sqrMagnitude;
                                if (sq < bestSq) { bestSq = sq; best = n; }
                            }
                            if (best != null && dnode.Connect(best)) AudioManager.Click();
                            else Toast.Show("<color=#ffb24d>No pole/generator within wire reach — place a Power Pole closer, then try again.</color>");
                        }
                    }
                }
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

            if (wb != null || pbSel != null)
            {
                // Buildings run automatically (no workers). Pause toggle — halt this machine to free
                // a shared input for another (priorities).
                if (wb != null || pbSel != null)
                {
                    bool isPaused = wb != null ? wb.Paused : pbSel.Paused;
                    if (GUILayout.Button(isPaused ? "<size=12><color=#9f9>▶ Resume</color></size>" : "<size=12>⏸ Pause</size>", _btn))
                    { if (wb != null) wb.TogglePause(); if (pbSel != null) pbSel.TogglePause(); }
                }

                // Manual paid UPGRADE — buy the next tier to speed this building up + change its look. Always
                // shown (with a clear "tier X/Y" header) for any upgradable building, so the option is obvious.
                {
                    int maxTier = wb != null ? wb.UpgradeTierCount : pbSel.UpgradeTierCount;
                    if (maxTier > 0)
                    {
                        var upTier = wb != null ? wb.PendingUpgrade : pbSel.PendingUpgrade;
                        int curTier = wb != null ? wb.Tier : pbSel.Tier;
                        GUILayout.Label($"<size=12><b><color=#cda>⚙ Upgrade</color></b>   <color=#999>tier {curTier} / {maxTier}</color></size>", _small);
                        if (upTier == null)
                            GUILayout.Label("<size=11><color=#9f9>✔ Fully upgraded — max tier reached.</color></size>", _small);
                        else
                        {
                            bool unlocked = wb != null ? wb.UpgradeUnlocked : pbSel.UpgradeUnlocked;
                            if (!unlocked)
                            {
                                string ageNm = upTier.unlockAge < Colony.AgeNames.Length ? Colony.AgeNames[upTier.unlockAge] : "a later age";
                                GUILayout.Label($"<size=11><color=#888>🔒 Next: <b>{upTier.name}</b> (×{upTier.speedMult:0.0} speed) — unlocks in the {ageNm}</color></size>", _small);
                            }
                            else
                            {
                                var carriedU = gatherer != null ? gatherer.Inventory : null;
                                bool afford = Economy.CanAfford(upTier.cost, carriedU);
                                string col = afford ? "#9f9" : "#f99";
                                if (GUILayout.Button($"<size=12><color=#9f9>⬆</color> <b>{upTier.name}</b> — ×{upTier.speedMult:0.0} speed   <color={col}>{AmountsText(upTier.cost)}</color></size>", _btn))
                                {
                                    bool ok = wb != null ? wb.TryUpgrade() : pbSel.TryUpgrade();
                                    Toast.Show(ok ? $"<color=#9f9>⬆ Upgraded to {upTier.name}!</color>" : "<color=#f99>Can't afford that upgrade yet.</color>");
                                }
                                if (!afford) GUILayout.Label("<size=10><color=#c98>(gather/produce the materials, then upgrade)</color></size>", _small);
                            }
                        }
                    }
                }

                // What this collector/workshop currently holds in its buffer.
                if (pbSel != null && pbSel.produces != null)
                    GUILayout.Label($"<size=12>Holds: {pbSel.produces.displayName} {pbSel.Buffer.Count(pbSel.produces)}/{pbSel.Buffer.capacity}   ·   Rate: {pbSel.RatePerMin:0.#}/min</size>", _small);
                else if (wb != null && wb.output != null)
                    GUILayout.Label($"<size=12>Holds: {wb.output.displayName} {wb.Buffer.Count(wb.output)}/{wb.Buffer.capacity}</size>", _small);

                // Configurable smelter (Basic/Advanced): show the current recipe + a switch button.
                if (wb != null && wb.HasRecipeChoice)
                {
                    var ins = new List<string>();
                    if (wb.inputs != null) foreach (var c in wb.inputs) if (c != null && c.item != null) ins.Add(c.item.displayName);
                    GUILayout.Label($"<size=12>Making: <b>{(wb.output != null ? wb.output.displayName : "?")}</b> <color=#888>from {string.Join(" + ", ins)}</color></size>", _small);
                    if (GUILayout.Button("<size=12>↻ Change recipe</size>", _btn)) wb.CycleRecipe();
                }
                // Powered machine: spell out the SPECIFIC power problem + how to fix it.
                if (wb != null && wb.RequiresPower)
                {
                    var ps = wb.PowerStatus;
                    if (ps == WorkshopBuilding.PowerState.Unwired)
                    {
                        GUILayout.Label("<size=12><color=#6cf>⚡ Not connected to the power grid</color> — select this machine (or a Power Pole) → <b>Connect wire</b> → click a Generator/Pole.</size>", _small);
                        // One-click hookup: wire straight to the nearest pole/generator/battery in reach.
                        var node = wb.GetComponent<PowerNode>();
                        if (node != null && GUILayout.Button("<size=12>⚡ Connect to nearest pole/generator</size>", _btn))
                        {
                            PowerNode best = null; float bestSq = float.MaxValue;
                            foreach (var n in PowerNode.All)
                            {
                                if (n == null || n == node || n.role == PowerNode.Role.Consumer || !n.CanLinkMore) continue;
                                float sq = (n.Pos - node.Pos).sqrMagnitude;
                                if (sq < bestSq) { bestSq = sq; best = n; }
                            }
                            if (best != null && node.Connect(best)) AudioManager.Click();
                            else Toast.Show("<color=#ffb24d>No pole/generator within wire reach — place a Power Pole closer, then try again.</color>");
                        }
                    }
                    else if (ps == WorkshopBuilding.PowerState.GridDead)
                        GUILayout.Label("<size=12><color=#c9f>⚡ Grid out of power</color> — it's wired, but the network generates nothing. Build/fuel a Generator, or add a Battery.</size>", _small);
                    else if (ps == WorkshopBuilding.PowerState.Brownout)
                        GUILayout.Label("<size=12><color=#fc6>⚡ Browning out — running slow</color> (demand exceeds generation). Add a Generator or a Battery to bring it to full speed.</size>", _small);
                }
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
            else if (blt != null)
            {
                if (blt.isFilter) DrawFilterBeltPanel(blt);
                else GUILayout.Label($"<size=12><color=#bbb>{(blt.item != null ? "Carrying " + blt.item.displayName : "Empty")}</color></size>", _small);
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

                // Personal BOAT — bought at a harbour so you can sail over water (WASD) and reach islands.
                if (dp.def != null && dp.def.isHarbour)
                {
                    if (PlayerController.HasBoat)
                        GUILayout.Label("<size=11><color=#6cf>⛵ Boat owned — walk onto the sea (WASD) to sail across to islands.</color></size>", _small);
                    else
                    {
                        var cb = gatherer != null ? gatherer.Inventory : null;
                        bool affordB = cb != null && Economy.Available(woodItem, cb) >= 12 && Economy.Available(stoneItem, cb) >= 8;
                        if (GUILayout.Button($"<size=12>⛵ Buy Boat  <color={(affordB ? "#9f9" : "#f99")}>(12 Wood, 8 Stone)</color></size>", _btn))
                        {
                            if (affordB)
                            {
                                Economy.SpendUpTo(woodItem, 12, cb); Economy.SpendUpTo(stoneItem, 8, cb);
                                PlayerController.HasBoat = true;
                                Toast.Show("<color=#9f9>⛵ Boat bought!</color> Walk onto the water (WASD) to sail — reach the island, then build a Harbour there.");
                            }
                            else Toast.Show("<color=#f99>Need 12 Wood + 8 Stone to build a boat.</color>");
                        }
                    }
                }

                if (dp.store.Total() == 0)
                {
                    if (GUILayout.Button($"<size=12>Handle: {acc} (change)</size>", _btn)) dp.CycleItem();
                }
                else GUILayout.Label("<size=11><color=#888>empty it to change type</color></size>", _small);

                // Lines serving this station — each listed with its own ✕ so you can delete a SPECIFIC line
                // (not just "the first"), plus its cargo, stop count and vehicle. The clearer line manager.
                RouteVehicle toDelete = null;
                int rcount = 0, idxR = 0;
                foreach (var rv in RouteVehicle.All)
                {
                    if (rv == null || !rv.Serves(dp)) continue;
                    rcount++; idxR++;
                    if (idxR > 5) continue; // cap visible rows
                    string ritem = (rv.a != null && rv.a.item != null) ? rv.a.item.displayName : "—";
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"<size=11><color=#9cf>● Line {idxR}</color> · {rv.StopCount} stops · cap {rv.capacity} · <color=#bbb>{ritem}</color></size>", _small);
                    if (GUILayout.Button("<size=11><color=#f99>✕</color></size>", _btn, GUILayout.Width(26))) toDelete = rv;
                    GUILayout.EndHorizontal();
                }
                if (rcount > 5) GUILayout.Label($"<size=10><color=#888>+{rcount - 5} more line(s)</color></size>", _small);
                if (toDelete != null) Destroy(toDelete.gameObject);
                if (rcount == 0) GUILayout.Label("<size=11><color=#888>No line yet. Lay TRACK to other Stations, then '+ Add line' → click each stop in visit order (you can revisit one to loop back) → RIGHT-CLICK to finish. The vehicle loops the stops, passing any it doesn't stop at. (Edit a line by deleting it with ✕ and re-adding.)</color></size>", _small);

                if (builder.LinkFrom != null && builder.LineContains(dp))
                    GUILayout.Label($"<size=11><color=#ffd24d>▶ Building a line ({builder.LineStopCount} stops)… click more Stations, then the FIRST station / right-click to finish.</color></size>", _small);
                else if (builder.LinkFrom != null)
                    GUILayout.Label("<size=11><color=#ffd24d>▶ Click this station to add it to the line.</color></size>", _small);
                else
                {
                    // Harbours run CARGO SHIPS; rail Stations run land vehicles — show the right tier + cost.
                    var tier = (dp.def != null && dp.def.isHarbour) ? builder.BestShipTier() : builder.BestRouteTier();
                    if (tier != null)
                    {
                        bool afford = builder.CanAfford(tier);
                        string cc = afford ? "#9f9" : "#f99";
                        string verb = (dp.def != null && dp.def.isHarbour) ? "+ Add ship line" : "+ Add line";
                        if (GUILayout.Button($"<size=12>{verb}  <color=#bbb>{tier.displayName}</color> <color={cc}>({CostList(tier.cost)})</color></size>", _btn))
                            builder.BeginStationLink(dp);
                    }
                    else GUILayout.Label("<size=11><color=#888>No transport unlocked yet</color></size>", _small);
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
            else if (cs != null)
            {
                if (!cs.MaterialsDone)
                    GUILayout.Label($"<size=15>Materials: {cs.deliveredUnits}/{cs.totalUnits}</size>", _small);
                else
                    GUILayout.Label($"<size=15>Building… {(int)(cs.BuildFraction * 100)}%</size>", _small);
            }
            else if (pwr != null)
            {
                // GENERATOR readout — status / power output / fuel buffer / consumption, so it's not a bare panel.
                if (pwr.renewable)
                {
                    string src = pwr.solar ? "daylight" : "wind";
                    GUILayout.Label($"<size=13><color=#9f9>● Generating</color>  <color=#bbb>(varies with {src})</color></size>", _small);
                    GUILayout.Label($"<size=12>⚡ Power: <b>{pwr.CurrentOutput:0}</b> / {pwr.output}  <color=#888>(now / rated)</color></size>", _small);
                    GUILayout.Label("<size=11><color=#bbb>Renewable — no fuel. Output rises and falls on its own, so lean on Batteries.</color></size>", _small);
                }
                else
                {
                    GUILayout.Label(pwr.Fueled
                        ? "<size=13><color=#9f9>● Running</color></size>"
                        : "<size=13><color=#f66>● No fuel — grid browning out</color></size>", _small);
                    GUILayout.Label($"<size=12>⚡ Power: <b>{pwr.CurrentOutput:0}</b> / {pwr.output}  <color=#888>(supplied / rated)</color></size>", _small);
                    if (pwr.fuel != null)
                    {
                        int cap = pwr.Buffer != null ? pwr.Buffer.capacity : 0;
                        float perMin = pwr.interval > 0f ? pwr.fuelPerCycle * 60f / pwr.interval : 0f;
                        string fcol = pwr.FuelStored <= 0 ? "#f66" : pwr.FuelStored < pwr.fuelPerCycle * 3 ? "#fd4" : "#9cf";
                        GUILayout.Label($"<size=12>Fuel: <color={fcol}>{pwr.fuel.displayName} {pwr.FuelStored}/{cap}</color></size>", _small);
                        GUILayout.Label($"<size=11><color=#bbb>Burns {pwr.fuelPerCycle} {pwr.fuel.displayName} every {pwr.interval:0.#}s  ≈ {perMin:0.#}/min</color></size>", _small);
                        if (pwr.FuelStored <= 0)
                            GUILayout.Label($"<size=11><color=#f66>⚠ Belt {pwr.fuel.displayName} into the cyan fuel edge (R aims it) to keep it running.</color></size>", _small);
                    }
                }
            }
            else if (bat != null)
            {
                // BATTERY readout — charge %, energy stored, and live charge/discharge flow.
                int pct = Mathf.RoundToInt(bat.Fraction * 100f);
                string fcol = bat.Fraction <= 0.05f ? "#f66" : bat.Fraction >= 0.95f ? "#9f9" : "#9cf";
                GUILayout.Label($"<size=13>Charge: <color={fcol}><b>{pct}%</b></color>  <color=#888>{bat.Stored:0}/{bat.capacity:0}</color></size>", _small);
                string flowStr = bat.Flow > 0.5f ? $"<color=#9f9>▲ charging {bat.Flow:0}</color>"
                    : bat.Flow < -0.5f ? $"<color=#fc8>▼ discharging {-bat.Flow:0}</color>"
                    : "<color=#888>idle</color>";
                GUILayout.Label($"<size=12>{flowStr}  <color=#888>· max {bat.rate:0}/s</color></size>", _small);
            }

            // Power wiring panel — shown for ANY wired node (generator / pole / battery / machine), so
            // it's additive to the branches above. Shows wire count + Connect/Disconnect controls.
            if (pnode != null) DrawWirePanel(pnode);

            GUILayout.EndScrollView(); // scrollable info above; Demolish/Close stay pinned + always visible below
            GUILayout.BeginHorizontal();
            demo = GUILayout.Button("Demolish", GUILayout.Height(28));
            close = GUILayout.Button("Close", GUILayout.Height(28));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            if (demo) builder.DemolishSelected();
            if (close) builder.Deselect();
        }

        // The mount GARAGE panel: buy age-gated mounts, switch which you ride, release one to free a slot.
        private void DrawGaragePanel()
        {
            int age = Colony.Instance != null ? Colony.Instance.Age : 0;
            var cb = gatherer != null ? gatherer.Inventory : null;
            int riding = PlayerController.RidingTier(age);
            GUILayout.Label($"<size=11><color=#bbb>Parking {PlayerController.OwnedCount()}/{PlayerController.GarageSlots}. Buy a mount up to your age, then ride it. On foot you still get a small per-age speed boost.</color></size>", _small);

            // On Foot row
            GUILayout.BeginHorizontal();
            GUILayout.Label("<size=13>On Foot</size>", _small);
            GUILayout.FlexibleSpace();
            if (riding == 0) GUILayout.Label("<size=11><color=#9f9>● riding</color></size>", _small);
            else if (GUILayout.Button("<size=11>Walk</size>", _btn, GUILayout.Width(60))) PlayerController.SetActive(0);
            GUILayout.EndHorizontal();

            for (int t = 1; t <= PlayerController.MountTierMax; t++)
            {
                string mname = PlayerController.Mounts[t].name;
                bool owned = PlayerController.OwnedMount[t];
                bool active = riding == t;
                bool ageOk = t <= PlayerController.MaxTierForAge(age);

                GUILayout.BeginHorizontal();
                GUILayout.Label($"<size=13>{mname}</size> <size=10><color=#888>{PlayerController.Mounts[t].speed:0.#} spd</color></size>", _small);
                GUILayout.FlexibleSpace();
                if (!ageOk)
                    GUILayout.Label("<size=10><color=#888>reach its age</color></size>", _small);
                else if (active)
                    GUILayout.Label("<size=11><color=#9f9>● riding</color></size>", _small);
                else if (owned)
                {
                    if (GUILayout.Button("<size=11>Ride</size>", _btn, GUILayout.Width(60))) PlayerController.SetActive(t);
                    if (GUILayout.Button("<size=10><color=#f99>✕</color></size>", _btn, GUILayout.Width(24))) PlayerController.Release(t);
                }
                else
                {
                    var cost = (PlayerController.MountCost != null && t < PlayerController.MountCost.Length) ? PlayerController.MountCost[t] : null;
                    bool slot = PlayerController.OwnedCount() < PlayerController.GarageSlots;
                    bool afford = cost != null && cb != null && Economy.CanAfford(cost, cb);
                    string cc = (afford && slot) ? "#9f9" : "#f99";
                    string label = !slot ? "Garage full" : $"Buy <color={cc}>({CostList(cost)})</color>";
                    if (GUILayout.Button($"<size=11>{label}</size>", _btn, GUILayout.Width(130)))
                    {
                        if (!slot) Toast.Show("<color=#f99>Garage full — release a parked mount (✕) or build another Garage.</color>");
                        else if (afford) { Economy.Spend(cost, cb); PlayerController.Buy(t); Toast.Show($"<color=#9f9>{mname} bought!</color> You're riding it now."); }
                        else Toast.Show($"<color=#f99>Need {CostList(cost)} for the {mname}.</color>");
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        // The wiring controls for a selected power node (generator / pole / battery / machine):
        // current wire count, battery charge, and Connect / Disconnect buttons.
        private static readonly List<ItemDefinition> _filterCandidates = new();
        // Filter-belt whitelist: toggle up to 5 items the belt will convey (empty = pass everything). The toggle
        // is APPLIED after rendering so the control count stays identical between the IMGUI Layout/Repaint passes.
        private void DrawFilterBeltPanel(Belt blt)
        {
            GUILayout.Label($"<size=12><b>Filter</b> — conveys only the picked items ({blt.filterItems.Count}/5)</size>", _small);
            if (blt.filterItems.Count == 0)
                GUILayout.Label("<size=11><color=#888>Nothing picked → passes everything. Tap items below.</color></size>", _small);
            _filterCandidates.Clear();
            foreach (var p in ProductionBuilding.All)
                if (p != null && p.produces != null && !p.produces.isLiquid && !_filterCandidates.Contains(p.produces)) _filterCandidates.Add(p.produces);
            foreach (var w in WorkshopBuilding.All)
                if (w != null && w.output != null && !w.output.isLiquid && !_filterCandidates.Contains(w.output)) _filterCandidates.Add(w.output);
            foreach (var f in blt.filterItems) if (f != null && !_filterCandidates.Contains(f)) _filterCandidates.Add(f);
            ItemDefinition toggle = null; bool clear = false;
            foreach (var it in _filterCandidates)
            {
                bool on = blt.filterItems.Contains(it);
                if (GUILayout.Button($"<size=12><color={(on ? "#9f9" : "#888")}>{(on ? "☑" : "☐")} {it.displayName}</color></size>", _btn)) toggle = it;
            }
            if (blt.filterItems.Count > 0 && GUILayout.Button("<size=11>Clear filter</size>", _btn)) clear = true;
            if (clear) blt.filterItems.Clear();
            else if (toggle != null)
            {
                if (blt.filterItems.Contains(toggle)) blt.filterItems.Remove(toggle);
                else if (blt.filterItems.Count < 5) blt.filterItems.Add(toggle);
                else Toast.Show("<color=#f99>A filter belt holds up to 5 items.</color>");
            }
        }

        private void DrawWirePanel(PowerNode node)
        {
            if (builder == null) return;
            GUILayout.Label($"<size=12><color=#9cf>🔌 Wires: {node.links.Count}/{node.maxConnections}</color></size>", _small);
            if (node.battery != null)
            {
                float frac = node.battery.Fraction;
                string fc = frac > 0.66f ? "#9f9" : frac > 0.25f ? "#fd4" : "#f99";
                string flow = node.battery.Flow > 0.1f ? "  <color=#9f9>▲ charging</color>"
                            : node.battery.Flow < -0.1f ? "  <color=#fc8>▼ discharging</color>" : "";
                GUILayout.Label($"<size=12>🔋 <color={fc}>{Mathf.RoundToInt(node.battery.Stored)}/{Mathf.RoundToInt(node.battery.capacity)}</color>{flow}</size>", _small);
            }
            if (builder.WireFrom == node)
                GUILayout.Label("<size=11><color=#ffd24d>▶ Now click the building to wire to… (Esc cancels)</color></size>", _small);
            else if (node.CanLinkMore)
            {
                if (GUILayout.Button("<size=12>🔌 Connect wire</size>", _btn)) builder.BeginWire(node);
            }
            else GUILayout.Label("<size=11><color=#888>All wire slots used — cut a wire to free a slot</color></size>", _small);
            // Delete a wire by CLICKING the cable in the world (you can see which is which) — not a list of identical rows.
            if (node.links.Count > 0)
            {
                if (builder.WireCutMode)
                    GUILayout.Label("<size=11><color=#fc8>✂ Click a wire in the world to cut it (Esc exits)</color></size>", _small);
                else if (GUILayout.Button("<size=12>✂ Cut a wire</size>", _btn)) builder.BeginWireCut();
                if (GUILayout.Button("<size=11>Disconnect ALL wires</size>", _btn))
                    for (int i = node.links.Count - 1; i >= 0; i--) node.Disconnect(node.links[i]);
            }
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

        // ---- Objectives box (top-right): the goals available RIGHT NOW (current age, any order) ----
        private void DrawObjectives()
        {
            var o = Objectives.Instance;
            if (o == null) { _objRect = default; return; }
            int age = Colony.Instance != null ? Colony.Instance.Age : 0;
            string ageName = Colony.AgeNames[Mathf.Clamp(age, 0, Colony.AgeNames.Length - 1)];

            // SEQUENTIAL: the current step (bright) + the one after it (faint "Up next"). One thing at a time.
            var steps = new List<Quest>();
            foreach (var q in o.ActivePending(2)) steps.Add(q);

            const float w = 320f;
            float inner = w - 24f;
            string header = $"<b>🎯 Objectives</b>   <size=11><color=#bbb>{ageName} · J for all</color></size>";
            var lines = new List<string>();
            if (steps.Count == 0) lines.Add("<size=13><color=#9f9>All objectives complete! 🎉</color></size>");
            else
            {
                string prog = "";
                if (steps[0].progress != null) { var (c, n) = steps[0].progress(); prog = $"   <color=#9cf>{Mathf.Clamp(c, 0, n)}/{n}</color>"; }
                lines.Add($"<size=14><color=#ffd24d>▸</color> <b>{steps[0].title}</b>{prog}"
                    + (string.IsNullOrEmpty(steps[0].rewardText) ? "" : $"  <size=11><color=#9c9>({steps[0].rewardText})</color></size>") + "</size>");
                if (steps.Count > 1) lines.Add($"<size=12><color=#999>Up next: {steps[1].title}</color></size>");
            }

            // Measure every line at the box's inner width and size the box to fit, so long titles that WRAP
            // are never clipped (the recurring text cut-off bug).
            float bodyH = _small.CalcHeight(new GUIContent(header), inner) + 4f;
            foreach (var l in lines) bodyH += _small.CalcHeight(new GUIContent(l), inner) + 3f;
            float h = bodyH + 16f;
            var rect = new Rect(_vw - w - 12f, 62f, w, h);
            _objRect = rect;
            PanelBg(rect);
            GUILayout.BeginArea(new Rect(rect.x + 12, rect.y + 8, inner, h - 14));
            GUILayout.Label(header, _small);
            GUILayout.Space(2f);
            foreach (var l in lines) GUILayout.Label(l, _small);
            GUILayout.EndArea();
        }

        /// <summary>Queue a centre-screen popup for the player's NEXT objective — called by Objectives at game
        /// start and at each Age. Shown one at a time; OK docks it back to the top-right box.</summary>
        public void ShowObjectiveReveal(Quest q)
        {
            if (q != null) _revealQueue.Add(q);
        }

        // The centre-screen objective REVEAL: ONE next-step objective, big in the middle to read, then "OK" docks
        // it to the top-right box. Waits behind the age card so they sequence; the OK click only flags a consume
        // (Update pops the queue) so the queue is stable across OnGUI passes. Height is MEASURED so it never clips.
        private void DrawObjectiveReveal()
        {
            if (_revealQueue.Count == 0 || _ageCardT > 0f) { _objRevealRect = default; return; }
            var q = _revealQueue[0];
            string ageName = Colony.AgeNames[Mathf.Clamp(q.age, 0, Colony.AgeNames.Length - 1)];

            float w = Mathf.Min(520f, _vw - 60f);
            float inner = w - 40f;
            string title = $"<size=20><color=#ffd24d><b>🎯 Objective — {ageName}</b></color></size>";
            string hint = "<size=12><color=#bbb>Your next step — do this, then the next appears. (full list: J)</color></size>";
            string body = $"<size=15><color=#ffd24d>▸</color> <b>{q.title}</b>"
                + (string.IsNullOrEmpty(q.rewardText) ? "" : $"   <color=#9c9>(reward: {q.rewardText})</color>") + "</size>";
            string okLabel = "<b>OK ✓</b>   <size=11>(or click this card)</size>";

            // Sum each element's WRAPPED height at the inner width + a comfortable margin → the box always fits.
            float th = _s.CalcHeight(new GUIContent(title), inner);
            float hh = _small.CalcHeight(new GUIContent(hint), inner);
            float bh = _small.CalcHeight(new GUIContent(body), inner);
            float btnH = _btn.CalcHeight(new GUIContent(okLabel), inner);
            float h = th + hh + bh + btnH + 52f; // generous fixed padding/gaps so nothing clips
            var r = new Rect(_vw / 2f - w / 2f, _vh * 0.32f, w, h);
            _objRevealRect = r;
            PanelBg(r);
            GUILayout.BeginArea(new Rect(r.x + 20, r.y + 14, inner, h - 24));
            GUILayout.Label(title, _s);
            GUILayout.Label(hint, _small);
            GUILayout.Space(4f);
            GUILayout.Label(body, _small);
            GUILayout.Space(6f);
            if (GUILayout.Button(okLabel, _btn)) _revealConsume = true;
            GUILayout.EndArea();

            var e = Event.current;
            if (e.type == EventType.MouseDown && r.Contains(e.mousePosition)) { _revealConsume = true; e.Use(); }
        }

        // ---- Objectives journal (J): every goal, grouped by Age, with status — multiple at once ----
        private void DrawObjectivesPanel()
        {
            var o = Objectives.Instance;
            float w = Mathf.Min(560f, _vw - 40f);
            float h = Mathf.Min(_vh - 120f, 600f);
            var r = new Rect(_vw / 2f - w / 2f, 70f, w, h);
            _objPanelRect = r;
            PanelBg(r);
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 14, r.width - 32, r.height - 28));
            int done = 0, total = 0;
            if (o != null) foreach (var q in o.quests) { total++; if (q.claimed) done++; }
            GUILayout.Label($"<size=20><b>🎯 Objectives</b></size>   <color=#9c9>{done}/{total} done</color>", _s);
            GUILayout.Label("<size=12><color=#bbb>One step at a time, in order — each goal unlocks the next, pulling you through the Ages. The ▸ marks what to do now.  (J or Close to exit)</color></size>", _small);
            GUILayout.Space(4);

            var current = o != null ? o.CurrentStep() : null;
            _objPanelScroll = GUILayout.BeginScrollView(_objPanelScroll);
            if (o != null)
            {
                int maxAge = 0;
                foreach (var q in o.quests) if (q.age > maxAge) maxAge = q.age;
                for (int a = 0; a <= maxAge; a++)
                {
                    var set = o.QuestsForAge(a);
                    if (set.Count == 0) continue;
                    string an = Colony.AgeNames[Mathf.Clamp(a, 0, Colony.AgeNames.Length - 1)];
                    GUILayout.Space(6);
                    GUILayout.Label($"<size=14><b><color=#cda>── {an} ──</color></b></size>", _small);
                    foreach (var q in set)
                    {
                        string badge, col, txtCol;
                        if (q.claimed) { badge = "✔"; col = "#9f9"; txtCol = "#9c9"; }                 // done
                        else if (q == current) { badge = "▸"; col = "#ffd24d"; txtCol = "#fff"; }      // do this now
                        else { badge = "○"; col = "#888"; txtCol = "#bbb"; }                            // upcoming
                        string reward = string.IsNullOrEmpty(q.rewardText) ? "" : $"  <size=11><color=#9c9>{q.rewardText}</color></size>";
                        GUILayout.Label($"<size=13><color={col}>{badge}</color> <color={txtCol}>{q.title}</color>{reward}</size>", _small);
                    }
                }
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("<size=12>Close (J)</size>", _btn)) _showObjectives = false;
            GUILayout.EndArea();
        }

        // ---- Toasts (centre) ----
        // A small cost readout that follows the cursor while dragging a belt/track stretch (set by BuildController);
        // the stretch's ghost also colours its unaffordable tail orange, so "looks buildable but isn't" is gone.
        private void DrawDragCost()
        {
            if (builder == null || string.IsNullOrEmpty(builder.DragCostLabel)) return;
            var content = new GUIContent($"<size=12>{builder.DragCostLabel}</size>");
            var m = Event.current.mousePosition;
            float w = Mathf.Min(380f, _small.CalcSize(content).x + 18f);
            float h = _small.CalcHeight(content, w - 16f) + 10f;
            var r = new Rect(Mathf.Clamp(m.x + 18f, 4f, _vw - w - 4f), Mathf.Clamp(m.y + 20f, 4f, _vh - h - 4f), w, h);
            PanelBg(r);
            GUI.Label(new Rect(r.x + 8, r.y + 5, r.width - 16, r.height - 10), content, _small);
        }

        private void DrawToasts()
        {
            // Constrained-width, word-wrapped, multi-line so long tips aren't cut off. Each toast's
            // height grows with its wrapped content so nothing clips and they don't overlap.
            float tw = Mathf.Min(900f, _vw - 40f);
            float x = (_vw - tw) / 2f;
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
            float w = Mathf.Min(900f, _vw - 24f);
            const float h = 44f;
            var r = new Rect(_vw / 2f - w / 2f, _vh - h - 6f, w, h); // fully on-screen (row 2 used to hang off the bottom)
            // Readability strip: the hints used to sit bare over the terrain and vanished on light ground.
            var prevCol = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(r.x - 10f, r.y - 4f, r.width + 20f, r.height + 8f), Texture2D.whiteTexture);
            GUI.color = prevCol;
            GUILayout.BeginArea(r);
            GUILayout.Label($"<size=13><color=#e8e8e8>B build · <color=#9cf>T research</color> · <color=#ffd24d>J goals</color> · <color=#9f9>P stats</color> · <color=#fa9>K alerts</color> · G guide · H help · M map · L lines · N minimap {(_showMinimap ? "on" : "off")} · Space pause</color></size>", _small);
            string sandbox = Economy.FreeBuild ? "<color=#9f9>SANDBOX</color> · " : "";
            string mode = Economy.LocalProduction ? "<color=#fc8>Local logistics</color>" : "<color=#8cf>Global pool</color>";
            GUILayout.Label($"<size=11><color=#cfcfcf>Q pipette · X demolish · {sandbox}Speed x{_speed:0} · {mode} (F7) · F1–F5 sandbox</color></size>", _small);
            GUILayout.EndArea();
        }

        // ---- Power Grid overview (bottom-left): live generation vs demand, battery, and how many machines
        //      are browning out — so an overloaded grid is FELT, not just guessed. Flashes red on overload. ----
        private void DrawPowerGridOverview()
        {
            if (!PowerNet.Active) { _powerRect = default; return; }
            PowerNet.EnsureFresh();
            float gen = PowerNet.TotalGen, dem = PowerNet.TotalDemand, stored = PowerNet.TotalStored, cap = PowerNet.TotalCapacity;
            if (gen <= 0.01f && dem <= 0.01f && cap <= 0.01f) { _powerRect = default; return; } // nothing wired yet
            bool overloaded = dem > gen + 0.01f;

            const float w = 246f;
            float h = cap > 0.01f ? 110f : 84f;
            var r = new Rect(12f, _vh - h - 46f, w, h);
            _powerRect = r;
            PanelBg(r);
            // Red top/bottom edges that pulse while the grid is overloaded — a felt warning.
            if (overloaded)
            {
                var rc = GUI.color;
                GUI.color = new Color(1f, 0.32f, 0.26f, 0.45f + 0.3f * Mathf.Sin(Time.unscaledTime * 6f));
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, 3f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(r.x, r.yMax - 3f, r.width, 3f), Texture2D.whiteTexture);
                GUI.color = rc;
            }

            float x = r.x + 12f, y = r.y + 8f, iw = w - 24f;
            GUI.Label(new Rect(x, y, iw, 20f), $"<b>⚡ Power Grid</b>{(overloaded ? "   <color=#f66>OVERLOADED</color>" : "")}", _small);
            y += 21f;
            GUI.Label(new Rect(x, y, iw, 18f), $"<size=12>Gen <b>{Mathf.RoundToInt(gen)}</b>   ·   Demand <b>{Mathf.RoundToInt(dem)}</b></size>", _small);
            y += 18f;
            float maxv = Mathf.Max(gen, dem, 1f);
            GuiBar(new Rect(x, y, iw, 8f), new Color(0.18f, 0.18f, 0.22f));                                                  // track
            GuiBar(new Rect(x, y, iw * Mathf.Clamp01(gen / maxv), 8f), overloaded ? new Color(0.9f, 0.42f, 0.3f) : new Color(0.4f, 0.85f, 0.46f)); // generation fill
            float dmx = x + iw * Mathf.Clamp01(dem / maxv);
            GuiBar(new Rect(dmx - 1f, y - 2f, 2f, 12f), Color.white);                                                        // demand marker
            y += 15f;
            if (cap > 0.01f)
            {
                float frac = Mathf.Clamp01(stored / cap);
                GUI.Label(new Rect(x, y, iw, 18f), $"<size=12>🔋 {Mathf.RoundToInt(stored)} / {Mathf.RoundToInt(cap)}</size>", _small);
                y += 18f;
                GuiBar(new Rect(x, y, iw, 7f), new Color(0.18f, 0.18f, 0.22f));
                GuiBar(new Rect(x, y, iw * frac, 7f), frac < 0.25f ? new Color(0.92f, 0.72f, 0.22f) : new Color(0.3f, 0.7f, 0.95f));
                y += 12f;
            }
            if (PowerNet.BrownoutMachines > 0)
                GUI.Label(new Rect(x, y, iw, 18f), $"<size=12><color=#fb6>⚠ {PowerNet.BrownoutMachines} machine{(PowerNet.BrownoutMachines == 1 ? "" : "s")} slowed (down to {Mathf.RoundToInt(PowerNet.WorstFactor * 100f)}%)</color></size>", _small);
            else
                GUI.Label(new Rect(x, y, iw, 18f), "<size=12><color=#7e7>all powered machines at full speed</color></size>", _small);
        }

        // A flat colour bar (track / fill), drawn with the white texture tinted via GUI.color.
        private static void GuiBar(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        // ---- Victory banner (shown once the Monument is completed) ----
        // ---- Research tree (T): spend research points on age advances + building unlocks ----
        // Research tree layout constants — a horizontal AGE SPINE (Stone→Tribal→…→Industrial) with each age's
        // optional unlocks BRANCHING below it, so the tree reads as connected branches instead of a flat list.
        private const float RTColW = 170f, RTNodeW = 150f, RTNodeH = 48f, RTRowPitch = 66f, RTIndent = 18f, RTTopY = 6f;

        private void DrawResearchPanel()
        {
            // A dedicated MODE, not a sidebar: a large centred panel over the dimmed world.
            float w = Mathf.Min(940f, _vw - 32f);
            float h = Mathf.Min(_vh - 70f, 660f);
            var r = new Rect(_vw / 2f - w / 2f, 56f, w, h);
            _researchRect = r;
            PanelBg(r);

            _techNode ??= new GUIStyle(GUI.skin.button) { richText = true, fontSize = 12, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            const float pad = 16f;

            // ---- Header: title, points, current goal, legend ----
            GUI.Label(new Rect(r.x + pad, r.y + 8, r.width - 2 * pad, 26),
                $"<size=20><b>🔬 Research Tree</b></size>   <color=#9cf><b>{Research.Points} pts</b></color>", _s);
            var goalTech = Research.NextAgeTech; var goalTier = Research.CurrentTier;
            string goal;
            if (goalTech != null)
            {
                int filled = Mathf.RoundToInt(Research.Fraction * 14f);
                string bar = new string('█', Mathf.Clamp(filled, 0, 14)) + new string('░', Mathf.Clamp(14 - filled, 0, 14));
                goal = $"<color=#ffd24d>⭐ Goal: <b>{goalTech.name}</b></color>  <color=#9cf>[{bar}]</color> {Research.Points}/{goalTech.cost}";
                if (goalTier != null && goalTier.item != null)
                    goal += $"   <color=#bcd>· craft <b>{goalTier.item.displayName}</b> → a Research Lodge ({goalTier.pointsPerItem} pt)</color>";
            }
            else goal = "<color=#9f9>⭐ All ages researched — spend leftover points on the upgrades below.</color>";
            GUI.Label(new Rect(r.x + pad, r.y + 36, r.width - 2 * pad, 20), $"<size=12>{goal}</size>", _small);
            GUI.Label(new Rect(r.x + pad, r.y + 56, r.width - 2 * pad, 18),
                "<size=11><color=#9f9>✔ researched</color>   <color=#8cf>✦ ready</color>   <color=#fc8>needs a building/item</color>   <color=#f99>short on points</color>   <color=#888>🔒 locked</color>   ·  hover a node for detail</size>", _small);

            // ---- Build the column/row layout from the tree (each Tech has ≤1 prereq → a clean forest) ----
            var tree = Research.Tree;
            int maxCol = 1;
            if (tree != null) foreach (var n in tree) if (n != null) maxCol = Mathf.Max(maxCol, ColOf(n));
            var backbone = new Research.Tech[maxCol + 1];   // the age node that heads each column (null at col 0 = Stone)
            var branchList = new List<Research.Tech>[maxCol + 1];
            for (int i = 0; i <= maxCol; i++) branchList[i] = new List<Research.Tech>();
            if (tree != null)
                foreach (var n in tree)
                {
                    if (n == null) continue;
                    if (n.advanceToAge >= 0 && n.advanceToAge <= maxCol) backbone[n.advanceToAge] = n; // age spine
                    else branchList[ColOf(n)].Add(n);                                                  // optional branch
                }

            float contentW = maxCol * RTColW + RTNodeW + RTIndent + 24f;
            int maxRows = 1;
            for (int c = 0; c <= maxCol; c++) maxRows = Mathf.Max(maxRows, 1 + branchList[c].Count);
            float contentH = RTTopY + maxRows * RTRowPitch + 8f;

            var view = new Rect(r.x + pad, r.y + 80, r.width - 2 * pad, r.height - 80 - 78);
            var content = new Rect(0, 0, Mathf.Max(contentW, view.width - 16f), Mathf.Max(contentH, view.height - 4f));
            _researchScroll = GUI.BeginScrollView(view, _researchScroll, content);

            // Faint band behind the age column you're currently IN, so the tree orients "you are here".
            int curAge = Colony.Instance != null ? Mathf.Clamp(Colony.Instance.Age, 0, maxCol) : 0;
            var bandC = GUI.color; GUI.color = new Color(0.95f, 0.82f, 0.35f, 0.09f);
            GUI.DrawTexture(new Rect(curAge * RTColW - 6f, 0f, RTNodeW + 12f, content.height), Texture2D.whiteTexture);
            GUI.color = bandC;

            var spineCol = new Color(0.95f, 0.82f, 0.35f, 0.55f); // amber age spine
            var branchCol = new Color(0.50f, 0.70f, 0.95f, 0.45f); // blue branch lines
            Rect stone = NodeRect(0, 0, true);

            // Connectors first (behind the nodes): horizontal age spine + vertical branch buses.
            for (int c = 1; c <= maxCol; c++)
            {
                if (backbone[c] == null) continue;
                Rect a = backbone[c - 1] != null ? NodeRect(c - 1, 0, true) : stone;
                Rect b = NodeRect(c, 0, true);
                HLine(a.xMax, b.xMin, a.center.y, spineCol);
            }
            for (int c = 0; c <= maxCol; c++)
            {
                if (branchList[c].Count == 0) continue;
                Rect head = backbone[c] != null ? NodeRect(c, 0, true) : stone;
                float busX = head.xMin + 11f;
                Rect last = NodeRect(c, branchList[c].Count, false);
                VLine(busX, head.yMax, last.center.y, branchCol);
                for (int k = 0; k < branchList[c].Count; k++)
                { Rect up = NodeRect(c, k + 1, false); HLine(busX, up.xMin, up.center.y, branchCol); }
            }

            // Nodes: the synthetic Stone start marker, then each column's spine node + its branches.
            var bc = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.30f, 0.34f, 0.24f);
            GUI.Box(stone, new GUIContent("<b>Stone Age</b>\n<size=10><color=#9f9>✔ you start here</color></size>"), _techNode);
            GUI.backgroundColor = bc;
            for (int c = 0; c <= maxCol; c++)
            {
                if (backbone[c] != null) DrawTechNode(backbone[c], NodeRect(c, 0, true), true);
                for (int k = 0; k < branchList[c].Count; k++) DrawTechNode(branchList[c][k], NodeRect(c, k + 1, false), false);
            }

            GUI.EndScrollView();

            // Hover detail (the node's description) + close.
            if (!string.IsNullOrEmpty(GUI.tooltip))
                GUI.Label(new Rect(r.x + pad, r.y + r.height - 74, r.width - 2 * pad, 42), $"<size=12><color=#cde>{GUI.tooltip}</color></size>", _small);
            if (GUI.Button(new Rect(r.x + r.width / 2f - 60, r.y + r.height - 30, 120, 24), "<size=12>Close (T)</size>", _btn)) _showResearch = false;
        }

        // Which column a node sits in: an age node uses its own target age; an optional unlock uses the target
        // age of its nearest AGE ancestor (walking prereqs), so it hangs under the age that unlocks it. Col 0 = Stone.
        private int ColOf(Research.Tech n)
        {
            if (n == null) return 0;
            if (n.advanceToAge >= 0) return n.advanceToAge;
            var p = Research.Node(n.prereq); int guard = 0;
            while (p != null && p.advanceToAge < 0 && guard++ < 32) p = Research.Node(p.prereq);
            return p != null ? p.advanceToAge : 0;
        }

        // Node rectangle in scroll-content space. Spine (age) nodes head their column; branch nodes are indented.
        private Rect NodeRect(int col, int row, bool spine)
        {
            float x = col * RTColW + (spine ? 0f : RTIndent);
            float y = RTTopY + row * RTRowPitch;
            return new Rect(x, y, spine ? RTNodeW : RTNodeW - RTIndent, RTNodeH);
        }

        private void DrawTechNode(Research.Tech n, Rect rect, bool spine)
        {
            Color bg; string status; int state; // 0 done · 1 ready · 2 needs build/item · 3 short points · 4 locked
            if (n.purchased) { bg = new Color(0.15f, 0.33f, 0.20f); status = "<color=#9f9>✔ researched</color>"; state = 0; }
            else if (!Research.PrereqMet(n))
            { var pre = Research.Node(n.prereq); bg = new Color(0.13f, 0.14f, 0.17f); status = $"<color=#888>🔒 {n.cost} · needs {(pre != null ? pre.name : n.prereq)}</color>"; state = 4; }
            else if (!Research.RequirementsMet(n))
            { bg = new Color(0.32f, 0.23f, 0.11f); status = $"<color=#fc8>{n.cost} · {Research.MissingRequirementsText(n)}</color>"; state = 2; }
            else if (Research.CanBuy(n))
            { bg = new Color(0.16f, 0.30f, 0.48f); status = $"<color=#9f9>✦ {n.cost} pts — research</color>"; state = 1; }
            else { bg = new Color(0.17f, 0.19f, 0.23f); status = $"<color=#f99>{n.cost} pts</color> <color=#888>(+{n.cost - Research.Points})</color>"; state = 3; }

            // Hover detail = the tech's description plus what it unlocks, so you know what you're buying.
            string tip = n.desc ?? "";
            if (n.unlocks != null && n.unlocks.Count > 0)
            {
                var names = new List<string>();
                foreach (var u in n.unlocks) if (u != null && !string.IsNullOrEmpty(u.displayName)) names.Add(u.displayName);
                if (names.Count > 0) tip = (tip.Length > 0 ? tip + "\n" : "") + $"<color=#8fe0a0>Unlocks: {string.Join(", ", names)}</color>";
            }
            var content = new GUIContent($"<b>{n.name}</b>\n<size=10>{status}</size>", tip);
            var bc = GUI.backgroundColor;
            GUI.backgroundColor = bg;
            bool clicked = GUI.Button(rect, content, _techNode);
            GUI.backgroundColor = bc;
            if (spine) // amber cap stripe marks the age spine
            {
                var cc = GUI.color; GUI.color = new Color(0.95f, 0.82f, 0.35f, 0.95f);
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), Texture2D.whiteTexture); GUI.color = cc;
            }
            if (clicked)
            {
                if (state == 1) Research.Buy(n);
                else if (state == 4) Toast.Show($"<color=#9cf>🔒 {n.name}</color> needs <b>{(Research.Node(n.prereq)?.name ?? n.prereq)}</b> first.");
                else if (state == 2) Toast.Show($"<color=#ffb24d>🔒 {n.name}:</color> {Research.MissingRequirementsText(n)}.");
                else if (state == 3) Toast.Show($"<color=#f99>{n.name}</color> needs <b>{n.cost - Research.Points}</b> more research pts.");
            }
        }

        // A horizontal / vertical connector line (a tinted 3px strip) drawn during Repaint.
        private void HLine(float x0, float x1, float y, Color col)
        {
            if (x1 < x0) { var t = x0; x0 = x1; x1 = t; }
            var c = GUI.color; GUI.color = col;
            GUI.DrawTexture(new Rect(x0, y - 1.5f, x1 - x0, 3f), Texture2D.whiteTexture); GUI.color = c;
        }
        private void VLine(float x, float y0, float y1, Color col)
        {
            if (y1 < y0) { var t = y0; y0 = y1; y1 = t; }
            var c = GUI.color; GUI.color = col;
            GUI.DrawTexture(new Rect(x - 1.5f, y0, 3f, y1 - y0), Texture2D.whiteTexture); GUI.color = c;
        }

        private void DrawWin()
        {
            var r = new Rect(_vw / 2f - 240, 150f, 480, 140);
            PanelBg(r);
            int peak = Colony.Instance != null ? Colony.Instance.PeakProsperity : 0;
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 14, r.width - 32, r.height - 28));
            GUILayout.Label("<color=#ffd24d><b>🏆 YOU WIN!</b></color>", _big);
            GUILayout.Label($"<size=16>You built a self-running factory.\nPeak Industry: <b>{peak}</b>   ·   <color=#bbb>keep playing if you like</color></size>", _toast);
            GUILayout.EndArea();
        }

        // ---- In-game Guide (G): mechanics + a resource reference ----
        // The global transport-line overview (L): every running line at a glance — its vehicle + consist,
        // how full it is, what it's carrying right now, and the stops it serves (with each stop's commodity).
        private void DrawLinesPanel()
        {
            float w = 660f, h = Mathf.Min(_vh - 70f, 600f);
            var r = new Rect(_vw / 2f - w / 2f, _vh / 2f - h / 2f, w, h);
            _linesRect = r;
            PanelBg(r);
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 12, r.width - 32, r.height - 24));
            int count = 0; foreach (var rv in RouteVehicle.All) if (rv != null) count++;
            GUILayout.Label($"<size=22><b>Transport lines</b></size>   <size=13><color=#bbb>({count} running · L to close)</color></size>", _s);
            _linesScroll = GUILayout.BeginScrollView(_linesScroll);

            if (count == 0)
                GUILayout.Label("<size=14><color=#cfcfcf>No lines yet. Build two Stations (or Harbours), then use <b>+ Add line</b> on a Station to link them. A loco hauls a CONSIST of wagons — one commodity per wagon — so a single line can carry MIXED cargo, and the wagon count grows as you age up.</color></size>", _small);

            foreach (var rv in RouteVehicle.All)
            {
                if (rv == null || rv.stops == null) continue;
                float frac = rv.LoadCapacity > 0 ? (float)rv.CurrentLoad / rv.LoadCapacity : 0f;
                string holds = rv.IsShip ? "1 hold" : $"{rv.WagonCount} wagon{(rv.WagonCount == 1 ? "" : "s")}";
                GUILayout.Label($"<size=16><b>{rv.VehicleName()}</b></size>  <size=12><color=#9cf>{holds} · {rv.StopCount} stops · {Mathf.RoundToInt(frac * 100f)}% full ({rv.CurrentLoad}/{rv.LoadCapacity})</color></size>", _small);
                GUILayout.Label($"<size=13>Cargo: {rv.CargoSummary()}</size>", _small);
                string lineWarn = rv.LineWarning();
                if (!string.IsNullOrEmpty(lineWarn)) GUILayout.Label($"<size=12><color=#ffb24d>⚠ {lineWarn}</color></size>", _small);

                GUILayout.Label("<size=12><color=#cfcfcf>Stops</color>  <size=10><color=#888>(click a mode to make a stop pickup-only or drop-only)</color></size></size>", _small);
                foreach (var st in rv.stops)
                {
                    if (st == null) continue;
                    GUILayout.BeginHorizontal();
                    string nm = st.def != null ? st.def.displayName : "Station";
                    string itxt = st.item != null ? $"<color=#{ColorUtility.ToHtmlStringRGB(st.item.color)}>[{st.item.displayName}]</color>" : "<color=#f99>[unset]</color>";
                    GUILayout.Label($"<size=12>{nm} {itxt}</size>", _small, GUILayout.Width(160));
                    int m = rv.StopModeOf(st);
                    string mlab = m == 1 ? "<color=#9f9>⬆ Load only</color>" : m == 2 ? "<color=#9cf>⬇ Unload only</color>" : "<color=#dfd>⇅ Load + Unload</color>";
                    if (GUILayout.Button($"<size=11>{mlab}</size>", _btn, GUILayout.Width(132))) rv.CycleStopMode(st);
                    GUILayout.EndHorizontal();
                }
                GUILayout.Space(8);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // P — per-item production vs consumption (per minute, ~4s smoothing) + what's in store. THE
        // factory-game decision screen: a red net on an item says exactly where to expand next.
        private void DrawStatsPanel()
        {
            float w = 560f, h = Mathf.Min(_vh - 70f, 620f);
            var r = new Rect(_vw / 2f - w / 2f, _vh / 2f - h / 2f, w, h);
            PanelBg(r);
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 12, r.width - 32, r.height - 24));
            GUILayout.Label("<size=22><b>Production</b></size>   <size=13><color=#bbb>(per minute, ~4s window · P to close)</color></size>", _s);

            var prod = new Dictionary<ItemDefinition, float>();
            foreach (var p in ProductionBuilding.All) if (p != null && p.produces != null && p.RatePerMin > 0.01f) { prod.TryGetValue(p.produces, out var v); prod[p.produces] = v + p.RatePerMin; }
            foreach (var wq in WorkshopBuilding.All) if (wq != null && wq.output != null && wq.RatePerMin > 0.01f) { prod.TryGetValue(wq.output, out var v); prod[wq.output] = v + wq.RatePerMin; }
            var cons = ProductionStats.ConsumedPerMin;
            var keys = new List<ItemDefinition>(prod.Keys);
            foreach (var kv in cons) if (kv.Value > 0.01f && !keys.Contains(kv.Key)) keys.Add(kv.Key);
            keys.Sort((a, b) => string.Compare(a.displayName, b.displayName, System.StringComparison.Ordinal));

            GUILayout.BeginHorizontal();
            GUILayout.Label("<size=12><color=#bbb>Item</color></size>", _small, GUILayout.Width(160));
            GUILayout.Label("<size=12><color=#9f9>+ /min</color></size>", _small, GUILayout.Width(75));
            GUILayout.Label("<size=12><color=#f99>− /min</color></size>", _small, GUILayout.Width(75));
            GUILayout.Label("<size=12><color=#bbb>net</color></size>", _small, GUILayout.Width(75));
            GUILayout.Label("<size=12><color=#bbb>stored</color></size>", _small, GUILayout.Width(75));
            GUILayout.EndHorizontal();

            _statsScroll = GUILayout.BeginScrollView(_statsScroll);
            if (keys.Count == 0)
                GUILayout.Label("<size=13><color=#cfcfcf>Nothing measured yet — rates appear as machines run.</color></size>", _small);
            foreach (var it in keys)
            {
                prod.TryGetValue(it, out float pr);
                float cr = cons.TryGetValue(it, out var cv) ? cv : 0f;
                float net = pr - cr;
                int stored = _totals != null && _totals.TryGetValue(it, out var sv) ? sv : 0;
                string netCol = net > 0.05f ? "9f9" : net < -0.05f ? "f99" : "ddd";
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<size=12><color=#{ColorUtility.ToHtmlStringRGB(it.color)}>{it.displayName}</color></size>", _small, GUILayout.Width(160));
                GUILayout.Label($"<size=12>{pr:0.#}</size>", _small, GUILayout.Width(75));
                GUILayout.Label($"<size=12>{cr:0.#}</size>", _small, GUILayout.Width(75));
                GUILayout.Label($"<size=12><color=#{netCol}>{net:+0.#;-0.#;0}</color></size>", _small, GUILayout.Width(75));
                GUILayout.Label($"<size=12>{stored}</size>", _small, GUILayout.Width(75));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // K — every building with a live problem, nearest first, with a compass bearing from the
        // player. The base-wide answer to "something's amber somewhere" without walking the map.
        private void DrawAlertsPanel()
        {
            float w = 560f, h = Mathf.Min(_vh - 70f, 620f);
            var r = new Rect(_vw / 2f - w / 2f, _vh / 2f - h / 2f, w, h);
            PanelBg(r);
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 12, r.width - 32, r.height - 24));

            var rows = new List<(float dist, string text)>();
            Vector3 me = gatherer != null ? gatherer.transform.position : Vector3.zero;
            void Add(Component c, string nm, string what, string col)
            {
                Vector3 d = c.transform.position - me;
                string dir = CompassDir(d);
                rows.Add((d.magnitude, $"<size=13><color=#{col}>●</color> <b>{nm}</b> — {what}  <color=#888>({d.magnitude:0}m {dir})</color></size>"));
            }
            foreach (var p in ProductionBuilding.All)
            {
                if (p == null || p.def == null) continue;
                var sc = p.StatusColor;
                if (sc == Status.Starved) Add(p, p.def.displayName, "out of resource — patch ran dry", "f66");
                else if (sc == Status.BackedUp) Add(p, p.def.displayName, "output full — belt it away", "fc6");
            }
            foreach (var wq in WorkshopBuilding.All)
            {
                if (wq == null || wq.def == null) continue;
                if (wq.NoPower) { Add(wq, wq.def.displayName, "NO POWER — wire it to a grid", "f44"); continue; }
                var sc = wq.StatusColor;
                if (sc == Status.Waiting) Add(wq, wq.def.displayName, $"waiting on inputs ({wq.MissingText()})", "fa6");
                else if (sc == Status.BackedUp) Add(wq, wq.def.displayName, "output full — belt it away", "fc6");
            }
            foreach (var pp in PowerPlant.All)
                if (pp != null && pp.def != null && !pp.renewable && pp.fuel != null && !pp.Fueled)
                    Add(pp, pp.def.displayName, $"out of fuel — belt {pp.fuel.displayName} into its intake", "f66");

            rows.Sort((a, b) => a.dist.CompareTo(b.dist));
            GUILayout.Label($"<size=22><b>Alerts</b></size>   <size=13><color=#bbb>({rows.Count} issue{(rows.Count == 1 ? "" : "s")} · nearest first · K to close)</color></size>", _s);
            _alertsScroll = GUILayout.BeginScrollView(_alertsScroll);
            if (rows.Count == 0)
                GUILayout.Label("<size=14><color=#9f9>All clear — every machine is running (or calmly idle).</color></size>", _small);
            int shown = 0;
            foreach (var (dist, text) in rows) { GUILayout.Label(text, _small); if (++shown >= 60) break; }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // Whitelist picker for a selected FILTER belt: tick up to 5 item types it passes.
        private void DrawFilterPicker(Belt blt)
            => DrawFilterList(blt.filterItems, "FILTER — passes only the ticked items (max 5):",
                              "nothing ticked = adopts the first item that arrives");

        // Shared whitelist picker (filter belts + Gantry crane arms): tick up to 5 of the items the
        // factory currently produces.
        private void DrawFilterList(List<ItemDefinition> filterItems, string caption, string emptyHint = "nothing ticked = no filter")
        {
            GUILayout.Label($"<size=12><color=#9cf>{caption}</color></size>", _small);
            var cands = new List<ItemDefinition>();
            void AddC(ItemDefinition i) { if (i != null && !i.isLiquid && !cands.Contains(i)) cands.Add(i); }
            foreach (var p in ProductionBuilding.All) if (p != null) AddC(p.produces);
            foreach (var wq in WorkshopBuilding.All) if (wq != null) AddC(wq.output);
            cands.Sort((a, b) => string.Compare(a.displayName, b.displayName, System.StringComparison.Ordinal));
            int col = 0;
            GUILayout.BeginHorizontal();
            foreach (var it in cands)
            {
                bool on = filterItems.Contains(it);
                if (GUILayout.Button($"<size=11>{(on ? "☑" : "☐")} {it.displayName}</size>", _btn, GUILayout.Width(118)))
                {
                    if (on) filterItems.Remove(it);
                    else if (filterItems.Count < 5) filterItems.Add(it);
                }
                if (++col % 2 == 0) { GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); }
            }
            GUILayout.EndHorizontal();
            if (filterItems.Count == 0)
                GUILayout.Label($"<size=11><color=#888>{emptyHint}</color></size>", _small);
        }

        private static string CompassDir(Vector3 d)
        {
            if (d.sqrMagnitude < 1f) return "here";
            float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg; // 0 = east
            string[] dirs = { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };
            return dirs[Mathf.RoundToInt(((a + 360f) % 360f) / 45f) % 8];
        }

        private void DrawGuide()
        {
            float w = 640f, h = Mathf.Min(_vh - 70f, 580f);
            var r = new Rect(_vw / 2f - w / 2f, _vh / 2f - h / 2f, w, h);
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
            var r = new Rect(_vw / 2f - 240, _vh / 2f - 225, 500, 460);
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
                "• A train hauls a CONSIST of wagons (one commodity each) — so a line carries MIXED cargo, and " +
                "the wagon count grows as you age up. Liquids ride in tankers between PIPE-fed stations. " +
                "Press <b>L</b> for the line overview.\n" +
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
            GUILayout.Label("<size=15>Press <b>H</b> to close  ·  Press <b>G</b> for the full Guide (mechanics + every resource)  ·  <b>M</b> opens the world map  ·  <b>L</b> lists transport lines  ·  <b>N</b> hides the minimap.</size>", _small);
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
                if (s != null && s.accepts != null && s.accepts.id == itemId) return true;
            return false;
        }

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
