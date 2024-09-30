using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Durak.States;
using UnityEngine;

namespace Durak
{
    public class CardHandManager : MonoBehaviour
    {
        [Header("Parent Size Fitting")] 
        [SerializeField] private RectTransform rectTransform;
        
        [Header("Setting")]
        [SerializeField][Range(0f, 1f)] private float cardOverlapPercentage = 0.3f;
        [SerializeField] private float hoverHeight = 50f;

        private readonly List<Vector3> _cardAnchoredBasePosition = new();
        private readonly List<RectTransform> _cardsInHand = new();
        private float currentBaseWidth;
        private GameObject currentHoverCard;

        public bool CanHover { get; set; } = true;

        private void Awake()
        {
            TurnStateController.OnGameComplete += DestroyCards;
        }

        private void OnDestroy()
        {
            TurnStateController.OnGameComplete -= DestroyCards;
        }

        private void DestroyCards()
        {
            for (var i = _cardsInHand.Count - 1; i >= 0; i--)
            {
                Destroy(_cardsInHand[i].gameObject);
                RemoveCard(_cardsInHand[i].gameObject);
            }
            _cardsInHand.Clear();
        }

        public void AddCard(GameObject newCard)
        {
            _cardsInHand.Add(newCard.transform as RectTransform);
            newCard.transform.SetParent(transform, true); // Set card parent to the CardHand object
            newCard.transform.rotation = Quaternion.identity;
            newCard.transform.localScale = Vector3.one;
            
            UpdateCardPositions();
            
            if (currentHoverCard != null)
            {
                OnCardHover(currentHoverCard);
            }
        }

        public void RemoveCard(GameObject oldCard)
        {
            _cardsInHand.Remove(oldCard.transform as RectTransform);
            oldCard.transform.SetParent(null);
            UpdateCardPositions();

            if (currentHoverCard != null)
            {
                OnCardHover(currentHoverCard);
            }
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

            currentBaseWidth = totalWidth + cardWidth;
            rectTransform.sizeDelta = new Vector2(currentBaseWidth, rectTransform.sizeDelta.y);
        }

        public void OnCardHover(GameObject hoverCard)
        {
            if (!CanHover) return;

            currentHoverCard = hoverCard;
            RectTransform hoverCardRect = (RectTransform)hoverCard.transform;
            float cardWidth = hoverCardRect.rect.width;

            for (int i = 0; i < _cardsInHand.Count; i++)
            {
                if (_cardsInHand[i].gameObject == hoverCard)
                {
                    _cardsInHand[i].DOAnchorPos(new Vector2(_cardAnchoredBasePosition[i].x, hoverHeight), 0.2f);
                }
                else if (i < _cardsInHand.IndexOf(hoverCardRect))
                {
                    _cardsInHand[i].DOAnchorPos(new Vector2(_cardAnchoredBasePosition[i].x - (cardWidth * cardOverlapPercentage), 0), 0.2f);
                }
                else
                {
                    _cardsInHand[i].DOAnchorPos(new Vector2(_cardAnchoredBasePosition[i].x + (cardWidth * cardOverlapPercentage), 0), 0.2f);
                }
            }

            float widthAddition = cardWidth * cardOverlapPercentage * 2;
            rectTransform.sizeDelta = new Vector2(currentBaseWidth + widthAddition, rectTransform.sizeDelta.y);
        }

        public void OnCardHoverEnd()
        {
            currentHoverCard = null;
            
            for (int i = 0; i < _cardsInHand.Count; i++)
            {
                _cardsInHand[i].DOAnchorPos(_cardAnchoredBasePosition[i], 0.2f);
            }
        }
    }
}
