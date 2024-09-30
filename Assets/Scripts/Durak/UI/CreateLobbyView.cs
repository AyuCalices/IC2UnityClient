using Plugins.EventNetworking.Component;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Durak.UI
{
    public class CreateLobbyView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_InputField lobbyNameInputField;
        [SerializeField] private TMP_InputField passwordInputField;
        [SerializeField] private TMP_InputField lobbyCapacityInputField;
        
        public void CreateLobby()
        {
            if (string.IsNullOrEmpty(lobbyNameInputField.text) || 
                string.IsNullOrEmpty(lobbyCapacityInputField.text) || 
                !int.TryParse(lobbyCapacityInputField.text, out int value)) return;
            
            NetworkManager.Instance.CreateLobby(lobbyNameInputField.text, value, passwordInputField.text);
        }
    }
}
