using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// A building under construction. It claims up to `maxBuilders` free workers,
    /// who haul the required materials here a few units at a time (consuming them
    /// from the pool on pickup) and then construct the building together — more
    /// builders = faster. On completion it spawns the real building.
    /// </summary>
    public class ConstructionSite : MonoBehaviour
    {
        public BuildingDefinition def;
        public float buildTime = 4f;
        public int maxBuilders = 3;

        /// <summary>A material line: how many still needed, and how many are already claimed in-transit.</summary>
        public class Mat { public ItemDefinition item; public int needed; public int claimed; }

        public readonly List<Mat> materials = new();
        public int totalUnits;
        public int deliveredUnits;
        public float buildProgress;

        public int BuilderCount => _builders.Count;
        public bool MaterialsDone => materials.Count == 0;
        public bool IsComplete => MaterialsDone && buildProgress >= buildTime;
        public float BuildFraction => Mathf.Clamp01(buildProgress / Mathf.Max(0.01f, buildTime));

        private readonly List<BuilderWorker> _builders = new();
        private SpriteRenderer _sr;
        private Color _baseColor;

        public static ConstructionSite Spawn(BuildingDefinition def, Vector3 pos)
        {
            var go = new GameObject("Site: " + def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = Vector3.one * (def.kind == BuildingKind.Collector ? 0.9f : 1.1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = new Color(def.color.r, def.color.g, def.color.b, 0.3f);
            sr.sortingOrder = 2;

            go.AddComponent<BoxCollider2D>(); // clickable to cancel

            var site = go.AddComponent<ConstructionSite>();
            site.def = def;
            site._sr = sr;
            site._baseColor = def.color;
            foreach (var c in def.cost)
            {
                if (c.item == null || c.amount <= 0) continue;
                site.materials.Add(new Mat { item = c.item, needed = c.amount, claimed = 0 });
                site.totalUnits += c.amount;
            }
            site.totalUnits = Mathf.Max(1, site.totalUnits);
            return site;
        }

        /// <summary>First material with units still un-claimed (to fetch next).</summary>
        public Mat NextFetchable()
        {
            foreach (var m in materials)
                if (m.needed - m.claimed > 0) return m;
            return null;
        }

        public Mat MatFor(ItemDefinition item)
        {
            foreach (var m in materials)
                if (m.item == item) return m;
            return null;
        }

        /// <summary>Reserve up to `qty` of `item` as in-transit; returns the amount reserved.</summary>
        public int Claim(ItemDefinition item, int qty)
        {
            var m = MatFor(item);
            if (m == null || qty <= 0) return 0;
            int q = Mathf.Clamp(qty, 0, m.needed - m.claimed);
            m.claimed += q;
            return q;
        }

        /// <summary>Deliver reserved units; reduces both needed and claimed.</summary>
        public void Deliver(ItemDefinition item, int qty)
        {
            var m = MatFor(item);
            if (m == null || qty <= 0) return;
            m.needed -= qty;
            m.claimed = Mathf.Max(0, m.claimed - qty);
            deliveredUnits += qty;
            if (m.needed <= 0) materials.Remove(m);
        }

        public void AddBuildProgress(float dt)
        {
            if (!MaterialsDone) return;
            buildProgress += dt;
            if (IsComplete) Complete();
        }

        void Update()
        {
            // Claim free workers as builders, up to the cap, while there's work left.
            if (!IsComplete)
            {
                while (_builders.Count < maxBuilders && Colony.Instance != null && Colony.Instance.ClaimWorker())
                    _builders.Add(BuilderWorker.Spawn(this));
            }
            _builders.RemoveAll(b => b == null);
            UpdateVisual();
        }

        private void Complete()
        {
            ReleaseBuilders();
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

        private void ReleaseBuilders()
        {
            int n = _builders.Count;
            foreach (var b in _builders) if (b != null) Destroy(b.gameObject);
            _builders.Clear();
            for (int i = 0; i < n; i++) Colony.Instance?.ReleaseWorker();
        }

        void OnDestroy() => ReleaseBuilders();

        private void UpdateVisual()
        {
            if (_sr == null) return;
            float frac = ((float)deliveredUnits / totalUnits + BuildFraction) * 0.5f;
            _sr.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, Mathf.Lerp(0.3f, 0.95f, frac));
        }
    }
}
