using System;
using System.Collections.Generic;
using UnityEngine;

namespace Durak
{
    [CreateAssetMenu]
    public class TableCardsRuntimeSet : RuntimeSet<Card>
    {
        private readonly Dictionary<int, int> _strengthLookup = new();

        [ContextMenu("Print All")]
        public void PrintAll()
        {
            foreach (var (key, value) in _strengthLookup)
            {
                Debug.Log(key);
            }
        }

        protected override void OnAfterAdd(Card value)
        {
            _strengthLookup.TryAdd(value.CardStrength, 0);
            _strengthLookup[value.CardStrength]++;
        }

        protected override void OnBeforeRemove(Card value)
        {
            if (!_strengthLookup.TryGetValue(value.CardStrength, out int count))
            {
                Debug.LogWarning("Couldn't find the desired card!");
                return;
            }

            if (count > 1)
            {
                _strengthLookup[value.CardStrength]--;
            }
            else if (count == 1)
            {
                _strengthLookup.Remove(value.CardStrength);
            }
        }

        protected override void OnBeforeRestore()
        {
            _strengthLookup.Clear();
        }

        public bool ContainsStrength(int strength)
        {
            return _strengthLookup.ContainsKey(strength);
        }
    }
}
