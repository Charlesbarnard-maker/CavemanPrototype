using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Minimal debug HUD via OnGUI. Shows population/food, the combined resource
    /// pool, the build menu, and worker-staffing for the building under the cursor.
    /// </summary>
    public class InventoryHud : MonoBehaviour
    {
        public PlayerGatherer gatherer;
        public BuildController builder;
        private GUIStyle _style;

        void OnGUI()
        {
            if (gatherer == null) return;
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 22, richText = true };

            GUILayout.BeginArea(new Rect(14, 12, 720, 760));

            // --- Colony line ---
            var colony = Colony.Instance;
            if (colony != null)
            {
                string starve = colony.Starving ? "  <color=#f55>STARVING</color>" : "";
                GUILayout.Label(
                    $"<b>People</b> {colony.Population}/{colony.Capacity}   " +
                    $"Free workers: {colony.FreeWorkers}{starve}", _style);
            }

            // --- Resources ---
            GUILayout.Label("<b>Resources</b>  <color=#bbb>(carried + stored)</color>", _style);
            var totals = Economy.Totals(gatherer.Inventory);
            if (totals.Count == 0) GUILayout.Label("<color=#bbb>nothing yet — gather some!</color>", _style);
            foreach (var kv in totals)
                GUILayout.Label($"{kv.Key.displayName}: {kv.Value}", _style);

            GUILayout.Space(10);

            if (builder != null && builder.buildables.Count > 0)
            {
                if (builder.PendingIndex >= 0)
                {
                    var def = builder.buildables[builder.PendingIndex];
                    string ok = builder.PlacementValid
                        ? "<color=#9f9>VALID — left-click to place</color>"
                        : "<color=#f99>invalid — check cost / position</color>";
                    GUILayout.Label($"<b>Placing {def.displayName}</b>  ({ok})", _style);
                    GUILayout.Label("Right-click or Esc to cancel", _style);
                }
                else
                {
                    GUILayout.Label("<b>Build</b>", _style);
                    for (int i = 0; i < builder.buildables.Count; i++)
                    {
                        var def = builder.buildables[i];
                        if (def == null) continue;
                        string color = builder.CanAfford(def) ? "#9f9" : "#f99";
                        GUILayout.Label($"[{i + 1}] {def.displayName} — <color={color}>{CostText(def)}</color>  <color=#bbb>({Role(def)})</color>", _style);
                    }

                    if (builder.Hovered != null)
                    {
                        var h = builder.Hovered;
                        GUILayout.Label($"<color=#ffd24d>{h.def.displayName}: workers {h.AssignedWorkers}/{h.maxWorkers}  —  '[' remove, ']' add</color>", _style);
                    }
                    GUILayout.Label("<color=#bbb>X = demolish under cursor (half refund)</color>", _style);
                }

                GUILayout.Label($"Buildings: {builder.BuildingsPlaced}", _style);
            }

            GUILayout.Space(10);
            GUILayout.Label("WASD move · left-click patch to gather", _style);
            GUILayout.Label("<color=#ffd24d>Workers are your bottleneck: house them, feed them, assign them.</color>", _style);

            GUILayout.EndArea();
        }

        private static string Role(BuildingDefinition def)
        {
            switch (def.kind)
            {
                case BuildingKind.Storage: return $"stores {Name(def.item)} (cap {def.capacity})";
                case BuildingKind.Housing: return $"houses {def.houseCapacity}";
                default: return $"+{def.outputPerCycle} {Name(def.item)}/{def.interval:0.#}s · needs workers · near {Name(def.item)}";
            }
        }

        private static string Name(ItemDefinition item) => item != null ? item.displayName : "?";

        private static string CostText(BuildingDefinition def)
        {
            var parts = new List<string>();
            foreach (var c in def.cost)
                if (c.item != null) parts.Add($"{c.amount} {c.item.displayName}");
            return parts.Count > 0 ? string.Join(", ", parts) : "free";
        }
    }
}
