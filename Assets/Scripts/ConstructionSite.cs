using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Factory-first: construction is INSTANT, so this is now just the static <see cref="SpawnFinished"/>
    /// dispatcher that maps a BuildingKind to its concrete Spawn (called by BuildController on placement).
    /// The MonoBehaviour body (materials / build progress) is legacy and no longer instantiated.
    /// </summary>
    public class ConstructionSite : MonoBehaviour
    {
        public BuildingDefinition def;
        public float buildTime = 4f;
        public Belt.Dir outDir = Belt.Dir.E; // output side chosen at placement (collectors/workshops)

        /// <summary>A material line: how many still needed, and how many are reserved in-transit.</summary>
        public class Mat { public ItemDefinition item; public int needed; public int claimed; }

        public readonly List<Mat> materials = new();
        public int totalUnits;
        public int deliveredUnits;
        public float buildProgress;

        public static readonly List<ConstructionSite> All = new();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        // --- Builder slots: fixed positions around the structure so multiple builders
        //     spread out (visible work) instead of stacking on one spot. Overflow
        //     builders (slot -1) still add progress but aren't given a unique position. ---
        public const int SlotCount = 8;
        private readonly bool[] _slotUsed = new bool[SlotCount];

        public int ClaimSlot()
        {
            for (int i = 0; i < SlotCount; i++)
                if (!_slotUsed[i]) { _slotUsed[i] = true; return i; }
            return -1; // all slots taken — this builder contributes but isn't placed
        }

        public void ReleaseSlot(int i) { if (i >= 0 && i < SlotCount) _slotUsed[i] = false; }

        /// <summary>World position a builder should stand at for slot `i` (ring around the site).</summary>
        public Vector3 SlotPosition(int i)
        {
            if (i < 0) return transform.position; // overflow: abstracted at the centre
            float r = (def != null ? Mathf.Max(def.FootW, def.FootH) : 1) * 0.5f + 0.55f;
            float ang = (i / (float)SlotCount) * Mathf.PI * 2f;
            return transform.position + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f);
        }

        public bool MaterialsDone => materials.Count == 0;
        public bool IsComplete => MaterialsDone && buildProgress >= buildTime;
        public float BuildFraction => Mathf.Clamp01(buildProgress / Mathf.Max(0.01f, buildTime));

        private SpriteRenderer _sr;
        private Color _baseColor;

        public static ConstructionSite Spawn(BuildingDefinition def, Vector3 pos, Belt.Dir outDir = Belt.Dir.E)
        {
            // Effective footprint after rotation (only rectangular kinds swap — matches BuildController/Depot).
            bool vertical = outDir == Belt.Dir.N || outDir == Belt.Dir.S;
            int w = def.FootW, h = def.FootH;
            if (w != h && vertical) { int t = w; w = h; h = t; }

            var go = new GameObject("Site: " + def.displayName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            float sb = def.kind == BuildingKind.Collector ? 0.9f : 1.1f;
            go.transform.localScale = new Vector3(w * sb, h * sb, 1f); // cover the footprint

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteDatabase.ForBuilding(def);
            sr.color = new Color(def.color.r, def.color.g, def.color.b, 0.3f);
            sr.sortingOrder = 2;

            go.AddComponent<BoxCollider2D>(); // clickable to cancel + blocks the footprint while building

            var site = go.AddComponent<ConstructionSite>();
            site.def = def;
            site.outDir = outDir;
            site._sr = sr;
            site._baseColor = def.color;
            // Self-build: the cost was paid up front at placement, so the site just assembles itself over
            // a short time that scales with cost + footprint — a few seconds for a hut, up to ~a minute for
            // a big build. (No builder units / hauling — factory-first stays hands-off.)
            int costUnits = 0;
            if (def.cost != null) foreach (var c in def.cost) if (c != null) costUnits += Mathf.Max(0, c.amount);
            int area = Mathf.Max(1, def.FootW * def.FootH);
            site.buildTime = Mathf.Clamp(3f + 0.22f * costUnits + 1.5f * (area - 1), 3f, 60f);
            return site;
        }

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

        public int Claim(ItemDefinition item, int qty)
        {
            var m = MatFor(item);
            if (m == null || qty <= 0) return 0;
            int q = Mathf.Clamp(qty, 0, m.needed - m.claimed);
            m.claimed += q;
            return q;
        }

        /// <summary>Release an in-transit reservation (e.g. a builder was removed mid-haul).</summary>
        public void Unclaim(ItemDefinition item, int qty)
        {
            var m = MatFor(item);
            if (m != null) m.claimed = Mathf.Max(0, m.claimed - qty);
        }

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
            // Self-construction: no builders / hauling (cost paid up front) — assemble over buildTime,
            // then become the finished building. (Esc/X/demolish cancels with a full refund — see
            // BuildController.DemolishSelected.)
            buildProgress += Time.deltaTime;
            UpdateVisual();
            if (buildProgress >= buildTime) Complete();
        }

        private void Complete()
        {
            SpawnFinished(def, transform.position, outDir);
            Destroy(gameObject);
        }

        /// <summary>Spawn the finished building of the given kind at a position (no construction).</summary>
        public static void SpawnFinished(BuildingDefinition def, Vector3 pos, Belt.Dir outDir = Belt.Dir.E)
        {
            switch (def.kind)
            {
                case BuildingKind.Storage: StorageBuilding.Spawn(def, pos, outDir); break;
                case BuildingKind.Workshop: WorkshopBuilding.Spawn(def, pos, outDir); break;
                case BuildingKind.Depot: Depot.Spawn(def, pos, outDir); break;
                case BuildingKind.Power: PowerPlant.Spawn(def, pos, outDir); break;
                case BuildingKind.Pole: PowerPole.Spawn(def, pos); break;
                case BuildingKind.Battery: Battery.Spawn(def, pos); break;
                case BuildingKind.Pump: WaterPump.Spawn(def, pos); break;
                case BuildingKind.Research: ResearchBuilding.Spawn(def, pos, outDir); break;
                default: ProductionBuilding.Spawn(def, pos, outDir); break;
            }
        }

        private void UpdateVisual()
        {
            if (_sr == null) return;
            // Fade the ghost in as it builds (translucent → solid), so progress reads at a glance.
            _sr.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, Mathf.Lerp(0.3f, 0.95f, BuildFraction));
        }
    }
}
