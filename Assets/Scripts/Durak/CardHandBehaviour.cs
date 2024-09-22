using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Durak
{
    public class CardHandManager : MonoBehaviour
    {
        [SerializeField] private List<GameObject> cardsInHand = new(); // List of cards in hand
        [SerializeField][Range(0f, 1f)] private float cardOverlapPercentage = 0.3f; // How much each card should overlap
        [SerializeField] private float hoverHeight = 50f; // The height offset for hovering cards

        private readonly List<Vector3> _cardAnchoredBasePosition = new();

        public bool CanHover { get; set; } = true;
        
        // Add a card to the hand
        public void AddCard(GameObject cardGo)
        {
            cardsInHand.Add(cardGo);
            cardGo.transform.SetParent(transform, true); // Set card parent to the CardHand object
            cardGo.transform.rotation = Quaternion.identity;
            UpdateCardPositions();
        }

        public void RemoveCard(GameObject card)
        {
            cardsInHand.Remove(card);
            UpdateCardPositions();
        }

        // Update card positions using RectTransform
        private void UpdateCardPositions()
        {
            //var completedTweens = DOTween.Complete(transform);
            _cardAnchoredBasePosition.Clear();

            if (cardsInHand.Count == 0) return;
            
            float cardWidth = cardsInHand[0].GetComponent<RectTransform>().rect.width;
            float totalWidth = (cardsInHand.Count - 1) * (cardWidth - (cardWidth * cardOverlapPercentage)); // Total width with overlap
            float startX = -totalWidth / 2; // Start positioning from the center of the hand

            for (int i = 0; i < cardsInHand.Count; i++)
            {
                RectTransform cardRect = cardsInHand[i].GetComponent<RectTransform>();
                float posX = startX + i * (cardWidth - (cardWidth * cardOverlapPercentage));
                cardRect.anchoredPosition = new Vector2(posX, 0); // Position cards horizontally
                _cardAnchoredBasePosition.Add(new Vector2(posX, 0));
            }
        }

        // Called when a card is hovered over
        public void OnCardHover(GameObject hoveredCard)
        {
            if (!CanHover) return;
            
            //TODO: this always must be calculated from base positions of the card
            float cardWidth = hoveredCard.GetComponent<RectTransform>().rect.width;

            for (int i = 0; i < cardsInHand.Count; i++)
            {
                GameObject cardGo = cardsInHand[i];
                RectTransform cardRect = cardGo.GetComponent<RectTransform>();

                if (cardGo == hoveredCard)
                {
                    //cardRect.anchoredPosition = new Vector2(cardRect.anchoredPosition.x, hoverHeight); // Move the hovered card up
                    cardRect.DOAnchorPos(new Vector2(_cardAnchoredBasePosition[i].x, hoverHeight), 0.2f);
                }
                else if (i < cardsInHand.IndexOf(hoveredCard))
                {
                    // Move cards to the left of the hovered card slightly to the left
                    //cardRect.anchoredPosition = new Vector2(cardRect.anchoredPosition.x - (cardWidth * cardOverlapPercentage), 0);
                    cardRect.DOAnchorPos(new Vector2(_cardAnchoredBasePosition[i].x - (cardWidth * cardOverlapPercentage), 0), 0.2f);
                }
                else
                {
                    // Move cards to the right of the hovered card slightly to the right
                    //cardRect.anchoredPosition = new Vector2(cardRect.anchoredPosition.x + (cardWidth * cardOverlapPercentage), 0);
                    cardRect.DOAnchorPos(new Vector2(_cardAnchoredBasePosition[i].x + (cardWidth * cardOverlapPercentage), 0), 0.2f);
                }
            }
        }

        // Called when the hover ends to reset all card positions
        public void OnCardHoverEnd(GameObject hoveredCard)
        {
            // Reset all card positions
            for (int i = 0; i < cardsInHand.Count; i++)
            {
                cardsInHand[i].GetComponent<RectTransform>().DOAnchorPos(_cardAnchoredBasePosition[i], 0.2f);
            }
        }
    }
}
