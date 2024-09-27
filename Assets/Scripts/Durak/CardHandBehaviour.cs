using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Durak
{
    public class CardHandManager : MonoBehaviour
    {
        [SerializeField] private List<RectTransform> cardsInHand = new(); // List of cards in hand
        [SerializeField][Range(0f, 1f)] private float cardOverlapPercentage = 0.3f; // How much each card should overlap
        [SerializeField] private float hoverHeight = 50f; // The height offset for hovering cards

        private readonly List<Vector3> _cardAnchoredBasePosition = new();

        public bool CanHover { get; set; } = true;
        
        public void AddCard(GameObject newCard)
        {
            cardsInHand.Add(newCard.GetComponent<RectTransform>());
            newCard.transform.SetParent(transform, true); // Set card parent to the CardHand object
            newCard.transform.rotation = Quaternion.identity;
            UpdateCardPositions();
        }

        public void RemoveCard(GameObject oldCard)
        {
            cardsInHand.Remove(oldCard.GetComponent<RectTransform>());
            oldCard.transform.SetParent(null);
            UpdateCardPositions();
        }

        private void UpdateCardPositions()
        {
            _cardAnchoredBasePosition.Clear();

            if (cardsInHand.Count == 0) return;
            
            float cardWidth = cardsInHand[0].rect.width;
            float totalWidth = (cardsInHand.Count - 1) * (cardWidth - (cardWidth * cardOverlapPercentage));
            float startX = -totalWidth / 2;

            for (int i = 0; i < cardsInHand.Count; i++)
            {
                RectTransform cardRect = cardsInHand[i];
                float posX = startX + i * (cardWidth - (cardWidth * cardOverlapPercentage));
                cardRect.anchoredPosition = new Vector2(posX, 0);
                _cardAnchoredBasePosition.Add(new Vector2(posX, 0));
            }
        }

        public void OnCardHover(GameObject hoverCard)
        {
            if (!CanHover) return;
            
            //TODO: this always must be calculated from base positions of the card
            float cardWidth = hoverCard.GetComponent<RectTransform>().rect.width;

            for (int i = 0; i < cardsInHand.Count; i++)
            {
                if (cardsInHand[i].gameObject == hoverCard)
                {
                    cardsInHand[i].DOAnchorPos(new Vector2(_cardAnchoredBasePosition[i].x, hoverHeight), 0.2f);
                }
                else if (i < cardsInHand.IndexOf(cardsInHand[i]))
                {
                    cardsInHand[i].DOAnchorPos(new Vector2(_cardAnchoredBasePosition[i].x - (cardWidth * cardOverlapPercentage), 0), 0.2f);
                }
                else
                {
                    cardsInHand[i].DOAnchorPos(new Vector2(_cardAnchoredBasePosition[i].x + (cardWidth * cardOverlapPercentage), 0), 0.2f);
                }
            }
        }

        public void OnCardHoverEnd()
        {
            for (int i = 0; i < cardsInHand.Count; i++)
            {
                cardsInHand[i].DOAnchorPos(_cardAnchoredBasePosition[i], 0.2f);
            }
        }
    }
}
