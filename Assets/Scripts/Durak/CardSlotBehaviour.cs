using UnityEngine;
using UnityEngine.EventSystems;

namespace Durak
{
    public class CardSlotBehaviour : MonoBehaviour, IDropHandler
    {
        [SerializeField] private TrumpTypeFocus trumpType;
        
        private CardController _firstCardInteraction;
        private CardController _secondCardInteraction;

        public void Initialize(CardController firstCardInteraction)
        {
            _firstCardInteraction = firstCardInteraction;
            _firstCardInteraction.CardInteraction.ConfirmDrop();
            
            PlaceCard(_firstCardInteraction, true);
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            if (_secondCardInteraction) return;
            
            var cardController = eventData.pointerDrag.GetComponent<CardController>();

            if (cardController.IsTrump(trumpType))
            {
                if (cardController.GetCardStrength(trumpType) <= _firstCardInteraction.GetCardStrength(trumpType)) return;
            }
            else if (cardController.CardType.Type == _firstCardInteraction.CardType.Type)
            {
                if (cardController.GetCardStrength(trumpType) <= _firstCardInteraction.GetCardStrength(trumpType)) return;
            }
            else
            {
                return;
            }
            
            _secondCardInteraction = cardController;
            _secondCardInteraction.CardInteraction.ConfirmDrop();

            PlaceCard(_secondCardInteraction, false);
        }

        private void PlaceCard(CardController targetCard, bool isTop)
        {
            targetCard.transform.SetParent(transform);
            targetCard.transform.localRotation = Quaternion.identity;
            
            var cardRect = targetCard.RectTransform;

            Vector2 anchor = isTop ? new Vector2(0, 1) : new Vector2(1, 0);
            cardRect.anchorMin = anchor;
            cardRect.anchorMax = anchor;
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition3D = isTop ? 
                new Vector3(cardRect.sizeDelta.x / 2, -cardRect.sizeDelta.y / 2, 0) : 
                new Vector3(-cardRect.sizeDelta.x / 2, cardRect.sizeDelta.y / 2, 0);
        }
    }
}
