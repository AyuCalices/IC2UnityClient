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
        [SerializeField] private CardDeck cardDeck;
        
        private readonly Dictionary<NetworkConnection, bool> _attackerGiveUpLookup = new();
    
        private void Awake()
        {
            DefenderGiveUpEvent.OnDefenderGiveUp = OnDefenderGiveUp;
            AttackerGiveUpEvent.OnAttackerGiveUp = OnAttackerGiveUp;
            
            StartGameState.OnStartGameCompleted += EnterTurnState;
            CardSlotBehaviour.OnCardPlaced += OnCardPlaced;
        }

        private void OnDestroy()
        {
            StartGameState.OnStartGameCompleted -= EnterTurnState;
            CardSlotBehaviour.OnCardPlaced -= OnCardPlaced;
        }

        private void OnCardPlaced()
        {
            int notCompletedCount = 0;
            foreach (var (networkConnection, cards) in playerCardsRuntimeDictionary.GetItems())
            {
                if (cards.Count == 0 && cardDeck.IsEmpty())
                {
                    Debug.LogWarning($"Player with networkConnection {networkConnection} is done!");

                    if (networkConnection.Equals(gameData.DefenderNetworkConnection))
                    {
                        OnDefenderTurnWin();
                    }
                }
                else
                {
                    notCompletedCount++;
                }
            }

            if (notCompletedCount <= 1)
            {
                //TODO: put these 4 lines into the end of a turn state
                OnDestroyTableCards?.Invoke();
                var cards = tableCardsRuntimeSet.GetItems();
                gameData.DestroyedCards.AddRange(cards);
                tableCardsRuntimeSet.Restore();
                gameStateManager.RequestState(new EndGameState());
            }

            SetupAttackerGiveUpState();
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
            
            NetworkManager.Instance.RequestRaiseEventCached(new DefenderGiveUpEvent());
        }
        
        public void AttackerGiveUp()
        {
            var localConnection = NetworkManager.Instance.LocalConnection;
            if (gameData.DefenderNetworkConnection.Equals(localConnection))
            {
                Debug.LogWarning("Tried to stop attacking as a defender!");
                return;
            }
            
            NetworkManager.Instance.RequestRaiseEventCached(new AttackerGiveUpEvent(localConnection));
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
                OnDefenderTurnWin();
                SetupAttackerGiveUpState();
            }
        }

        private void OnDefenderTurnWin()
        {
            OnDestroyTableCards?.Invoke();
            var cards = tableCardsRuntimeSet.GetItems();
            gameData.DestroyedCards.AddRange(cards);
            tableCardsRuntimeSet.Restore();
            gameStateManager.RequestState(new TurnState(gameData, cardSpawner, 1));
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
        public static Action<NetworkConnection> OnAttackerGiveUp { get; set; }
        
        private readonly NetworkConnection _attacker;

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
