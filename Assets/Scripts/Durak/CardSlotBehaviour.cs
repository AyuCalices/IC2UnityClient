using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Durak
{
    public class CardSlotBehaviour : MonoBehaviour, IDropHandler
    {
        private CardBehaviour _firstCard;
        private CardBehaviour _secondCard;

        public void Initialize(CardBehaviour firstCard)
        {
            _firstCard = firstCard;
            _firstCard.ConfirmDrop();
            
            _firstCard.transform.SetParent(transform);
            _firstCard.transform.localRotation = Quaternion.identity;
            
            var firstCardRect = _firstCard.GetComponent<RectTransform>();
            firstCardRect.anchorMin = new Vector2(0, 1);
            firstCardRect.anchorMax = new Vector2(0, 1);
            firstCardRect.pivot = new Vector2(0.5f, 0.5f);
            firstCardRect.anchoredPosition3D = new Vector3(firstCardRect.sizeDelta.x / 2, -firstCardRect.sizeDelta.y / 2, 0);
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            _secondCard = eventData.pointerDrag.GetComponent<CardBehaviour>();
            _secondCard.ConfirmDrop();
            
            _secondCard.transform.SetParent(transform);
            var secondCardRect = _secondCard.GetComponent<RectTransform>();
            
            secondCardRect.anchorMin = new Vector2(1, 0); // Anchor to lower-right corner
            secondCardRect.anchorMax = new Vector2(1, 0);
            secondCardRect.pivot = new Vector2(0.5f, 0.5f);
            secondCardRect.anchoredPosition3D = new Vector3(-secondCardRect.sizeDelta.x / 2, secondCardRect.sizeDelta.y / 2, 0);
        }
    }
}
