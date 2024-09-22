using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace Durak
{
    public class CardDeck : MonoBehaviour
    {
        [SerializeField] private TrumpTypeFocus trumpTypeFocus;
        [SerializeField] private List<CardGenerator> cardGenerators;
        [SerializeField] private bool useSeed;
        [SerializeField] private int seed;
        
        private readonly List<Card> _drawableCards = new();
 
        private void Start()
        {
            foreach (var cardGenerator in cardGenerators)
            {
                _drawableCards.Add(cardGenerator.Generate());
            }

            if (useSeed)
            {
                UnityEngine.Random.InitState(seed);
            }
            Shuffle();

            Debug.Log($"Trump is {_drawableCards[0].CardType}");
            trumpTypeFocus.SetFocus(_drawableCards[0].CardType);
        }

        private void Shuffle()
        {
            var n = _drawableCards.Count;
            while (n > 1)
            {
                n--;
                var k = UnityEngine.Random.Range(0, n + 1);
                (_drawableCards[k], _drawableCards[n]) = (_drawableCards[n], _drawableCards[k]);
            }
        }

        public Card DrawCard()
        {
            Card card = _drawableCards[^1];
            _drawableCards.RemoveAt(_drawableCards.Count - 1);
            return card;
        }
        
        public List<Card> DrawCards(int drawCount)
        {
            var cards = _drawableCards.GetRange(_drawableCards.Count - 1 - drawCount, drawCount);
            _drawableCards.RemoveRange(_drawableCards.Count - 1 - drawCount, drawCount);
            return cards;
        }
        
        [ContextMenu("Fill Type")]
        private void FillType()
        {
            cardGenerators = FindAllCardGenerators();
        }
        
        private static List<CardGenerator> FindAllCardGenerators()
        {
            // List to store all found CardGenerator instances
            List<CardGenerator> foundGenerators = new List<CardGenerator>();

            // Get all asset paths in the project
            string[] guids = AssetDatabase.FindAssets("t:CardGenerator");

            // Loop through each GUID to get the asset path and load the asset
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                CardGenerator cardGenerator = AssetDatabase.LoadAssetAtPath<CardGenerator>(assetPath);

                if (cardGenerator != null)
                {
                    foundGenerators.Add(cardGenerator);
                }
            }

            // Return all found instances as an array
            return foundGenerators;
        }
    }
}
