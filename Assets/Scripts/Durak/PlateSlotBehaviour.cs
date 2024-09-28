using System.Collections.Generic;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.NetworkEvent;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Durak
{
    public class PlateSlotBehaviour : NetworkObject, IDropHandler
    {
        [SerializeField] private PlayerCardsRuntimeDictionary playerCardsRuntimeDictionary;
        [SerializeField] private CardSlotBehaviour cardSlotBehaviourPrefab;

        [SerializeField] private List<CardSlotBehaviour> instantiatedCardSlots = new ();
        
        public void OnDrop(PointerEventData eventData)
        {
            var pointerDrag = eventData.pointerDrag;
            if (pointerDrag.GetComponent<CardInteraction>().GetStateType() != typeof(DragState)) return;

            var cardSlotBehaviour = NetworkManager.Instance.NetworkInstantiateObject(cardSlotBehaviourPrefab, this, 
                cardSlotBehaviour => new PlateSlotInstantiationCompleteEvent(this, cardSlotBehaviour));
            
            cardSlotBehaviour.Initialize(pointerDrag.GetComponent<CardController>());
        }

        public void AddCardSlotBehaviour(CardSlotBehaviour cardSlotBehaviour)
        {
            instantiatedCardSlots.Add(cardSlotBehaviour);
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
