using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using Plugins.EventNetworking.DataTransferObject;
using Plugins.EventNetworking.NetworkEvent;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Durak
{
    public class LobbyBehaviour : NetworkObject
    {
        [SerializeField] private int minPlayerCount = 2;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private UnityEvent onClientStartsGame;
        
        private readonly Dictionary<NetworkConnection, (bool isReady, string playerName)> _networkConnections = new();
        private string _playerName;
        private bool _isReady;
        
        private void Start()
        {
            UpdateIsReadyEvent.InitializeStatic(this);
            ShareNameEvent.InitializeStatic(this);
            
            NetworkManager.Instance.ConnectToServer();
        }
        
        public override void OnConnected(NetworkConnection ownConnection)
        {
            NetworkManager.Instance.FetchLobby();
        }

        [ContextMenu("Print Dictionary")]
        public void PrintDictionary()
        {
            foreach (var keyValuePair in _networkConnections)
            {
                Debug.Log(keyValuePair.Key + " " + keyValuePair.Value.isReady + " " + keyValuePair.Value.playerName);
            }
        }

        public override void OnLobbiesFetched(LobbiesData[] lobbiesData)
        {
            if (lobbiesData.Any(data => data.name == NetworkManager.Instance.DefaultLobbyName))
            {
                NetworkManager.Instance.JoinLobby();
                return;
            }
            
            NetworkManager.Instance.CreateLobby();
        }

        public override void OnLobbyCreated()
        {
            var networkManager = NetworkManager.Instance;

            inputField.text = GenerateRandomName(4);
            _playerName = inputField.text;
            _networkConnections.TryAdd(networkManager.LocalConnection, (false, _playerName));
            
            //when creating the lobby, there is no other client -> no event needed
        }

        public override void OnLobbyJoined(JoinLobbyClientData joinLobbyClientData)
        {
            var networkManager = NetworkManager.Instance;
            
            inputField.text = GenerateRandomName(4);
            _playerName = inputField.text;
            foreach (var networkConnection in networkManager.LobbyConnections)
            {
                _networkConnections.TryAdd(networkConnection, (false, _playerName));
            }
            
            networkManager.RequestRaiseEvent(new ShareNameEvent(this, networkManager.LocalConnection, _playerName));
        }

        public override void OnClientJoinedLobby(NetworkConnection joinedClient)
        {
            _networkConnections.TryAdd(joinedClient, (false, string.Empty));
            var networkManager = NetworkManager.Instance;
            
            if (_isReady)   //new clients only need to be updated, if it is not the default value
            {
                networkManager.RequestRaiseEvent(new UpdateIsReadyEvent(this, networkManager.LocalConnection, _isReady));
            }
            
            networkManager.RequestRaiseEvent(new ShareNameEvent(this, networkManager.LocalConnection, _playerName));
        }

        public override void OnClientLeftLobby(NetworkConnection disconnectedClient)
        {
            _networkConnections.Remove(disconnectedClient);
        }

        public void UpdateIsReadyNetworkEvent(bool isReady)
        {
            if (_isReady == isReady) return;
            
            var networkManager = NetworkManager.Instance;
            _isReady = isReady;
            networkManager.RequestRaiseEvent(new UpdateIsReadyEvent(this, networkManager.LocalConnection, isReady));
        }

        public void SetIsReady(NetworkConnection networkConnection, bool isReady)
        {
            if (_networkConnections.TryGetValue(networkConnection, out (bool isReady, string playerName) value) && value.isReady != isReady)
            {
                _networkConnections[networkConnection] = (isReady, value.playerName);
            }

            if (_networkConnections.Count >= minPlayerCount && _networkConnections.All(x => x.Value.isReady))
            {
                onClientStartsGame?.Invoke();
                //TODO: differently
                gameObject.SetActive(false);
            }
        }

        public void SetName(NetworkConnection networkConnection, string playerName)
        {
            if (_networkConnections.TryGetValue(networkConnection, out (bool isReady, string playerName) value) && value.playerName != playerName)
            {
                _networkConnections[networkConnection] = (value.isReady, playerName);
            }
        }
        
        //TODO: custom input
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
    
    public readonly struct UpdateIsReadyEvent : INetworkEvent
    {
        private static LobbyBehaviour _lobbyBehaviour;
        
        //serialized
        private readonly NetworkConnection _networkConnection;
        private readonly bool _isReady;

        public UpdateIsReadyEvent(LobbyBehaviour lobbyBehaviour, NetworkConnection networkConnection, bool isReady)
        {
            _lobbyBehaviour = lobbyBehaviour;
            _networkConnection = networkConnection;
            _isReady = isReady;
        }

        public static void InitializeStatic(LobbyBehaviour lobbyBehaviour)
        {
            _lobbyBehaviour = lobbyBehaviour;
        }
        
        public void PerformEvent()
        {
            _lobbyBehaviour.SetIsReady(_networkConnection, _isReady);
            Debug.Log(_networkConnection + " " + _isReady);
        }
    }
    
    public readonly struct ShareNameEvent : INetworkEvent
    {
        private static LobbyBehaviour _lobbyBehaviour;
        
        //serailized
        private readonly NetworkConnection _networkConnection;
        private readonly string _playerName;

        public ShareNameEvent(LobbyBehaviour lobbyBehaviour, NetworkConnection networkConnection, string playerName)
        {
            _lobbyBehaviour = lobbyBehaviour;
            _networkConnection = networkConnection;
            _playerName = playerName;
        }
        
        public static void InitializeStatic(LobbyBehaviour lobbyBehaviour)
        {
            _lobbyBehaviour = lobbyBehaviour;
        }
        
        public void PerformEvent()
        {
            _lobbyBehaviour.SetName(_networkConnection, _playerName);
        }
    }
}
