using System;
using DataTypes.StateMachine;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.NetworkEvent;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Durak.States
{
    public class StartGameStateController : MonoBehaviour, IState
    {
        public static event Action OnStartGameCompleted;
        
        [SerializeField] private GameData gameData;
        [SerializeField] private GameBalancing gameBalancing;
        [SerializeField] private GameStateManager gameStateManager;
        
        private CardDeckGenerator _cardDeckGenerator;
        
        private void Awake()
        {
            _cardDeckGenerator = new CardDeckGenerator(gameData, gameBalancing);
            StartGameStateEvent.OnPerformEvent += NetworkedStartGameState;
            gameStateManager.OnStateAuthorityAcquired += InitializeCardDeck;
        }

        private void OnDestroy()
        {
            StartGameStateEvent.OnPerformEvent -= NetworkedStartGameState;
            gameStateManager.OnStateAuthorityAcquired -= InitializeCardDeck;
        }

        private void InitializeCardDeck()
        {
            int seed = gameBalancing.UseCustomSeed ? gameBalancing.CustomSeed : Guid.NewGuid().GetHashCode();
            NetworkManager.Instance.RequestRaiseEventCached(new StartGameStateEvent(seed));
        }

        private void NetworkedStartGameState(int gameSeed)
        {
            gameData.Seed = gameSeed;
            gameStateManager.RequestState(this);
        }

        public void Enter()
        {
            gameData.AddPlayerCardsToDestroyed();
            gameData.Restore();
            
            _cardDeckGenerator.InitializeDeck(gameData.Seed);

            gameData.Trump = gameData.DeckCards[0];
            
            OnStartGameCompleted?.Invoke();
        }

        public void Execute()
        {
        }

        public void Exit()
        {
            var lobbyConnections = NetworkManager.Instance.LobbyConnections;
            gameData.RotateDefenderIndex(Random.Range(0, lobbyConnections.Count));
        }
    }
    
    public readonly struct StartGameStateEvent : INetworkEvent
    {
        public static event Action<int> OnPerformEvent;
        
        //serialized
        private readonly int _seed;

        public StartGameStateEvent(int seed)
        {
            _seed = seed;
        }

        public void PerformEvent()
        {
            OnPerformEvent?.Invoke(_seed);
        }
    }
}
