using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Minimal debug HUD via OnGUI (no Canvas wiring needed yet). Shows the
    /// inventory and the current crafting status. Replace with proper UI later.
    /// </summary>
    public class InventoryHud : MonoBehaviour
    {
        public PlayerGatherer gatherer;
        public PlayerCrafter crafter;
        private GUIStyle _style;

        void OnGUI()
        {
            if (gatherer == null) return;
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 22, richText = true };

            GUILayout.BeginArea(new Rect(14, 12, 480, 540));

            GUILayout.Label("<b>Inventory</b>", _style);
            foreach (var kv in gatherer.Inventory.Items)
                GUILayout.Label($"{kv.Key.displayName}: {kv.Value}", _style);

            GUILayout.Space(12);

            if (crafter != null && crafter.recipe != null)
            {
                if (crafter.Owned)
                {
                    GUILayout.Label($"<b>{crafter.recipe.displayName}</b> equipped  " +
                                    $"<color=#9f9>(gather x{crafter.recipe.gatherPower})</color>", _style);
                }
                else
                {
                    string color = crafter.CanCraft() ? "#9f9" : "#f99";
                    GUILayout.Label($"Press <b>C</b> to craft {crafter.recipe.displayName}", _style);
                    GUILayout.Label($"<color={color}>Needs {CostText(crafter.recipe)}</color>", _style);
                }
            }

            GUILayout.Space(12);
            GUILayout.Label("WASD / arrows to move", _style);
            GUILayout.Label("Left-click a highlighted rock/tree to gather", _style);

            GUILayout.EndArea();
        }

        private static string CostText(ToolDefinition tool)
        {
            var parts = new List<string>();
            foreach (var c in tool.cost)
                if (c.item != null) parts.Add($"{c.amount} {c.item.displayName}");
            return string.Join(", ", parts);
        }
    }
}
