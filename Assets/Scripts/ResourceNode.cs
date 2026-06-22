using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A harvestable object (rock, tree). Depletes on each harvest, then respawns
    /// after a delay so the world doesn't run dry. Highlights when targetable.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class ResourceNode : MonoBehaviour
    {
        public ItemDefinition yields;
        public int yieldPerHit = 1;
        public int maxHits = 5;
        [Tooltip("Seconds to regrow after being fully harvested. 0 = never respawn.")]
        public float respawnSeconds = 8f;

        private int _hitsRemaining;
        private Vector3 _baseScale;
        private Color _baseColor;
        private SpriteRenderer _sr;
        private Collider2D _col;

        public bool IsAvailable => _hitsRemaining > 0 && _col != null && _col.enabled;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _col = GetComponent<Collider2D>();
            _baseScale = transform.localScale;
            _baseColor = _sr != null ? _sr.color : Color.white;
            _hitsRemaining = maxHits;
        }

        /// <summary>Harvest one hit's worth (scaled by gather power) into the inventory.</summary>
        public bool Harvest(Inventory inventory, int power = 1)
        {
            if (inventory == null || yields == null || _hitsRemaining <= 0) return false;

            inventory.Add(yields, yieldPerHit * Mathf.Max(1, power));
            _hitsRemaining--;

            // Cheap juice: shrink as it depletes so harvesting feels responsive.
            transform.localScale *= 0.88f;

            if (_hitsRemaining <= 0) Deplete();
            return true;
        }

        private void Deplete()
        {
            if (_sr != null) _sr.enabled = false;
            if (_col != null) _col.enabled = false;
            if (respawnSeconds > 0f) Invoke(nameof(Respawn), respawnSeconds);
        }

        private void Respawn()
        {
            _hitsRemaining = maxHits;
            transform.localScale = _baseScale;
            if (_sr != null) { _sr.enabled = true; _sr.color = _baseColor; }
            if (_col != null) _col.enabled = true;
        }

        /// <summary>Brighten the sprite when the player can target this node.</summary>
        public void SetHighlighted(bool on)
        {
            if (_sr == null || !_sr.enabled) return;
            _sr.color = on ? Color.Lerp(_baseColor, Color.white, 0.45f) : _baseColor;
        }
    }
}
