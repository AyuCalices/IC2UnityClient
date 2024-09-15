using System;
using System.Collections.Generic;
using EventNetworking.Core;
using EventNetworking.DataTransferObject;
using EventNetworking.Identification;
using EventNetworking.NetworkEvent;
using UnityCommunity.UnitySingleton;
using UnityEngine;

namespace EventNetworking.Component
{
    public enum ErrorType { LobbyAlreadyExists, AlreadyInLobby, LobbyNotFound, LobbyFull, InvalidPassword, NotInLobby, NoLobbyJoined}
    
    public sealed class NetworkManager : PersistentMonoSingleton<NetworkManager>
    {
        //write about cecil -> to complex, because of that -> delegate
        
        /* Features:
         * Events for Messaging
         * Events are cashed at server for each lobby and provided on connect
         * No Host System -> everyone should have all data
         * Client Identification
         * Ownership of Objects -> should cover Race Conditions
         * Type support for everything that NewtonsoftJson can serialize + NetworkObject
         * Callbacks for network events
         */
        
        //TODO: maybe class for stuff inside the lobby & maybe class for lobby connecting -> low prio
        
        //TODO: build game and improve based on what is needed for that game -> the game should be the prime target
        //TODO: implemen OnClientDisconnected & OnDisconnect callback
        //TODO: check what to do for host migration and reconnecting

        [SerializeField] private PrefabRegistry prefabRegistry;
        [SerializeField] private float keepAliveInterval = 20;
        [SerializeField] private string lobbyName = "lobbyName";
        [SerializeField] private int lobbyCapacity = 4;
        
        public Dictionary<string, NetworkObject> NetworkObjects { get; } = new();
        public HashSet<NetworkConnection> LobbyConnections { get; } = new();
        public NetworkConnection LocalConnection { get; private set; }

        
        private NetworkController _networkController;
        

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            
            _networkController = new NetworkController(this, NetworkObjects, prefabRegistry, keepAliveInterval);
        }
        
        private void OnApplicationQuit()
        {
            DisconnectFromServer();
        }

        #endregion

        #region Public Lobby and Connection

        [ContextMenu(nameof(ConnectToServer))]
        public void ConnectToServer()
        {
            if (!_networkController.IsConnected)
            {
                _networkController.ConnectToServer();
            }
            else
            {
                Debug.LogWarning("Already connected to server!");
            }
        }

        [ContextMenu(nameof(DisconnectFromServer))]
        public void DisconnectFromServer()
        {
            if (_networkController.IsConnected)
            {
                _networkController.DisconnectFromServer();
            }
            else
            {
                Debug.LogWarning("Not connected to server!");
            }
        }
        
        [ContextMenu(nameof(FetchLobby))]
        public void FetchLobby()
        {
            _networkController.FetchLobby();
        }
        
        [ContextMenu(nameof(CreateLobby))]
        public void CreateLobby()
        {
            _networkController.CreateLobby(lobbyName, lobbyCapacity, "password");
        }
        
        [ContextMenu(nameof(JoinLobby))]
        public void JoinLobby()
        {
            _networkController.JoinLobby(lobbyName, "password");
        }

        [ContextMenu(nameof(LeaveLobby))]
        public void LeaveLobby()
        {
            _networkController.LeaveLobby();
        }

        #endregion

        #region Public Instantiation
        
        public bool RequestRaiseEvent<T>(T lockstepEvent, Action onBeforeValidEvent = null) where T : struct, INetworkEvent
        {
            return _networkController.RequestRaiseEvent(lockstepEvent, onBeforeValidEvent);
        }

        public NetworkObject NetworkInstantiate(NetworkObject networkObject)
        {
            var newNetworkObject = Instantiate(networkObject);
            var newID = newNetworkObject.SceneGuid;
        
            var instantiationEvent = new InstantiateEvent(networkObject, newID, LocalConnection);
            RequestRaiseEvent(instantiationEvent);

            newNetworkObject.OnNetworkInstantiate();
            return newNetworkObject;
        }
        
        public NetworkObject NetworkInstantiate(NetworkObject networkObject, Vector3 position, Quaternion rotation)
        {
            var newNetworkObject = Instantiate(networkObject, position, rotation);
            var newID = newNetworkObject.SceneGuid;
        
            var instantiationEvent = new InstantiatePosRotEvent(networkObject, newID, LocalConnection, position, rotation);
            RequestRaiseEvent(instantiationEvent);

            newNetworkObject.OnNetworkInstantiate();
            return newNetworkObject;
        }
        
        public NetworkObject NetworkInstantiate(NetworkObject networkObject, Vector3 position, Quaternion rotation, NetworkObject parent)
        {
            var newNetworkObject = Instantiate(networkObject, position, rotation, parent.transform);
            var newID = newNetworkObject.SceneGuid;
        
            var instantiationEvent = new InstantiatePosRotParentEvent(networkObject, newID, LocalConnection, position, rotation, parent);
            RequestRaiseEvent(instantiationEvent);

            newNetworkObject.OnNetworkInstantiate();
            return newNetworkObject;
        }
        
        public NetworkObject NetworkInstantiate(NetworkObject networkObject, NetworkObject parent)
        {
            var newNetworkObject = Instantiate(networkObject, parent.transform);
            var newID = newNetworkObject.SceneGuid;
        
            var instantiationEvent = new InstantiateParentEvent(networkObject, newID, LocalConnection, parent);
            RequestRaiseEvent(instantiationEvent);

            newNetworkObject.OnNetworkInstantiate();
            return newNetworkObject;
        }
        
        public NetworkObject NetworkInstantiate(NetworkObject networkObject, NetworkObject parent, bool worldPositionStays)
        {
            var newNetworkObject = Instantiate(networkObject, parent.transform);
            var newID = newNetworkObject.SceneGuid;
        
            var instantiationEvent = new InstantiateParentStaysEvent(networkObject, newID, LocalConnection, parent, worldPositionStays);
            RequestRaiseEvent(instantiationEvent);

            newNetworkObject.OnNetworkInstantiate();
            return newNetworkObject;
        }
        
        public void NetworkDestroy(NetworkObject networkObject)
        {
            if (networkObject.IsUnityNull() || string.IsNullOrEmpty(networkObject.SceneGuid)) return;
        
            var instantiationEvent = new DestroyEvent(networkObject);
            if (RequestRaiseEvent(instantiationEvent, () => Destroy(networkObject.gameObject)))
            {
                networkObject.OnNetworkDestroy();
            }
        }

        #endregion

        #region Internal Callbacks

        internal void OnError(ReceivedMessage receivedMessage)
        {
            Debug.LogError($"{nameof(OnError)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            
            // Try to parse the received message reason to an ErrorType enum
            if (Enum.TryParse(receivedMessage.reason, out ErrorType errorType))
            {
                foreach (var keyValuePair in NetworkObjects)
                {
                    keyValuePair.Value.OnError(errorType);
                }
            }
            else
            {
                Debug.LogError($"Unknown error type received: {receivedMessage.reason}");
            }
        }

        internal void OnConnected(ReceivedMessage receivedMessage, NetworkConnection ownConnection)
        {
            LocalConnection = ownConnection;
            
            Debug.Log($"{nameof(OnConnected)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            foreach (var keyValuePair in NetworkObjects)
            {
                keyValuePair.Value.OnConnected(receivedMessage, ownConnection);
            }
        }

        internal void OnLobbiesFetched(ReceivedMessage receivedMessage, LobbiesData lobbiesData)
        {
            Debug.Log($"{nameof(OnLobbiesFetched)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            foreach (var keyValuePair in NetworkObjects)
            {
                keyValuePair.Value.OnLobbiesFetched(receivedMessage, lobbiesData);
            }
        }

        //TODO: the lobby stuff is not buffered by event
        internal void OnLobbyCreated(ReceivedMessage receivedMessage)
        {
            Debug.Log($"{nameof(OnLobbyCreated)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            foreach (var keyValuePair in NetworkObjects)
            {
                keyValuePair.Value.OnLobbyCreated(receivedMessage);
            }
        }

        //TODO: the lobby stuff is not buffered by event
        internal void OnLobbyJoining(ReceivedMessage receivedMessage, JoinLobbyClientData joinLobbyClientData)
        {
            foreach (var clientID in joinLobbyClientData.clientIDs)
            {
                LobbyConnections.Add(new NetworkConnection(clientID));
            }
            
            Debug.Log($"{nameof(OnLobbyJoining)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            foreach (var keyValuePair in NetworkObjects)
            {
                keyValuePair.Value.OnLobbyJoining(receivedMessage, joinLobbyClientData);
            }
        }

        //TODO: the lobby stuff is not buffered by event
        internal void OnLobbyJoined(ReceivedMessage receivedMessage, JoinLobbyClientData joinLobbyClientData)
        {
            Debug.Log($"{nameof(OnLobbyJoined)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            foreach (var keyValuePair in NetworkObjects)
            {
                keyValuePair.Value.OnLobbyJoined(receivedMessage, joinLobbyClientData);
            }
        }

        //TODO: the lobby stuff is not buffered by event
        internal void OnClientJoinedLobby(ReceivedMessage receivedMessage, NetworkConnection joinedClient)
        {
            LobbyConnections.Add(joinedClient);
            
            Debug.Log($"{nameof(OnClientJoinedLobby)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            foreach (var keyValuePair in NetworkObjects)
            {
                keyValuePair.Value.OnClientJoinedLobby(receivedMessage, joinedClient);
            }
        }

        //TODO: the lobby stuff is not buffered by event
        internal void OnLeaveLobby(ReceivedMessage receivedMessage)
        {
            LobbyConnections.Clear();
            
            Debug.Log($"{nameof(OnLeaveLobby)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            foreach (var keyValuePair in NetworkObjects)
            {
                keyValuePair.Value.OnLeaveLobby(receivedMessage);
            }
        }

        //TODO: the lobby stuff is not buffered by event
        internal void OnClientLeftLobby(ReceivedMessage receivedMessage, NetworkConnection disconnectedClient)
        {
            LobbyConnections.RemoveWhere(x => x.ConnectionID == disconnectedClient.ConnectionID);
            
            Debug.Log($"{nameof(OnClientLeftLobby)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            foreach (var keyValuePair in NetworkObjects)
            {
                keyValuePair.Value.OnClientLeftLobby(receivedMessage, disconnectedClient);
            }
        }

        #endregion
    }
}
