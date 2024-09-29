using Plugins.EventNetworking.Core;
using TMPro;
using UnityEngine;

namespace Durak.UI
{
    public class LobbyClientElement : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private GameObject isReadyImage;
        [SerializeField] private GameObject notReadyImage;
        
        public NetworkConnection NetworkConnection { get; set; }

        public void UpdateName(string playerName)
        {
            playerNameText.text = playerName;
            
        }

        public void UpdateIsReady(bool isReady)
        {
            isReadyImage.SetActive(isReady);
            notReadyImage.SetActive(!isReady);
        }
    }
}
