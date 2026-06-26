using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// COSMETIC ONLY — a little cutter/miner that walks from its collector out to the bound resource
    /// node, chops it (a visual nudge), and carries back, on a loop. Pure eye-candy so a hut reads as
    /// "doing something" and is satisfying to watch. It does NOT gather or gate production — the
    /// collector produces on its own fixed timer (see <see cref="ProductionBuilding"/>). Designed to
    /// be upgraded later (more / faster cutters per age) to mirror a higher output tier.
    /// </summary>
    public class Harvester : MonoBehaviour
    {
        public ProductionBuilding home;
        public float moveSpeed = 3f;
        public float chopTime = 1.2f;

        private enum St { ToNode, Chop, ToHome }
        private St _state = St.ToNode;
        private float _chop, _nudge;
        private SpriteRenderer _sr;
        private Color _baseColor;

        public static Harvester Spawn(ProductionBuilding home)
        {
            var go = new GameObject("Cutter");
            go.transform.position = home.transform.position;
            go.transform.localScale = Vector3.one * 0.3f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Circle();
            sr.sortingOrder = 12;
            var h = go.AddComponent<Harvester>();
            h.home = home;
            h._sr = sr;
            h._baseColor = new Color(0.96f, 0.91f, 0.62f);
            sr.color = h._baseColor;
            return h;
        }

        void Update()
        {
            if (home == null) { Destroy(gameObject); return; }
            var node = home.Source;

            // Nothing to cut (paused / no node / depleted) → wait at the hut.
            if (home.Paused || node == null || !node.HasResource) { MoveTo(home.transform.position); _state = St.ToNode; return; }

            switch (_state)
            {
                case St.ToNode:
                    if (MoveTo(node.transform.position)) { _state = St.Chop; _chop = 0f; _nudge = 0f; }
                    break;
                case St.Chop:
                    _chop += Time.deltaTime;
                    _nudge -= Time.deltaTime;
                    if (_nudge <= 0f) { node.Nudge(); _nudge = 0.3f; } // shake the tree/rock
                    if (_chop >= chopTime) _state = St.ToHome;
                    break;
                case St.ToHome:
                    if (MoveTo(home.transform.position)) _state = St.ToNode;
                    break;
            }

            // Tint toward the resource colour while carrying home (a load).
            if (_sr != null)
                _sr.color = _state == St.ToHome && node.yields != null
                    ? Color.Lerp(_baseColor, node.yields.color, 0.6f) : _baseColor;
        }

        private bool MoveTo(Vector3 t)
        {
            t.z = 0f; var p = transform.position; p.z = 0f;
            transform.position = Vector3.MoveTowards(p, t, moveSpeed * Time.deltaTime);
            return (transform.position - t).sqrMagnitude < 0.04f;
        }
    }
}
