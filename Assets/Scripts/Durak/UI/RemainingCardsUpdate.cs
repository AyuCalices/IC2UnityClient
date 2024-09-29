using System;
using Durak.States;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Durak.UI
{
    public class RemainingCardsUpdate : MonoBehaviour
    {
        [SerializeField] private GameData gameData;
        [SerializeField] private Image image;
        [SerializeField] private TMP_Text remainingCards;

        private void Start()
        {
            StartGameStateController.OnStartGameCompleted += SetTrump;
        }

        private void OnDestroy()
        {
            StartGameStateController.OnStartGameCompleted -= SetTrump;
        }

        private void Update()
        {
            if (gameData.DeckCards != null)
            {
                remainingCards.text = gameData.DeckCards.Count.ToString();
            }
        }
        
        private void SetTrump()
        {
            image.sprite = gameData.DeckCards[0].Sprite;
        }
    }
}
