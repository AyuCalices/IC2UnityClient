using System.Collections.Generic;
using UnityEngine;

namespace Durak
{
    public class RuntimeSet<T> : ScriptableObject
    {
        [SerializeField] protected List<T> items = new();
        
        private void OnEnable()
        {
            Restore();
        }
    
        public List<T> GetItems()
        {
            return items;
        }

        public void Add(T value)
        {
            if (!items.Contains(value))
            {
                items.Add(value);
                OnAfterAdd(value);
            }
        }
        
        protected virtual void OnAfterAdd(T value) {}

        public void Remove(T value)
        {
            if (items.Contains(value))
            {
                OnBeforeRemove(value);
                items.Remove(value);
            }
        }
        
        protected virtual void OnBeforeRemove(T value) {}

        public virtual void Restore()
        {
            OnBeforeRestore();
            items.Clear();
        }
        
        protected virtual void OnBeforeRestore() {}
    }
}
