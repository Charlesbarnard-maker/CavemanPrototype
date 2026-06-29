using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Headless sanity check for the map generator (no GUI, no Play mode). Run with:
    ///   Unity -batchmode -quit -executeMethod Caveman.MapGenAudit.Run -logFile &lt;log&gt;
    /// For several seeds it reports the biome mix, whether water exists within reach of spawn (Water Pump /
    /// Refinery need it), how much of the map is impassable Mountain, and — by flood-filling Walkable cells
    /// from the origin — whether every resource zone is reachable on foot. A regression guard for the gen rewrite.
    /// </summary>
    public static class MapGenAudit
    {
        const int Half = 280;
        static readonly float[] ZoneDist = { 64f, 84f, 100f, 120f, 140f };
        static readonly Terrain[] ZoneBiome = { Terrain.Plains, Terrain.Hills, Terrain.Forest, Terrain.Hills, Terrain.Hills };

        // The actual biome-gated zone centre for corridor k (mirrors GameBootstrap.Zone): the natural home
        // biome found along the corridor, else the corridor-end fallback.
        static Vector2 ZoneCenter(int k)
        {
            float a = TerrainGrid.CorridorAngle(k, 5);
            if (TerrainGrid.FindBiomeAlong(a, ZoneBiome[k], ZoneDist[k] * 0.7f, ZoneDist[k] * 1.35f, out var f))
                return new Vector2(f.x, f.y);
            float d = ZoneDist[k] + (TerrainGrid.Hash01(k + 7) - 0.5f) * 20f;
            return new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d);
        }

        public static void Run()
        {
            int seedsOk = 0; const int seedsTotal = 6;
            for (int s = 0; s < seedsTotal; s++)
            {
                float seed = (s + 1) * 137.13f;
                TerrainGrid.Generate(Half, seed, 22f);
                TerrainGrid.CarveCorridors(5, 190f, 1);

                int plains = 0, forest = 0, hills = 0, water = 0, mountain = 0, total = 0;
                for (int y = -Half; y <= Half; y++)
                    for (int x = -Half; x <= Half; x++)
                    {
                        total++;
                        switch (TerrainGrid.At(x, y))
                        {
                            case Terrain.Plains: plains++; break;
                            case Terrain.Forest: forest++; break;
                            case Terrain.Hills: hills++; break;
                            case Terrain.Water: water++; break;
                            case Terrain.Mountain: mountain++; break;
                        }
                    }

                bool waterNearSpawn = TerrainGrid.HasWaterNear(Vector3.zero, 45f);

                var reached = FloodFromOrigin();
                int zonesReached = 0;
                var zoneStatus = new StringBuilder();
                int zonesNatural = 0;
                string[] zn = { "clay", "copper", "wood", "stone", "iron" };
                for (int k = 0; k < 5; k++)
                {
                    float a = TerrainGrid.CorridorAngle(k, 5);
                    bool nat = TerrainGrid.FindBiomeAlong(a, ZoneBiome[k], ZoneDist[k] * 0.7f, ZoneDist[k] * 1.35f, out var fz);
                    if (nat) zonesNatural++;
                    var zc = ZoneCenter(k);
                    bool ok = NearReached(reached, Mathf.RoundToInt(zc.x), Mathf.RoundToInt(zc.y), 6);
                    if (ok) zonesReached++;
                    zoneStatus.Append($" {zn[k]}:{(nat ? "nat" : "PAINT")}{(ok ? "" : "!BLOCKED")}");
                }

                float Pct(int n) => 100f * n / total;
                Debug.Log($"[MapGenAudit] seed={seed:F0} | plains={Pct(plains):F0}% forest={Pct(forest):F0}% hills={Pct(hills):F0}% water={Pct(water):F0}% mtn={Pct(mountain):F1}% | waterNearSpawn={waterNearSpawn} | reach={zonesReached}/5 natural-biome={zonesNatural}/5 |{zoneStatus}");
                if (waterNearSpawn && zonesReached == 5 && mountain > 0 && water > 0 && plains > 0) seedsOk++;
            }
            Debug.Log($"[MapGenAudit] RESULT {seedsOk}/{seedsTotal} seeds fully OK (water near spawn, all zones reachable, mountains+water+plains all present)");
        }

        // Bake a faithful snapshot of the generated world (terrain + the real zone/island painting) to a PNG
        // so the map can be eyeballed headlessly. Run: -executeMethod Caveman.MapGenAudit.Snapshot
        public static void Snapshot()
        {
            float seed = 137.13f;
            TerrainGrid.Generate(Half, seed, 22f);
            TerrainGrid.CarveCorridors(5, 190f, 1);

            // Replicate GameBootstrap's biome-gated zones: only the FALLBACK paints a region (a found natural
            // biome is left as-is). The dot for each zone centre is drawn after, so the PNG matches the game.
            float[] zr = { 30f, 30f, 34f, 32f, 32f };
            for (int k = 0; k < 5; k++)
            {
                float a = TerrainGrid.CorridorAngle(k, 5);
                if (!TerrainGrid.FindBiomeAlong(a, ZoneBiome[k], ZoneDist[k] * 0.7f, ZoneDist[k] * 1.35f, out _))
                {
                    float d = ZoneDist[k] + (TerrainGrid.Hash01(k + 7) - 0.5f) * 20f;
                    TerrainGrid.PaintBlob(new Vector3(Mathf.Cos(a) * d, Mathf.Sin(a) * d, 0f), zr[k], ZoneBiome[k], 4, 0.45f);
                }
            }
            float oa = TerrainGrid.CorridorAngle(0, 5);
            TerrainGrid.Paint(new Vector3(Mathf.Cos(oa) * 80f, Mathf.Sin(oa) * 80f, 0f), 14f, Terrain.Plains);
            float ia = TerrainGrid.CorridorAngle(2, 5) + 0.9f;
            var ic = new Vector2(Mathf.Cos(ia) * 168f, Mathf.Sin(ia) * 168f);
            TerrainGrid.CarveWater(new Vector3(ic.x, ic.y, 0f), 34f);
            Vector2[] blobs = { new Vector2(0, 0), new Vector2(7, 3), new Vector2(-6, 5), new Vector2(4, -7), new Vector2(-5, -5) };
            float[] radii = { 9f, 6f, 6.5f, 5.5f, 5f };
            for (int b = 0; b < blobs.Length; b++)
                TerrainGrid.Paint(new Vector3(ic.x + blobs[b].x, ic.y + blobs[b].y, 0f), radii[b], Terrain.Hills);

            int size = Half * 2 + 1;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Color c = TerrainGrid.ColorOf(TerrainGrid.At(x - Half, y - Half));
                    // mark spawn origin in magenta for orientation
                    if (Mathf.Abs(x - Half) <= 1 && Mathf.Abs(y - Half) <= 1) c = new Color(1f, 0f, 1f);
                    px[y * size + x] = c;
                }
            // Ring each biome-gated zone centre in white so its home biome is visible (forest→wood, hills→metals…).
            for (int k = 0; k < 5; k++)
            {
                var zc = ZoneCenter(k);
                int mx = Mathf.RoundToInt(zc.x) + Half, my = Mathf.RoundToInt(zc.y) + Half;
                for (int oy = -4; oy <= 4; oy++)
                    for (int ox = -4; ox <= 4; ox++)
                    {
                        int px2 = mx + ox, py2 = my + oy;
                        if (px2 < 0 || py2 < 0 || px2 >= size || py2 >= size) continue;
                        if (Mathf.Abs(ox) == 4 || Mathf.Abs(oy) == 4) px[py2 * size + px2] = new Color32(255, 255, 255, 255);
                    }
            }
            tex.SetPixels32(px);
            tex.Apply();
            string outPath = Path.Combine(Path.GetTempPath(), "caveman-map-snapshot.png");
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Debug.Log($"[MapGenAudit] snapshot written: {outPath}");
        }

        // Render the actual WORLD through a camera (terrain bake + scattered decorations) and composite the warm
        // vignette, so the "filled-in / warm" look can be eyeballed headlessly. Run: -executeMethod Caveman.MapGenAudit.LookSnapshot
        public static void LookSnapshot()
        {
            TerrainGrid.Generate(Half, 137.13f, 22f);
            TerrainGrid.CarveCorridors(5, 190f, 1);
            TerrainGrid.SpawnRenderer(); // sets the warm/textured MapTex
            new GameObject("Snap_Sway").AddComponent<SwayAnimator>();
            DecorScatter.Populate();
            var decorRoot = GameObject.Find("Decor");
            Debug.Log($"[MapGenAudit] decorations placed: {(decorRoot != null ? decorRoot.transform.childCount : 0)}");

            // Pixel-COMPOSITE the preview (URP batch-mode Camera.Render doesn't draw sprites): terrain base from
            // the real baked MapTex, then blit each decoration sprite's texture at its world position, then vignette.
            var mt = TerrainGrid.MapTex;
            float cenX = 24f, cenY = 12f, halfW = 15f, halfH = 11.25f; int ppu = 24; // region to preview (world units → px)
            int W = (int)(halfW * 2 * ppu), H = (int)(halfH * 2 * ppu);
            float ox = cenX - halfW, oy = cenY - halfH; // world coord at output (0,0)
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    // nearest-sample the supersampled MapTex (mirrors the in-game Point filter → sharp grain).
                    // MapTex covers (2*Half+1) world units, so world→normalized = (w+Half)/(2*Half+1).
                    float wx = ox + x / (float)ppu, wy = oy + y / (float)ppu;
                    if (mt == null) { px[y * W + x] = new Color(0.16f, 0.19f, 0.16f); continue; }
                    int sx = Mathf.Clamp(Mathf.RoundToInt((wx + Half) / (2f * Half + 1f) * mt.width), 0, mt.width - 1);
                    int sy = Mathf.Clamp(Mathf.RoundToInt((wy + Half) / (2f * Half + 1f) * mt.height), 0, mt.height - 1);
                    px[y * W + x] = mt.GetPixel(sx, sy);
                }
            if (decorRoot != null)
                foreach (Transform d in decorRoot.transform)
                {
                    var sr = d.GetComponent<SpriteRenderer>();
                    if (sr == null || sr.sprite == null) continue;
                    Vector3 p = d.position;
                    if (p.x < ox - 1f || p.x > ox + W / (float)ppu + 1f || p.y < oy - 1f || p.y > oy + H / (float)ppu + 1f) continue;
                    BlitWorld(px, W, H, sr.sprite, p.x, p.y, d.localScale.x, ox, oy, ppu, sr.color);
                }
            // warm vignette: amber centre wash + cool dark corners
            float mcx = W * 0.5f, mcy = H * 0.5f, maxR = Mathf.Sqrt(mcx * mcx + mcy * mcy);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dd = Mathf.Sqrt((x - mcx) * (x - mcx) + (y - mcy) * (y - mcy)) / maxR;
                    var c = px[y * W + x];
                    float warm = 0.10f * (1f - dd);
                    c = new Color(c.r + 0.45f * warm, c.g + 0.28f * warm, c.b + 0.04f * warm);
                    float dark = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((dd - 0.55f) / 0.45f)) * 0.40f;
                    c = Color.Lerp(c, new Color(0.06f, 0.07f, 0.12f), dark);
                    px[y * W + x] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
                }
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels(px); tex.Apply();
            string outPath = Path.Combine(Path.GetTempPath(), "caveman-look-snapshot.png");
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Debug.Log($"[MapGenAudit] look snapshot: {outPath}");

            foreach (var n in new[] { "Terrain", "Decor", "Snap_Sway" })
            { var go = GameObject.Find(n); if (go != null) Object.DestroyImmediate(go); }
        }

        // Dump the new splitter/merger junction sprites next to a plain belt tile (all tinted a belt brown,
        // on a grass background) so they can be eyeballed for "do they fit the belt family?".
        // Run: -executeMethod Caveman.MapGenAudit.JunctionSnapshot
        public static void JunctionSnapshot()
        {
            var tint = new Color(0.55f, 0.40f, 0.24f); // a representative belt brown (def.color tints the white body)
            var bg = new Color(0.31f, 0.41f, 0.25f);   // plains/grass backdrop
            Sprite[] sprites = { PlaceholderArt.BeltSprite(1, 0), PlaceholderArt.SplitterSprite(), PlaceholderArt.MergerSprite() };
            const int tile = 64, gap = 24, scale = 3;
            int outW = (tile * scale) * 3 + gap * 4, outH = tile * scale + gap * 2;
            var outPx = new Color[outW * outH];
            for (int i = 0; i < outPx.Length; i++) outPx[i] = bg;
            for (int s = 0; s < sprites.Length; s++)
            {
                var src = sprites[s].texture.GetPixels(); // 64x64
                int ox = gap + s * (tile * scale + gap), oy = gap;
                for (int y = 0; y < tile * scale; y++)
                    for (int x = 0; x < tile * scale; x++)
                    {
                        var p = src[(y / scale) * tile + (x / scale)];
                        if (p.a < 0.01f) continue; // transparent → keep grass
                        var c = new Color(p.r * tint.r, p.g * tint.g, p.b * tint.b);
                        outPx[(oy + y) * outW + (ox + x)] = c;
                    }
            }
            var tex = new Texture2D(outW, outH, TextureFormat.RGBA32, false);
            tex.SetPixels(outPx); tex.Apply();
            string outPath = Path.Combine(Path.GetTempPath(), "caveman-junctions.png");
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Debug.Log($"[MapGenAudit] junction snapshot (belt | splitter | merger): {outPath}");
        }

        // Composite a faux "under construction" tile (materializing building + scaffold overlay + builder +
        // progress bar) to a PNG so the construction FX can be eyeballed. Run: -executeMethod Caveman.MapGenAudit.ConstructionSnapshot
        public static void ConstructionSnapshot()
        {
            const int W = 320, H = 280; var px = new Color[W * H];
            var bg = new Color(0.31f, 0.41f, 0.25f);
            for (int i = 0; i < px.Length; i++) px[i] = bg;
            int bx = 96, by = 90, bs = 128; // building cell: 128px at (96,90)
            // materializing building (a brown body at ~55% built)
            Blit(px, W, H, PlaceholderArt.Square(), bx, by, bs, new Color(0.5f, 0.4f, 0.28f), 0.62f);
            // scaffold overlay (same cell)
            Blit(px, W, H, PlaceholderArt.Scaffold(), bx, by, bs, new Color(0.62f, 0.45f, 0.26f), 0.95f);
            // builder worker at the bottom-left corner (Stone job = sledge), ~64px
            Blit(px, W, H, PlaceholderArt.CollectorWorker((int)PlaceholderArt.WorkerJob.Stone, 0, 0, 1), bx - 36, by - 18, 72, Color.white, 1f);
            // progress bar above (bg + ~55% green fill)
            FillRect(px, W, H, bx, by + bs + 14, bs, 16, new Color(0.08f, 0.08f, 0.10f, 1f));
            FillRect(px, W, H, bx + 2, by + bs + 16, (int)((bs - 4) * 0.55f), 12, new Color(0.42f, 0.92f, 0.46f, 1f));
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false); tex.SetPixels(px); tex.Apply();
            string outPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "caveman-construction.png");
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Debug.Log($"[MapGenAudit] construction snapshot: {outPath}");
        }

        static void Blit(Color[] dst, int W, int H, Sprite sp, int ox, int oy, int size, Color tint, float alpha)
        {
            var src = sp.texture.GetPixels(); int s = sp.texture.width;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int dx = ox + x, dy = oy + y; if (dx < 0 || dy < 0 || dx >= W || dy >= H) continue;
                    var p = src[(y * s / size) * s + (x * s / size)];
                    float a = p.a * alpha; if (a < 0.01f) continue;
                    var c = new Color(p.r * tint.r, p.g * tint.g, p.b * tint.b);
                    int di = dy * W + dx;
                    dst[di] = new Color(c.r * a + dst[di].r * (1 - a), c.g * a + dst[di].g * (1 - a), c.b * a + dst[di].b * (1 - a));
                }
        }

        // Alpha-composite a sprite's full texture at WORLD position (wx,wy), sized `scale` world units (sprite is
        // 1 unit at scale 1), into a region whose pixel (0,0) is world (originX,originY) at `ppu` pixels/unit.
        static void BlitWorld(Color[] dst, int W, int H, Sprite sp, float wx, float wy, float scale, float originX, float originY, int ppu, Color tint)
        {
            var t = sp.texture; int sw = t.width, sh = t.height;
            var src = t.GetPixels();
            int drawPx = Mathf.RoundToInt(scale * ppu);
            if (drawPx < 1) return;
            float cxp = (wx - originX) * ppu, cyp = (wy - originY) * ppu;
            int x0 = Mathf.RoundToInt(cxp - drawPx * 0.5f), y0 = Mathf.RoundToInt(cyp - drawPx * 0.5f);
            for (int y = 0; y < drawPx; y++)
                for (int x = 0; x < drawPx; x++)
                {
                    int dx = x0 + x, dy = y0 + y;
                    if (dx < 0 || dy < 0 || dx >= W || dy >= H) continue;
                    var p = src[(y * sh / drawPx) * sw + (x * sw / drawPx)];
                    float a = p.a * tint.a;
                    if (a < 0.01f) continue;
                    int di = dy * W + dx;
                    dst[di] = new Color(p.r * tint.r * a + dst[di].r * (1 - a), p.g * tint.g * a + dst[di].g * (1 - a), p.b * tint.b * a + dst[di].b * (1 - a));
                }
        }

        static void FillRect(Color[] dst, int W, int H, int ox, int oy, int w, int h, Color c)
        {
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            { int dx = ox + x, dy = oy + y; if (dx >= 0 && dy >= 0 && dx < W && dy < H) dst[dy * W + dx] = c; }
        }

        static bool[] FloodFromOrigin()
        {
            int size = Half * 2 + 1;
            var seen = new bool[size * size];
            int Idx(int x, int y) => (y + Half) * size + (x + Half);
            var q = new Queue<Vector2Int>();
            seen[Idx(0, 0)] = true; q.Enqueue(new Vector2Int(0, 0));
            var dirs = new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                foreach (var dd in dirs)
                {
                    int nx = c.x + dd.x, ny = c.y + dd.y;
                    if (nx < -Half || nx > Half || ny < -Half || ny > Half) continue;
                    int idx = Idx(nx, ny);
                    if (seen[idx] || !TerrainGrid.Walkable(new Vector2Int(nx, ny))) continue;
                    seen[idx] = true; q.Enqueue(new Vector2Int(nx, ny));
                }
            }
            return seen;
        }

        static bool NearReached(bool[] reached, int cx, int cy, int r)
        {
            int size = Half * 2 + 1;
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < -Half || nx > Half || ny < -Half || ny > Half) continue;
                    if (reached[(ny + Half) * size + (nx + Half)]) return true;
                }
            return false;
        }
    }
}
