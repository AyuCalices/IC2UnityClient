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
        [SerializeField] private bool useSeed;
        [SerializeField] private int shuffleSeed;
        
        private List<Card> _drawableCards = new();
        
        public int GenerateSeed() => useSeed ? shuffleSeed : Guid.NewGuid().GetHashCode();

        public void InitializeDeck(int seed)
        {
            foreach (var cardGenerator in cardGenerators)
            {
                _drawableCards.Add(cardGenerator.Generate());
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

        public Card DrawCard()
        {
            Card card = _drawableCards[^1];
            _drawableCards.RemoveAt(_drawableCards.Count - 1);
            return card;
        }
        
        public List<Card> DrawCards(int drawCount)
        {
            if (drawCount < 0) return new List<Card>();
            
            var cards = _drawableCards.GetRange(_drawableCards.Count - 1 - drawCount, drawCount);
            _drawableCards.RemoveRange(_drawableCards.Count - 1 - drawCount, drawCount);
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
