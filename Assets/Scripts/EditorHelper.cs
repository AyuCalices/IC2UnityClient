using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Reflection;
using Durak;
using UnityEditor;

public class EditorHelper : MonoBehaviour
{
    [SerializeField] private List<CardType> cardTypes;
    [SerializeField] private int minCardStrength;
    [SerializeField] private int maxCardStrength;
    
    // Paths to the input (source) and output (destination) folders
    public string outputFolderPath = "Assets/Data/Cards";
    
#if UNITY_EDITOR
    
    [ContextMenu("Generate")]
    void CropAllImagesInFolder()
    {
        // Ensure the output folder exists
        if (!Directory.Exists(outputFolderPath))
        {
            Directory.CreateDirectory(outputFolderPath);
        }
        
        foreach (var cardType in cardTypes)
        {
            int strengthRange = maxCardStrength - minCardStrength;
            for (int i = 0; i < strengthRange + 1; i++)
            {
                CreateCroppedImageScriptableObject(cardType, minCardStrength + i);
            }
        }
        
        void CreateCroppedImageScriptableObject(CardType cardType, int strength)
        {
            // Create an instance of the CroppedImageData ScriptableObject
            CardGenerator croppedImageData = ScriptableObject.CreateInstance<CardGenerator>();
            
            var cardTypeInfo = croppedImageData.GetType().GetField("cardType", BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            cardTypeInfo.SetValue(croppedImageData, cardType);
            
            var cardStrengthInfo = croppedImageData.GetType().GetField("cardStrength", BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            cardStrengthInfo.SetValue(croppedImageData, strength);
            
            if (!Directory.Exists($"{outputFolderPath}/{cardType.Type}"))
            {
                Directory.CreateDirectory($"{outputFolderPath}/{cardType.Type}");
            }

            // Save the ScriptableObject as an asset in the project folder
            string assetPath = $"{outputFolderPath}/{cardType.Type}/{cardType.Type}_{strength}.asset";
            AssetDatabase.CreateAsset(croppedImageData, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log("Cropped image ScriptableObject saved to: " + assetPath);
        }
    }
    
#endif
}
