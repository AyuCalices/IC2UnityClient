using System;
using System.Collections.Generic;
using Plugins.EventNetworking.Component;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Durak
{
    public class CardDeck : NetworkObject
    {
        [SerializeField] private TrumpTypeFocus trumpTypeFocus;
        [SerializeField] private List<CardGenerator> cardGenerators;
        [SerializeField] private int maxCardCount;
        [SerializeField] private bool useSeed;
        [SerializeField] private int shuffleSeed;

        private int _cardCountDelta;
        private List<Card> _drawableCards = new();
        
        public int GenerateSeed() => useSeed ? shuffleSeed : Guid.NewGuid().GetHashCode();

        public void InitializeDeck(int seed)
        {
            foreach (var cardGenerator in cardGenerators)
            {
                _drawableCards.Add(cardGenerator.Generate());
                
                _cardCountDelta++;
                if (_cardCountDelta >= maxCardCount)
                {
                    break;
                }
            }
            
            Random.InitState(seed);
            Shuffle();

            Debug.Log($"Trump is {_drawableCards[0].CardType}");
            trumpTypeFocus.SetFocus(_drawableCards[0].CardType);
        }

        public void DisposeDeck()
        {
            _drawableCards.Clear();
            trumpTypeFocus.ClearFocus();
        }

        private void Shuffle()
        {
            var n = _drawableCards.Count;
            while (n > 1)
            {
                n--;
                var k = Random.Range(0, n + 1);
                (_drawableCards[k], _drawableCards[n]) = (_drawableCards[n], _drawableCards[k]);
            }
        }

        public bool IsEmpty() => _drawableCards.Count == 0;

        public bool TryDrawCard(out Card card)
        {
            card = default;
            if (_drawableCards.Count == 0) return false;
            
            card = _drawableCards[^1];
            _drawableCards.RemoveAt(_drawableCards.Count - 1);
            return true;
        }
        
        public List<Card> TryDrawCards(int drawCount)
        {
            if (drawCount < 0 || _drawableCards.Count == 0) return new List<Card>();

            if (drawCount > _drawableCards.Count)
            {
                drawCount = _drawableCards.Count;
            }
            
            var cards = _drawableCards.GetRange(_drawableCards.Count - drawCount, drawCount);
            _drawableCards.RemoveRange(_drawableCards.Count - drawCount, drawCount);
            return cards;
        }
        
        [ContextMenu("Utility: Find All Card Generators")]
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
