using System;
using System.Collections.Generic;
using Plugins.EventNetworking.Core;
using Plugins.EventNetworking.DataTransferObject;
using Plugins.EventNetworking.Identification;
using Plugins.EventNetworking.NetworkEvent;
using UnityCommunity.UnitySingleton;
using UnityEngine;

namespace Plugins.EventNetworking.Component
{
    public enum ErrorType { LobbyAlreadyExists, AlreadyInLobby, LobbyNotFound, LobbyFull, InvalidPassword, NotInLobby, NoLobbyJoined }
    
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
        [SerializeField] private bool connectOnAwake;
        
        [Header("Development")]
        [SerializeField] private string defaultLobbyName = "lobbyName";
        [SerializeField] private int defaultLobbyCapacity = 4;
        [SerializeField] private bool debug;
        
        public int DefaultLobbyCapacity
        {
            get => defaultLobbyCapacity;
            set => defaultLobbyCapacity = value;
        }

        public string DefaultLobbyName
        {
            get => defaultLobbyName;
            set => defaultLobbyName = value;
        }
        
        public Dictionary<string, NetworkObject> NetworkObjects { get; } = new();
        public List<NetworkConnection> LobbyConnections { get; } = new();
        public NetworkConnection LocalConnection { get; private set; }

        
        private NetworkController _networkController;
        

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            
            _networkController = new NetworkController(this, NetworkObjects, prefabRegistry, keepAliveInterval);

            if (connectOnAwake)
            {
                ConnectToServer();
            }
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
        public void CreateLobby(string lobbyName = "", int capacity = 0, string password = "")
        {
            _networkController.CreateLobby(string.IsNullOrEmpty(lobbyName) ? defaultLobbyName : lobbyName, capacity == 0 ? defaultLobbyCapacity : capacity, password);
        }
        
        [ContextMenu(nameof(JoinLobby))]
        public void JoinLobby(string lobbyName = "", string password = "")
        {
            _networkController.JoinLobby(string.IsNullOrEmpty(lobbyName) ? defaultLobbyName : lobbyName, password);
        }

        [ContextMenu(nameof(LeaveLobby))]
        public void LeaveLobby()
        {
            _networkController.LeaveLobby();
        }

        #endregion

        #region Public Instantiation
        
        public void RequestRaiseEvent(INetworkEvent networkEvent, params INetworkEvent[] stackingNetworkEvents)
        {
            _networkController.RequestRaiseEvent(networkEvent, stackingNetworkEvents);
        }
        
        public void RequestRaiseEventCached(INetworkEvent networkEvent, params INetworkEvent[] stackingNetworkEvents)
        {
            _networkController.RequestRaiseEventCache(networkEvent, stackingNetworkEvents);
        }
        
        public void NetworkShareRuntimeObject<T>(T networkObject, params INetworkEvent[] onInstantiateCompleteNetworkEvents) where T : NetworkObject
        {
            if (string.IsNullOrEmpty(networkObject.SceneGuid))
            {
                Debug.LogError("Sharing a prefab is not allowed!");
                return;
            }
            
            NetworkObject parentNetworkObject = null;
            if (networkObject.transform.parent != null && !networkObject.transform.parent.TryGetComponent(out parentNetworkObject))
            {
                Debug.LogWarning($"Couldn't identify the parent for object {networkObject.name}!");
            }
            
            if (!prefabRegistry.TryGetPrefab(networkObject.PrefabGuid, out var prefabObject))
            {
                Debug.LogError($"The prefab for object {networkObject.gameObject.name} could not be found!");
                return;
            }

            INetworkEvent instantiationEvent;
            if (parentNetworkObject == null)
            {
                instantiationEvent = new InstantiatePosRotEvent(prefabObject, networkObject.SceneGuid, 
                    LocalConnection, networkObject.transform.position, networkObject.transform.rotation);
            }
            else
            {
                instantiationEvent = new InstantiatePosRotParentEvent(prefabObject, networkObject.SceneGuid, 
                    LocalConnection, networkObject.transform.position, networkObject.transform.rotation, parentNetworkObject);
            }
            
            HandleNetworkInstantiation(networkObject, instantiationEvent, onInstantiateCompleteNetworkEvents);
        }

        public T NetworkInstantiatePrefab<T>(T networkObject) where T : NetworkObject
        {
            var prefabObject = ValidateAndGetPrefab(networkObject);
            if (prefabObject == null) return null;
            
            var newNetworkObject = Instantiate(prefabObject.GetComponent<T>());
            var instantiationEvent = new InstantiateEvent(networkObject, newNetworkObject.SceneGuid, LocalConnection);
            return HandleNetworkInstantiation(newNetworkObject, instantiationEvent);
        }
        
        public T NetworkInstantiatePrefab<T>(T networkObject, Vector3 position, Quaternion rotation) where T : NetworkObject
        {
            var prefabObject = ValidateAndGetPrefab(networkObject);
            if (prefabObject == null) return null;
            
            var newNetworkObject = Instantiate(prefabObject.GetComponent<T>(), position, rotation);
            var instantiationEvent = new InstantiatePosRotEvent(networkObject, newNetworkObject.SceneGuid, LocalConnection, position, rotation);
            return HandleNetworkInstantiation(newNetworkObject, instantiationEvent);
        }

        public T NetworkInstantiatePrefab<T>(T networkObject, Vector3 position, Quaternion rotation, NetworkObject parent) where T : NetworkObject
        {
            var prefabObject = ValidateAndGetPrefab(networkObject);
            if (prefabObject == null) return null;
            
            var newNetworkObject = Instantiate(prefabObject.GetComponent<T>(), position, rotation, parent.transform);
            var instantiationEvent = new InstantiatePosRotParentEvent(networkObject, newNetworkObject.SceneGuid, LocalConnection, position, rotation, parent);
            return HandleNetworkInstantiation(newNetworkObject, instantiationEvent);
        }

        public T NetworkInstantiatePrefab<T>(T networkObject, NetworkObject parent) where T : NetworkObject
        {
            var prefabObject = ValidateAndGetPrefab(networkObject);
            if (prefabObject == null) return null;
            
            var newNetworkObject = Instantiate(prefabObject.GetComponent<T>(), parent.transform);
            var instantiationEvent = new InstantiateParentEvent(networkObject, newNetworkObject.SceneGuid, LocalConnection, parent);
            return HandleNetworkInstantiation(newNetworkObject, instantiationEvent);
        }

        public T NetworkInstantiatePrefab<T>(T networkObject, NetworkObject parent, bool worldPositionStays) where T : NetworkObject
        {
            var prefabObject = ValidateAndGetPrefab(networkObject);
            if (prefabObject == null) return null;
            
            var newNetworkObject = Instantiate(prefabObject.GetComponent<T>(), parent.transform, worldPositionStays);
            var instantiationEvent = new InstantiateParentStaysEvent(networkObject, newNetworkObject.SceneGuid, LocalConnection, parent, worldPositionStays);
            return HandleNetworkInstantiation(newNetworkObject, instantiationEvent);
        }
        
        public void NetworkDestroy(NetworkObject networkObject)
        {
            if (networkObject.IsUnityNull() || string.IsNullOrEmpty(networkObject.SceneGuid)) return;

            if (!networkObject.HasOwner || networkObject.Owner.Equals(NetworkManager.Instance.LocalConnection))
            {
                var instantiationEvent = new DestroyEvent(networkObject);
                networkObject.OnNetworkDestroy();
                Destroy(networkObject.gameObject);
                RequestRaiseEventCached(instantiationEvent);
            }
        }

        #endregion

        #region Private Instatiation
        
        private NetworkObject ValidateAndGetPrefab<T>(T networkObject) where T : NetworkObject
        {
            if (!string.IsNullOrEmpty(networkObject.SceneGuid))
            {
                return networkObject;
            }
            
            if (!prefabRegistry.TryGetPrefab(networkObject.PrefabGuid, out var prefabObject))
            {
                Debug.LogError($"The prefab for object {networkObject.gameObject.name} could not be found!");
                return null;
            }
            return prefabObject;
        }
        
        private TNetworkObject HandleNetworkInstantiation<TNetworkObject>(TNetworkObject newNetworkObject, INetworkEvent instantiationEvent, 
            params INetworkEvent[] networkEvents) where TNetworkObject : NetworkObject
        {
            RequestRaiseEventCached(instantiationEvent, networkEvents);
            newNetworkObject.OnNetworkInstantiate();
            return newNetworkObject;
        }

        #endregion

        #region Internal Callbacks

        internal void OnError(ReceivedMessage receivedMessage)
        {
            if (debug)
            {
                Debug.LogError($"{nameof(OnError)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            }
            
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
            if (debug)
            {
                Debug.Log($"{nameof(OnConnected)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            }
            
            LocalConnection = ownConnection;
            
            foreach (var keyValuePair in NetworkObjects)
            {
                keyValuePair.Value.OnConnected(ownConnection);
            }
        }

        internal void OnLobbiesFetched(ReceivedMessage receivedMessage, LobbiesData[] lobbiesData)
        {
            if (debug)
            {
                Debug.Log($"{nameof(OnLobbiesFetched)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            }
            
            foreach (var keyValuePair in NetworkObjects)
            {
                keyValuePair.Value.OnLobbiesFetched(lobbiesData);
            }
        }

        internal void OnLobbyCreated(ReceivedMessage receivedMessage)
        {
            if (debug)
            {
                Debug.Log($"{nameof(OnLobbyCreated)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            }
            
            LobbyConnections.Add(LocalConnection);
            
            foreach (var keyValuePair in NetworkObjects)
            {
                keyValuePair.Value.OnLobbyCreated();
            }
        }

        internal void OnLobbyJoining(ReceivedMessage receivedMessage, JoinLobbyClientData joinLobbyClientData)
        {
            if (debug)
            {
                Debug.Log($"{nameof(OnLobbyJoining)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            }
            
            foreach (var clientID in joinLobbyClientData.clientIDs)
            {
                LobbyConnections.Add(new NetworkConnection(clientID));
            }
            
            foreach (var keyValuePair in NetworkObjects)
            {
                keyValuePair.Value.OnLobbyJoining(joinLobbyClientData);
            }
        }

        internal void OnLobbyJoined(ReceivedMessage receivedMessage, JoinLobbyClientData joinLobbyClientData)
        {
            if (debug)
            {
                Debug.Log($"{nameof(OnLobbyJoined)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            }
            
            foreach (var keyValuePair in NetworkObjects)
            {
                keyValuePair.Value.OnLobbyJoined(joinLobbyClientData);
            }
        }

        internal void OnClientJoinedLobby(ReceivedMessage receivedMessage, NetworkConnection joinedClient)
        {
            if (debug)
            {
                Debug.Log($"{nameof(OnClientJoinedLobby)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            }
            
            LobbyConnections.Add(joinedClient);
            
            foreach (var keyValuePair in NetworkObjects)
            {
                keyValuePair.Value.OnClientJoinedLobby(joinedClient);
            }
        }

        internal void OnLeaveLobby(ReceivedMessage receivedMessage)
        {
            if (debug)
            {
                Debug.Log($"{nameof(OnLeaveLobby)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            }
            
            LobbyConnections.Clear();
            
            foreach (var keyValuePair in NetworkObjects)
            {
                keyValuePair.Value.OnLeaveLobby();
            }
        }

        internal void OnClientLeftLobby(ReceivedMessage receivedMessage, NetworkConnection disconnectedClient)
        {
            if (debug)
            {
                Debug.Log($"{nameof(OnClientLeftLobby)}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
            }
            
            LobbyConnections.RemoveAll(x => x.ConnectionID == disconnectedClient.ConnectionID);
            
            foreach (var keyValuePair in NetworkObjects)
            {
                keyValuePair.Value.InternalOnClientLeftLobby(disconnectedClient);
            }
        }

        internal void OnEventResponse(ReceivedMessage receivedMessage, INetworkEvent networkEvent)
        {
            if (debug)
            {
                Debug.Log($"EventResponse: {networkEvent.GetType()}");
            }
        }

        #endregion
    }
}
