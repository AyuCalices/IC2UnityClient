using System;
using DataTypes.StateMachine;
using EventNetworking.Component;
using EventNetworking.Core;
using EventNetworking.Core.Callbacks;
using EventNetworking.DataTransferObject;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Durak.States
{
    public class GameStateManager : NetworkObject, IStateManaged, IOnAfterAcquireOwnership, IOnBeforeLoseOwnership
    {
        public event Action OnStateAuthorityAcquired;
        public event Action OnStateAuthorityLost;

        private StateMachine _stateMachine;
        private string _currentState;

        protected override void Awake()
        {
            base.Awake();
            _stateMachine = new StateMachine();
            _stateMachine.Initialize(new IdleState());
        }

        public void RequestStateAuthority()
        {
            SecureRequestOwnership();
        }

        public void ReleaseStateAuthority()
        {
            SecureReleaseOwnership();
        }
        
        public void OnAfterAcquireOwnership(NetworkConnection oldConnection, NetworkConnection newConnection)
        {
            OnStateAuthorityAcquired?.Invoke();
        }
        
        public void OnBeforeLoseOwnership(NetworkConnection oldConnection, NetworkConnection newConnection)
        {
            OnStateAuthorityLost?.Invoke();
        }

        public void RequestState(IState requestedState)
        {
            _stateMachine.ChangeState(requestedState);

            if (_stateMachine.GetCurrentState() != null)
            {
                _currentState = _stateMachine.GetCurrentState().ToString();
            }
        }
    }

    [Serializable]
    public class IdleState : IState
    {
        public void Enter() { }

        public void Execute() { }

        public void Exit() { }
    }
    
    [Serializable]
    public class StartGameState : IState
    {
        public static event Action OnStartGameCompleted;
        
        private readonly CardDeck _cardDeck;
        private readonly int _shuffleSeed;

        public StartGameState(CardDeck cardDeck, int shuffleSeed)
        {
            _cardDeck = cardDeck;
            _shuffleSeed = shuffleSeed;
        }
        
        public void Enter()
        {
            _cardDeck.InitializeDeck(_shuffleSeed);
            Debug.LogWarning("Seed: " + _shuffleSeed);
            OnStartGameCompleted?.Invoke();
        }

        public void Execute()
        {
        }

        public void Exit()
        {
        }
    }

    [Serializable]
    public class TurnState : IState
    {
        private readonly CardSpawner _cardSpawner;

        public TurnState(CardSpawner cardSpawner)
        {
            _cardSpawner = cardSpawner;
        }
        
        public void Enter()
        {
            _cardSpawner.DrawCardsForAll();
            var randomStartIndex = Random.Range(0, NetworkManager.Instance.LobbyConnections.Count);
            var firstDefender = NetworkManager.Instance.LobbyConnections[randomStartIndex];
            Debug.Log("first defender: " + firstDefender.ConnectionID);
            foreach (var instanceLobbyConnection in NetworkManager.Instance.LobbyConnections)
            {
                Debug.Log(instanceLobbyConnection.ConnectionID);
            }
        }

        public void Execute()
        {
        }

        public void Exit()
        {
        }
    }
    
    [Serializable]
    public class EndGameState : IState
    {
        public void Enter()
        {
        }

        public void Execute()
        {
        }

        public void Exit()
        {
        }
    }
}
