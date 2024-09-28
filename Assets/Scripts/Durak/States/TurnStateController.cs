using System;
using System.Collections.Generic;
using System.Linq;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using Plugins.EventNetworking.NetworkEvent;
using UnityEngine;

namespace Durak.States
{
    public class TurnStateController : MonoBehaviour
    {
        public static event Action OnPickupTableCards;
        public static event Action OnDestroyTableCards;
        
        [SerializeField] private GameData gameData;
        [SerializeField] private PlayerCardsRuntimeDictionary playerCardsRuntimeDictionary;
        [SerializeField] private TableCardsRuntimeSet tableCardsRuntimeSet;
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private CardSpawner cardSpawner;
        
        private readonly Dictionary<NetworkConnection, bool> _attackerGiveUpLookup = new();
    
        private void Awake()
        {
            DefenderGiveUpEvent.OnDefenderGiveUp = OnDefenderGiveUp;
            AttackerGiveUpEvent.OnAttackerGiveUp = OnAttackerGiveUp;
            
            StartGameState.OnStartGameCompleted += EnterTurnState;
            CardSlotBehaviour.OnPlacedCard += SetupAttackerGiveUpState;
        }

        private void OnDestroy()
        {
            StartGameState.OnStartGameCompleted -= EnterTurnState;
            CardSlotBehaviour.OnPlacedCard -= SetupAttackerGiveUpState;
        }

        private void EnterTurnState()
        {
            gameStateManager.RequestState(new TurnState(gameData, cardSpawner, 0));
            SetupAttackerGiveUpState();
        }

        public void DefenderGiveUp()
        {
            var localConnection = NetworkManager.Instance.LocalConnection;
            if (!gameData.DefenderNetworkConnection.Equals(localConnection))
            {
                Debug.LogWarning("Tried to stop defending as an attacker!");
                return;
            }
            
            NetworkManager.Instance.RequestRaiseEvent(new DefenderGiveUpEvent(), true);
        }
        
        public void AttackerGiveUp()
        {
            var localConnection = NetworkManager.Instance.LocalConnection;
            if (gameData.DefenderNetworkConnection.Equals(localConnection))
            {
                Debug.LogWarning("Tried to stop attacking as a defender!");
                return;
            }
            
            NetworkManager.Instance.RequestRaiseEvent(new AttackerGiveUpEvent(localConnection), true);
        }

        private void OnDefenderGiveUp()
        {
            var localConnection = NetworkManager.Instance.LocalConnection;
            if (gameData.DefenderNetworkConnection.Equals(localConnection))
            {
                OnPickupTableCards?.Invoke();
            }
            else
            {
                OnDestroyTableCards?.Invoke();
            }

            if (playerCardsRuntimeDictionary.TryGetValue(gameData.DefenderNetworkConnection, out List<Card> cards))
            {
                cards.AddRange(tableCardsRuntimeSet.GetItems());
                tableCardsRuntimeSet.Restore();
            }
            
            gameStateManager.RequestState(new TurnState(gameData, cardSpawner, 2));
        }

        private void SetupAttackerGiveUpState()
        {
            _attackerGiveUpLookup.Clear();
            foreach (var lobbyConnection in NetworkManager.Instance.LobbyConnections)
            {
                if (lobbyConnection.Equals(gameData.DefenderNetworkConnection)) continue;
                
                _attackerGiveUpLookup[lobbyConnection] = false;
            }
        }
        
        private void OnAttackerGiveUp(NetworkConnection networkConnection)
        {
            _attackerGiveUpLookup[networkConnection] = true;
            
            if (_attackerGiveUpLookup.All(x => x.Value))
            {
                OnDestroyTableCards?.Invoke();
                var cards = tableCardsRuntimeSet.GetItems();
                gameData.DestroyedCards.AddRange(cards);
                tableCardsRuntimeSet.Restore();
                
                gameStateManager.RequestState(new TurnState(gameData, cardSpawner, 1));
                SetupAttackerGiveUpState();
            }
        }
    }

    public readonly struct DefenderGiveUpEvent : INetworkEvent
    {
        public static Action OnDefenderGiveUp { get; set; }
        
        public void PerformEvent()
        {
            OnDefenderGiveUp?.Invoke();
        }
    }
    
    public readonly struct AttackerGiveUpEvent : INetworkEvent
    {
        private readonly NetworkConnection _attacker;
        public static Action<NetworkConnection> OnAttackerGiveUp { get; set; }

        public AttackerGiveUpEvent(NetworkConnection attacker)
        {
            _attacker = attacker;
        }
        
        public void PerformEvent()
        {
            OnAttackerGiveUp?.Invoke(_attacker);
        }
    }
}
