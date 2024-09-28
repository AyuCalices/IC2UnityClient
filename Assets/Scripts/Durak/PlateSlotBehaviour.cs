using System.Collections.Generic;
using Durak.States;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.NetworkEvent;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Durak
{
    public class PlateSlotBehaviour : NetworkObject, IDropHandler
    {
        [SerializeField] private GameData gameData;
        [SerializeField] private PlayerCardsRuntimeDictionary playerCardsRuntimeDictionary;
        [SerializeField] private TableCardsRuntimeSet tableCardsRuntimeSet;
        [SerializeField] private TrumpTypeFocus trumpType;
        [SerializeField] private CardSlotBehaviour cardSlotBehaviourPrefab;

        private List<CardSlotBehaviour> _instantiatedCardSlots = new ();

        protected override void Awake()
        {
            base.Awake();

            TurnStateController.OnDestroyTableCards += DestroyCardSlots;
            TurnStateController.OnPickupTableCards += DestroyCardSlots;
        }

        private void OnDestroy()
        {
            TurnStateController.OnDestroyTableCards -= DestroyCardSlots;
            TurnStateController.OnPickupTableCards += DestroyCardSlots;
        }

        private void DestroyCardSlots()
        {
            _instantiatedCardSlots.Clear();
        }

        public void OnDrop(PointerEventData eventData)
        {
            var pointerDrag = eventData.pointerDrag;
            if (pointerDrag.GetComponent<CardInteraction>().GetStateType() != typeof(DragState)) return;

            //defender can't attack
            if (gameData.PlayerRoleType is PlayerRoleType.Defender) return;
            
            //first attack needs to be done by the attacker
            if (tableCardsRuntimeSet.GetItems().Count == 0 && gameData.PlayerRoleType is not PlayerRoleType.FirstAttacker) return;
            
            //attacks after the fist must match
            var cardController = pointerDrag.GetComponent<CardController>();
            if (tableCardsRuntimeSet.GetItems().Count > 0 && !tableCardsRuntimeSet.ContainsStrength(cardController.Card.CardStrength)) return;

            var cardSlotBehaviour = NetworkManager.Instance.NetworkInstantiateObject(cardSlotBehaviourPrefab, this, 
                cardSlotBehaviour => new PlateSlotInstantiationCompleteEvent(this, cardSlotBehaviour));
            
            cardSlotBehaviour.Initialize(cardController);
        }

        public void AddCardSlotBehaviour(CardSlotBehaviour cardSlotBehaviour)
        {
            _instantiatedCardSlots.Add(cardSlotBehaviour);
        }
    }

    public readonly struct PlateSlotInstantiationCompleteEvent : INetworkEvent
    {
        private readonly PlateSlotBehaviour _plateSlotBehaviour;
        private readonly CardSlotBehaviour _cardSlotBehaviour;

        public PlateSlotInstantiationCompleteEvent(PlateSlotBehaviour plateSlotBehaviour, CardSlotBehaviour cardSlotBehaviour)
        {
            _plateSlotBehaviour = plateSlotBehaviour;
            _cardSlotBehaviour = cardSlotBehaviour;
        }
        
        public void PerformEvent()
        {
            _plateSlotBehaviour.AddCardSlotBehaviour(_cardSlotBehaviour);
        }
    }
}
