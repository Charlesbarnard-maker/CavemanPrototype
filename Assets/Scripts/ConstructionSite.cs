using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Self-construction: on placement BuildController calls <see cref="Spawn"/>, which drops a timed site that
    /// assembles itself over <see cref="buildTime"/> (cost paid up front — no builder hauling) while showing
    /// scaffold + builders + a progress bar, then calls <see cref="SpawnFinished"/> to swap in the real building.
    /// <see cref="SpawnFinished"/> is also the static BuildingKind→concrete-Spawn dispatcher.
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

        // --- "Under construction" FX: scaffolding over the footprint + animated builder(s) hammering away +
        //     a progress bar, so a building visibly READS as being built (not just a faint ghost). All are
        //     children of the site, so they're destroyed automatically when it completes or is cancelled. ---
        private Transform _fxRoot;          // counter-scaled so its children render at world scale
        private SpriteRenderer _scaffoldSr;
        private Transform _b1, _b2; private SpriteRenderer _b1Sr, _b2Sr; private Vector3 _b1Base, _b2Base;
        private Transform _barFill; private float _barW;
        private int _builderAge, _builderTier, _frame; private float _frameT, _animT;

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
            site.CreateFx(w, h, sb);
            if (def.kind == BuildingKind.Depot) site.AddDepotDeckPreview(w, h, outDir); // tiled deck, not a stretched square

            // Preview the finished building's I/O slots ON the blueprint, so you can orient before it exists —
            // mirrors each kind's own PlacePorts call (e.g. a Large Warehouse shows its 2 in + 2 out). Kinds that
            // don't route belt items on a fixed edge (poles, batteries, pumps, garages, depots, generators) show none.
            switch (def.kind)
            {
                case BuildingKind.Storage:
                    Ports.PlacePorts(go.transform, w, h, outDir, true, true, singlePort: def.singlePort, dualPort: def.dualPort);
                    break;
                case BuildingKind.Workshop:
                    Ports.PlacePorts(go.transform, w, h, outDir,
                        def.inputs != null && def.inputs.Count > 0, true,
                        def.inputs != null && def.inputs.Count > 1, singlePort: true);
                    break;
                case BuildingKind.Research:
                    Ports.PlacePorts(go.transform, w, h, outDir, true, false, singlePort: true); // input notch only
                    break;
                case BuildingKind.Depot:
                case BuildingKind.Power:
                case BuildingKind.Pole:
                case BuildingKind.Battery:
                case BuildingKind.Pump:
                case BuildingKind.Garage:
                    break; // no fixed belt I/O edge to preview
                default: // Collector & friends → output arrow only
                    Ports.PlacePorts(go.transform, w, h, outDir, false, true, singlePort: true);
                    break;
            }
            return site;
        }

        // Build the construction FX (scaffold overlay + builder workers + progress bar) around the site.
        private void CreateFx(int w, int h, float sb)
        {
            // A counter-scaled root: the site GO itself is stretched to the footprint, so to place fixed-size
            // workers/bars we cancel that scale here and position children in plain world units from the centre.
            var rootGo = new GameObject("BuildFx");
            rootGo.transform.SetParent(transform, false);
            float sx = Mathf.Max(0.001f, w * sb), sy = Mathf.Max(0.001f, h * sb);
            rootGo.transform.localScale = new Vector3(1f / sx, 1f / sy, 1f);
            _fxRoot = rootGo.transform;

            // Scaffolding overlay — a child of the SITE so it inherits the footprint scale (covers the build).
            var scaff = new GameObject("Scaffold");
            scaff.transform.SetParent(transform, false);
            scaff.transform.localScale = Vector3.one;
            _scaffoldSr = scaff.AddComponent<SpriteRenderer>();
            _scaffoldSr.sprite = PlaceholderArt.Scaffold();
            _scaffoldSr.color = new Color(0.62f, 0.45f, 0.26f, 0.95f);
            _scaffoldSr.sortingOrder = _sr.sortingOrder + 4;

            // Builder worker(s) — reuse the worker art (a sledge "Stone" job reads as a builder), age-themed.
            _builderAge = Colony.Instance != null ? Colony.Instance.Age : 0;
            _builderTier = Mathf.Clamp(_builderAge, 0, 2);
            float hw = w * 0.5f, hh = h * 0.5f;
            _b1 = MakeBuilder("Builder", new Vector3(-hw - 0.1f, -hh + 0.15f, 0f), false, out _b1Sr);
            _b1Base = _b1.localPosition;
            if (w * h >= 4) { _b2 = MakeBuilder("Builder2", new Vector3(hw + 0.1f, -hh + 0.15f, 0f), true, out _b2Sr); _b2Base = _b2.localPosition; }

            // Progress bar above the site (left-anchored fill, grown in AnimateFx).
            _barW = Mathf.Max(0.9f, w * 0.9f);
            float topY = hh + 0.5f;
            MakeQuad("BarBg", new Vector3(0f, topY, 0f), new Vector3(_barW + 0.1f, 0.22f, 1f), new Color(0.08f, 0.08f, 0.10f, 0.85f), 14);
            _barFill = MakeQuad("BarFill", new Vector3(-_barW * 0.5f, topY, 0f), new Vector3(0.001f, 0.14f, 1f), new Color(0.42f, 0.92f, 0.46f, 0.95f), 15);
        }

        private Transform MakeBuilder(string name, Vector3 localPos, bool faceLeft, out SpriteRenderer sr)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_fxRoot, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(faceLeft ? -0.7f : 0.7f, 0.7f, 1f);
            sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.CollectorWorker((int)PlaceholderArt.WorkerJob.Stone, _builderTier, _builderAge, 0);
            sr.sortingOrder = 11; // above buildings — always visible at the work site
            return go.transform;
        }

        private Transform MakeQuad(string name, Vector3 localPos, Vector3 localScale, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_fxRoot, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Square();
            sr.color = color;
            sr.sortingOrder = order;
            return go.transform;
        }

        // While a DEPOT builds, preview its platform as the SAME per-cell tiled deck the finished building uses
        // (parented to the counter-scaled FX root, so each tile renders at world scale) instead of the base
        // square sprite stretched 3:1 and flipped on rotate. The stretched base renderer is hidden; the scaffold
        // + builders still read as "under construction". Matches Depot.BuildDeck's rotation convention exactly.
        private void AddDepotDeckPreview(int w, int h, Belt.Dir face)
        {
            if (_sr != null) _sr.enabled = false; // hide the stretched square; the tiles stand in for it
            bool vertical = face == Belt.Dir.N || face == Belt.Dir.S;
            bool harbour = def != null && def.isHarbour;
            var sprite = harbour ? PlaceholderArt.HarbourDeckTile() : PlaceholderArt.StationDeckTile();
            float rot = vertical ? 90f : 0f; // deck art is authored for a horizontal (E–W) lane
            Vector3 center = transform.position;
            Color tint = def != null ? new Color(def.color.r, def.color.g, def.color.b, 0.9f) : Color.white;
            int order = (_sr != null ? _sr.sortingOrder : 2) + 1; // above the base, below the scaffold (+4)
            foreach (var c in Footprint.Cells(center, w, h))
            {
                var t = new GameObject("deckPreview");
                t.transform.SetParent(_fxRoot, false);
                t.transform.localPosition = new Vector3(c.x - center.x, c.y - center.y, 0f);
                t.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
                var sr = t.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = tint;
                sr.sortingOrder = order;
            }
        }

        // Animate the FX each frame: grow the progress bar, cycle the builders' work frames + a hammering bob,
        // and gently fade the scaffold out as the build finishes (it's "coming down").
        private void AnimateFx()
        {
            float frac = BuildFraction;
            if (_barFill != null)
            {
                float wgt = Mathf.Max(0.001f, _barW * frac);
                var ls = _barFill.localScale; ls.x = wgt; _barFill.localScale = ls;
                var lp = _barFill.localPosition; lp.x = -_barW * 0.5f + wgt * 0.5f; _barFill.localPosition = lp;
            }
            _frameT += Time.deltaTime;
            if (_frameT >= 0.16f)
            {
                _frameT = 0f; _frame = (_frame + 1) % 3;
                if (_b1Sr != null) _b1Sr.sprite = PlaceholderArt.CollectorWorker((int)PlaceholderArt.WorkerJob.Stone, _builderTier, _builderAge, _frame);
                if (_b2Sr != null) _b2Sr.sprite = PlaceholderArt.CollectorWorker((int)PlaceholderArt.WorkerJob.Stone, _builderTier, _builderAge, (_frame + 1) % 3);
            }
            _animT += Time.deltaTime;
            if (_b1 != null) { var p = _b1Base; p.y += Mathf.Abs(Mathf.Sin(_animT * 9f)) * 0.13f; _b1.localPosition = p; }
            if (_b2 != null) { var p = _b2Base; p.y += Mathf.Abs(Mathf.Sin(_animT * 9f + 1.5f)) * 0.13f; _b2.localPosition = p; }
            if (_scaffoldSr != null) _scaffoldSr.color = new Color(0.62f, 0.45f, 0.26f, Mathf.Lerp(0.95f, 0.4f, frac));
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
            AnimateFx();
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
                case BuildingKind.Garage: Garage.Spawn(def, pos); break;
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
