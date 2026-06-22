using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Minimal debug HUD via OnGUI. Shows the combined resource pool (carried +
    /// all buffers/storage), the build menu, and the current placement state.
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

            GUILayout.BeginArea(new Rect(14, 12, 700, 680));

            GUILayout.Label("<b>Resources</b>  <color=#bbb>(carried + stored)</color>", _style);
            var totals = Economy.Totals(gatherer.Inventory);
            if (totals.Count == 0) GUILayout.Label("<color=#bbb>nothing yet — gather some!</color>", _style);
            foreach (var kv in totals)
                GUILayout.Label($"{kv.Key.displayName}: {kv.Value}", _style);

            GUILayout.Space(12);

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
                        string role = def.kind == BuildingKind.Storage
                            ? $"stores {Name(def.item)} (cap {def.capacity})"
                            : $"+{def.outputPerCycle} {Name(def.item)}/{def.interval:0.#}s · near {Name(def.item)}";
                        GUILayout.Label(
                            $"[{i + 1}] {def.displayName} — <color={color}>{CostText(def)}</color>  <color=#bbb>({role})</color>",
                            _style);
                    }
                    GUILayout.Label("<color=#bbb>X = demolish under cursor (half refund)</color>", _style);
                }

                GUILayout.Label($"Buildings: {builder.BuildingsPlaced}", _style);
            }

            GUILayout.Space(12);
            GUILayout.Label("WASD/arrows move · left-click patch to gather", _style);
            GUILayout.Label("<color=#ffd24d>Place a collector near a patch — its worker harvests for you. Put storage beside the collector.</color>", _style);

            GUILayout.EndArea();
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
