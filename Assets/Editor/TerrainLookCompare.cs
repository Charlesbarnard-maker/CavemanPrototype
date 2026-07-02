using System.IO;
using UnityEngine;

namespace Caveman
{
    /// <summary>Headless A/B of the terrain look: bakes the SAME world once with the procedural grain and
    /// once with the hand-painted tiles, writing centre-crop PNGs for side-by-side review. Run with:
    ///   Unity -batchmode -quit -executeMethod Caveman.TerrainLookCompare.Run -logFile &lt;log&gt;
    /// Output dir: env var CAVEMAN_LOOK_OUT (falls back to %TEMP%).</summary>
    public static class TerrainLookCompare
    {
        public static void Run()
        {
            try
            {
                string outDir = System.Environment.GetEnvironmentVariable("CAVEMAN_LOOK_OUT");
                if (string.IsNullOrEmpty(outDir)) outDir = Path.GetTempPath();
                Directory.CreateDirectory(outDir);

                TerrainGrid.Generate(120, 77.7f, 22f); // fixed seed → identical world in both shots
                TerrainGrid.CarveCorridors(5, 100f, 1);

                Snapshot(false, Path.Combine(outDir, "terrain_before.png"));
                Snapshot(true, Path.Combine(outDir, "terrain_after.png"));
                Debug.Log("[TerrainLookCompare] DONE");
            }
            catch (System.Exception e) { Debug.LogError($"[TerrainLookCompare] EXCEPTION: {e}"); }
        }

        private static void Snapshot(bool paintedMode, string path)
        {
            TerrainGrid.UsePaintedTerrain = paintedMode;
            var old = GameObject.Find("Terrain");
            if (old != null) Object.DestroyImmediate(old);
            TerrainGrid.SpawnRenderer();

            var tex = TerrainGrid.MapTex;
            int cw = Mathf.Min(1100, tex.width), ch = Mathf.Min(820, tex.height);
            int cx = (tex.width - cw) / 2, cy = (tex.height - ch) / 2;
            var crop = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
            crop.SetPixels(tex.GetPixels(cx, cy, cw, ch));
            crop.Apply();
            File.WriteAllBytes(path, crop.EncodeToPNG());
            Debug.Log($"[TerrainLookCompare] wrote {path} ({cw}x{ch}, painted={paintedMode})");
        }
    }
}
