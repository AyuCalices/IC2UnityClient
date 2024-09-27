using System.Collections.Generic;
using Plugins.EventNetworking.Core;
using UnityEngine;

namespace Durak
{
    [CreateAssetMenu]
    public class PlayerCardsRuntimeDictionary : RuntimeDictionary<NetworkConnection, List<Card>>
    {
        public int FindCardIndex(NetworkConnection networkConnection, Card card)
        {
            if (!Items.TryGetValue(networkConnection, out List<Card> cards))
            {
                Debug.LogWarning("Could not find the wanted card!");
                return -1;
            }

            if (!cards.Contains(card))
            {
                Debug.LogWarning("Could not find the wanted card!");
                return -1;
            }

            return cards.IndexOf(card);
        }

        public Card GetCard(NetworkConnection networkConnection, int index)
        {
            if (!Items.TryGetValue(networkConnection, out List<Card> cards))
            {
                Debug.LogWarning("Could not find the wanted card!");
                return null;
            }

            if (index >= cards.Count)
            {
                Debug.LogWarning("Could not find the wanted card!");
                return null;
            }

            return cards[index];
        }
    }
}
