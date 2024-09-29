using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Durak
{
    [CreateAssetMenu]
    public class GameBalancing : ScriptableObject
    {
        [field: SerializeField] public int MinPlayerCount { get; set; } = 2;
        [field: SerializeField] public List<CardGenerator> CardGenerators { get; set; } = new();
        [field: SerializeField] public int TargetCardCount { get; set; } = 6;
        [field: SerializeField] public int MaxCardCount { get; set; } = 256;
        [field: SerializeField] public bool UseCustomSeed { get; set; } = false;
        [field: SerializeField] public int CustomSeed { get; set; } = 0;
        
        
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
    }
}
