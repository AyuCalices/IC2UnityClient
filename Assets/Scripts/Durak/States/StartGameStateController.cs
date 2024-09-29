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
            StartGameStateEvent.InitializeStatic(gameData, gameStateManager, cardDeck);
            gameStateManager.OnStateAuthorityAcquired += InitializeCardDeck;
        }

        private void OnDestroy()
        {
            gameStateManager.OnStateAuthorityAcquired -= InitializeCardDeck;
        }

        private void InitializeCardDeck()
        {
            NetworkManager.Instance.RequestRaiseEventCached(new StartGameStateEvent(cardDeck.GenerateSeed()));
        }
    }
    
    public readonly struct StartGameStateEvent : INetworkEvent
    {
        private static GameData _gameData;
        private static GameStateManager _gameStateManager;
        private static CardDeck _cardDeck;
        
        //serialized
        private readonly int _seed;

        public StartGameStateEvent(int seed)
        {
            _seed = seed;
        }

        public static void InitializeStatic(GameData gameData, GameStateManager gameStateManager, CardDeck cardDeck)
        {
            _gameData = gameData;
            _gameStateManager = gameStateManager;
            _cardDeck = cardDeck;
        }

        public void PerformEvent()
        {
            _gameStateManager.RequestState(new StartGameState(_gameData, _cardDeck, _seed));
        }
    }
}
