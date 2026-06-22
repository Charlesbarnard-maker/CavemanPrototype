using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Caveman
{
    /// <summary>
    /// Places production buildings at the player's location for a resource cost.
    /// Number keys map to buildable types (1 -> first, 2 -> second, ...).
    /// Spending manually-gathered resources to build an auto-gatherer is the
    /// core loop: this is how the player stops doing the work themselves.
    /// </summary>
    public class BuildController : MonoBehaviour
    {
        public PlayerGatherer gatherer;
        public List<BuildingDefinition> buildables = new();

        public int BuildingsPlaced { get; private set; }

        private Inventory Inv => gatherer != null ? gatherer.Inventory : null;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) TryBuild(0);
            if (kb.digit2Key.wasPressedThisFrame) TryBuild(1);
            if (kb.digit3Key.wasPressedThisFrame) TryBuild(2);
        }

        public bool CanAfford(BuildingDefinition def)
        {
            if (def == null || Inv == null) return false;
            foreach (var c in def.cost)
                if (c.item == null || Inv.Count(c.item) < c.amount) return false;
            return true;
        }

        private void TryBuild(int index)
        {
            if (index < 0 || index >= buildables.Count) return;

            var def = buildables[index];
            if (!CanAfford(def)) return;

            foreach (var c in def.cost)
                Inv.TryRemove(c.item, c.amount);

            // Place just beside the player; as you move, buildings spread out.
            Vector3 pos = transform.position + new Vector3(1.2f, 0f, 0f);
            ProductionBuilding.Spawn(def, pos, Inv);
            BuildingsPlaced++;
        }
    }
}
