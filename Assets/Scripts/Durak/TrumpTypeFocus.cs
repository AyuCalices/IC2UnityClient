using UnityEngine;

namespace Durak
{
    [CreateAssetMenu]
    public class TrumpTypeFocus : ScriptableObject
    {
        [SerializeField] private int trumpStrengthAddition;
        
        private CardType _trumpType;

        public CardType TrumpType => _trumpType;
        public bool IsInitialized => _trumpType != null;

        public void SetFocus(CardType cardType)
        {
            _trumpType = cardType;
        }

        public void ClearFocus()
        {
            _trumpType = null;
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
