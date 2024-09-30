using System;
using System.Collections.Generic;
using Durak.States;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.NetworkEvent;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Durak
{
    //DONE
    public class PlateSlotBehaviour : NetworkObject, IDropHandler
    {
        [SerializeField] private GameData gameData;
        [SerializeField] private PlayerDataRuntimeSet playerDataRuntimeSet;
        [SerializeField] private CardSlotBehaviour cardSlotBehaviourPrefab;

        private List<CardSlotBehaviour> _instantiatedCardSlots = new ();

        protected override void Awake()
        {
            base.Awake();

            PlateSlotInstantiationCompleteEvent.OnPerformEvent += AddCardSlotBehaviour;
            TurnStateController.OnDefenderWinsTurn += DestroyCardSlots;
            TurnStateController.OnGameComplete += DestroyCardSlots;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            PlateSlotInstantiationCompleteEvent.OnPerformEvent -= AddCardSlotBehaviour;
            TurnStateController.OnDefenderWinsTurn -= DestroyCardSlots;
            TurnStateController.OnGameComplete -= DestroyCardSlots;
        }

        private void DestroyCardSlots()
        {
            _instantiatedCardSlots.Clear();
        }

        public void OnDrop(PointerEventData eventData)
        {
            var pointerDrag = eventData.pointerDrag;
            if (pointerDrag.GetComponent<CardInteraction>().GetStateType() != typeof(DragState)) return;

            var playerData = playerDataRuntimeSet.GetLocalPlayerData();
            
            //defender can't attack
            if (playerData.RoleType is PlayerRoleType.Defender) return;
            
            //first attack needs to be done by the attacker
            if (gameData.TableCards.Count == 0 && playerData.RoleType is not PlayerRoleType.FirstAttacker) return;
            
            //attacks after the fist must match
            var cardController = pointerDrag.GetComponent<CardController>();
            if (gameData.TableCards.Count > 0 && !gameData.TableCardsContainStrength(cardController.Card.CardStrength)) return;

            var instantiatedObject = Instantiate(cardSlotBehaviourPrefab, transform);
            NetworkManager.Instance.NetworkShareRuntimeObject(instantiatedObject, new PlateSlotInstantiationCompleteEvent(instantiatedObject));
            
            instantiatedObject.Initialize(cardController);
        }

        public void AddCardSlotBehaviour(CardSlotBehaviour cardSlotBehaviour)
        {
            _instantiatedCardSlots.Add(cardSlotBehaviour);
        }
    }

    public readonly struct PlateSlotInstantiationCompleteEvent : INetworkEvent
    {
        public static event Action<CardSlotBehaviour> OnPerformEvent;
        
        //serialized
        private readonly CardSlotBehaviour _cardSlotBehaviour;

        public PlateSlotInstantiationCompleteEvent(CardSlotBehaviour cardSlotBehaviour)
        {
            _cardSlotBehaviour = cardSlotBehaviour;
        }
        
        public void PerformEvent()
        {
            OnPerformEvent?.Invoke(_cardSlotBehaviour);
        }
    }
}
