using System;
using DataTypes.StateMachine;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using Plugins.EventNetworking.Core.Callbacks;
using Plugins.EventNetworking.NetworkEvent;
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
            _currentState = _stateMachine.GetCurrentState().ToString();
        }

        private void Update()
        {
            _stateMachine.Update();
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
            _currentState = _stateMachine.GetCurrentState().ToString();
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
            var randomDefenderIndex = Random.Range(0, NetworkManager.Instance.LobbyConnections.Count);
            var previousIndex = randomDefenderIndex - 1;
            var attackerIndex = previousIndex >= 0 ? previousIndex : NetworkManager.Instance.LobbyConnections.Count - 1;
            var defender = NetworkManager.Instance.LobbyConnections[randomDefenderIndex];
            var attacker = NetworkManager.Instance.LobbyConnections[attackerIndex];
        }

        public void Execute()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                NetworkManager.Instance.RequestRaiseEvent(new ForceDrawCardEvent(_cardSpawner, NetworkManager.Instance.LocalConnection), true);
            }
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
    
    public readonly struct ForceDrawCardEvent : INetworkEvent
    {
        private readonly CardSpawner _cardSpawner;
        private readonly NetworkConnection _networkConnection;

        public ForceDrawCardEvent(CardSpawner cardSpawner, NetworkConnection networkConnection)
        {
            _cardSpawner = cardSpawner;
            _networkConnection = networkConnection;
        }
        
        public void PerformEvent()
        {
            _cardSpawner.ForceAddCardForPlayer(_networkConnection);
        }
    }
}
