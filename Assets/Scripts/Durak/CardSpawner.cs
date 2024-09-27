using System.Collections.Generic;
using EventNetworking.Component;
using EventNetworking.Core;
using UnityEngine;

namespace Durak
{
    public class CardSpawner : MonoBehaviour
    {
        [SerializeField] private CardDeck cardDeck;
        [SerializeField] private CardController cardControllerPrefab; 
        [SerializeField] private CardHandManager handManager;
        [SerializeField] private int targetCardCount;

        private readonly Dictionary<NetworkConnection, List<Card>> _playerCards = new();

        public void DrawCardsForAll()
        {
            foreach (var connection in NetworkManager.Instance.LobbyConnections)
            {
                DrawCardsForPlayer(connection);
            }
        }

        private void DrawCardsForPlayer(NetworkConnection networkConnection)
        {
            var cards = SaveGetCards(networkConnection);
                
            var newCards = DrawCards(cards, targetCardCount);

            if (networkConnection.Equals(NetworkManager.Instance.LocalConnection))
            {
                InstantiateCards(newCards);
            }
        }

        private List<Card> SaveGetCards(NetworkConnection networkConnection)
        {
            if (!_playerCards.TryGetValue(networkConnection, out List<Card> cards))
            {
                cards = new List<Card>();
                _playerCards.Add(networkConnection, cards);
            }

            return cards;
        }

        private List<Card> DrawCards(List<Card> cards, int targetCount)
        {
            var newCards = cardDeck.DrawCards(targetCount - cards.Count);
            cards.AddRange(newCards);
            return newCards;
        }

        private void InstantiateCards(List<Card> cards)
        {
            foreach (var card in cards)
            {
                InstantiateCard(card);
            }
        }

        private void InstantiateCard(Card card)
        {
            CardController newCard = Instantiate(cardControllerPrefab, transform, false);
            newCard.SetCard(card);
            handManager.AddCard(newCard.gameObject);
        }
    }
}