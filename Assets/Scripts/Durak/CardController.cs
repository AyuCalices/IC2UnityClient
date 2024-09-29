using System;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Durak
{
    //done
    public class CardController : NetworkObject
    {
        [SerializeField] private PlayerDataRuntimeSet playerDataRuntimeSet;
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
            var card = playerDataRuntimeSet.GetCard(networkConnection, cardIndex);
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

        public bool IsTrump(CardType cardType)
        {
            return CardType.Type == cardType.Type;
        }
    }
}
