using System;
using System.Collections.Generic;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Plain C# (no MonoBehaviour): the pure simulation side. Used both for the
    /// player's carried items and for each building's internal buffer/store.
    /// An optional capacity caps the TOTAL item count (0 = unlimited).
    /// </summary>
    public class Inventory
    {
        private readonly Dictionary<ItemDefinition, int> _items = new();

        public event Action Changed;

        /// <summary>Max total item count across all types. 0 = unlimited.</summary>
        public int capacity = 0;

        public IReadOnlyDictionary<ItemDefinition, int> Items => _items;

        public int Total()
        {
            int t = 0;
            foreach (var kv in _items) t += kv.Value;
            return t;
        }

        /// <summary>Adds up to `amount`, respecting capacity. Returns the amount actually added.</summary>
        public int Add(ItemDefinition item, int amount = 1)
        {
            if (item == null || amount <= 0) return 0;

            int accepted = amount;
            if (capacity > 0)
            {
                int space = capacity - Total();
                if (space <= 0) return 0;
                accepted = Mathf.Min(accepted, space);
            }

            _items.TryGetValue(item, out int current);
            _items[item] = current + accepted;
            Changed?.Invoke();
            return accepted;
        }

        /// <summary>Removes exactly `amount` if available; otherwise removes nothing.</summary>
        public bool TryRemove(ItemDefinition item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;
            _items.TryGetValue(item, out int current);
            if (current < amount) return false;
            _items[item] = current - amount;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Removes up to `amount`; returns how many were actually removed.</summary>
        public int RemoveUpTo(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return 0;
            _items.TryGetValue(item, out int current);
            int taken = Mathf.Min(current, amount);
            if (taken > 0)
            {
                _items[item] = current - taken;
                Changed?.Invoke();
            }
            return taken;
        }

        public int Count(ItemDefinition item)
        {
            _items.TryGetValue(item, out int c);
            return c;
        }
    }
}
