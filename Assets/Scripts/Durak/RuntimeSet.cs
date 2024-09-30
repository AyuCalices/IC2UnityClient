using System;
using System.Collections.Generic;
using UnityEngine;

namespace Durak
{
    public abstract class RuntimeSet<T> : ScriptableObject
    {
        [field: SerializeField] protected List<T> items = new();

        public event Action<T> OnItemAdded;
        public event Action<T> OnItemRemoved;
        public event Action OnItemsCleared;

        public List<T> GetItems() => items;

        public void AddItem(T item)
        {
            if (!items.Contains(item))
            {
                items.Add(item);
                OnItemAdded?.Invoke(item);
            }
        }

        public void RemoveItem(T item)
        {
            if (items.Contains(item))
            {
                OnItemRemoved?.Invoke(item);
                items.Remove(item);
            }
        }
        
        public void Restore()
        {
            items.Clear();
            OnItemsCleared?.Invoke();
        }
    }
}
