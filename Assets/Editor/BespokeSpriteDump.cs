// Headless visual-pass helper: renders every bespoke building sprite to PNG contact sheets so the
// procedural art can be eyeballed without entering Play mode. Run via:
//   Unity -batchmode -quit -projectPath <proj> -executeMethod BespokeSpriteDump.Dump -logFile <log>
// Writes to C:\Users\charl\CavemanArtPreview\ (outside Assets, so Unity doesn't import the output).
// NOT shipped art — a dev tool. Lives under Assets/Editor so it compiles into the Editor assembly.
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Caveman; // PlaceholderArt lives in the Caveman namespace

public static class BespokeSpriteDump
{
    // (displayName, def.color, age) for every entry in PlaceholderArt.BespokeBuilding — colors
    // transcribed verbatim from GameBootstrap so the tint matches the running game.
    static readonly System.ValueTuple<string, Color, int>[] Items = new System.ValueTuple<string, Color, int>[]
    {
        // Age 0 (Stone)
        ("Wood Hut",        new Color(0.80f,0.52f,0.25f), 0),
        ("Stone Pit",       new Color(0.45f,0.52f,0.62f), 0),
        ("Sawmill",         new Color(0.66f,0.50f,0.30f), 0),
        ("Idea Bench",      new Color(0.80f,0.74f,0.46f), 0),
        ("Research Lodge",  new Color(0.60f,0.48f,0.34f), 0),
        ("Woodpile",        new Color(0.62f,0.40f,0.20f), 0),
        ("Station",         new Color(0.50f,0.45f,0.55f), 0),
        ("Harbour",         new Color(0.40f,0.52f,0.62f), 0),
        ("Warehouse",       new Color(0.55f,0.52f,0.45f), 0),
        ("Water Barrel",    new Color(0.35f,0.50f,0.72f), 0),
        // Age 1 (Tribal)
        ("Clay Pit",        new Color(0.68f,0.46f,0.36f), 1),
        ("Copper Mine",     new Color(0.78f,0.52f,0.32f), 1),
        ("Iron Mine",       new Color(0.50f,0.48f,0.40f), 1),
        ("Charcoal Burner", new Color(0.62f,0.58f,0.54f), 1),
        ("Basic Smelter",   new Color(0.74f,0.52f,0.42f), 1),
        ("Scroll Maker",    new Color(0.84f,0.78f,0.50f), 1),
        ("Clay Pile",       new Color(0.60f,0.42f,0.34f), 1),
        ("Garage",          new Color(0.60f,0.50f,0.38f), 1),
        // Age 2 (Bronze)
        ("Kiln",            new Color(0.70f,0.42f,0.34f), 2),
        ("Potter",          new Color(0.72f,0.50f,0.40f), 2),
        ("Advanced Smelter",new Color(0.66f,0.60f,0.52f), 2),
        ("Refinery",        new Color(0.42f,0.37f,0.30f), 2),
        ("Drafting Table",  new Color(0.52f,0.66f,0.82f), 2),
        ("Brick Yard",      new Color(0.66f,0.40f,0.34f), 2),
        ("Oil Tank",        new Color(0.30f,0.28f,0.34f), 2),
        ("Wood Generator",  new Color(0.55f,0.40f,0.25f), 2),
        ("Battery",         new Color(0.32f,0.62f,0.50f), 2),
        ("Water Pump",      new Color(0.30f,0.55f,0.78f), 2),
        ("Booster Pump",    new Color(0.45f,0.62f,0.72f), 2),
        ("Oil Well",        new Color(0.26f,0.22f,0.28f), 2),
        // Age 3 (Iron)
        ("Toolmaker",       new Color(0.50f,0.55f,0.60f), 3),
        ("Engineering Lab", new Color(0.40f,0.56f,0.82f), 3),
        ("Coal Generator",  new Color(0.30f,0.30f,0.34f), 3),
        ("Oil Generator",   new Color(0.30f,0.26f,0.22f), 3),
        // Age 4 (Industrial)
        ("Monument",        new Color(0.88f,0.84f,0.62f), 4),
    };

    const int Native = 64;   // sprite size
    const int Scale  = 3;    // upscale (nearest) so detail is visible
    const int Cell   = Native * Scale; // 192
    const int Pad    = 12;

    [MenuItem("Tools/Dump Bespoke Sprites")]
    public static void Dump()
    {
        string outDir = @"C:\Users\charl\CavemanArtPreview";
        Directory.CreateDirectory(outDir);

        // group by age into rows so each row band == one age
        int maxAge = 0; foreach (var it in Items) maxAge = Mathf.Max(maxAge, it.Item3);
        int rows = maxAge + 1;
        int[] perRow = new int[rows];
        foreach (var it in Items) perRow[it.Item3]++;
        int cols = 0; foreach (int c in perRow) cols = Mathf.Max(cols, c);

        int W = Pad + cols * (Cell + Pad);
        int H = Pad + rows * (Cell + Pad);

        var groundBg = new Color(0.41f, 0.47f, 0.33f); // grass-ish, like the in-game terrain
        var darkBg   = new Color(0.16f, 0.16f, 0.19f);
        var tinted = Fill(W, H, groundBg);
        var raw    = Fill(W, H, darkBg);

        int[] rowCursor = new int[rows];
        var report = new StringBuilder();
        report.AppendLine($"Sheet {W}x{H}, cell {Cell}px (sprite {Native}x{Scale}), rows=age 0..{maxAge}");
        report.AppendLine("Each row = one age; left-to-right order below.\n");

        foreach (var it in Items)
        {
            string name = it.Item1; Color col = it.Item2; int age = it.Item3;
            int c = rowCursor[age]++;
            int r = age;
            int ox = Pad + c * (Cell + Pad);
            // row 0 (age 0) at TOP of the image; texture space has y up, so top row = high y.
            int oyTop = H - Pad - r * (Cell + Pad);   // y of the top edge of this cell
            int oy = oyTop - Cell;                     // bottom edge (where sprite ground sits)

            var sp = PlaceholderArt.BespokeBuilding(name);
            report.AppendLine($"age {age} col {c}: {name}  tint({col.r:0.00},{col.g:0.00},{col.b:0.00})  {(sp == null ? "*** NULL ***" : "ok")}");
            if (sp == null) continue;

            var tex = sp.texture;
            var src = tex.GetPixels32();
            int tw = tex.width, th = tex.height;
            Blit(tinted, W, H, src, tw, th, ox, oy, col, groundBg);
            Blit(raw,    W, H, src, tw, th, ox, oy, Color.white, darkBg);
        }

        WritePng(tinted, W, H, Path.Combine(outDir, "bespoke_tinted.png"));
        WritePng(raw,    W, H, Path.Combine(outDir, "bespoke_raw.png"));
        File.WriteAllText(Path.Combine(outDir, "layout.txt"), report.ToString());

        // Zoomed individuals (8x) for a watchlist, tinted, on ground bg — for close inspection.
        string[] watch = { "Garage", "Charcoal Burner" };
        const int Z = 8;
        foreach (var name in watch)
        {
            Color col = default; bool found = false;
            foreach (var it in Items) if (it.Item1 == name) { col = it.Item2; found = true; break; }
            if (!found) continue;
            var sp = PlaceholderArt.BespokeBuilding(name);
            if (sp == null) continue;
            var tex = sp.texture; var src = tex.GetPixels32();
            int tw = tex.width, th = tex.height;
            int zw = tw * Z, zh = th * Z;
            var img = Fill(zw, zh, groundBg);
            for (int sy = 0; sy < th; sy++)
                for (int sx = 0; sx < tw; sx++)
                {
                    Color32 sc = src[sy * tw + sx];
                    float a = sc.a / 255f; if (a <= 0f) continue;
                    Color outc = new Color(
                        groundBg.r * (1 - a) + (sc.r / 255f) * col.r * a,
                        groundBg.g * (1 - a) + (sc.g / 255f) * col.g * a,
                        groundBg.b * (1 - a) + (sc.b / 255f) * col.b * a, 1f);
                    for (int yy = 0; yy < Z; yy++)
                        for (int xx = 0; xx < Z; xx++)
                            img[(sy * Z + yy) * zw + (sx * Z + xx)] = outc;
                }
            WritePng(img, zw, zh, Path.Combine(outDir, "zoom_" + name.Replace(" ", "") + ".png"));
        }
        DumpMounts(outDir, groundBg);
        DumpWorkers(outDir, groundBg);
        DumpMachinery(outDir, groundBg);
        DumpTrains(outDir, groundBg);
        Debug.Log("[BespokeSpriteDump] wrote contact sheets + zooms + mounts + workers + machinery + trains to " + outDir);
    }

    // Train parts: locos tier 0..4 (Donkey/Ox/Horse/Steam/Diesel) + a cargo wagon + a liquid tanker, frame 1.
    static void DumpTrains(string outDir, Color groundBg)
    {
        const int scale = 5, cell = Native * scale, pad = 14;
        int cols = 7, rows = 1;
        int W = pad + cols * (cell + pad), H = pad + rows * (cell + pad);
        var sheet = Fill(W, H, groundBg);
        var parts = new Sprite[7];
        for (int t = 0; t < 5; t++) parts[t] = PlaceholderArt.TrainLoco(t, 1);
        parts[5] = PlaceholderArt.CargoWagon(1);
        parts[6] = PlaceholderArt.LiquidWagon(1);
        var cargoTint = new Color(0.6f, 0.62f, 0.7f); // sample tint for the wagon load areas
        for (int i = 0; i < 7; i++)
        {
            int ox = pad + i * (cell + pad), oy = pad;
            Color tint = i >= 5 ? cargoTint : Color.white;
            Blit5(sheet, W, H, parts[i], tint, ox, oy, scale, groundBg);
        }
        WritePng(sheet, W, H, Path.Combine(outDir, "trains.png"));
    }

    // Upgrade machinery overlay escalation: rows = sample buildings, cols = tier 0..3, each = the building
    // (tinted by def.color) with the tier's machinery overlay (white) composited on top, mid-animation.
    static void DumpMachinery(string outDir, Color groundBg)
    {
        var samples = new System.ValueTuple<string, Color>[]
        {
            ("Wood Hut",  new Color(0.80f,0.52f,0.25f)),
            ("Iron Mine", new Color(0.50f,0.48f,0.40f)),
            ("Sawmill",   new Color(0.66f,0.50f,0.30f)),
        };
        const int cols = 4, scale = 5, cell = Native * scale, pad = 14;
        int rows = samples.Length;
        int W = pad + cols * (cell + pad), H = pad + rows * (cell + pad);
        var sheet = Fill(W, H, groundBg);
        for (int r = 0; r < rows; r++)
            for (int tier = 0; tier < cols; tier++)
            {
                var baseSp = PlaceholderArt.BespokeBuilding(samples[r].Item1);
                var mach = PlaceholderArt.TierMachinery(tier, 1);
                int ox = pad + tier * (cell + pad);
                int oy = (H - pad - r * (cell + pad)) - cell;
                // base building, tinted by def.color
                Blit5(sheet, W, H, baseSp, samples[r].Item2, ox, oy, scale, groundBg);
                // machinery overlay, white tint
                if (mach != null) Blit5(sheet, W, H, mach, Color.white, ox, oy, scale, default, true);
            }
        WritePng(sheet, W, H, Path.Combine(outDir, "machinery.png"));
    }

    // upscaled composite blit of a sprite × tint at (ox,oy); over=true means composite onto existing pixels.
    static void Blit5(Color[] dst, int W, int H, Sprite sp, Color tint, int ox, int oy, int scale, Color bg, bool over = false)
    {
        if (sp == null) return;
        var tex = sp.texture; var src = tex.GetPixels32(); int tw = tex.width, th = tex.height;
        for (int sy = 0; sy < th; sy++)
            for (int sx = 0; sx < tw; sx++)
            {
                Color32 sc = src[sy * tw + sx]; float a = sc.a / 255f; if (a <= 0f) continue;
                float rr = (sc.r / 255f) * tint.r, gg = (sc.g / 255f) * tint.g, bb = (sc.b / 255f) * tint.b;
                for (int yy = 0; yy < scale; yy++)
                    for (int xx = 0; xx < scale; xx++)
                    {
                        int bx = ox + sx * scale + xx, by = oy + sy * scale + yy;
                        if (bx < 0 || bx >= W || by < 0 || by >= H) continue;
                        Color under = over ? dst[by * W + bx] : bg;
                        dst[by * W + bx] = new Color(under.r * (1 - a) + rr * a, under.g * (1 - a) + gg * a, under.b * (1 - a) + bb * a, 1f);
                    }
            }
    }

    // Collector workers: rows = job (Wood/Stone/Clay/Ore), cols = upgrade tier 0..3 (3 = machine), each
    // shown at the colony age that tier naturally reaches, mid-stride (frame 1). White tint, like in-game.
    static void DumpWorkers(string outDir, Color groundBg)
    {
        int[] tierAge = { 0, 2, 3, 4 };           // tier 0 stone, 1 bronze, 2 iron, 3 industrial
        const int rows = 4, cols = 4, scale = 4, cell = Native * scale, pad = 12;
        int W = pad + cols * (cell + pad), H = pad + rows * (cell + pad);
        var sheet = Fill(W, H, groundBg);
        for (int job = 0; job < rows; job++)
            for (int tier = 0; tier < cols; tier++)
            {
                var sp = PlaceholderArt.CollectorWorker(job, tier, tierAge[tier], 1);
                if (sp == null) continue;
                var tex = sp.texture; var src = tex.GetPixels32(); int tw = tex.width, th = tex.height;
                int ox = pad + tier * (cell + pad);
                int oy = (H - pad - job * (cell + pad)) - cell;
                for (int sy = 0; sy < th; sy++)
                    for (int sx = 0; sx < tw; sx++)
                    {
                        Color32 sc = src[sy * tw + sx]; float a = sc.a / 255f; if (a <= 0f) continue;
                        Color outc = new Color(groundBg.r * (1 - a) + (sc.r / 255f) * a,
                                               groundBg.g * (1 - a) + (sc.g / 255f) * a,
                                               groundBg.b * (1 - a) + (sc.b / 255f) * a, 1f);
                        for (int yy = 0; yy < scale; yy++)
                            for (int xx = 0; xx < scale; xx++)
                            {
                                int bx = ox + sx * scale + xx, by = oy + sy * scale + yy;
                                if (bx >= 0 && bx < W && by >= 0 && by < H) sheet[by * W + bx] = outc;
                            }
                    }
            }
        WritePng(sheet, W, H, Path.Combine(outDir, "workers.png"));
    }

    // Render the per-age player mounts (rows = age 0..4, cols = walk frame 0..2) at white tint,
    // exactly as PlayerAvatar shows them.
    static void DumpMounts(string outDir, Color groundBg)
    {
        const int rows = 5, cols = 3, scale = 4, cell = Native * scale, pad = 12;
        int W = pad + cols * (cell + pad);
        int H = pad + rows * (cell + pad);
        var sheet = Fill(W, H, groundBg);
        for (int age = 0; age < rows; age++)
            for (int f = 0; f < cols; f++)
            {
                var sp = PlaceholderArt.PlayerMount(age, f);
                if (sp == null) continue;
                var tex = sp.texture; var src = tex.GetPixels32();
                int tw = tex.width, th = tex.height;
                int ox = pad + f * (cell + pad);
                int oyTop = H - pad - age * (cell + pad);
                int oy = oyTop - cell;
                for (int sy = 0; sy < th; sy++)
                    for (int sx = 0; sx < tw; sx++)
                    {
                        Color32 sc = src[sy * tw + sx];
                        float a = sc.a / 255f; if (a <= 0f) continue;
                        Color outc = new Color(
                            groundBg.r * (1 - a) + (sc.r / 255f) * a,
                            groundBg.g * (1 - a) + (sc.g / 255f) * a,
                            groundBg.b * (1 - a) + (sc.b / 255f) * a, 1f);
                        for (int yy = 0; yy < scale; yy++)
                            for (int xx = 0; xx < scale; xx++)
                            {
                                int bx = ox + sx * scale + xx, by = oy + sy * scale + yy;
                                if (bx >= 0 && bx < W && by >= 0 && by < H) sheet[by * W + bx] = outc;
                            }
                    }
            }
        WritePng(sheet, W, H, Path.Combine(outDir, "mounts.png"));
    }

    static Color[] Fill(int w, int h, Color bg)
    {
        var px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = bg;
        return px;
    }

    // Blit a 64x64 sprite (with transparency) upscaled by Scale onto the sheet at (ox,oy) bottom-left,
    // multiplying by `tint` and compositing over `bg`.
    static void Blit(Color[] dst, int W, int H, Color32[] src, int tw, int th, int ox, int oy, Color tint, Color bg)
    {
        for (int sy = 0; sy < th; sy++)
            for (int sx = 0; sx < tw; sx++)
            {
                Color32 s = src[sy * tw + sx];
                float a = s.a / 255f;
                if (a <= 0f) continue;
                float rr = (s.r / 255f) * tint.r;
                float gg = (s.g / 255f) * tint.g;
                float bb = (s.b / 255f) * tint.b;
                Color outc = new Color(bg.r * (1 - a) + rr * a, bg.g * (1 - a) + gg * a, bg.b * (1 - a) + bb * a, 1f);
                int bx = ox + sx * Scale, by = oy + sy * Scale;
                for (int yy = 0; yy < Scale; yy++)
                    for (int xx = 0; xx < Scale; xx++)
                    {
                        int px = bx + xx, py = by + yy;
                        if (px < 0 || px >= W || py < 0 || py >= H) continue;
                        dst[py * W + px] = outc;
                    }
            }
    }

    static void WritePng(Color[] px, int w, int h, string path)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }
}
