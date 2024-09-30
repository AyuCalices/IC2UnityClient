using System;
using System.Collections.Generic;
using System.Linq;
using Durak.Networking;
using Durak.UI;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using Plugins.EventNetworking.DataTransferObject;
using Plugins.EventNetworking.NetworkEvent;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Durak
{
    public class LobbyBehaviour : NetworkObject
    {
        [Header("Data")]
        [SerializeField] private GameBalancing gameBalancing;
        [SerializeField] private PlayerDataRuntimeSet playerDataRuntimeSet;
        
        [Header("Instantiate")]
        [SerializeField] private LobbyClientElement lobbyClientElementPrefab;
        [SerializeField] private Transform instantiationParent;
        
        [Header("UI")]
        [SerializeField] private TMP_InputField inputField;
        
        [Header("Events")]
        [SerializeField] private UnityEvent onClientStartsGame;
        [SerializeField] private UnityEvent onLobbyJoined;
        
        private readonly Dictionary<NetworkConnection, bool> _isReadyLookup = new();
        private readonly List<LobbyClientElement> _instantiatedLobbyClientElements = new();

        #region Unity Lifecycle

        private void Start()
        {
            UpdateIsReadyEvent.OnPerformEvent += SetIsReady;
            ShareNameEvent.OnPerformEvent += SetName;
            
            NetworkManager.Instance.ConnectToServer();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            UpdateIsReadyEvent.OnPerformEvent -= SetIsReady;
            ShareNameEvent.OnPerformEvent -= SetName;
        }

        #endregion
        
        public void ToggleIsReadyNetworkEvent()
        {
            if (string.IsNullOrEmpty(inputField.text)) return;
            
            var networkManager = NetworkManager.Instance;
            networkManager.RequestRaiseEvent(new UpdateIsReadyEvent(networkManager.LocalConnection, !_isReadyLookup[networkManager.LocalConnection]));
        }

        #region Networking Lifecycle

        public override void OnConnected(NetworkConnection ownConnection)
        {
            NetworkManager.Instance.FetchLobby();
        }

        public override void OnLobbyCreated()
        {
            var networkConnection = NetworkManager.Instance.LocalConnection;
            
            //setup data
            AddElement(networkConnection);
            
            //set name
            SetName(NetworkManager.Instance.LocalConnection, inputField.text);
            
            onLobbyJoined?.Invoke();
        }

        public override void OnLobbyJoined(JoinLobbyClientData joinLobbyClientData)
        {
            var networkManager = NetworkManager.Instance;
            
            //setup data
            foreach (var networkConnection in networkManager.LobbyConnections)
            {
                AddElement(networkConnection);
            }
            
            //set name
            SetName(NetworkManager.Instance.LocalConnection, inputField.text);
            networkManager.RequestRaiseEvent(new ShareNameEvent(networkManager.LocalConnection, inputField.text));
            
            onLobbyJoined?.Invoke();
        }

        public override void OnClientJoinedLobby(NetworkConnection joinedClient)
        {
            var networkManager = NetworkManager.Instance;
            
            //setup data
            AddElement(joinedClient);
            
            //set is ready
            if (_isReadyLookup.TryGetValue(networkManager.LocalConnection, out bool isReady) && isReady)   //new clients only need to be updated, if it is not the default value
            {
                networkManager.RequestRaiseEvent(new UpdateIsReadyEvent(networkManager.LocalConnection, true));
            }
            
            //set name
            networkManager.RequestRaiseEvent(new ShareNameEvent(networkManager.LocalConnection, playerDataRuntimeSet.GetLocalPlayerData().Name));
        }

        public override void OnClientLeftLobby(NetworkConnection disconnectedClient)
        {
            _isReadyLookup.Remove(disconnectedClient);
        }

        #endregion

        private void AddElement(NetworkConnection networkConnection)
        {
            _isReadyLookup.TryAdd(networkConnection, false);
            playerDataRuntimeSet.AddItem(new PlayerData(networkConnection));
            
            var instantiatedElement = Instantiate(lobbyClientElementPrefab, instantiationParent);
            instantiatedElement.NetworkConnection = networkConnection;
            _instantiatedLobbyClientElements.Add(instantiatedElement);
        }

        private bool CanStart()
        {
            return _isReadyLookup.Count >= gameBalancing.MinPlayerCount && _isReadyLookup.All(x => x.Value);
        }
        
        private void SetIsReady(NetworkConnection networkConnection, bool isReady)
        {
            _isReadyLookup[networkConnection] = isReady;
            
            _instantiatedLobbyClientElements.Find(x => x.NetworkConnection.Equals(networkConnection))?.UpdateIsReady(isReady);

            if (CanStart())
            {
                onClientStartsGame?.Invoke();
                
                //reset isReady
                foreach (var playerData in playerDataRuntimeSet.GetItems())
                {
                    _isReadyLookup[playerData.Connection] = false;
                }
                
                foreach (var instantiatedLobbyClientElement in _instantiatedLobbyClientElements)
                {
                    instantiatedLobbyClientElement.UpdateIsReady(false);
                }
            }
        }

        private void SetName(NetworkConnection networkConnection, string playerName)
        {
            playerDataRuntimeSet.GetPlayerData(networkConnection).Name = playerName;
            
            _instantiatedLobbyClientElements.Find(x => x.NetworkConnection.Equals(networkConnection))?.UpdateName(playerName);
        }
        
        [ContextMenu("Print Dictionary")]
        public void PrintDictionary()
        {
            foreach (var keyValuePair in _isReadyLookup)
            {
                Debug.Log(keyValuePair.Key + " " + keyValuePair.Value);
            }
        }
    }
    
    public readonly struct UpdateIsReadyEvent : INetworkEvent
    {
        public static event Action<NetworkConnection, bool> OnPerformEvent;
        
        //serialized
        private readonly NetworkConnection _networkConnection;
        private readonly bool _isReady;

        public UpdateIsReadyEvent(NetworkConnection networkConnection, bool isReady)
        {
            _networkConnection = networkConnection;
            _isReady = isReady;
        }
        
        public void PerformEvent()
        {
            OnPerformEvent?.Invoke(_networkConnection, _isReady);
        }
    }
    
    public readonly struct ShareNameEvent : INetworkEvent
    {
        public static event Action<NetworkConnection, string> OnPerformEvent;
        
        //serailized
        private readonly NetworkConnection _networkConnection;
        private readonly string _playerName;

        public ShareNameEvent(NetworkConnection networkConnection, string playerName)
        {
            _networkConnection = networkConnection;
            _playerName = playerName;
        }
        
        public void PerformEvent()
        {
            OnPerformEvent?.Invoke(_networkConnection, _playerName);
        }
    }
}
