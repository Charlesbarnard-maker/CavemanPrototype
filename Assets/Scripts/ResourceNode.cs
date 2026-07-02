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

        // Runtime: how many collectors currently target this node. Used to SPREAD collectors across a
        // cluster — a node another collector already works is a worse pick — so they don't all hammer the
        // same patch while fuller nodes sit unused nearby. Maintained by ProductionBuilding. Not serialized.
        [System.NonSerialized] public int Claims;

        public static readonly List<ResourceNode> All = new();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        private int _amount;
        private float _regenTimer;
        private float _shake;
        private Vector3 _baseScale;
        private Color _baseColor;
        private SpriteRenderer _sr;
        private bool _highlighted;

        private const float ShakeDur = 0.3f;

        public bool HasResource => _amount > 0;
        public int Amount => _amount;

        // --- Save/load accessors (position/scale/yields/capacity/regen are public or set at create) ---
        internal float RegenTimerForSave => _regenTimer;
        internal Color BaseColorForSave => _baseColor;
        internal float BaseScaleForSave => _baseScale.x;
        /// <summary>Load: restore the EXACT saved remaining amount + regen phase. Deliberately NOT clamped to
        /// capacity — a fresh node initialises to a default amount before its real (sometimes smaller) capacity is
        /// assigned, so live nodes can legitimately sit a little over capacity; clamping here would make save/load
        /// lossy (the round-trip self-test caught it). Re-applies the depletion scale/tint.</summary>
        internal void SetAmountForLoad(int amount, float regenTimer)
        {
            _amount = Mathf.Max(0, amount);
            _regenTimer = regenTimer;
            if (_sr != null) ApplyScale();
        }

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
            if (_amount <= 0 && regenAmount <= 0) Destroy(gameObject); // finite vein exhausted
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
            ApplyTint(f);
        }

        // Fade a depleting patch toward a dull grey-brown so an over-harvested node visibly reads
        // as "nearly tapped out" (slow/failing production) — not just smaller. Skipped while the
        // patch is highlighted (placement targeting), which owns the colour then.
        private void ApplyTint(float f)
        {
            if (_sr == null || _highlighted) return;
            Color spent = Color.Lerp(_baseColor, new Color(0.32f, 0.30f, 0.28f), 0.65f); // washed-out / exhausted
            _sr.color = Color.Lerp(spent, _baseColor, Mathf.Clamp01(0.25f + 0.75f * f));
        }

        /// <summary>Highlight the patch under the cursor. reachable = a bright white glow ("click to mine");
        /// out of reach = a cool blue glow ("there's a resource here — walk closer").</summary>
        public void SetHighlighted(bool on, bool reachable = true)
        {
            if (_sr == null) return;
            _highlighted = on;
            if (on) _sr.color = reachable ? Color.Lerp(_baseColor, Color.white, 0.45f)
                                          : Color.Lerp(_baseColor, new Color(0.55f, 0.75f, 1f), 0.4f);
            else ApplyTint(capacity > 0 ? (float)_amount / capacity : 0f); // restore depletion shade
        }
    }
}
