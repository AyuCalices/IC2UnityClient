using System;
using System.Collections.Generic;
using Durak.States;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using Plugins.EventNetworking.NetworkEvent;
using UnityEngine;

namespace Durak
{
    //done
    public class CardHandSpawner : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private PlayerDataRuntimeSet playerDataRuntimeSet;
        [SerializeField] private GameData gameData;
        [SerializeField] private GameBalancing gameBalancing;

        [Header("Objects")]
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private CardController cardControllerPrefab;

        [Header("Debug")]
        [SerializeField] private bool debugDrawEnabled = true;
        [SerializeField, Range(1, 10)] private int debugDrawCount = 1;

        private void Awake()
        {
            TurnStateController.OnEnterTurnState += DrawCardsForAll;
            ForceDrawCardEvent.OnPerformEvent += ForceAddCardForPlayer;
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) && gameStateManager.CurrentState is TurnStateController && debugDrawEnabled)
            {
                NetworkManager.Instance.RequestRaiseEventCached(new ForceDrawCardEvent(NetworkManager.Instance.LocalConnection));
            }
        }

        private void OnDestroy()
        {
            TurnStateController.OnEnterTurnState -= DrawCardsForAll;
            ForceDrawCardEvent.OnPerformEvent -= ForceAddCardForPlayer;
        }

        private void DrawCardsForAll()
        {
            foreach (var connection in NetworkManager.Instance.LobbyConnections)
            {
                DrawCardsForPlayer(connection, gameBalancing.TargetCardCount);
            }
        }

        /// <summary>
        /// No Race-Condition security!
        /// </summary>
        private void ForceAddCardForPlayer(NetworkConnection networkConnection)
        {
            var cards = playerDataRuntimeSet.GetCards(networkConnection);
                
            var newCards = DrawCards(cards, cards.Count + debugDrawCount);

            if (networkConnection.Equals(NetworkManager.Instance.LocalConnection))
            {
                InstantiateHandCards(newCards);
            }
        }

        private void DrawCardsForPlayer(NetworkConnection networkConnection, int cardCount)
        {
            var cards = playerDataRuntimeSet.GetCards(networkConnection);
                
            var newCards = DrawCards(cards, cardCount);

            if (networkConnection.Equals(NetworkManager.Instance.LocalConnection))
            {
                InstantiateHandCards(newCards);
            }
        }

        private List<Card> DrawCards(List<Card> cards, int targetCount)
        {
            var newCards = gameData.TryDrawCards(targetCount - cards.Count);
            cards.AddRange(newCards);
            return newCards;
        }

        private void InstantiateHandCards(List<Card> cards)
        {
            foreach (var card in cards)
            {
                InstantiateHandCard(card);
            }
        }

        private void InstantiateHandCard(Card card)
        {
            CardController newCard = Instantiate(cardControllerPrefab, transform, false);
            newCard.SetCard(card, typeof(HandState));
        }
    }
    
    public readonly struct ForceDrawCardEvent : INetworkEvent
    {
        public static event Action<NetworkConnection> OnPerformEvent;
        
        //serialized
        private readonly NetworkConnection _networkConnection;

        public ForceDrawCardEvent(NetworkConnection networkConnection)
        {
            _networkConnection = networkConnection;
        }
        
        public void PerformEvent()
        {
            OnPerformEvent?.Invoke(_networkConnection);
        }
    }
}