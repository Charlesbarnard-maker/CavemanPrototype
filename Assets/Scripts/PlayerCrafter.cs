using UnityEngine;
using UnityEngine.InputSystem;

namespace Caveman
{
    /// <summary>
    /// Crafts a single tool (the Stone Axe for now) when the player presses C and
    /// has the required resources. Consumes the cost and boosts the gatherer's
    /// power. Extends naturally to a list of recipes later.
    /// </summary>
    public class PlayerCrafter : MonoBehaviour
    {
        public ToolDefinition recipe;
        public PlayerGatherer gatherer;

        public bool Owned { get; private set; }

        private Inventory Inv => gatherer != null ? gatherer.Inventory : null;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.cKey.wasPressedThisFrame) return;
            TryCraft();
        }

        public bool CanCraft()
        {
            if (Owned || recipe == null || Inv == null) return false;
            foreach (var c in recipe.cost)
                if (c.item == null || Inv.Count(c.item) < c.amount) return false;
            return true;
        }

        public bool TryCraft()
        {
            if (!CanCraft()) return false;
            foreach (var c in recipe.cost)
                Inv.TryRemove(c.item, c.amount);

            Owned = true;
            if (gatherer != null)
                gatherer.GatherPower = Mathf.Max(1, recipe.gatherPower);
            return true;
        }
    }
}
