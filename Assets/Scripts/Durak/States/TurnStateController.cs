using System;
using System.Collections.Generic;
using System.Linq;
using DataTypes.StateMachine;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using Plugins.EventNetworking.NetworkEvent;
using UnityEngine;

namespace Durak.States
{
    public class TurnStateController : MonoBehaviour, IState
    {
        public static event Action OnEnterTurnState;
        public static event Action OnDefenderWinsTurn;
        public static event Action OnAttackerWinsTurn;
        
        [SerializeField] private GameData gameData;
        [SerializeField] private PlayerDataRuntimeSet playerDataRuntimeSet;
        [SerializeField] private CardHandSpawner cardHandSpawner;
        [SerializeField] private GameObject defenderButton;
        [SerializeField] private GameObject attackerButton;

        private GameStateManager _gameStateManager;
        private readonly Dictionary<NetworkConnection, bool> _attackerGiveUpLookup = new();
        private int _defenderRotationCount;

        private void Awake()
        {
            _gameStateManager = GetComponent<GameStateManager>();
            
            StartGameStateController.OnStartGameCompleted += EnterTurnState;
            
            DefenderGiveUpEvent.OnPerformEvent += OnDefenderGiveUp;
            AttackerGiveUpEvent.OnPerformEvent += OnAttackerGiveUp;
        }

        private void OnDestroy()
        {
            StartGameStateController.OnStartGameCompleted -= EnterTurnState;
            
            DefenderGiveUpEvent.OnPerformEvent -= OnDefenderGiveUp;
            AttackerGiveUpEvent.OnPerformEvent -= OnAttackerGiveUp;
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

        
        #region State Logic

        public void Enter()
        {
            CardSlotBehaviour.OnCardPlaced += UpdateGameIsComplete;
            CardSlotBehaviour.OnCardPlaced += SetupAttackerGiveUpState;
            
            OnEnterTurnState?.Invoke();
            
            InitializeAttackerDefender();
            Debug.Log("Player Role: " + playerDataRuntimeSet.GetLocalPlayerData().RoleType);
            UpdateLocalButtonUI();
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

            var localPlayerRole = playerDataRuntimeSet.GetLocalPlayerData().RoleType;
            if (localPlayerRole is PlayerRoleType.Attacker)
            {
                attackerButton.SetActive(true);
            }
        }

        private void UpdateLocalButtonUI()
        {
            var localPlayerRole = playerDataRuntimeSet.GetLocalPlayerData().RoleType;
            if (localPlayerRole is PlayerRoleType.Defender)
            {
                attackerButton.SetActive(false);
                defenderButton.SetActive(true);
            }
            else if (localPlayerRole is PlayerRoleType.Attacker)
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
                if (playerData.Cards.Count == 0 && gameData.CanDrawCard())
                {
                    if (playerData.RoleType is PlayerRoleType.Defender)
                    {
                        Debug.LogWarning($"Defender with networkConnection {playerData.Connection} is done!");
                        OnDefenderTurnWin(new EndGameState());
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
                _gameStateManager.RequestState(new EndGameState());
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
            gameData.TryAddTableCardsToDefender();
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
}
