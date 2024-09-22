using System;
using UnityEngine;
using UnityEngine.UI;

namespace Durak
{
    public class CardController : MonoBehaviour
    {
        [SerializeField] private Image image;

        private Card _containedCard;
        
        public CardType CardType => _containedCard.CardType;
        public CardInteraction CardInteraction { get; private set; }
        public RectTransform RectTransform { get; private set; }

        private void Awake()
        {
            CardInteraction = GetComponent<CardInteraction>();
            RectTransform = GetComponent<RectTransform>();
        }

        public void SetCard(Card card)
        {
            _containedCard = card;
            image.sprite = _containedCard.Sprite;
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
