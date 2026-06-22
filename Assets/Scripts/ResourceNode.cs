using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A harvestable object in the world (a rock, a tree). Holds which item it
    /// yields and how many hits it survives. Depletes on each harvest.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ResourceNode : MonoBehaviour
    {
        public ItemDefinition yields;
        public int yieldPerHit = 1;
        public int hitsRemaining = 5;

        /// <summary>Harvest one hit's worth into the given inventory. Returns false if depleted/invalid.</summary>
        public bool Harvest(Inventory inventory)
        {
            if (inventory == null || yields == null || hitsRemaining <= 0) return false;

            inventory.Add(yields, yieldPerHit);
            hitsRemaining--;

            // Cheap juice: shrink as it depletes so harvesting feels responsive.
            transform.localScale *= 0.88f;

            if (hitsRemaining <= 0)
                Destroy(gameObject);

            return true;
        }
    }
}
