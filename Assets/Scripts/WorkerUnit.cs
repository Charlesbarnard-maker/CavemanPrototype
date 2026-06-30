using System;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A visible little worker (an age-skinned caveman) attached to a production building. For a COLLECTOR
    /// it walks out to the bound resource node, "gathers" with a chopping bob, walks back, and repeats; for
    /// a WORKSHOP it works in place. Purely COSMETIC — the building's own timer does the real gathering —
    /// but it makes extraction read as people working, not invisible machines. Several spawn per building
    /// and clean themselves up when the building is demolished.
    /// </summary>
    public class WorkerUnit : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Transform _home;
        private Func<bool> _isWorking;
        private Func<ResourceNode> _node;   // null = stationary worker (workshop)
        private int _age = -1;
        private float _speed = 1.8f;
        private float _wait;
        private Vector3 _target;
        private enum St { AtHome, ToNode, Gather, ToHome }
        private St _state = St.AtHome;
        private int _frame; private float _animT; private Vector3 _lastWalkPos; private int _shownKey = -1;

        // Collector workers are JOB-aware (axe/sledge/shovel/pick) and TECH-aware (the building's upgrade
        // tier steps their tool stone→bronze→iron, then a powered MACHINE at tier 3). -1 = a workshop hand
        // (the generic age-skinned caveman).
        private int _job = -1;
        private Func<int> _tier;

        /// <summary>Spawn `count` gatherers for a collector — they walk to its node and back.</summary>
        public static void SpawnForCollector(ProductionBuilding pb, int count)
        {
            if (pb == null) return;
            int job = PlaceholderArt.JobForItem(pb.produces != null ? pb.produces.id : null);
            for (int i = 0; i < count; i++)
                Make(pb.transform, () => pb != null && pb.Working, () => pb != null ? pb.Source : null, i,
                     job, () => pb != null ? pb.Tier : 0);
        }

        /// <summary>Spawn `count` workers for a workshop — they work in place while it's producing.</summary>
        public static void SpawnForWorkshop(WorkshopBuilding wb, int count)
        {
            if (wb == null) return;
            for (int i = 0; i < count; i++)
                Make(wb.transform, () => wb != null && wb.Working, null, i, -1, null);
        }

        private static void Make(Transform home, Func<bool> working, Func<ResourceNode> node, int i, int job, Func<int> tier)
        {
            var go = new GameObject("Worker");
            go.transform.position = home.position;
            go.transform.localScale = Vector3.one * 0.42f; // small — clearly a person beside a building
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 6; // above building bodies (5)
            var w = go.AddComponent<WorkerUnit>();
            w._sr = sr; w._home = home; w._isWorking = working; w._node = node;
            w._job = job; w._tier = tier;
            w._speed = 1.7f + 0.25f * i;
            w._wait = 0.3f * i; // stagger so they don't move in lockstep
            w._lastWalkPos = home.position;
        }

        void Update()
        {
            if (_home == null || _isWorking == null) { Destroy(gameObject); return; }

            int age = Colony.Instance != null ? Colony.Instance.Age : 0;
            _age = age;
            // Walk animation: cycle frames while actually moving, stand on frame 0 otherwise.
            bool moving = (transform.position - _lastWalkPos).sqrMagnitude > 0.00004f;
            _lastWalkPos = transform.position;
            if (moving) { _animT += Time.deltaTime; if (_animT >= 0.13f) { _animT = 0f; _frame = (_frame + 1) % 3; } }
            else { _frame = 0; }
            // Collector: a job-appropriate worker that tech-escalates by the building's upgrade tier
            // (tool stone→bronze→iron → a MACHINE at tier 3). Workshop hand (_job < 0): the plain caveman.
            int tierNow = (_job >= 0 && _tier != null) ? _tier() : 0;
            int key = _job >= 0 ? (((_job * 4 + tierNow) * 5 + age) * 3 + _frame) : (1000 + age * 3 + _frame);
            if (key != _shownKey && _sr != null)
            {
                _shownKey = key;
                _sr.sprite = _job >= 0 ? PlaceholderArt.CollectorWorker(_job, tierNow, age, _frame)
                                       : PlaceholderArt.Caveman(age, _frame);
                _sr.color = Color.white;
            }

            bool working = _isWorking();
            Vector3 home = _home.position;

            // Stationary worker (workshop): stand at the building and bob while it's producing.
            if (_node == null)
            {
                MoveToward(home + new Vector3(0f, -0.18f, 0f));
                if (_sr != null) _sr.enabled = true;
                SetChop(working ? Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 14f : 0f);
                return;
            }

            var src = _node();
            if (!working || src == null) // idle: return home and hide there
            {
                SetChop(0f);
                bool atHome = MoveToward(home);
                if (_sr != null) _sr.enabled = !atHome;
                _state = St.AtHome;
                return;
            }
            if (_sr != null) _sr.enabled = true;

            switch (_state)
            {
                case St.AtHome:
                    SetChop(0f);
                    _wait -= Time.deltaTime;
                    MoveToward(home);
                    if (_wait <= 0f) { _target = StandBy(src.transform.position, home); _state = St.ToNode; }
                    break;
                case St.ToNode:
                    SetChop(0f);
                    // Track the collector's CURRENT node live — if it rebound to a fuller / less-contested
                    // patch mid-walk, head there instead of trekking to the now-stale spot it set out for.
                    _target = StandBy(src.transform.position, home);
                    if (MoveToward(_target)) { _state = St.Gather; _wait = UnityEngine.Random.Range(0.8f, 1.5f); }
                    break;
                case St.Gather:
                    _wait -= Time.deltaTime;
                    SetChop(Mathf.Abs(Mathf.Sin(Time.time * 12f)) * 18f); // chopping/mining bob
                    if (_wait <= 0f) { SetChop(0f); _state = St.ToHome; }
                    break;
                case St.ToHome:
                    SetChop(0f);
                    if (MoveToward(home)) { _state = St.AtHome; _wait = UnityEngine.Random.Range(0.3f, 0.7f); }
                    break;
            }
        }

        // A spot just short of the node, on the side facing home, so workers don't stand on the node centre.
        private static Vector3 StandBy(Vector3 nodePos, Vector3 home)
        {
            Vector3 dir = home - nodePos; dir.z = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.right; else dir.Normalize();
            return nodePos + dir * 0.6f;
        }

        private void SetChop(float angleDeg) => transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

        // Move toward t; flip to face travel; returns true once arrived.
        private bool MoveToward(Vector3 t)
        {
            t.z = 0f;
            Vector3 p = transform.position; p.z = 0f;
            float dx = t.x - p.x;
            if (Mathf.Abs(dx) > 0.03f)
            {
                var sc = transform.localScale;
                sc.x = Mathf.Abs(sc.x) * (dx < 0f ? -1f : 1f);
                transform.localScale = sc;
            }
            transform.position = Vector3.MoveTowards(p, t, _speed * Time.deltaTime);
            return (transform.position - t).sqrMagnitude < 0.02f;
        }
    }
}
