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
        public System.Action reward;
        public string rewardText;
        public bool claimed;
        public bool isWin; // completing this quest wins the game
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
        private int _revealedThrough = -1; // highest Age whose objective set we've already popped centre-screen
        void Awake() => Instance = this;

        private int CurrentAge => Colony.Instance != null ? Colony.Instance.Age : 0;

        void Update()
        {
            _t += Time.deltaTime;
            if (_t < 0.5f) return;
            _t = 0f;
            int age = CurrentAge;

            // Reveal each newly-unlocked Age's objective SET centre-screen (incl. Age 0 at game start). Stepping
            // age-by-age means even an F3 multi-age skip still queues each Age's set in order.
            if (age > _revealedThrough)
            {
                for (int a = _revealedThrough + 1; a <= age; a++)
                {
                    var set = QuestsForAge(a);
                    if (set.Count > 0 && InventoryHud.Instance != null) InventoryHud.Instance.ShowObjectiveReveal(a, set);
                }
                _revealedThrough = age;
            }

            // Claim any UNLOCKED (age ≤ current) goal whose condition is met — in ANY order, so the player has
            // freedom in how they complete an Age. Completing one pays its reward + pops a toast.
            foreach (var q in quests)
            {
                if (q.claimed || q.age > age) continue;
                if (q.done != null && q.done())
                {
                    q.claimed = true;
                    q.reward?.Invoke();
                    if (q.isWin) Won = true;
                    Toast.Show($"<color=#9f9>✔ {q.title}</color>" + (string.IsNullOrEmpty(q.rewardText) ? "" : $"   <size=15>{q.rewardText}</size>"));
                }
            }
        }

        /// <summary>All goals belonging to a given Age (for the reveal popup + the journal).</summary>
        public List<Quest> QuestsForAge(int age)
        {
            var list = new List<Quest>();
            foreach (var q in quests) if (q.age == age) list.Add(q);
            return list;
        }

        /// <summary>The goals available to work on RIGHT NOW — unclaimed and unlocked by age, current Age first
        /// (then any leftovers from earlier Ages), capped at `max` for the HUD box.</summary>
        public IEnumerable<Quest> ActivePending(int max)
        {
            int age = CurrentAge, c = 0;
            for (int a = age; a >= 0; a--)
                foreach (var q in quests)
                {
                    if (q.age != a || q.claimed) continue;
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
