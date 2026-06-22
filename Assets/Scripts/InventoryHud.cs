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
        public ItemDefinition woodItem, stoneItem, foodItem;

        /// <summary>True when the cursor is over an interactive HUD panel.</summary>
        public static bool PointerOverUI { get; private set; }

        private GUIStyle _s, _small, _big;
        private bool _paused;
        private bool _showHelp;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame)
            {
                _paused = !_paused;
                Time.timeScale = _paused ? 0f : 1f;
            }
            if (kb.hKey.wasPressedThisFrame) _showHelp = !_showHelp;
        }

        void OnDisable() => Time.timeScale = 1f;

        void OnGUI()
        {
            if (gatherer == null) return;
            _s ??= new GUIStyle(GUI.skin.label) { fontSize = 20, richText = true };
            _small ??= new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true };
            _big ??= new GUIStyle(GUI.skin.label) { fontSize = 40, richText = true, alignment = TextAnchor.MiddleCenter };

            DrawStatus();
            DrawObjective();
            if (builder != null) DrawBuildBarOrPlacement();
            DrawSelectedPanel();      // also sets PointerOverUI
            DrawFooter();
            if (_showHelp) DrawHelp();
            if (_paused) GUI.Label(new Rect(0, 60, Screen.width, 60), "<b>PAUSED</b>  <size=18>(space)</size>", _big);
        }

        // ---- Top-left: status ----
        private void DrawStatus()
        {
            GUILayout.BeginArea(new Rect(12, 10, 520, 120));
            var c = Colony.Instance;
            if (c != null)
            {
                string starve = c.Starving ? "   <color=#f55>STARVING</color>" : "";
                GUILayout.Label($"<b>People</b> {c.Population}/{c.Capacity}    <b>Free</b> {c.FreeWorkers}{starve}", _s);
            }
            var totals = Economy.Totals(gatherer.Inventory);
            GUILayout.Label(ResLine(totals), _s);
            GUILayout.EndArea();
        }

        private string ResLine(Dictionary<ItemDefinition, int> totals)
        {
            int Get(ItemDefinition i) { if (i == null) return 0; totals.TryGetValue(i, out int v); return v; }
            return $"<b>Wood</b> {Get(woodItem)}   <b>Stone</b> {Get(stoneItem)}   <b>Food</b> {Get(foodItem)}";
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
            if (!HasCollector("food") && wood < 4) return "Click the green trees to gather Wood.";
            if (!HasCollector("food")) return "Press 3, then click near the bushes (bottom) to place a Forager Hut — food keeps people alive.";
            if (!HasCollector("wood")) return "Press 1, then click near the trees to place a Wood Hut.";
            if (!HasStorage("wood")) return "Press 4 to place a Wood Warehouse (collects wood from anywhere).";
            if (HousingCount() <= 1) return "Press 7 to build a House so your population can grow.";
            return "Settlement running! Click a building to manage its workers; expand at will.";
        }

        // ---- Build bar / placement (bottom-left) ----
        private void DrawBuildBarOrPlacement()
        {
            float h = builder.PendingIndex >= 0 ? 60 : (builder.buildables.Count * 20 + 34);
            GUILayout.BeginArea(new Rect(12, Screen.height - h - 8, 420, h));

            if (builder.PendingIndex >= 0)
            {
                var def = builder.buildables[builder.PendingIndex];
                string ok = builder.PlacementValid ? "<color=#9f9>click to place</color>" : "<color=#f99>move to a valid spot</color>";
                GUILayout.Label($"<b>Placing {def.displayName}</b> — {ok}", _s);
                GUILayout.Label("<size=15>right-click / Esc to cancel</size>", _s);
            }
            else
            {
                GUILayout.Label("<b>Build</b>  <size=14>(press number, then click)</size>", _small);
                for (int i = 0; i < builder.buildables.Count; i++)
                {
                    var def = builder.buildables[i];
                    if (def == null) continue;
                    string col = builder.CanAfford(def) ? "#9f9" : "#f99";
                    GUILayout.Label($"[{i + 1}] {def.displayName}  <color={col}>{CostText(def)}</color>", _small);
                }
            }
            GUILayout.EndArea();
        }

        // ---- Selected building manage panel (bottom-right) ----
        private void DrawSelectedPanel()
        {
            var sel = builder != null ? builder.Selected : null;
            if (sel == null) { PointerOverUI = false; return; }

            var rect = new Rect(Screen.width - 290, Screen.height - 200, 278, 188);
            if (Event.current.type == EventType.Repaint)
                PointerOverUI = rect.Contains(Event.current.mousePosition);

            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 12, rect.y + 10, rect.width - 24, rect.height - 20));

            var pb = sel.GetComponent<ProductionBuilding>();
            var sb = sel.GetComponent<StorageBuilding>();
            var hb = sel.GetComponent<HousingBuilding>();
            string name = pb != null ? pb.def.displayName : sb != null ? sb.def.displayName : hb != null ? hb.def.displayName : "Building";
            GUILayout.Label($"<b>{name}</b>", _s);

            bool add = false, rem = false, demo, close;
            if (pb != null)
            {
                GUILayout.Label($"Workers: {pb.AssignedWorkers}/{pb.maxWorkers}    <size=14>(free: {(Colony.Instance != null ? Colony.Instance.FreeWorkers : 0)})</size>", _small);
                GUILayout.BeginHorizontal();
                rem = GUILayout.Button("- worker", GUILayout.Height(30));
                add = GUILayout.Button("+ worker", GUILayout.Height(30));
                GUILayout.EndHorizontal();
            }
            else if (sb != null)
            {
                GUILayout.Label($"<size=15>Stores {sb.accepts.displayName}: {sb.Store.Total()}/{sb.def.capacity}</size>", _small);
            }
            else if (hb != null)
            {
                GUILayout.Label($"<size=15>Houses {hb.houseCapacity} people</size>", _small);
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            demo = GUILayout.Button("Demolish", GUILayout.Height(28));
            close = GUILayout.Button("Close", GUILayout.Height(28));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            if (add) builder.AssignSelected();
            if (rem) builder.UnassignSelected();
            if (demo) builder.DemolishSelected();
            if (close) builder.Deselect();
        }

        private void DrawFooter()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 260, 10, 250, 28));
            GUILayout.Label("<size=15>Space pause  ·  H help</size>", _small);
            GUILayout.EndArea();
        }

        private void DrawHelp()
        {
            var r = new Rect(Screen.width / 2f - 230, Screen.height / 2f - 150, 460, 300);
            GUI.Box(r, GUIContent.none);
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 12, r.width - 32, r.height - 24));
            GUILayout.Label("<b>How to play</b>", _s);
            GUILayout.Label("<size=15>" +
                "• WASD / arrows to move.\n" +
                "• Left-click a tree/rock/bush to gather by hand.\n" +
                "• Press a number, then click, to place a building.\n" +
                "• Collectors need WORKERS — each is one person.\n" +
                "• Click a building to manage it (add/remove workers, demolish).\n" +
                "• People need Food and Housing. Build foragers + houses.\n" +
                "• Space = pause.  Esc = cancel/deselect.  X = demolish selected.\n" +
                "</size>", _small);
            GUILayout.Label("<size=15>Press H to close.</size>", _small);
            GUILayout.EndArea();
        }

        // ---- helpers ----
        private int Avail(ItemDefinition i) => Economy.Available(i, gatherer.Inventory);

        private static bool HasCollector(string itemId)
        {
            foreach (var p in FindObjectsByType<ProductionBuilding>())
                if (p.produces != null && p.produces.id == itemId) return true;
            return false;
        }

        private static bool HasStorage(string itemId)
        {
            foreach (var s in FindObjectsByType<StorageBuilding>())
                if (s.accepts != null && s.accepts.id == itemId) return true;
            return false;
        }

        private static int HousingCount() => FindObjectsByType<HousingBuilding>().Length;

        private static string CostText(BuildingDefinition def)
        {
            var parts = new List<string>();
            foreach (var c in def.cost)
                if (c.item != null) parts.Add($"{c.amount} {c.item.displayName}");
            return parts.Count > 0 ? string.Join(", ", parts) : "free";
        }
    }
}
