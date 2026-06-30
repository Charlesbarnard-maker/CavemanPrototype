using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>A single guided goal: a condition to meet and an optional reward.</summary>
    public class Quest
    {
        public string title;
        public int age;          // the Age this goal belongs to — its whole set unlocks together when you reach it
        public System.Func<bool> done;
        public System.Func<(int have, int need)> progress; // optional live counter for the HUD (e.g. 7/15); null = none
        public System.Action reward;
        public string rewardText;
        public bool claimed;
        public bool isWin; // completing this quest wins the game
        public BuildingDefinition highlightBuilding; // the build-menu entry to flash while this goal is active (null = none)
    }

    /// <summary>
    /// The guided objectives ladder — the main "what do I do next / why advance" hook.
    /// Shows the next few goals; completing one pays a reward, pops a toast, and reveals
    /// the next, pulling the player through the ages.
    /// </summary>
    public class Objectives : MonoBehaviour
    {
        public static Objectives Instance { get; private set; }
        public List<Quest> quests = new();

        /// <summary>Set once the win quest is completed — drives the victory banner.</summary>
        public bool Won { get; private set; }

        private float _t;
        private int _poppedForAge = -1; // highest Age whose FIRST step we've already popped centre-screen
        void Awake() => Instance = this;

        /// <summary>The current step — the first goal not yet done (null = all complete). Objectives advance ONE
        /// at a time, in order, so the player always has a single clear "do this next".</summary>
        public Quest CurrentStep()
        {
            foreach (var q in quests) if (!q.claimed) return q;
            return null;
        }

        void Update()
        {
            _t += Time.deltaTime;
            if (_t < 0.5f) return;
            _t = 0f;

            // SEQUENTIAL: only the current step can complete. Claim it (and any already-satisfied steps behind it)
            // in order — so progression is simple, one thing at a time: wood, then stone, then…
            while (true)
            {
                var cur = CurrentStep();
                if (cur == null) break;
                if (cur.done != null && cur.done())
                {
                    cur.claimed = true;
                    cur.reward?.Invoke();
                    if (cur.isWin) Won = true;
                    Toast.Show($"<color=#9f9>✔ {cur.title}</color>" + (string.IsNullOrEmpty(cur.rewardText) ? "" : $"   <size=15>{cur.rewardText}</size>"));
                    continue; // the next step may already be satisfied → keep claiming in order
                }
                break;
            }

            // Pop a centre-screen popup for the FIRST step of each Age as you reach it (incl. Age 0 at game start)
            // — ONE objective at a time, not the whole set. Between popups the top-right box steps you onward.
            var step = CurrentStep();
            if (step != null && step.age > _poppedForAge && InventoryHud.Instance != null)
            {
                _poppedForAge = step.age;
                InventoryHud.Instance.ShowObjectiveReveal(step);
            }
        }

        /// <summary>All goals belonging to a given Age (for the journal grouping).</summary>
        public List<Quest> QuestsForAge(int age)
        {
            var list = new List<Quest>();
            foreach (var q in quests) if (q.age == age) list.Add(q);
            return list;
        }

        /// <summary>The next `max` goals to work on, in order (current step first) — for the HUD box.</summary>
        public IEnumerable<Quest> ActivePending(int max)
        {
            int c = 0;
            foreach (var q in quests)
            {
                if (q.claimed) continue;
                yield return q;
                if (++c >= max) yield break;
            }
        }

        public bool AllDone
        {
            get { foreach (var q in quests) if (!q.claimed) return false; return true; }
        }
    }

    /// <summary>Transient on-screen messages (objective complete, age reached, …).</summary>
    public static class Toast
    {
        public class Item { public string msg; public float t; }
        public static readonly List<Item> Items = new();
        public static void Show(string msg) => Items.Add(new Item { msg = msg, t = 4.5f });
    }

    /// <summary>Tiny world-anchored "+N" popups for hand-gathering — they float up from the node
    /// and fade out so each manual harvest reads clearly. Drawn by InventoryHud.DrawGatherPopups.</summary>
    public static class GatherPopup
    {
        public const float Life = 0.85f; // seconds on screen
        public class Item { public Vector3 world; public string text; public Color color; public float t; }
        public static readonly List<Item> Items = new();
        public static void Show(Vector3 world, string text, Color color)
            => Items.Add(new Item { world = world, text = text, color = color, t = Life });
    }
}
