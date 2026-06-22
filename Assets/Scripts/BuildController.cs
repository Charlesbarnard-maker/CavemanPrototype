using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Caveman
{
    /// <summary>
    /// Placement-mode building + worker staffing. Number keys pick a building; a
    /// ghost follows the cursor (green = valid). Collectors need a nearby patch;
    /// storage/housing place anywhere. New collectors auto-assign one free worker.
    /// Hover a collector and press '[' / ']' to remove/add workers. 'X' demolishes.
    /// </summary>
    public class BuildController : MonoBehaviour
    {
        public PlayerGatherer gatherer;
        public List<BuildingDefinition> buildables = new();
        [Tooltip("How close a matching resource patch must be to place a collector.")]
        public float placeNodeRange = 6f;

        public int BuildingsPlaced { get; private set; }
        public int PendingIndex { get; private set; } = -1;
        public bool PlacementValid { get; private set; }
        public ProductionBuilding Hovered { get; private set; }

        public static bool IsPlacing { get; private set; }

        private Inventory Carried => gatherer != null ? gatherer.Inventory : null;
        private Camera _cam;
        private GameObject _ghost;
        private SpriteRenderer _ghostSr;

        void Awake() => _cam = Camera.main;

        void Update()
        {
            if (_cam == null) _cam = Camera.main;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null) return;

            // Number keys select a buildable.
            for (int i = 0; i < buildables.Count && i < 9; i++)
            {
                if (kb[Key.Digit1 + i].wasPressedThisFrame) { Select(i); break; }
            }

            if (kb.escapeKey.wasPressedThisFrame) CancelPlacement();

            if (PendingIndex >= 0)
            {
                UpdatePlacement(mouse);
                Hovered = null;
            }
            else
            {
                Hovered = BuildingUnderCursor(mouse);
                if (mouse != null && kb.xKey.wasPressedThisFrame) TryRemoveUnderCursor(mouse);
                if (Hovered != null)
                {
                    if (kb.rightBracketKey.wasPressedThisFrame) Hovered.TryAssign();
                    if (kb.leftBracketKey.wasPressedThisFrame) Hovered.Unassign();
                }
            }
        }

        private void Select(int index)
        {
            if (index < 0 || index >= buildables.Count || buildables[index] == null) return;
            PendingIndex = index;
            IsPlacing = true;
            EnsureGhost();
        }

        private void EnsureGhost()
        {
            if (_ghost == null)
            {
                _ghost = new GameObject("BuildGhost");
                _ghostSr = _ghost.AddComponent<SpriteRenderer>();
                _ghostSr.sprite = PlaceholderArt.Square();
                _ghostSr.sortingOrder = 20;
            }
            _ghost.SetActive(true);
        }

        private void UpdatePlacement(Mouse mouse)
        {
            if (_cam == null || mouse == null || _ghost == null) return;

            var def = buildables[PendingIndex];
            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            world.z = 0f;
            _ghost.transform.position = world;
            _ghost.transform.localScale = Vector3.one * (def.kind == BuildingKind.Collector ? 0.9f : 1.1f);

            bool affordable = Economy.CanAfford(def.cost, Carried);
            bool placeOk = def.kind != BuildingKind.Collector
                           || HasMatchingNodeNear(world, def.item, placeNodeRange);
            PlacementValid = affordable && placeOk;

            _ghostSr.color = PlacementValid
                ? new Color(def.color.r, def.color.g, def.color.b, 0.55f)
                : new Color(1f, 0.3f, 0.3f, 0.45f);

            if (mouse.leftButton.wasPressedThisFrame && PlacementValid)
            {
                Economy.Spend(def.cost, Carried);
                switch (def.kind)
                {
                    case BuildingKind.Storage:
                        StorageBuilding.Spawn(def, world);
                        break;
                    case BuildingKind.Housing:
                        HousingBuilding.Spawn(def, world);
                        break;
                    default:
                        var pb = ProductionBuilding.Spawn(def, world);
                        pb.TryAssign(); // auto-staff one worker if available
                        break;
                }
                BuildingsPlaced++;
                CancelPlacement();
            }
            else if (mouse.rightButton.wasPressedThisFrame)
            {
                CancelPlacement();
            }
        }

        private void CancelPlacement()
        {
            PendingIndex = -1;
            PlacementValid = false;
            IsPlacing = false;
            if (_ghost != null) _ghost.SetActive(false);
        }

        public bool CanAfford(BuildingDefinition def) => def != null && Economy.CanAfford(def.cost, Carried);

        private ProductionBuilding BuildingUnderCursor(Mouse mouse)
        {
            if (_cam == null || mouse == null) return null;
            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(world);
            return hit != null ? hit.GetComponent<ProductionBuilding>() : null;
        }

        private static bool HasMatchingNodeNear(Vector3 pos, ItemDefinition item, float range)
        {
            float rsq = range * range;
            foreach (var n in FindObjectsByType<ResourceNode>())
            {
                if (n == null || n.yields != item) continue;
                if (((Vector2)(n.transform.position - pos)).sqrMagnitude <= rsq) return true;
            }
            return false;
        }

        private void TryRemoveUnderCursor(Mouse mouse)
        {
            if (_cam == null) return;
            Vector3 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(world);
            if (hit == null) return;

            var pb = hit.GetComponent<ProductionBuilding>();
            var sb = hit.GetComponent<StorageBuilding>();
            var hb = hit.GetComponent<HousingBuilding>();
            BuildingDefinition rdef = pb != null ? pb.def : sb != null ? sb.def : hb != null ? hb.def : null;
            GameObject target = pb != null ? pb.gameObject : sb != null ? sb.gameObject : hb != null ? hb.gameObject : null;
            if (rdef == null || target == null) return;

            if (pb != null) while (pb.AssignedWorkers > 0) pb.Unassign(); // free the workers

            if (Carried != null)
                foreach (var c in rdef.cost)
                    Carried.Add(c.item, Mathf.Max(0, c.amount / 2));

            BuildingsPlaced = Mathf.Max(0, BuildingsPlaced - 1);
            Destroy(target);
        }
    }
}
