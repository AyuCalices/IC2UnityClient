using UnityEngine;

namespace Durak
{
    public class CardSpawner : MonoBehaviour
    {
        public CardDeck cardDeck;
        public CardController cardControllerPrefab; // Card prefab to instantiate
        public CardHandManager handManager; // Reference to hand manager

        // Example: Add a card when spacebar is pressed
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var card = cardDeck.DrawCard();
                InstantiateCard(card);
            }
            
            if (Input.GetKeyDown(KeyCode.M))
            {
                var cards = cardDeck.DrawCards(3);
                foreach (var card in cards)
                {
                    InstantiateCard(card);
                }
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