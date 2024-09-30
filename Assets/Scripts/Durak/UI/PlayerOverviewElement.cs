using System;
using Durak.Networking;
using Durak.States;
using Plugins.EventNetworking.Component;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Durak.UI
{
    public class PlayerOverviewElement : MonoBehaviour
    {
        [SerializeField] private GameData gameData;
        
        [Header("Text")]
        [SerializeField] private TMP_Text playerName;
        [SerializeField] private TMP_Text cardCount;

        [Header("Role")] 
        [SerializeField] private Image image;
        [SerializeField] private Sprite swordSprite;
        [SerializeField] private Sprite shieldSprite;

        [Header("Background Color")] 
        [SerializeField] private Image background;
        [SerializeField] private Color defaultColor;
        [SerializeField] private Color highlightColor;

        public PlayerData PlayerData { get; set; }
        
        public void UpdateUI()
        {
            playerName.text = string.IsNullOrEmpty(PlayerData.Name) ? "" : PlayerData.Name;
            cardCount.text = PlayerData.Cards == null ? "" : PlayerData.Cards.Count.ToString();
            
            switch (PlayerData.RoleType)
            {
                case PlayerRoleType.None:
                    image.sprite = null;
                    break;
                case PlayerRoleType.Defender:
                    image.sprite = shieldSprite;
                    break;
                case PlayerRoleType.FirstAttacker:
                    image.sprite = swordSprite;
                    break;
                case PlayerRoleType.Attacker:
                    image.sprite = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
