using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Durak.Networking;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.DataTransferObject;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Durak.UI
{
    public class LobbyListSpawner : NetworkObject
    {
        [Header("Refresh Time")]
        [SerializeField] private float refreshLobbyListTime;
        
        [Header("Instantiation")]
        [SerializeField] private LobbyListElement lobbyListElementPrefab;
        [SerializeField] private Transform instantiationParent;
        
        [Header("Development")]
        [SerializeField] private bool generateRandomName;
        [SerializeField] private bool autoJoinLobby;
        [SerializeField] private UnityEvent onAutoJoinLobby;
        
        [Header("UI")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private UnityEvent tryEnterCreateLobbyView;
        
        private readonly List<LobbyListElement> _instantiatedLobbyListElements = new ();

        private void Start()
        {
            if (generateRandomName)
            {
                inputField.text = GenerateRandomName(4);
            }
        }

        private void OnConnectedToServer()
        {
            NetworkManager.Instance.FetchLobby();
        }

        public void RefreshLobbyList()
        {
            NetworkManager.Instance.FetchLobby();
        }

        public override void OnLeaveLobby()
        {
            NetworkManager.Instance.FetchLobby();
        }

        public override void OnLobbiesFetched(LobbiesData[] lobbiesData)
        {
            DestroyElements();
            InstantiateElements(lobbiesData);

            if (autoJoinLobby)
            {
                AutoJoinLobby(lobbiesData);
            }
        }

        private void AutoJoinLobby(LobbiesData[] lobbiesData)
        {
            if (lobbiesData.Any(data => data.name == NetworkManager.Instance.DefaultLobbyName))
            {
                NetworkManager.Instance.JoinLobby();
                return;
            }
            
            NetworkManager.Instance.CreateLobby();
            
            onAutoJoinLobby.Invoke();
        }

        private void DestroyElements()
        {
            for (var i = _instantiatedLobbyListElements.Count - 1; i >= 0; i--)
            {
                Destroy(_instantiatedLobbyListElements[i].gameObject);
            }
            _instantiatedLobbyListElements.Clear();
        }

        private void InstantiateElements(LobbiesData[] lobbiesData)
        {
            foreach (var data in lobbiesData)
            {
                var instantiatedElement = Instantiate(lobbyListElementPrefab, instantiationParent);
                instantiatedElement.Initialize(data, ConnectToLobby);
                _instantiatedLobbyListElements.Add(instantiatedElement);
            }
        }

        private void ConnectToLobby(string lobbyName, string password)
        {
            if (string.IsNullOrEmpty(inputField.text)) return;
        
            NetworkManager.Instance.JoinLobby(lobbyName, password);
        }

        public void TryEnterCreateLobbyView()
        {
            if (string.IsNullOrEmpty(inputField.text)) return;
            
            tryEnterCreateLobbyView?.Invoke();
        }
        
        private string GenerateRandomName(int n)
        {
            // Define the characters allowed in the key
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            System.Random random = new System.Random();
            StringBuilder keyBuilder = new StringBuilder();

            // Create a key with n elements
            for (int i = 0; i < n; i++)
            {
                // Append a random character from the allowed characters
                keyBuilder.Append(chars[random.Next(chars.Length)]);
            }

            // Return the key as a string
            return keyBuilder.ToString();
        }
    }
}
