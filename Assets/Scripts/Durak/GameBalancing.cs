using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Durak
{
    [CreateAssetMenu]
    public class GameBalancing : ScriptableObject
    {
        [Header("Lobby")]
        [SerializeField] private int minPlayerCount = 2;
        
        [Header("Deck")]
        [SerializeField] private List<CardGenerator> cardGenerators;
        [SerializeField] private int maxDeckCardCount = 256;
        [SerializeField] private int playerHandCardCount = 6;
        
        [Header("Custom Seed")]
        [SerializeField] private bool useCustomSeed;
        [SerializeField] private int customSeed;
        
        
        public int MinPlayerCount
        {
            get => minPlayerCount;
            private set => minPlayerCount = value;
        }
        
        public List<CardGenerator> CardGenerators { 
            get => cardGenerators;
            private set => cardGenerators = value;
        }
        
        public int MaxDeckCardCount
        {
            get => maxDeckCardCount; 
            private set => maxDeckCardCount = value;
        }
        
        public int PlayerHandCardCount { 
            get => playerHandCardCount;
            private set => playerHandCardCount = value;
        }

        public bool UseCustomSeed
        {
            get => useCustomSeed; 
            private set => useCustomSeed = value;
        }

        public int CustomSeed
        {
            get => customSeed; 
            private set => customSeed = value;
        }
        
#if UNITY_EDITOR
        
        [ContextMenu("Utility: Find All Card Generators")]
        private void FillType()
        {
            CardGenerators = FindAllCardGenerators();
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
#endif
        
    }
}
