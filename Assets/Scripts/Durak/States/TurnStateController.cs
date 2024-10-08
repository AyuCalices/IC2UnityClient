using System;
using System.Collections.Generic;
using System.Linq;
using DataTypes.StateMachine;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using Plugins.EventNetworking.NetworkEvent;
using UnityEngine;
using UnityEngine.Events;

namespace Durak.States
{
    public class TurnStateController : NetworkObject, IState
    {
        public static event Action OnEnterTurnState;
        public static event Action OnDefenderWinsTurn;
        public static event Action OnAttackerWinsTurn;
        public static event Action OnGameComplete;

        [SerializeField] private GameData gameData;
        [SerializeField] private PlayerDataRuntimeSet playerDataRuntimeSet;
        [SerializeField] private CardHandSpawner cardHandSpawner;
        [SerializeField] private GameObject defenderButton;
        [SerializeField] private GameObject attackerButton;
        [SerializeField] private UnityEvent onGameComplete;

        private GameStateManager _gameStateManager;
        private readonly Dictionary<NetworkConnection, bool> _attackerGiveUpLookup = new();
        private int _defenderRotationCount;

        protected override void Awake()
        {
            base.Awake();
            
            _gameStateManager = GetComponent<GameStateManager>();
            
            StartGameStateController.OnStartGameCompleted += EnterTurnState;
            
            DefenderGiveUpEvent.OnPerformEvent += OnDefenderGiveUp;
            AttackerGiveUpEvent.OnPerformEvent += OnAttackerGiveUp;
            LeaveLobbyEvent.OnPerformEvent += AddCardToDeck;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            StartGameStateController.OnStartGameCompleted -= EnterTurnState;
            
            DefenderGiveUpEvent.OnPerformEvent -= OnDefenderGiveUp;
            AttackerGiveUpEvent.OnPerformEvent -= OnAttackerGiveUp;
            LeaveLobbyEvent.OnPerformEvent -= AddCardToDeck;
        }

        #region Public Methods

        public void DefenderGiveUp()
        {
            var localPlayerData = playerDataRuntimeSet.GetLocalPlayerData();
            
            if (localPlayerData.RoleType != PlayerRoleType.Defender)
            {
                Debug.LogWarning("Tried to stop defending as an attacker!");
                return;
            }
            
            NetworkManager.Instance.RequestRaiseEventCached(new DefenderGiveUpEvent());
        }
        
        public void AttackerGiveUp()
        {
            var localPlayerData = playerDataRuntimeSet.GetLocalPlayerData();
            
            if (localPlayerData.RoleType == PlayerRoleType.Defender)
            {
                Debug.LogWarning("Tried to stop attacking as a defender!");
                return;
            }
            
            NetworkManager.Instance.RequestRaiseEventCached(new AttackerGiveUpEvent(localPlayerData.Connection));
        }

        #endregion

        private void AddCardToDeck(NetworkConnection disconnectedClient)
        {
            var playerData = playerDataRuntimeSet.GetItems().Find(x => x.Connection.Equals(disconnectedClient));
            gameData.DeckCards.AddRange(playerData.Cards);
            playerDataRuntimeSet.RemoveItem(playerData);
        }

        #region State Logic

        public void Enter()
        {
            CardSlotBehaviour.OnCardPlaced += UpdateGameIsComplete;
            CardSlotBehaviour.OnCardPlaced += SetupAttackerGiveUpState;
            
            OnEnterTurnState?.Invoke();
            
            InitializeAttackerDefender();
            SetupAttackerGiveUpState();
        }

        public void Execute() { }

        public void Exit()
        {
            CardSlotBehaviour.OnCardPlaced -= UpdateGameIsComplete;
            CardSlotBehaviour.OnCardPlaced -= SetupAttackerGiveUpState;
            
            gameData.TableCards.Clear();
        }

        #endregion
        
        

        #region Private Methods
        
        private void EnterTurnState()
        {
            _defenderRotationCount = 0;
            _gameStateManager.RequestState(this);
        }

        private void InitializeAttackerDefender()
        {
            var lobbyConnections = NetworkManager.Instance.LobbyConnections;
            
            //define defender
            var defenderConnection = gameData.RotateDefenderIndex(_defenderRotationCount);
            
            //define first attacker
            int firstAttackerIndex= gameData.CurrentDefenderRotationIndex - 1 < 0 ? lobbyConnections.Count - 1 : gameData.CurrentDefenderRotationIndex - 1;
            var firstAttackerConnection = lobbyConnections[firstAttackerIndex];

            //set roleType
            foreach (var playerData in playerDataRuntimeSet.GetItems())
            {
                if (defenderConnection.Equals(playerData.Connection))
                {
                    playerData.RoleType = PlayerRoleType.Defender;
                }
                else if (firstAttackerConnection.Equals(playerData.Connection))
                {
                    playerData.RoleType = PlayerRoleType.FirstAttacker;
                }
                else
                {
                    playerData.RoleType = PlayerRoleType.Attacker;
                }
            }
        }

        private void SetupAttackerGiveUpState()
        {
            _attackerGiveUpLookup.Clear();
            
            foreach (var playerData in playerDataRuntimeSet.GetItems())
            {
                if (playerData.RoleType == PlayerRoleType.Defender) continue;
                
                _attackerGiveUpLookup[playerData.Connection] = false;
            }
            
            UpdateLocalButtonUI();
        }

        private void UpdateLocalButtonUI()
        {
            var localPlayerRole = playerDataRuntimeSet.GetLocalPlayerData().RoleType;
            if (localPlayerRole is PlayerRoleType.Defender)
            {
                attackerButton.SetActive(false);
                defenderButton.SetActive(true);
            }
            else
            {
                attackerButton.SetActive(true);
                defenderButton.SetActive(false);
            }
        }
        
        private void UpdateGameIsComplete()
        {
            var notCompletedCount = 0;
            foreach (var playerData in playerDataRuntimeSet.GetItems())
            {
                if (playerData.Cards.Count == 0 && !gameData.CanDrawCard())
                {
                    if (playerData.RoleType is PlayerRoleType.Defender)
                    {
                        Debug.LogWarning($"Defender with networkConnection {playerData.Connection} is done!");
                        OnDefenderTurnWin(new IdleState());

                        if (_gameStateManager.Owner.Equals(NetworkManager.Instance.LocalConnection))
                        {
                            _gameStateManager.SecureReleaseOwnership();
                            NetworkManager.Instance.RequestClearEventCache();
                        }
                        
                        onGameComplete?.Invoke();
                        OnGameComplete?.Invoke();;
                        return;
                    }

                    Debug.LogWarning($"Attacker with networkConnection {playerData.Connection} is done!");
                }
                else
                {
                    notCompletedCount++;
                }
            }

            if (notCompletedCount <= 1)
            {
                _gameStateManager.RequestState(new IdleState());
                
                if (_gameStateManager.Owner.Equals(NetworkManager.Instance.LocalConnection))
                {
                    _gameStateManager.SecureReleaseOwnership();
                    NetworkManager.Instance.RequestClearEventCache();
                }
                
                onGameComplete?.Invoke();
                OnGameComplete?.Invoke();
            }
        }
        
        private void OnAttackerGiveUp(NetworkConnection networkConnection)
        {
            _attackerGiveUpLookup[networkConnection] = true;
            
            if (_attackerGiveUpLookup.All(x => x.Value))
            {
                OnDefenderTurnWin(this);
            }
        }
        
        private void OnDefenderTurnWin(IState completeState)
        {
            gameData.RemoveTableCards();
            OnDefenderWinsTurn?.Invoke();
            
            _defenderRotationCount = 1;
            _gameStateManager.RequestState(completeState);
        }
        
        private void OnDefenderGiveUp()
        {
            gameData.AddTableCardsToDefender();
            OnAttackerWinsTurn?.Invoke();

            _defenderRotationCount = 2;
            _gameStateManager.RequestState(this);
        }

        #endregion
    }

    public readonly struct DefenderGiveUpEvent : INetworkEvent
    {
        public static event Action OnPerformEvent;
        
        public void PerformEvent()
        {
            OnPerformEvent?.Invoke();
        }
    }
    
    public readonly struct AttackerGiveUpEvent : INetworkEvent
    {
        public static event Action<NetworkConnection> OnPerformEvent;
        
        private readonly NetworkConnection _attacker;

        public AttackerGiveUpEvent(NetworkConnection attacker)
        {
            _attacker = attacker;
        }
        
        public void PerformEvent()
        {
            OnPerformEvent?.Invoke(_attacker);
        }
    }
    
    public readonly struct LeaveLobbyEvent : INetworkEvent
    {
        public static event Action<NetworkConnection> OnPerformEvent;
        
        private readonly NetworkConnection _networkConnection;
        
        public LeaveLobbyEvent(NetworkConnection networkConnection)
        {
            _networkConnection = networkConnection;
        }
        
        public void PerformEvent()
        {
            OnPerformEvent?.Invoke(_networkConnection);
        }
    }
}
