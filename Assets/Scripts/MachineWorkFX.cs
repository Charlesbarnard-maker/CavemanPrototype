using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Cosmetic "this machine is actively working" FX for an automated resource building: a pumping
    /// mechanical arm/drill aimed at the resource it taps, plus dust puffs kicked out on each work
    /// cycle. Purely VISUAL — there are NO workers and NO logic here; it is driven entirely by the
    /// building's existing gather loop via SetWorking / SetTarget / Strike, and never touches gameplay.
    /// Keeps the "fully automated machines" design (machines do the work, not people) while making
    /// extraction read as alive. Built from procedural sprites (no art assets).
    ///
    /// Lives on its OWN GameObject at the building's position (NOT parented to the building, whose
    /// transform is non-uniformly scaled to its footprint — parenting would skew the arm/dust). The
    /// owning building creates it and Destroys it in OnDisable.
    /// </summary>
    public class MachineWorkFX : MonoBehaviour
    {
        private Transform _arm;
        private SpriteRenderer _armSr;

        private bool _working;
        private float _strike;             // decays 1→0 after each work pulse (sharper pump + dust burst)
        private float _phase;              // idle pump oscillation
        private Vector3 _aim = Vector3.up; // world (== local) direction the arm points / dust flies — toward the node
        private const float ArmRest = 0.20f, ArmReach = 0.30f;

        private const int PuffCount = 6;   // 2-3 spawned per strike, ~0.5s life → a handful active at once
        private SpriteRenderer[] _puffs;
        private Vector3[] _puffVel;
        private float[] _puffLife, _puffAge;
        private int _puffNext;

        /// <summary>Create the FX as a standalone object at `worldPos` (scale 1, no parent skew).</summary>
        public static MachineWorkFX Attach(Vector3 worldPos)
        {
            var go = new GameObject("WorkFX");
            go.transform.position = worldPos;
            return go.AddComponent<MachineWorkFX>();
        }

        void Awake()
        {
            // Mechanical arm/drill — a short dark bar (long axis = local +Y) that pumps toward the node.
            var armGo = new GameObject("Arm");
            armGo.transform.SetParent(transform, false);
            _armSr = armGo.AddComponent<SpriteRenderer>();
            _armSr.sprite = PlaceholderArt.Square();
            _armSr.color = new Color(0.30f, 0.30f, 0.34f);
            _armSr.sortingOrder = 6; // above the building body (5)
            armGo.transform.localScale = new Vector3(0.16f, 0.34f, 1f);
            _arm = armGo.transform;
            _armSr.enabled = false;

            _puffs = new SpriteRenderer[PuffCount];
            _puffVel = new Vector3[PuffCount];
            _puffLife = new float[PuffCount];
            _puffAge = new float[PuffCount];
            for (int i = 0; i < PuffCount; i++)
            {
                var p = new GameObject("Dust");
                p.transform.SetParent(transform, false);
                var sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = PlaceholderArt.Circle();
                sr.color = new Color(0.80f, 0.74f, 0.62f, 0f); // dust, starts transparent
                sr.sortingOrder = 7;
                sr.enabled = false;
                _puffs[i] = sr;
            }
            UpdateArm(0f);
        }

        public void SetWorking(bool working) => _working = working;

        /// <summary>Aim the arm + dust at the resource being tapped (its world position).</summary>
        public void SetTarget(Vector3 worldTarget)
        {
            Vector3 d = worldTarget - transform.position;
            if (((Vector2)d).sqrMagnitude > 1e-4f) _aim = ((Vector2)d).normalized;
        }

        /// <summary>One work pulse: a sharper arm pump + a small burst of dust toward the resource.</summary>
        public void Strike()
        {
            _strike = 1f;
            int n = 2 + (Random.value > 0.5f ? 1 : 0);
            Vector3 tip = _aim * (ArmRest + ArmReach);
            for (int k = 0; k < n; k++)
            {
                int i = _puffNext;
                _puffNext = (_puffNext + 1) % PuffCount;
                var sr = _puffs[i];
                sr.enabled = true;
                sr.transform.localPosition = tip;
                sr.transform.localScale = Vector3.one * Random.Range(0.10f, 0.16f);
                Vector2 spread = Random.insideUnitCircle * 0.35f;
                _puffVel[i] = ((Vector2)_aim + spread).normalized * Random.Range(0.5f, 1.0f);
                _puffLife[i] = Random.Range(0.35f, 0.60f);
                _puffAge[i] = 0f;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;

            // Arm: a gentle idle pump while working, a sharper extension right after each strike.
            if (_strike > 0f) _strike = Mathf.Max(0f, _strike - dt * 4f);
            _phase += dt * (_working ? 6f : 0f);
            float pump = _working ? 0.5f + 0.5f * Mathf.Sin(_phase) : 0f;
            UpdateArm(pump);
            _armSr.enabled = _working || _strike > 0f; // hide when the machine isn't extracting

            // Dust puffs: drift outward, slow (drag), fade + grow, then disable.
            for (int i = 0; i < PuffCount; i++)
            {
                var sr = _puffs[i];
                if (!sr.enabled) continue;
                _puffAge[i] += dt;
                float u = _puffLife[i] > 0f ? _puffAge[i] / _puffLife[i] : 1f;
                if (u >= 1f) { sr.enabled = false; continue; }
                sr.transform.localPosition += _puffVel[i] * dt;
                _puffVel[i] *= Mathf.Max(0f, 1f - dt * 1.5f);
                var c = sr.color; c.a = (1f - u) * 0.7f; sr.color = c;
                sr.transform.localScale = Vector3.one * Mathf.Lerp(0.10f, 0.22f, u);
            }
        }

        private void UpdateArm(float pump01)
        {
            float reach = ArmRest + ArmReach * pump01 + _strike * 0.12f;
            _arm.localPosition = _aim * reach;
            float ang = Mathf.Atan2(_aim.y, _aim.x) * Mathf.Rad2Deg - 90f; // align bar's long axis (+Y) to the aim
            _arm.localRotation = Quaternion.Euler(0f, 0f, ang);
        }
    }
}
