using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Durak
{
    public abstract class RuntimeDictionary<TKey, TValue> : ScriptableObject
    {
        protected readonly Dictionary<TKey, TValue> Items = new();
        
        private void OnEnable()
        {
            Restore();
        }
    
        public Dictionary<TKey, TValue> GetItems()
        {
            return Items;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return Items.TryGetValue(key, out value);
        }

        public bool ContainsKey(TKey key)
        {
            return Items.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            Items.Add(key, value);
        }

        public void Remove(TKey key)
        {
            Items.Remove(key);
        }

        public void Restore()
        {
            Items.Clear();
        }
    }
}
