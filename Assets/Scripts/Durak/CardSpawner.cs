using UnityEngine;

namespace Durak
{
    public class CardSpawner : MonoBehaviour
    {
        public GameObject cardPrefab; // Card prefab to instantiate
        public CardHandManager handManager; // Reference to hand manager

        // Example: Add a card when spacebar is pressed
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                GameObject newCard = Instantiate(cardPrefab, transform, true);
                handManager.AddCard(newCard);
            }
        }
    }
}