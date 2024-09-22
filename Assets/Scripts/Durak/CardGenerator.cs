using UnityEngine;
using UnityEngine.Serialization;

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

    public class Card
    {
        public CardType CardType { get; }
        public int CardStrength { get; }
        public Sprite Sprite { get; }

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
