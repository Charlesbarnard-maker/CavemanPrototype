// Bespoke procedural art for the COLLECTOR WORKERS — the little people who walk out to a resource node and
// gather. Job-appropriate (axe/sledge/shovel/pick) + tech-progressing: stone -> bronze -> iron tool material
// across upgrade tiers 0-2, becoming a powered MACHINE harvester at tier 3 ("Powered Machine"). Clothing
// palette tracks the colony AGE so there's always an advancing feel. Rendered at white tint by WorkerUnit.
// 64x64, fy=0 bottom, centre pivot, baked full colour + a dark outline. Frames 0..2 are the walk cycle.
using UnityEngine;

namespace Caveman
{
    public static partial class PlaceholderArt
    {
        // job: 0 Wood(axe) 1 Stone(sledge) 2 Clay(shovel) 3 Ore(pick).  tier: 0..3 (3 = machine).
        public enum WorkerJob { Wood = 0, Stone = 1, Clay = 2, Ore = 3 }

        /// <summary>Map a produced item id to the worker job (its tool/animation theme).</summary>
        public static int JobForItem(string id)
        {
            switch (id)
            {
                case "stone": return (int)WorkerJob.Stone;
                case "clay": return (int)WorkerJob.Clay;
                case "ore":
                case "copper_ore": return (int)WorkerJob.Ore;
                default: return (int)WorkerJob.Wood; // wood + anything else → a chopper
            }
        }

        private static Sprite[] _worker; // [(job*4+tier)*5+age]*3+frame
        public static Sprite CollectorWorker(int job, int tier, int age, int frame)
        {
            job = Mathf.Clamp(job, 0, 3); tier = Mathf.Clamp(tier, 0, 3);
            age = Mathf.Clamp(age, 0, 4); frame = Mathf.Clamp(frame, 0, 2);
            if (_worker == null) _worker = new Sprite[4 * 4 * 5 * 3];
            int keyIdx = ((job * 4 + tier) * 5 + age) * 3 + frame;
            if (_worker[keyIdx] != null) return _worker[keyIdx];

            const int s = 64; var tex = NewTex(s); var px = new Color[s * s];
            if (tier >= 3) BakeHarvester(px, s, job, frame);
            else BakeHumanWorker(px, s, job, tier, age, frame);

            // dark outline around the silhouette
            var dark = new Color(0.08f, 0.06f, 0.06f, 1f);
            var outPx = new Color[s * s];
            System.Array.Copy(px, outPx, px.Length);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                if (px[y * s + x].a > 0.01f) continue;
                bool adj = false;
                for (int oy = -1; oy <= 1 && !adj; oy++) for (int ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0) continue;
                    int nx = x + ox, ny = y + oy;
                    if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                    if (px[ny * s + nx].a > 0.01f) { adj = true; break; }
                }
                if (adj) outPx[y * s + x] = dark;
            }
            tex.SetPixels(outPx); tex.Apply();
            _worker[keyIdx] = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _worker[keyIdx];
        }

        // ---- the human worker (tiers 0-2): age-palette body + a job tool whose head material steps by tier ----
        private static void BakeHumanWorker(Color[] px, int s, int job, int tier, int age, int frame)
        {
            // skin
            var skinL = new Color(0.93f, 0.76f, 0.59f, 1f);
            var skin = new Color(0.84f, 0.65f, 0.49f, 1f);
            var skinD = new Color(0.66f, 0.47f, 0.34f, 1f);
            var dark = new Color(0.10f, 0.08f, 0.08f, 1f);
            var eyeW = new Color(0.97f, 0.96f, 0.93f, 1f);

            Color clothL, clothM, clothD, hairL, hairM, hairD;
            AgePalette(age, out clothL, out clothM, out clothD, out hairL, out hairM, out hairD);

            // tool head material by tier: 0 stone, 1 bronze, 2 iron
            Color mH, mM, mD;
            ToolMetal(tier, out mH, out mM, out mD);
            var handleL = new Color(0.62f, 0.46f, 0.28f, 1f);
            var handleM = new Color(0.48f, 0.35f, 0.21f, 1f);
            var handleD = new Color(0.34f, 0.24f, 0.14f, 1f);

            // walk cycle
            float lLeg = frame == 1 ? 0.045f : frame == 2 ? -0.022f : 0f;
            float rLeg = frame == 2 ? 0.045f : frame == 1 ? -0.022f : 0f;
            float armSwing = frame == 1 ? 0.03f : frame == 2 ? -0.03f : 0f;

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1); float dxc = fx - 0.5f; Color c = Clear;

                // legs
                if (dxc >= -0.18f && dxc <= -0.04f && fy >= 0.07f + lLeg && fy <= 0.33f)
                { c = clothM; if (dxc <= -0.14f) c = clothD; else if (dxc >= -0.08f) c = clothL; if (fy <= 0.135f + lLeg) c = skinD; }
                if (dxc >= 0.04f && dxc <= 0.18f && fy >= 0.07f + rLeg && fy <= 0.33f)
                { c = clothM; if (dxc >= 0.14f) c = clothD; else if (dxc <= 0.08f) c = clothL; if (fy <= 0.135f + rLeg) c = skinD; }
                // feet
                if (dxc >= -0.20f && dxc <= -0.02f && fy >= 0.05f + lLeg && fy <= 0.085f + lLeg) c = skinD;
                if (dxc >= 0.02f && dxc <= 0.20f && fy >= 0.05f + rLeg && fy <= 0.085f + rLeg) c = skinD;

                // torso / tunic
                if (Mathf.Abs(dxc) <= 0.17f && fy >= 0.31f && fy <= 0.58f)
                { c = clothM; if (dxc > 0.08f || fy < 0.35f) c = clothD; else if (dxc < -0.05f && fy > 0.46f) c = clothL; }

                // left arm (hangs, swings)
                if (dxc >= -0.26f && dxc <= -0.16f && fy >= 0.36f + armSwing && fy <= 0.55f)
                { c = skin; if (dxc <= -0.22f) c = skinD; else c = skinL; }
                if (Disc(dxc + 0.21f, fy - (0.345f + armSwing), 0.045f)) { c = skin; if (dxc + 0.21f > 0.01f) c = skinD; else c = skinL; }
                // right arm raised to grip the tool
                if (dxc >= 0.16f && dxc <= 0.26f && fy >= 0.40f - armSwing && fy <= 0.58f)
                { c = skin; if (dxc >= 0.22f) c = skinD; else c = skinL; }
                if (Disc(dxc - 0.215f, fy - (0.45f - armSwing), 0.05f)) { c = skin; if (dxc - 0.215f > 0.01f) c = skinD; else c = skinL; }

                // head
                float hcy = 0.72f;
                bool head = Disc(dxc, fy - hcy, 0.125f);
                if (head) { c = skin; if (dxc < -0.03f && fy > hcy) c = skinL; else if (dxc > 0.05f || fy < hcy - 0.05f) c = skinD; }
                if (Mathf.Abs(dxc) <= 0.05f && fy >= 0.58f && fy <= 0.61f) c = skin; // neck
                if (head)
                {
                    if (Disc(dxc + 0.05f, fy - (hcy + 0.01f), 0.024f)) c = eyeW;
                    if (Disc(dxc + 0.048f, fy - (hcy + 0.006f), 0.012f)) c = dark;
                    if (Disc(dxc - 0.05f, fy - (hcy + 0.01f), 0.024f)) c = eyeW;
                    if (Disc(dxc - 0.052f, fy - (hcy + 0.006f), 0.012f)) c = dark;
                }
                // hair cap
                if (Disc(dxc, fy - (hcy + 0.07f), 0.13f) && fy >= hcy + 0.03f)
                { c = hairM; if (dxc < -0.02f) c = hairL; else if (dxc > 0.06f) c = hairD; }

                px[y * s + x] = c;
            }

            // ---- JOB TOOL, drawn over the right hand (hx) so a chop-rotation swings it ----
            float hx = 0.79f;
            DrawJobTool(px, s, job, hx, handleL, handleM, handleD, mH, mM, mD);
        }

        private static void DrawJobTool(Color[] px, int s, int job, float hx,
            Color handleL, Color handleM, Color handleD, Color mH, Color mM, Color mD)
        {
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1); Color c = Clear;

                // shaft common to all tools (a wooden handle up from the hand)
                if (fx >= hx - 0.020f && fx <= hx + 0.020f && fy >= 0.30f && fy <= 0.74f)
                { c = handleM; if (fx >= hx + 0.006f) c = handleD; else if (fx <= hx - 0.006f) c = handleL; }

                switch (job)
                {
                    case (int)WorkerJob.Wood: // AXE — a blade wedge at the top, biting left
                        if (fx >= hx - 0.12f && fx <= hx + 0.02f && fy >= 0.72f && fy <= 0.86f)
                        {
                            float t = (0.86f - fy) / 0.14f;                       // 0 top → 1 bottom
                            if (fx >= hx - 0.02f - 0.10f * t) { c = mM; if (fx < hx - 0.06f) c = mH; else if (fy < 0.75f) c = mD; }
                        }
                        break;
                    case (int)WorkerJob.Stone: // SLEDGE / MAUL — a big rectangular head
                        if (fx >= hx - 0.075f && fx <= hx + 0.075f && fy >= 0.74f && fy <= 0.86f)
                        { c = mM; if (fy < 0.78f || fx > hx + 0.03f) c = mD; else if (fy > 0.82f && fx < hx) c = mH; }
                        break;
                    case (int)WorkerJob.Clay: // SHOVEL — a rounded spade blade
                        if (Disc(fx - hx, fy - 0.80f, 0.075f) && fy <= 0.82f)
                        { c = mM; if (fx > hx + 0.01f) c = mD; else c = mH; }
                        if (fx >= hx - 0.022f && fx <= hx + 0.022f && fy >= 0.78f && fy <= 0.82f) c = mD; // socket
                        break;
                    default: // ORE — PICKAXE: a bold curved head, spikes dipping to points at both ends
                        {
                            float u = (fx - hx) / 0.105f;                       // -1..1 across the head
                            if (Mathf.Abs(u) <= 1f && fy >= 0.74f && fy <= 0.88f)
                            {
                                float arc = 0.835f - 0.055f * (u * u);          // higher centre, dips to the spikes
                                float thick = 0.022f * (1f - 0.72f * Mathf.Abs(u)); // taper to sharp points
                                if (Mathf.Abs(fy - arc) <= thick)
                                { c = mM; if (Mathf.Abs(u) > 0.6f) c = mH; else if (fy < arc) c = mD; }
                            }
                        }
                        break;
                }
                if (c.a > 0f) px[y * s + x] = c;
            }
        }

        // ---- the tier-3 MACHINE harvester: a treaded power unit with a job tool-head on an arm ----
        private static void BakeHarvester(Color[] px, int s, int job, int frame)
        {
            var bodyM = new Color(0.42f, 0.45f, 0.52f, 1f);   // steel body
            var bodyL = new Color(0.60f, 0.63f, 0.70f, 1f);
            var bodyD = new Color(0.26f, 0.28f, 0.34f, 1f);
            var tread = new Color(0.14f, 0.14f, 0.17f, 1f);
            var treadM = new Color(0.30f, 0.30f, 0.34f, 1f);
            var warn = new Color(0.95f, 0.78f, 0.20f, 1f);    // hazard stripe
            Color mH = new Color(0.78f, 0.80f, 0.86f, 1f), mM = new Color(0.56f, 0.58f, 0.64f, 1f), mD = new Color(0.34f, 0.36f, 0.42f, 1f);
            var glow = new Color(1f, 0.62f, 0.18f, 1f);
            float phase = frame * 0.33f;

            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float fx = x / (float)(s - 1), fy = y / (float)(s - 1); float dxc = fx - 0.5f; Color c = Clear;

                // tracked base
                if (fy >= 0.06f && fy <= 0.22f && Mathf.Abs(dxc) <= 0.34f)
                {
                    c = treadM;
                    if (Frac(fx * 12f + phase) < 0.45f) c = tread;                // moving track lugs
                    if (fy < 0.09f || fy > 0.19f) c = tread;
                }
                // hull
                if (fy >= 0.22f && fy <= 0.50f && Mathf.Abs(dxc) <= 0.30f)
                {
                    c = bodyM;
                    if (fy > 0.42f && dxc < 0.05f) c = bodyL; else if (dxc > 0.16f || fy < 0.27f) c = bodyD;
                    if (fy >= 0.235f && fy <= 0.275f && Frac(fx * 7f) < 0.5f) c = warn; // hazard stripe
                }
                // cab / vent + an engine glow
                if (fy >= 0.50f && fy <= 0.62f && dxc >= -0.20f && dxc <= 0.06f) { c = bodyD; if (fy > 0.57f) c = bodyM; }
                if (Disc(dxc + 0.10f, fy - 0.355f, 0.035f)) c = glow;             // status lamp
                // exhaust stack
                if (fx >= 0.18f && fx <= 0.235f && fy >= 0.50f && fy <= 0.66f) c = bodyD;

                // TOOL ARM reaching up-right, with the job head at the tip (the bit it works with)
                float ax = fx - (0.30f + (fy - 0.40f) * 0.55f);
                if (Mathf.Abs(ax) <= 0.024f && fy >= 0.38f && fy <= 0.72f) { c = mM; if (ax > 0.006f) c = mD; else c = mH; }
                // head at the tip ~ (0.476, 0.74) depending on job
                float tx = fx - 0.50f, ty = fy - 0.76f;
                switch (job)
                {
                    case (int)WorkerJob.Wood: // circular BUZZSAW
                        if (Disc(tx, ty, 0.085f)) { c = mM; if (Frac((Mathf.Atan2(ty, tx) / 6.2832f) * 10f + phase) < 0.4f) c = mH; if (tx * tx + ty * ty < 0.02f * 0.02f) c = mD; }
                        break;
                    case (int)WorkerJob.Stone: // JACKHAMMER chisel
                        if (Mathf.Abs(tx) <= 0.02f && fy >= 0.70f && fy <= 0.82f) c = mM;
                        if (Mathf.Abs(tx) <= 0.035f && fy >= 0.66f && fy <= 0.70f) c = mD;
                        break;
                    case (int)WorkerJob.Clay: // AUGER spiral bit
                        if (Mathf.Abs(tx) <= 0.03f && fy >= 0.66f && fy <= 0.84f) { c = mM; if (Frac(fy * 18f + phase) < 0.5f) c = mD; }
                        break;
                    default: // DRILL cone
                        if (fy >= 0.66f && fy <= 0.82f) { float w = (fy - 0.66f) / 0.16f * 0.05f; if (Mathf.Abs(tx) <= w) { c = mM; if (tx > 0f) c = mD; else c = mH; } }
                        break;
                }
                if (c.a > 0f) px[y * s + x] = c;
            }
        }

        // per-age clothing + hair palette (matches the Caveman costume progression)
        private static void AgePalette(int age, out Color clothL, out Color clothM, out Color clothD,
                                       out Color hairL, out Color hairM, out Color hairD)
        {
            switch (age)
            {
                case 0:
                    clothL = new Color(0.60f, 0.45f, 0.30f, 1f); clothM = new Color(0.46f, 0.33f, 0.21f, 1f); clothD = new Color(0.31f, 0.21f, 0.12f, 1f);
                    hairL = new Color(0.35f, 0.26f, 0.19f, 1f); hairM = new Color(0.23f, 0.16f, 0.11f, 1f); hairD = new Color(0.12f, 0.08f, 0.06f, 1f); break;
                case 1:
                    clothL = new Color(0.74f, 0.54f, 0.38f, 1f); clothM = new Color(0.59f, 0.41f, 0.28f, 1f); clothD = new Color(0.42f, 0.28f, 0.18f, 1f);
                    hairL = new Color(0.88f, 0.40f, 0.31f, 1f); hairM = new Color(0.72f, 0.27f, 0.22f, 1f); hairD = new Color(0.51f, 0.17f, 0.14f, 1f); break;
                case 2:
                    clothL = new Color(0.82f, 0.72f, 0.47f, 1f); clothM = new Color(0.66f, 0.56f, 0.34f, 1f); clothD = new Color(0.49f, 0.41f, 0.23f, 1f);
                    hairL = new Color(0.93f, 0.69f, 0.43f, 1f); hairM = new Color(0.80f, 0.55f, 0.30f, 1f); hairD = new Color(0.59f, 0.39f, 0.19f, 1f); break;
                case 3:
                    clothL = new Color(0.66f, 0.70f, 0.76f, 1f); clothM = new Color(0.50f, 0.54f, 0.60f, 1f); clothD = new Color(0.35f, 0.39f, 0.45f, 1f);
                    hairL = new Color(0.82f, 0.86f, 0.92f, 1f); hairM = new Color(0.66f, 0.70f, 0.76f, 1f); hairD = new Color(0.47f, 0.51f, 0.57f, 1f); break;
                default:
                    clothL = new Color(0.40f, 0.56f, 0.76f, 1f); clothM = new Color(0.26f, 0.42f, 0.62f, 1f); clothD = new Color(0.16f, 0.29f, 0.46f, 1f);
                    hairL = new Color(0.99f, 0.91f, 0.42f, 1f); hairM = new Color(0.92f, 0.80f, 0.25f, 1f); hairD = new Color(0.69f, 0.57f, 0.15f, 1f); break;
            }
        }

        // tool-head material by upgrade tier: 0 knapped stone, 1 bronze, 2 iron/steel
        private static void ToolMetal(int tier, out Color hi, out Color mid, out Color dk)
        {
            switch (tier)
            {
                case 0: hi = new Color(0.66f, 0.66f, 0.70f, 1f); mid = new Color(0.50f, 0.50f, 0.54f, 1f); dk = new Color(0.34f, 0.34f, 0.38f, 1f); break; // stone
                case 1: hi = new Color(0.86f, 0.60f, 0.32f, 1f); mid = new Color(0.68f, 0.45f, 0.23f, 1f); dk = new Color(0.48f, 0.30f, 0.15f, 1f); break; // bronze
                default: hi = new Color(0.80f, 0.82f, 0.88f, 1f); mid = new Color(0.58f, 0.60f, 0.66f, 1f); dk = new Color(0.38f, 0.40f, 0.46f, 1f); break; // iron/steel
            }
        }
    }
}
