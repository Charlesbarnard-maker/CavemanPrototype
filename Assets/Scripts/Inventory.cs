using System;
using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Plain C# (no MonoBehaviour): the pure simulation side. Knows nothing about
    /// rendering or input. This separation is what lets the systems scale toward a
    /// factory game and even port to 3D later.
    /// </summary>
    public class Inventory
    {
        private readonly Dictionary<ItemDefinition, int> _items = new();

        /// <summary>Fired whenever a count changes (HUD listens to this).</summary>
        public event Action Changed;

        public IReadOnlyDictionary<ItemDefinition, int> Items => _items;

        public void Add(ItemDefinition item, int amount = 1)
        {
            if (item == null || amount <= 0) return;
            _items.TryGetValue(item, out int current);
            _items[item] = Mathf.Min(current + amount, item.maxStack);
            Changed?.Invoke();
        }

        public bool TryRemove(ItemDefinition item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;
            _items.TryGetValue(item, out int current);
            if (current < amount) return false;
            _items[item] = current - amount;
            Changed?.Invoke();
            return true;
        }

        public int Count(ItemDefinition item)
        {
            _items.TryGetValue(item, out int c);
            return c;
        }
    }
}
