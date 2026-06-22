using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Minimal debug HUD via OnGUI so we don't need to wire up a Canvas yet.
    /// Replace with proper uGUI/UI Toolkit once the loop feels good.
    /// </summary>
    public class InventoryHud : MonoBehaviour
    {
        public PlayerGatherer gatherer;
        private GUIStyle _style;

        void OnGUI()
        {
            if (gatherer == null) return;
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 22, richText = true };

            GUILayout.BeginArea(new Rect(14, 12, 360, 500));
            GUILayout.Label("<b>Inventory</b>", _style);
            foreach (var kv in gatherer.Inventory.Items)
                GUILayout.Label($"{kv.Key.displayName}: {kv.Value}", _style);

            GUILayout.Space(10);
            GUILayout.Label("WASD / arrows to move", _style);
            GUILayout.Label("Left-click a rock or tree to gather", _style);
            GUILayout.EndArea();
        }
    }
}
