using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A resource patch (rock, tree) holding a finite, slowly-regenerating amount.
    /// Both manual gathering and production buildings draw from the same pool, so
    /// over-harvesting a patch starves whatever depends on it — the first bit of
    /// emergent spatial pressure that pushes the player to expand.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class ResourceNode : MonoBehaviour
    {
        public ItemDefinition yields;
        public int capacity = 30;
        public int regenAmount = 1;
        [Tooltip("Seconds between each regeneration tick.")]
        public float regenInterval = 1.5f;

        public static readonly List<ResourceNode> All = new();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        private int _amount;
        private float _regenTimer;
        private float _shake;
        private Vector3 _baseScale;
        private Color _baseColor;
        private SpriteRenderer _sr;

        private const float ShakeDur = 0.3f;

        public bool HasResource => _amount > 0;
        public int Amount => _amount;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
            _baseColor = _sr != null ? _sr.color : Color.white;
            _amount = capacity;
            ApplyScale();
        }

        void Update()
        {
            // Chop recoil: brief wobble when struck (the "knock it down" feel).
            if (_shake > 0f)
            {
                _shake -= Time.deltaTime;
                float t = Mathf.Max(0f, _shake) / ShakeDur;
                float ang = Mathf.Sin(_shake * 50f) * 10f * t;
                transform.rotation = Quaternion.Euler(0f, 0f, ang);
            }
            else if (transform.rotation != Quaternion.identity)
            {
                transform.rotation = Quaternion.identity;
            }

            // Slow regeneration back up to capacity.
            if (_amount < capacity)
            {
                _regenTimer += Time.deltaTime;
                if (_regenTimer >= regenInterval)
                {
                    _regenTimer -= regenInterval;
                    _amount = Mathf.Min(capacity, _amount + regenAmount);
                    ApplyScale();
                }
            }
        }

        /// <summary>Trigger the chop recoil wobble.</summary>
        public void Nudge() => _shake = ShakeDur;

        /// <summary>Pull up to `requested` units out; returns how many were actually taken.</summary>
        public int Extract(int requested)
        {
            if (requested <= 0 || _amount <= 0) return 0;
            int taken = Mathf.Min(requested, _amount);
            _amount -= taken;
            ApplyScale();
            return taken;
        }

        /// <summary>Manual harvest of `per` units into an inventory.</summary>
        public bool Harvest(Inventory inventory, int per = 1)
        {
            if (inventory == null || yields == null) return false;
            int taken = Extract(per);
            if (taken <= 0) return false;
            inventory.Add(yields, taken);
            return true;
        }

        private void ApplyScale()
        {
            float f = capacity > 0 ? (float)_amount / capacity : 0f;
            transform.localScale = _baseScale * Mathf.Lerp(0.35f, 1f, f);
        }

        /// <summary>Brighten the patch when the player can target it.</summary>
        public void SetHighlighted(bool on)
        {
            if (_sr == null) return;
            _sr.color = on ? Color.Lerp(_baseColor, Color.white, 0.45f) : _baseColor;
        }
    }
}
