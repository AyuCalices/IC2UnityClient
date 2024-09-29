using System;
using Plugins.EventNetworking.DataTransferObject;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Durak.Networking
{
    public class LobbyListElement : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text lobbyNameText;
        [SerializeField] private TMP_Text playerCountText;
        [SerializeField] private Image passwordImage;

        private Action _onButtonPressedEvent;

        public void Initialize(LobbiesData lobbiesData, Action<string, string> onButtonPressedEvent)
        {
            lobbyNameText.text = lobbiesData.name;
            playerCountText.text = lobbiesData.playerCount + "/" + lobbiesData.capacity;
            passwordImage.gameObject.SetActive(lobbiesData.requiresPassword);
            button.onClick.AddListener(() => onButtonPressedEvent.Invoke(lobbiesData.name, null));
        }

        private void OnDestroy()
        {
            button.onClick.RemoveAllListeners();
        }
    }
}
