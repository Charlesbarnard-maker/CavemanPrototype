using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A building under construction. It claims a free worker as a builder, who
    /// hauls the required materials here one load at a time (consuming them from
    /// the pool on pickup), then constructs the building over a short build time.
    /// On completion it spawns the real building and the builder is freed.
    /// </summary>
    public class ConstructionSite : MonoBehaviour
    {
        public BuildingDefinition def;
        public float buildTime = 2.5f;

        public readonly List<ItemAmount> remaining = new(); // materials not yet delivered
        public int totalLoads;
        public int deliveredLoads;
        public float buildProgress;
        public bool HasBuilder { get; private set; }

        private SpriteRenderer _sr;
        private Color _baseColor;
        private BuilderWorker _builder;

        public bool MaterialsDone => remaining.Count == 0;
        public bool IsComplete => MaterialsDone && buildProgress >= buildTime;
        public float BuildFraction => Mathf.Clamp01(buildProgress / Mathf.Max(0.01f, buildTime));

        public static ConstructionSite Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject("Site: " + def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * (def.kind == BuildingKind.Collector ? 0.9f : 1.1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = new Color(def.color.r, def.color.g, def.color.b, 0.3f);
            sr.sortingOrder = 2;

            go.AddComponent<BoxCollider2D>(); // clickable for select/cancel

            var site = go.AddComponent<ConstructionSite>();
            site.def = def;
            site._sr = sr;
            site._baseColor = def.color;
            foreach (var c in def.cost)
                if (c.item != null && c.amount > 0) site.remaining.Add(new ItemAmount(c.item, c.amount));
            site.totalLoads = Mathf.Max(1, site.remaining.Count);
            return site;
        }

        public ItemAmount NextNeeded() => remaining.Count > 0 ? remaining[0] : null;

        public void DeliverFirst()
        {
            if (remaining.Count > 0)
            {
                remaining.RemoveAt(0);
                deliveredLoads++;
            }
        }

        public void AddBuildProgress(float dt)
        {
            if (!MaterialsDone) return;
            buildProgress += dt;
            if (IsComplete) Complete();
        }

        void Update()
        {
            // Acquire a builder when one is free.
            if (!HasBuilder && Colony.Instance != null && Colony.Instance.ClaimWorker())
            {
                HasBuilder = true;
                _builder = BuilderWorker.Spawn(this);
            }
            UpdateVisual();
        }

        private void Complete()
        {
            ReleaseBuilder(); // free first so a finished collector can auto-staff
            switch (def.kind)
            {
                case BuildingKind.Storage: StorageBuilding.Spawn(def, transform.position); break;
                case BuildingKind.Housing: HousingBuilding.Spawn(def, transform.position); break;
                default:
                    var pb = ProductionBuilding.Spawn(def, transform.position);
                    pb.TryAssign();
                    break;
            }
            Destroy(gameObject);
        }

        private void ReleaseBuilder()
        {
            if (_builder != null) { Destroy(_builder.gameObject); _builder = null; }
            if (HasBuilder) { Colony.Instance?.ReleaseWorker(); HasBuilder = false; }
        }

        void OnDestroy()
        {
            // If demolished mid-build, free the builder/claim.
            if (HasBuilder) { Colony.Instance?.ReleaseWorker(); HasBuilder = false; }
            if (_builder != null) Destroy(_builder.gameObject);
        }

        private void UpdateVisual()
        {
            if (_sr == null) return;
            float frac = (deliveredLoads + BuildFraction) / (totalLoads + 1f);
            _sr.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, Mathf.Lerp(0.3f, 0.95f, frac));
        }
    }
}
