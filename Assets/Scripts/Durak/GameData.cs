using System;
using System.Collections.Generic;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using UnityEngine;

namespace Durak
{
    [CreateAssetMenu]
    public class GameData : ScriptableObject
    {
        [SerializeField] private PlayerDataRuntimeSet playerDataRuntimeSet;
        
        [field: SerializeField] public int Seed { get; set; }
        [field: SerializeField] public Card Trump { get; set; }
        [field: SerializeField] public List<Card> DeckCards { get; set; }
        [field: SerializeField] public List<Card> TableCards { get; set; }
        [field: SerializeField] public List<Card> GameRemovedCards { get; set; }
        [field: SerializeField] public int CurrentDefenderRotationIndex { get; private set; }

        private const int TrumpStrengthAddition = 100;

        private void OnEnable()
        {
            Restore();
        }

        public void Restore()
        {
            Seed = 0;
            Trump = null;
            DeckCards.Clear();
            TableCards.Clear();
            GameRemovedCards.Clear();
            CurrentDefenderRotationIndex = 0;
        }
        
        public bool CanDrawCard() => DeckCards.Count != 0;
        
        public List<Card> TryDrawCards(int drawCount)
        {
            if (drawCount < 0 || DeckCards.Count == 0) return new List<Card>();

            if (drawCount > DeckCards.Count)
            {
                drawCount = DeckCards.Count;
            }
            
            var cards = DeckCards.GetRange(DeckCards.Count - drawCount, drawCount);
            DeckCards.RemoveRange(DeckCards.Count - drawCount, drawCount);
            return cards;
        }
        
        public int GetCardStrength(Card card)
        {
            var finalStrength = card.CardStrength;
            
            if (Trump.CardType == card.CardType)
            {
                finalStrength += TrumpStrengthAddition;
            }

            return finalStrength;
        }

        public bool TableCardsContainStrength(int strength)
        {
            return TableCards.Exists(x => x.CardStrength == strength);
        }

        public void AddTableCard(NetworkConnection networkConnection, Card card)
        {
            var playerData = playerDataRuntimeSet.GetPlayerData(networkConnection);
            playerData.Cards.Remove(card);
            TableCards.Add(card);
        }

        public void RemoveTableCards()
        {
            GameRemovedCards.AddRange(TableCards);
            TableCards.Clear();
        }

        public void AddTableCardsToDefender()
        {
            playerDataRuntimeSet.GetDefenderPlayerData().Cards.AddRange(TableCards);;
            TableCards.Clear();
        }
        
        public void AddPlayerCardsToDestroyed()
        {
            foreach (var playerData in playerDataRuntimeSet.GetItems())
            {
                GameRemovedCards.AddRange(playerData.Cards);
                playerData.Cards.Clear();
            }
        }

        public NetworkConnection RotateDefenderIndex(int rotationCount)
        {
            var lobbyConnections = NetworkManager.Instance.LobbyConnections;
            CurrentDefenderRotationIndex = (CurrentDefenderRotationIndex + rotationCount) % lobbyConnections.Count;
            return lobbyConnections[CurrentDefenderRotationIndex];
        }
    }
}
