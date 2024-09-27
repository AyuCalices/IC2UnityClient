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

        private CardSpawner _cardSpawner;
        private Card _containedCard;
        
        public CardType CardType => _containedCard.CardType;
        public CardInteraction CardInteraction { get; private set; }
        public RectTransform RectTransform { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            
            CardInteraction = GetComponent<CardInteraction>();
            RectTransform = GetComponent<RectTransform>();
        }

        public void SetCard(CardSpawner cardSpawner, Card card, Type entryStateType)
        {
            _cardSpawner = cardSpawner;
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

        public int GetCardIndex(NetworkConnection networkConnection)
        {
            return playerCardsRuntimeDictionary.FindCardIndex(networkConnection, _containedCard);
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
