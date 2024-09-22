using UnityEngine;

namespace Durak
{
    [CreateAssetMenu]
    public class TrumpTypeFocus : ScriptableObject
    {
        [SerializeField] private int trumpStrengthAddition;
        
        private CardType _trumpType;

        public CardType TrumpType => _trumpType;

        public void SetFocus(CardType cardType)
        {
            _trumpType = cardType;
        }

        public int GetCardStrength(Card card)
        {
            var finalStrength = card.CardStrength;
            
            if (_trumpType == card.CardType)
            {
                finalStrength += trumpStrengthAddition;
            }

            return finalStrength;
        }
    }
}
