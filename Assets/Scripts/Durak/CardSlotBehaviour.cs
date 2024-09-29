using System;
using System.Collections;
using Durak.States;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using Plugins.EventNetworking.NetworkEvent;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Durak
{
    public class CardSlotBehaviour : NetworkObject, IDropHandler
    {
        public static event Action OnCardPlaced;
        
        [SerializeField] private GameData gameData;
        [SerializeField] private PlayerDataRuntimeSet playerDataRuntimeSet;

        private CardController FirstCardController { get; set; }
        private CardController SecondCardController { get; set; }

        protected override void Awake()
        {
            base.Awake();

            TurnStateController.OnDefenderWinsTurn += DestroyCardsAndSlot;
            TurnStateController.OnAttackerWinsTurn += AttackerWin;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            TurnStateController.OnDefenderWinsTurn -= DestroyCardsAndSlot;
            TurnStateController.OnAttackerWinsTurn -= AttackerWin;
        }

        private void AttackerWin()
        {
            var localPlayerData = playerDataRuntimeSet.GetLocalPlayerData();
            
            if (localPlayerData.RoleType == PlayerRoleType.Defender)
            {
                DefenderPickupCard();
            }
            else
            {
                DestroyCardsAndSlot();
            }
        }

        private void DefenderPickupCard()
        {
            if (SecondCardController != null)
            {
                SecondCardController.CardInteraction.InitializeAsHandCard();
            }
            
            if (FirstCardController != null)
            {
                FirstCardController.CardInteraction.InitializeAsHandCard();
                Destroy(gameObject);
            }
        }
        
        private void DestroyCardsAndSlot()
        {
            if (SecondCardController != null)
            {
                Destroy(SecondCardController.gameObject);
            }
            
            if (FirstCardController != null)
            {
                Destroy(FirstCardController.gameObject);
                Destroy(gameObject);
            }
        }

        public void Initialize(CardController firstCardController)
        {
            StartCoroutine(FirstNetworkShareObject(firstCardController));
        }

        public void OnDrop(PointerEventData eventData)
        {
            //is the second card slot empty?
            if (SecondCardController) return;
            
            //has it the correct state?
            var pointerDrag = eventData.pointerDrag;
            if (pointerDrag.GetComponent<CardInteraction>().GetStateType() != typeof(DragState)) return;

            //only an attack can defend
            var localPlayerData = playerDataRuntimeSet.GetLocalPlayerData();
            if (localPlayerData.RoleType != PlayerRoleType.Defender) return;
            
            //can the card be placed?
            var newCardController = pointerDrag.GetComponent<CardController>();
            if (newCardController.IsTrump(gameData.TrumpType))
            {
                if (gameData.GetCardStrength(newCardController.Card) <= gameData.GetCardStrength(FirstCardController.Card)) return;
            }
            else if (newCardController.CardType.Type == FirstCardController.CardType.Type)
            {
                if (gameData.GetCardStrength(newCardController.Card) <= gameData.GetCardStrength(FirstCardController.Card)) return;
            }
            else
            {
                return;
            }
            
            StartCoroutine(SecondNetworkShareObject(newCardController));
        }
        
        public void InitializeFirstSlot(NetworkConnection networkConnection, CardController firstCardController)
        {
            gameData.AddTableCard(networkConnection, firstCardController.Card);
            FirstCardController = firstCardController;
            OnCardPlaced?.Invoke();
        }

        public void InitializeSecondSlot(NetworkConnection networkConnection, CardController secondCardController)
        {
            gameData.AddTableCard(networkConnection, secondCardController.Card);
            SecondCardController = secondCardController;
            OnCardPlaced?.Invoke();
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
        
        private IEnumerator FirstNetworkShareObject(CardController firstCardController)
        {
            PlaceCard(firstCardController, true);
            
            yield return null;
            
            var localConnection = NetworkManager.Instance.LocalConnection;
            var index = playerDataRuntimeSet.GetCardIndex(localConnection, firstCardController.Card);
            NetworkManager.Instance.NetworkShareRuntimeObject(firstCardController, new FirstPlaceCardEvent(this, firstCardController, localConnection, index));
            
            InitializeFirstSlot(localConnection, firstCardController);
        }
        
        private IEnumerator SecondNetworkShareObject(CardController secondCardController)
        {
            PlaceCard(secondCardController, false);
            
            yield return null;

            var localConnection = NetworkManager.Instance.LocalConnection;
            var index = playerDataRuntimeSet.GetCardIndex(localConnection, secondCardController.Card);
            NetworkManager.Instance.NetworkShareRuntimeObject(secondCardController, new SecondPlaceCardEvent(this, secondCardController, localConnection, index));
            
            InitializeFirstSlot(localConnection, secondCardController);
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
            
            _cardController.SetCardByRuntimeDictionaryIndex(_networkConnection, _cardIndex, typeof(DroppedState));
            _cardSlotBehaviour.InitializeFirstSlot(_networkConnection, _cardController);
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
            
            _cardController.SetCardByRuntimeDictionaryIndex(_networkConnection, _cardIndex, typeof(DroppedState));
            _cardSlotBehaviour.InitializeSecondSlot(_networkConnection, _cardController);
        }
    }
}
