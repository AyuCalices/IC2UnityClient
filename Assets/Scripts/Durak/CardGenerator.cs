using System;
using UnityEngine;

namespace Durak
{
    public class CardGenerator : ScriptableObject, IGenerator<Card>
    {
        [SerializeField] private CardType cardType;
        [SerializeField] private int cardStrength;
        [SerializeField] private Sprite cardSprite;

        public Card Generate()
        {
            return new Card(cardType, cardStrength, cardSprite);
        }
    }

    [Serializable]
    public class Card
    {
        [field: SerializeField] public CardType CardType { get; private set; }
        [field: SerializeField] public int CardStrength { get; private set; }
        [field: SerializeField] public Sprite Sprite { get; private set; }

        public Card(CardType cardType, int cardStrength, Sprite sprite)
        {
            CardType = cardType;
            CardStrength = cardStrength;
            Sprite = sprite;
        }
    }

    public interface IGenerator<out T>
    {
        T Generate();
    }
}
