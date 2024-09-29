using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Durak
{
    public class CardHandManager : MonoBehaviour
    {
        [SerializeField][Range(0f, 1f)] private float cardOverlapPercentage = 0.3f;
        [SerializeField] private float hoverHeight = 50f;

        private readonly List<Vector3> _cardAnchoredBasePosition = new();
        private readonly List<RectTransform> _cardsInHand = new();

        public bool CanHover { get; set; } = true;
        
        public void AddCard(GameObject newCard)
        {
            _cardsInHand.Add(newCard.transform as RectTransform);
            newCard.transform.SetParent(transform, true); // Set card parent to the CardHand object
            newCard.transform.rotation = Quaternion.identity;
            newCard.transform.localScale = Vector3.one;
            UpdateCardPositions();
        }

        public void RemoveCard(GameObject oldCard)
        {
            _cardsInHand.Remove(oldCard.transform as RectTransform);
            oldCard.transform.SetParent(null);
            UpdateCardPositions();
        }

        private void UpdateCardPositions()
        {
            _cardAnchoredBasePosition.Clear();

            if (_cardsInHand.Count == 0) return;
            
            float cardWidth = _cardsInHand[0].rect.width;
            float totalWidth = (_cardsInHand.Count - 1) * (cardWidth - (cardWidth * cardOverlapPercentage));
            float startX = -totalWidth / 2;

            for (int i = 0; i < _cardsInHand.Count; i++)
            {
                RectTransform cardRect = _cardsInHand[i];
                float posX = startX + i * (cardWidth - (cardWidth * cardOverlapPercentage));
                cardRect.anchoredPosition = new Vector2(posX, 0);
                _cardAnchoredBasePosition.Add(new Vector2(posX, 0));
            }
        }

        public void OnCardHover(GameObject hoverCard)
        {
            if (!CanHover) return;
            
            //TODO: this always must be calculated from base positions of the card
            float cardWidth = ((RectTransform)hoverCard.transform).rect.width;

            for (int i = 0; i < _cardsInHand.Count; i++)
            {
                if (_cardsInHand[i].gameObject == hoverCard)
                {
                    _cardsInHand[i].DOAnchorPos(new Vector2(_cardAnchoredBasePosition[i].x, hoverHeight), 0.2f);
                }
                else if (i < _cardsInHand.IndexOf(_cardsInHand[i]))
                {
                    _cardsInHand[i].DOAnchorPos(new Vector2(_cardAnchoredBasePosition[i].x - (cardWidth * cardOverlapPercentage), 0), 0.2f);
                }
                else
                {
                    _cardsInHand[i].DOAnchorPos(new Vector2(_cardAnchoredBasePosition[i].x + (cardWidth * cardOverlapPercentage), 0), 0.2f);
                }
            }
        }

        public void OnCardHoverEnd()
        {
            for (int i = 0; i < _cardsInHand.Count; i++)
            {
                _cardsInHand[i].DOAnchorPos(_cardAnchoredBasePosition[i], 0.2f);
            }
        }
    }
}
