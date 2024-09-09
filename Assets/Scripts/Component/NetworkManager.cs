using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core;
using DataTransferObject;
using Identification;
using NetworkEvent;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityCommunity.UnitySingleton;
using UnityEngine;

namespace Component
{
    public class NetworkManager : PersistentMonoSingleton<NetworkManager>
    {
        //write about cecil -> to complex, because of that -> delegate
        //a way to correctly identify every client -> test
        //NO host -> everyone needs to hold the data -> means, host migration is not needed as well. BUT: if a new client joins the room, he needs to fetch all data -> test
        //events aka RPC's is the main way for communication -> test
        //No predicition needed
        
        //TODO: various type support: Instantiation of Objects & it might be neccessary to keep references over the server?
        //TODO: reconnect

        [SerializeField] private PrefabRegistry prefabRegistry;
        [SerializeField] private double keepAliveInterval = 20;
        [SerializeField] private string lobbyName = "lobbyName";
        [SerializeField] private int lobbyCapacity = 4;
        
        
        public Dictionary<string, NetworkObject> NetworkObjects { get; } = new();   //must be non static!
        public HashSet<NetworkConnection> LobbyConnections { get; private set; } = new();
        public NetworkConnection LocalConnection { get; private set; }
        

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationToken;

        private const string ErrorResponse = "errorResponse";
        
        private const string ConnectedResponse = "connectedResponse";

        private const string FetchLobbyRequest = "fetchLobbyRequest";
        private const string FetchLobbyResponse = "fetchLobbyResponse";

        private const string CreateLobbyRequest = "createLobbyRequest";
        private const string CreateLobbyResponse = "createLobbyResponse";

        private const string JoinLobbyRequest = "joinLobbyRequest";
        private const string JoinLobbyClientResponse = "joinLobbyClientResponse";
        private const string JoinLobbyBroadcastResponse = "joinLobbyBroadcastResponse";

        private const string LeaveLobbyRequest = "leaveLobbyRequest";
        private const string LeaveLobbyClientResponse = "leaveLobbyClientResponse";
        private const string LeaveLobbyBroadcastResponse = "leaveLobbyBroadcastResponse";
        
        private const string EventRequest = "clientEventRequest";
        private const string EventResponse = "clientEventResponse";

        protected override void Awake()
        {
            base.Awake();
            
            _cancellationToken = new CancellationTokenSource();
        }

        [ContextMenu("Connect To Server")]
        public async void ConnectToServer()
        {
            _webSocket = new ClientWebSocket();
            _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(keepAliveInterval);
            
            try
            {
                // Connect to the WebSocket server
                await _webSocket.ConnectAsync(new System.Uri("ws://localhost:8080"), _cancellationToken.Token);
                Debug.Log("Connected to WebSocket server");

                // Start listening to messages
                await ListenForMessages(_cancellationToken.Token);
            }
            catch (WebSocketException ex)
            {
                Debug.LogError($"WebSocket error during connection: {ex.Message}");
            }
        }

        private void OnApplicationQuit()
        {
            try
            {
                if (_webSocket != null)
                {
                    if (_webSocket.State == WebSocketState.Open)
                    {
                        _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Application closing", _cancellationToken.Token).Wait();
                    }
                    _webSocket.Dispose();
                }
                _cancellationToken.Cancel();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during application quit: {ex.Message}");
            }
        }
        
        private async Task ListenForMessages(CancellationToken token)
        {
            var buffer = new byte[1024];
            var messageBuffer = new List<byte>();

            try
            {
                while (_webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            // Add the received bytes to the message buffer
                            messageBuffer.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, token);
                        }

                    } while (!result.EndOfMessage); // Continue receiving until EndOfMessage is true

                    // Once the entire message is received, convert it to a string
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(messageBuffer.ToArray());
                        messageBuffer.Clear();
                        
                        // Process the received message
                        var receivedMessage = JsonConvert.DeserializeObject<ReceivedMessage>(message);
                        switch (receivedMessage.type)
                        {
                            case ErrorResponse:
                                Debug.LogError($"{ErrorResponse}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
                                break;
                            case ConnectedResponse:
                                Debug.Log($"{ConnectedResponse}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
                                var connectedData = JsonConvert.DeserializeObject<JoinLobbyBroadcastData>(receivedMessage.message);
                                LocalConnection = new NetworkConnection(connectedData.clientID);
                                break;
                            case FetchLobbyResponse:
                                Debug.Log($"{FetchLobbyResponse}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
                                break;
                            case CreateLobbyResponse:
                                Debug.Log($"{CreateLobbyResponse}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
                                break;
                            case JoinLobbyClientResponse:
                                Debug.Log($"{JoinLobbyClientResponse}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
                                var joinLobbyClientData = JsonConvert.DeserializeObject<JoinLobbyClientData>(receivedMessage.message);
                                foreach (var clientID in joinLobbyClientData.clientIDs)
                                {
                                    LobbyConnections.Add(new NetworkConnection(clientID));
                                }
                                
                                foreach (var objLockstepEvent in joinLobbyClientData.lobbyData)
                                {
                                    DeserializeLockstepEvent(JsonConvert.DeserializeObject<RPCRequestData>(objLockstepEvent)).PerformEvent();
                                }
                                break;
                            
                            case JoinLobbyBroadcastResponse:
                                Debug.Log($"{JoinLobbyBroadcastResponse}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
                                var joinLobbyBroadcastData = JsonConvert.DeserializeObject<JoinLobbyBroadcastData>(receivedMessage.message);
                                LobbyConnections.Add(new NetworkConnection(joinLobbyBroadcastData.clientID));
                                break;
                            
                            case LeaveLobbyClientResponse:
                                Debug.Log($"{LeaveLobbyClientResponse}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
                                LobbyConnections.Clear();
                                break;
                            case LeaveLobbyBroadcastResponse:
                                Debug.Log($"{LeaveLobbyBroadcastResponse}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
                                var leaveLobbyBroadcastData = JsonConvert.DeserializeObject<LeaveLobbyBroadcastData>(receivedMessage.message);
                                LobbyConnections.RemoveWhere(x => x.ConnectionID == leaveLobbyBroadcastData.clientID);
                                break;
                            case EventResponse:
                                var instance = DeserializeLockstepEvent(JsonConvert.DeserializeObject<RPCRequestData>(receivedMessage.message));
                                instance.PerformEvent();
                                // Handle the data as needed
                                break;
                            default:
                                Debug.LogWarning("Unknown message type received.");
                                break;
                        }
                    }
                }
            }
            catch (WebSocketException ex)
            {
                Debug.LogError($"WebSocket error during message reception: {ex.Message}");
            }
        }
        
        private async void SendMessageToServer(string message)
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await _webSocket.SendAsync(new System.ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cancellationToken.Token);
                }
                catch (WebSocketException ex)
                {
                    Debug.LogError($"WebSocket error while sending message: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Unexpected error: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning("WebSocket is not open. Cannot send message.");
            }
        }
        
        [ContextMenu("Fetch Lobby")]
        public void FetchLobby()
        {
            var joinMessage = new
            {
                type = FetchLobbyRequest
            };
            string jsonMessage = JsonConvert.SerializeObject(joinMessage);
            SendMessageToServer(jsonMessage);
        }
        
        [ContextMenu("Create Lobby")]
        public void CreateLobby()
        {
            var joinMessage = new
            {
                type = CreateLobbyRequest,
                lobby = lobbyName,
                capacity = lobbyCapacity,
                password = "password"
            };
            string jsonMessage = JsonConvert.SerializeObject(joinMessage);
            SendMessageToServer(jsonMessage);
        }
        
        [ContextMenu("Join Lobby")]
        public void JoinLobby()
        {
            var joinMessage = new
            {
                type = JoinLobbyRequest,
                lobby = lobbyName,
                password = "password"
            };
            string jsonMessage = JsonConvert.SerializeObject(joinMessage);
            SendMessageToServer(jsonMessage);
        }

        [ContextMenu("Leave Lobby")]
        public void LeaveLobby()
        {
            var leaveMessage = new
            {
                type = LeaveLobbyRequest
            };
            string jsonMessage = JsonConvert.SerializeObject(leaveMessage);
            SendMessageToServer(jsonMessage);
        }

        private object ConvertToSerializable(object obj)
        {
            if (obj is NetworkObject networkObject)
            {
                if (!string.IsNullOrEmpty(networkObject.SceneGuid))
                {
                    return networkObject.SceneGuid;
                }

                if (!string.IsNullOrEmpty(networkObject.PrefabGuid))
                {
                    return networkObject.PrefabGuid;
                }

                Debug.LogError($"{typeof(NetworkObject)} neither has a {nameof(networkObject.SceneGuid)} nor a {networkObject.PrefabGuid} to identify it! Please make sure it has one!");
                return string.Empty;
            }

            if (obj is NetworkConnection networkConnection)
            {
                return networkConnection.ConnectionID;
            }

            return obj;
        }

        public bool RequestRaiseEvent<T>(T lockstepEvent, Action onBeforeValidEvent = null) where T : struct, INetworkEvent
        {
            if (lockstepEvent.ValidateRequest())
            {
                onBeforeValidEvent?.Invoke();
                RaiseEvent(lockstepEvent);
                return true;
            }

            return false;
        }
        
        public void NetworkInstantiate(NetworkObject networkObject)
        {
            var newNetworkObject = Instantiate(networkObject);
            var newID = newNetworkObject.SceneGuid;
        
            var instantiationEvent = new InstantiateEvent(networkObject, newID, LocalConnection);
            RequestRaiseEvent(instantiationEvent);

            newNetworkObject.OnNetworkInstantiate();
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
        
        private void RaiseEvent<T>(T lockstepEvent) where T : struct, INetworkEvent
        {
            var fieldInfos = lockstepEvent.GetType().GetFields();
            var propertyInfos = lockstepEvent.GetType().GetProperties();
            
            var jObject = new JArray();
            
            foreach (var fieldInfo in fieldInfos)
            {
                var serializedObj = ConvertToSerializable(fieldInfo.GetValue(lockstepEvent));
                jObject.Add(JToken.FromObject(serializedObj));
            }
            
            foreach (var propertyInfo in propertyInfos)
            {
                var serializedObj = ConvertToSerializable(propertyInfo.GetValue(lockstepEvent));
                jObject.Add(JToken.FromObject(serializedObj));
            }
            
            var rpcRequest = new RPCRequestData
            {
                lockstepType = lockstepEvent.GetType().AssemblyQualifiedName,
                Data = jObject
            };

            var message = new
            {
                type = EventRequest,
                data = rpcRequest
            };
            
            var jsonMessage = JsonConvert.SerializeObject(message);
            SendMessageToServer(jsonMessage);
        }
        
        private object ConvertFromSerializable(Type targetType, JToken obj)
        {
            if (targetType == typeof(NetworkObject))
            {
                var id = obj.ToObject<string>();

                if (NetworkObjects.TryGetValue(id, out NetworkObject networkObject))
                {
                    return networkObject;
                }
                
                if (prefabRegistry.TryGetPrefab(id, out networkObject))
                {
                    return networkObject;
                }
                
                Debug.LogWarning($"Couldn't identify any {typeof(NetworkObject)} with id {id}.");
                return null;
            }
            
            if (targetType == typeof(NetworkConnection))
            {
                return new NetworkConnection(obj.ToObject<string>());
            }

            return obj.ToObject(targetType);
        }
        
        private INetworkEvent DeserializeLockstepEvent(RPCRequestData rpcRequestData)
        {
            var type = Type.GetType(rpcRequestData.lockstepType);
            if (type == null) return null;

            var instance = Activator.CreateInstance(type);
            
            var fieldInfos = instance.GetType().GetFields();
            var fieldObjects = new object[fieldInfos.Length];
            for (var i = 0; i < fieldInfos.Length; i++)
            {
                fieldObjects[i] = ConvertFromSerializable(fieldInfos[i].FieldType, rpcRequestData.Data[i]);
            }
            
            var propertyInfos = instance.GetType().GetProperties();
            var propertyObjects = new object[propertyInfos.Length];
            for (var i = 0; i < propertyInfos.Length; i++)
            {
                propertyObjects[i] = ConvertFromSerializable(propertyInfos[i].PropertyType, rpcRequestData.Data[i]);
            }
            
            for (var i = 0; i < fieldInfos.Length; i++)
            {
                var fieldInfo = fieldInfos[i];
                
                fieldInfo.SetValue(instance, fieldObjects[i]);
            }
            
            for (var i = 0; i < propertyInfos.Length; i++)
            {
                var propertyInfo = propertyInfos[i];
                
                propertyInfo.SetValue(instance, propertyObjects[i]);
            }

            return instance as INetworkEvent;
        }
    }
}
