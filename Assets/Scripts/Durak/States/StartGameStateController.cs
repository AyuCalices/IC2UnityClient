using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.NetworkEvent;
using UnityEngine;

namespace Durak.States
{
    public class StartGameStateController : MonoBehaviour
    {
        [SerializeField] private GameData gameData;
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private CardDeck cardDeck;
        
        private void Awake()
        {
            StartGameStateEvent.GameData = gameData;
            gameStateManager.OnStateAuthorityAcquired += InitializeCardDeck;
        }

        private void OnDestroy()
        {
            gameStateManager.OnStateAuthorityAcquired -= InitializeCardDeck;
        }

        private void InitializeCardDeck()
        {
            NetworkManager.Instance.RequestRaiseEvent(new StartGameStateEvent(gameStateManager, cardDeck, cardDeck.GenerateSeed()), true);
        }
    }
    
    public readonly struct StartGameStateEvent : INetworkEvent
    {
        public static GameData GameData { get; set; }
        
        private readonly GameStateManager _gameStateManager;
        private readonly CardDeck _cardDeck;
        private readonly int _seed;

        public StartGameStateEvent(GameStateManager gameStateManager, CardDeck cardDeck, int seed)
        {
            _gameStateManager = gameStateManager;
            _cardDeck = cardDeck;
            _seed = seed;
        }

        public void PerformEvent()
        {
            _gameStateManager.RequestState(new StartGameState(GameData, _cardDeck, _seed));
        }
    }
}
