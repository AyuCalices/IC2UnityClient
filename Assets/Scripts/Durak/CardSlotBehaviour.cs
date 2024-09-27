using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.NetworkEvent;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Durak
{
    public class CardSlotBehaviour : NetworkObject, IDropHandler
    {
        [SerializeField] private PlayerCardsRuntimeDictionary playerCardsRuntimeDictionary;
        [SerializeField] private TrumpTypeFocus trumpType;
        
        private CardController _firstCardInteraction;
        private CardController _secondCardInteraction;

        public void Initialize(CardController firstCardInteraction)
        {
            //place
            _firstCardInteraction = firstCardInteraction;
            PlaceCard(_firstCardInteraction, true);

            int spawnIndex = firstCardInteraction.GetCardIndex(NetworkManager.Instance.LocalConnection);
            playerCardsRuntimeDictionary.GetCard(NetworkManager.Instance.LocalConnection, spawnIndex);
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            //is the second card slot empty?
            if (_secondCardInteraction) return;
            
            //has it the correct state?
            var pointerDrag = eventData.pointerDrag;
            if (pointerDrag.GetComponent<CardInteraction>().GetStateType() != typeof(DragState)) return;
            
            //can the card be placed?
            var cardController = pointerDrag.GetComponent<CardController>();
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
            
            //place
            _secondCardInteraction = cardController;
            PlaceCard(_secondCardInteraction, false);
        }

        private void PlaceCard(CardController targetCard, bool isTop)
        {
            targetCard.CardInteraction.ConfirmDrop();
            
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

    public readonly struct PlaceCardEvent : INetworkEvent
    {
        
        public void PerformEvent()
        {
        }
    }
}
