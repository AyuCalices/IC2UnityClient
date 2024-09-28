using System.Collections.Generic;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using UnityEngine;

namespace Durak
{
    public class CardSpawner : NetworkObject
    {
        [SerializeField] private PlayerCardsRuntimeDictionary playerCardsRuntimeDictionary;
        [SerializeField] private CardDeck cardDeck;
        [SerializeField] private CardController cardControllerPrefab; 
        [SerializeField] private CardHandManager handManager;
        [SerializeField] private int targetCardCount;

        public void DrawCardsForAll()
        {
            foreach (var connection in NetworkManager.Instance.LobbyConnections)
            {
                DrawCardsForPlayer(connection, targetCardCount);
            }
        }

        /// <summary>
        /// No Race-Condition security!
        /// </summary>
        public void ForceAddCardForPlayer(NetworkConnection networkConnection, int count = 1)
        {
            var cards = SaveGetCards(networkConnection);
                
            var newCards = DrawCards(cards, cards.Count + count);

            if (networkConnection.Equals(NetworkManager.Instance.LocalConnection))
            {
                InstantiateHandCards(newCards);
            }
        }

        private void DrawCardsForPlayer(NetworkConnection networkConnection, int cardCount)
        {
            var cards = SaveGetCards(networkConnection);
                
            var newCards = DrawCards(cards, cardCount);

            if (networkConnection.Equals(NetworkManager.Instance.LocalConnection))
            {
                InstantiateHandCards(newCards);
            }
        }

        private List<Card> SaveGetCards(NetworkConnection networkConnection)
        {
            if (!playerCardsRuntimeDictionary.TryGetValue(networkConnection, out List<Card> cards))
            {
                cards = new List<Card>();
                playerCardsRuntimeDictionary.Add(networkConnection, cards);
            }

            return cards;
        }

        private List<Card> DrawCards(List<Card> cards, int targetCount)
        {
            var newCards = cardDeck.DrawCards(targetCount - cards.Count);
            cards.AddRange(newCards);
            return newCards;
        }

        private void InstantiateHandCards(List<Card> cards)
        {
            foreach (var card in cards)
            {
                InstantiateHandCard(card);
            }
        }

        private void InstantiateHandCard(Card card)
        {
            CardController newCard = Instantiate(cardControllerPrefab, transform, false);
            newCard.SetCard(card, typeof(HandState));
        }
    }
}