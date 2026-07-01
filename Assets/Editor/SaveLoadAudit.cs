using System.IO;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Headless check for the TERRAIN serialization — the largest, run-length-encoded save blob. This part is
    /// lifecycle-free (pure static TerrainGrid), so unlike the building round-trip it CAN run in batch edit mode:
    ///   Unity -batchmode -quit -executeMethod Caveman.SaveLoadAudit.Run -logFile &lt;log&gt;
    /// It generates a world, serializes it, regenerates a DIFFERENT world, deserializes the saved bytes back over
    /// it, and asserts the biome map is restored exactly (and actually changed in between). The building/route/
    /// inventory round-trip needs the MonoBehaviour lifecycle, so it's validated in-game via SaveSystem.SelfTest
    /// (press F10 while playing).
    /// </summary>
    public static class SaveLoadAudit
    {
        public static void Run()
        {
            try
            {
                TerrainGrid.Generate(80, 42.5f, 12f);
                int sumA = Checksum();

                var ms = new MemoryStream();
                using (var w = new BinaryWriter(ms, System.Text.Encoding.UTF8, true)) TerrainGrid.SerializeTo(w);
                long bytes = ms.Length;
                ms.Position = 0;

                TerrainGrid.Generate(80, 999.9f, 12f); // a genuinely different world
                int sumMid = Checksum();

                using (var r = new BinaryReader(ms)) TerrainGrid.DeserializeFrom(r);
                int sumB = Checksum();

                bool pass = sumA == sumB && sumA != sumMid;
                Debug.Log($"[SaveLoadAudit] terrain: A={sumA} mid(diff world)={sumMid} B(restored)={sumB} rleBytes={bytes}");
                if (pass) Debug.Log("[SaveLoadAudit] RESULT PASS — terrain serialize/deserialize is exact.");
                else Debug.LogError("[SaveLoadAudit] RESULT FAIL — terrain did not round-trip (A != B, or the world never changed).");
            }
            catch (System.Exception e) { Debug.LogError($"[SaveLoadAudit] EXCEPTION: {e}"); }
        }

        static int Checksum()
        {
            int s = 17;
            for (int y = -80; y <= 80; y++)
                for (int x = -80; x <= 80; x++)
                    s = s * 31 + (int)TerrainGrid.At(x, y);
            return s;
        }
    }
}
