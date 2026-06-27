using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>A single guided goal: a condition to meet and an optional reward.</summary>
    public class Quest
    {
        public string title;
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
        void Awake() => Instance = this;

        void Update()
        {
            _t += Time.deltaTime;
            if (_t < 0.5f) return;
            _t = 0f;

            int shown = 0;
            foreach (var q in quests)
            {
                if (q.claimed) continue;
                if (q.done != null && q.done())
                {
                    q.claimed = true;
                    q.reward?.Invoke();
                    if (q.isWin) Won = true;
                    Toast.Show($"<color=#9f9>✔ {q.title}</color>" + (string.IsNullOrEmpty(q.rewardText) ? "" : $"   <size=15>{q.rewardText}</size>"));
                    continue;
                }
                if (++shown >= 3) break; // only the first 3 pending are "active"
            }
        }

        /// <summary>The first `n` not-yet-completed goals (for the HUD list).</summary>
        public IEnumerable<Quest> Active(int n)
        {
            int c = 0;
            foreach (var q in quests)
            {
                if (q.claimed) continue;
                yield return q;
                if (++c >= n) yield break;
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
