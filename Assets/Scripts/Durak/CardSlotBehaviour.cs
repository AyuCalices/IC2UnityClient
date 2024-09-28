using System.Collections;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using Plugins.EventNetworking.NetworkEvent;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Durak
{
    public class CardSlotBehaviour : NetworkObject, IDropHandler
    {
        [SerializeField] private PlayerCardsRuntimeDictionary playerCardsRuntimeDictionary;
        [SerializeField] private TrumpTypeFocus trumpType;
        
        public CardController FirstCardController { get; set; }
        public CardController SecondCardController { get; set; }

        public void Initialize(CardController firstCardController)
        {
            FirstCardController = firstCardController;
            PlaceCard(FirstCardController, true);
            StartCoroutine(FirstNetworkShareObject());
        }

        public void OnDrop(PointerEventData eventData)
        {
            //is the second card slot empty?
            if (SecondCardController) return;
            
            //has it the correct state?
            var pointerDrag = eventData.pointerDrag;
            if (pointerDrag.GetComponent<CardInteraction>().GetStateType() != typeof(DragState)) return;
            
            //can the card be placed?
            var cardController = pointerDrag.GetComponent<CardController>();
            if (cardController.IsTrump(trumpType))
            {
                if (cardController.GetCardStrength(trumpType) <= FirstCardController.GetCardStrength(trumpType)) return;
            }
            else if (cardController.CardType.Type == FirstCardController.CardType.Type)
            {
                if (cardController.GetCardStrength(trumpType) <= FirstCardController.GetCardStrength(trumpType)) return;
            }
            else
            {
                return;
            }
            
            //place
            SecondCardController = cardController;
            PlaceCard(SecondCardController, false);
            StartCoroutine(SecondNetworkShareObject());
        }

        private void PlaceCard(CardController targetCard, bool isTop)
        {
            targetCard.CardInteraction.ConfirmDrop();
            
            targetCard.transform.SetParent(transform);
            targetCard.transform.localRotation = Quaternion.identity;
            
            var cardRect = targetCard.transform as RectTransform;

            Vector2 anchor = isTop ? new Vector2(0, 1) : new Vector2(1, 0);
            cardRect.anchorMin = anchor;
            cardRect.anchorMax = anchor;
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition3D = isTop ? 
                new Vector3(cardRect.sizeDelta.x / 2, -cardRect.sizeDelta.y / 2, 0) : 
                new Vector3(-cardRect.sizeDelta.x / 2, cardRect.sizeDelta.y / 2, 0);
        }
        
        private IEnumerator FirstNetworkShareObject()
        {
            yield return null;

            var localConnection = NetworkManager.Instance.LocalConnection;
            var index = playerCardsRuntimeDictionary.FindCardIndex(localConnection, FirstCardController.Card);
            NetworkManager.Instance.NetworkShareObject(FirstCardController, x => new FirstPlaceCardEvent(this, x, localConnection, index));
        }
        
        private IEnumerator SecondNetworkShareObject()
        {
            yield return null;

            var localConnection = NetworkManager.Instance.LocalConnection;
            var index = playerCardsRuntimeDictionary.FindCardIndex(localConnection, SecondCardController.Card);
            NetworkManager.Instance.NetworkShareObject(SecondCardController, x => new SecondPlaceCardEvent(this, x, localConnection, index));
        }
    }

    public readonly struct FirstPlaceCardEvent : INetworkEvent
    {
        private readonly CardSlotBehaviour _cardSlotBehaviour;
        private readonly CardController _cardController;
        private readonly NetworkConnection _networkConnection;
        private readonly int _cardIndex;

        public FirstPlaceCardEvent(CardSlotBehaviour cardSlotBehaviour, CardController cardController, NetworkConnection networkConnection, int cardIndex)
        {
            _cardSlotBehaviour = cardSlotBehaviour;
            _cardController = cardController;
            _networkConnection = networkConnection;
            _cardIndex = cardIndex;
        }

        public void PerformEvent()
        {
            if (_networkConnection.Equals(NetworkManager.Instance.LocalConnection)) return;
            
            _cardSlotBehaviour.FirstCardController = _cardController;
            _cardController.SetCardByRuntimeDictionaryIndex(_networkConnection, _cardIndex, typeof(DroppedState));
        }
    }
    
    public readonly struct SecondPlaceCardEvent : INetworkEvent
    {
        private readonly CardSlotBehaviour _cardSlotBehaviour;
        private readonly CardController _cardController;
        private readonly NetworkConnection _networkConnection;
        private readonly int _cardIndex;

        public SecondPlaceCardEvent(CardSlotBehaviour cardSlotBehaviour, CardController cardController, NetworkConnection networkConnection, int cardIndex)
        {
            _cardSlotBehaviour = cardSlotBehaviour;
            _cardController = cardController;
            _networkConnection = networkConnection;
            _cardIndex = cardIndex;
        }

        public void PerformEvent()
        {
            if (_networkConnection.Equals(NetworkManager.Instance.LocalConnection)) return;
            
            _cardSlotBehaviour.SecondCardController = _cardController;
            _cardController.SetCardByRuntimeDictionaryIndex(_networkConnection, _cardIndex, typeof(DroppedState));
        }
    }
}
