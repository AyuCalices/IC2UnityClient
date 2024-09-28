using System;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Durak
{
    public class CardController : NetworkObject
    {
        [SerializeField] private PlayerCardsRuntimeDictionary playerCardsRuntimeDictionary;
        [SerializeField] private Image image;

        private Card _containedCard;

        public Card Card => _containedCard;
        public CardType CardType => _containedCard.CardType;
        public CardInteraction CardInteraction { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            
            CardInteraction = GetComponent<CardInteraction>();
        }
        
        public void SetCardByRuntimeDictionaryIndex(NetworkConnection networkConnection, int cardIndex, Type entryStateType)
        {
            var card = playerCardsRuntimeDictionary.GetCard(networkConnection, cardIndex);
            SetCard(card, entryStateType);
        }

        public void SetCard(Card card, Type entryStateType)
        {
            _containedCard = card;
            image.sprite = _containedCard.Sprite;

            if (entryStateType == typeof(HandState))
            {
                CardInteraction.InitializeAsHandCard();
            }
            else if (entryStateType == typeof(DroppedState))
            {
                CardInteraction.InitializeAsDroppedCard();
            }
        }

        public bool IsTrump(TrumpTypeFocus trumpTypeFocus)
        {
            return CardType.Type == trumpTypeFocus.TrumpType.Type;
        }

        public int GetCardStrength(TrumpTypeFocus trumpTypeFocus)
        {
            return trumpTypeFocus.GetCardStrength(_containedCard);
        }
    }
}
