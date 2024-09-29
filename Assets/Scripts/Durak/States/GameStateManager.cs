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

        private readonly GameData _gameData;
        private readonly CardDeck _cardDeck;
        private readonly int _shuffleSeed;

        public StartGameState(GameData gameData, CardDeck cardDeck, int shuffleSeed)
        {
            _gameData = gameData;
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
            var lobbyConnections = NetworkManager.Instance.LobbyConnections;
            _gameData.DefenderLobbyConnectionsIndex = Random.Range(0, lobbyConnections.Count);
        }
    }
    
    public enum PlayerRoleType { Defender, FirstAttacker, Attacker }

    [Serializable]
    public class TurnState : IState
    {
        private readonly GameData _gameData;
        private readonly CardSpawner _cardSpawner;
        private readonly int _nextDefenderJump;

        //TODO: implement logic to finish the game

        public TurnState(GameData gameData, CardSpawner cardSpawner, int nextDefenderJump)
        {
            _gameData = gameData;
            _cardSpawner = cardSpawner;
            _nextDefenderJump = nextDefenderJump;

            //TODO: place somewhere else
            ForceDrawCardEvent.InitializeStatic(_cardSpawner);
        }
        
        public void Enter()
        {
            _cardSpawner.DrawCardsForAll();
            var lobbyConnections = NetworkManager.Instance.LobbyConnections;
            var localConnection = NetworkManager.Instance.LocalConnection;
            
            //define defender
            for (int i = 0; i < _nextDefenderJump; i++)
            {
                _gameData.DefenderLobbyConnectionsIndex = _gameData.DefenderLobbyConnectionsIndex + 1 >= lobbyConnections.Count ? 0 : _gameData.DefenderLobbyConnectionsIndex + 1;
            }
            _gameData.DefenderNetworkConnection = lobbyConnections[_gameData.DefenderLobbyConnectionsIndex];
            
            //define first attacker
            int firstAttackerIndex= _gameData.DefenderLobbyConnectionsIndex - 1 < 0 ? lobbyConnections.Count - 1 : _gameData.DefenderLobbyConnectionsIndex - 1;
            var firstAttackerConnection = lobbyConnections[firstAttackerIndex];

            if (_gameData.DefenderNetworkConnection.Equals(localConnection))
            {
                _gameData.PlayerRoleType = PlayerRoleType.Defender;
            }
            else if (firstAttackerConnection.Equals(localConnection))
            {
                _gameData.PlayerRoleType = PlayerRoleType.FirstAttacker;
            }
            else
            {
                _gameData.PlayerRoleType = PlayerRoleType.Attacker;
            }
            
            Debug.Log("Player Role: " + _gameData.PlayerRoleType);
        }

        public void Execute()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                NetworkManager.Instance.RequestRaiseEventCached(new ForceDrawCardEvent(NetworkManager.Instance.LocalConnection));
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
            Debug.LogWarning("Game Completed");
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
        private static CardSpawner _cardSpawner;
        
        //serialized
        private readonly NetworkConnection _networkConnection;

        public ForceDrawCardEvent(NetworkConnection networkConnection)
        {
            _networkConnection = networkConnection;
        }

        public static void InitializeStatic(CardSpawner cardSpawner)
        {
            _cardSpawner = cardSpawner;
        }
        
        public void PerformEvent()
        {
            _cardSpawner.ForceAddCardForPlayer(_networkConnection);
        }
    }
}
