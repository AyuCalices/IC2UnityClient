using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.DataTransferObject;
using Plugins.EventNetworking.Identification;
using Plugins.EventNetworking.NetworkEvent;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEngine;

namespace Plugins.EventNetworking.Core
{
    public class NetworkController
    {
        private readonly NetworkManager _networkManager;
        private readonly Dictionary<string, NetworkObject> _networkObjects;
        private readonly PrefabRegistry _prefabRegistry;
        private readonly CancellationTokenSource _cancellationToken;
        private readonly float _keepAliveInterval;
        private ClientWebSocket _webSocket;

        public bool IsConnected => _webSocket != null;
        
        
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
        
        private const string clearCacheEventRequest = "clearCacheEventRequest";
        private const string CacheEventRequest = "cacheEventRequest";
        private const string EventRequest = "clientEventRequest";
        private const string EventResponse = "clientEventResponse";
        
        private const BindingFlags DefaultFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        
        //TODO: currently tightly coupled -> in a future version it must be less coupled
        public NetworkController(NetworkManager networkManager, Dictionary<string, NetworkObject> networkObjects, PrefabRegistry prefabRegistry, float keepAliveInterval)
        {
            _networkManager = networkManager;
            _networkObjects = networkObjects;
            _prefabRegistry = prefabRegistry;
            _cancellationToken = new CancellationTokenSource();
            _keepAliveInterval = keepAliveInterval;
        }

        #region Public

        public async void ConnectToServer()
        {
            _webSocket = new ClientWebSocket();
            _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(_keepAliveInterval);
            
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
        
        public void DisconnectFromServer()
        {
            try
            {
                if (_webSocket != null)
                {
                    _networkManager.OnDisconnect();
                    
                    if (_webSocket.State == WebSocketState.Open)
                    {
                        _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Application closing", _cancellationToken.Token).Wait();
                    }
                    _webSocket.Dispose();
                    _webSocket = null;
                }
                _cancellationToken.Cancel();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during application quit: {ex.Message}");
            }
        }
        
        public void RequestRaiseEvent(INetworkEvent networkEvent, params INetworkEvent[] stackingNetworkEvents)
        {
            RequestRaiseEvent(EventRequest, networkEvent, stackingNetworkEvents);
        }
        
        public void RequestRaiseEventCache(INetworkEvent networkEvent, params INetworkEvent[] stackingNetworkEvents)
        {
            RequestRaiseEvent(CacheEventRequest, networkEvent, stackingNetworkEvents);
        }

        public void RequestClearEventCache()
        {
            var clearCacheMessage = new
            {
                type = clearCacheEventRequest
            };
            var jsonMessage = JsonConvert.SerializeObject(clearCacheMessage);
            SendMessageToServer(jsonMessage);
        }
        
        public void FetchLobby()
        {
            var joinMessage = new
            {
                type = FetchLobbyRequest
            };
            var jsonMessage = JsonConvert.SerializeObject(joinMessage);
            SendMessageToServer(jsonMessage);
        }
        
        public void CreateLobby(string lobbyName, int lobbyCapacity, string newPassword = null)
        {
            var joinMessage = new
            {
                type = CreateLobbyRequest,
                lobby = lobbyName,
                capacity = lobbyCapacity,
                password = newPassword
            };
            var jsonMessage = JsonConvert.SerializeObject(joinMessage);
            SendMessageToServer(jsonMessage);
        }
        
        public void JoinLobby(string lobbyName, string newPassword = null)
        {
            var joinMessage = new
            {
                type = JoinLobbyRequest,
                lobby = lobbyName,
                password = newPassword
            };
            var jsonMessage = JsonConvert.SerializeObject(joinMessage);
            SendMessageToServer(jsonMessage);
        }
        
        public void LeaveLobby()
        {
            var leaveMessage = new
            {
                type = LeaveLobbyRequest
            };
            var jsonMessage = JsonConvert.SerializeObject(leaveMessage);
            SendMessageToServer(jsonMessage);
        }

        #endregion

        #region Private Methods
        
        private void RequestRaiseEvent(string cacheEvent, INetworkEvent networkEvent, params INetworkEvent[] stackingNetworkEvents)
        {
            string serializedEvent;
            
            if (stackingNetworkEvents.Length > 0)
            {
                var networkEventGroup = new NetworkEventGroup(networkEvent, stackingNetworkEvents);
                serializedEvent = SerializeNetworkEvent(networkEventGroup, cacheEvent);
            }
            else
            {
                serializedEvent = SerializeNetworkEvent(networkEvent, cacheEvent);
            }
            
            SendMessageToServer(serializedEvent);
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

        //cant make this truly multithreaded -> clear order must be supported
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
                                _networkManager.OnError(receivedMessage);
                                break;
                            case ConnectedResponse:
                                var connectedData = JsonConvert.DeserializeObject<JoinLobbyBroadcastData>(receivedMessage.message);
                                _networkManager.OnConnected(receivedMessage, new NetworkConnection(connectedData.clientID));
                                break;
                            case FetchLobbyResponse:
                                var lobbiesData = JsonConvert.DeserializeObject<LobbiesData[]>(receivedMessage.message);
                                _networkManager.OnLobbiesFetched(receivedMessage, lobbiesData);
                                break;
                            case CreateLobbyResponse:
                                _networkManager.OnLobbyCreated(receivedMessage);
                                break;
                            case JoinLobbyClientResponse:
                                var joinLobbyClientData = JsonConvert.DeserializeObject<JoinLobbyClientData>(receivedMessage.message);
                                _networkManager.OnLobbyJoining(receivedMessage, joinLobbyClientData);
                                
                                foreach (var objLockstepEvent in joinLobbyClientData.lobbyData)
                                {
                                    DeserializeNetworkEvent(JsonConvert.DeserializeObject<RPCRequestData>(objLockstepEvent)).PerformEvent();
                                }
                                
                                _networkManager.OnLobbyJoined(receivedMessage, joinLobbyClientData);
                                break;
                            
                            case JoinLobbyBroadcastResponse:
                                var joinLobbyBroadcastData = JsonConvert.DeserializeObject<JoinLobbyBroadcastData>(receivedMessage.message);
                                _networkManager.OnClientJoinedLobby(receivedMessage, new NetworkConnection(joinLobbyBroadcastData.clientID));
                                break;
                            
                            case LeaveLobbyClientResponse:
                                _networkManager.OnLeaveLobby(receivedMessage);
                                break;
                            case LeaveLobbyBroadcastResponse:
                                var leaveLobbyBroadcastData = JsonConvert.DeserializeObject<LeaveLobbyBroadcastData>(receivedMessage.message);
                                _networkManager.OnClientLeftLobby(receivedMessage, new NetworkConnection(leaveLobbyBroadcastData.clientID));
                                break;
                            case EventResponse:     //this is a callback by definition
                                var networkEvent = DeserializeNetworkEvent(JsonConvert.DeserializeObject<RPCRequestData>(receivedMessage.message));
                                _networkManager.OnEventResponse(receivedMessage, networkEvent);
                                networkEvent.PerformEvent();
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
        
        private string SerializeNetworkEvent(INetworkEvent networkEvent, string eventType)
        {
            var rpcRequest = CreateRPCRequestData(networkEvent, eventType);

            var message = new
            {
                type = eventType,
                data = rpcRequest
            };
            
            return JsonConvert.SerializeObject(message);
        }
        
        private RPCRequestData CreateRPCRequestData(INetworkEvent networkEvent, string eventType)
        {
            var fieldInfos = networkEvent.GetType().GetFields(DefaultFlags);
            var propertyInfos = networkEvent.GetType().GetProperties(DefaultFlags);
            
            var jObject = new JArray();
            
            foreach (var fieldInfo in fieldInfos)
            {
                var serializedObj = ConvertToSerializable(fieldInfo.GetValue(networkEvent), eventType);
                jObject.Add(JToken.FromObject(serializedObj));
            }
            
            foreach (var propertyInfo in propertyInfos)
            {
                var serializedObj = ConvertToSerializable(propertyInfo.GetValue(networkEvent), eventType);
                jObject.Add(JToken.FromObject(serializedObj));
            }
            
            return new RPCRequestData
            {
                lockstepType = networkEvent.GetType().AssemblyQualifiedName,
                Data = jObject
            };
        }

        private object ConvertToSerializable(object obj, string eventType)
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

            if (obj is INetworkEvent[] networkEventGroup)
            {
                RPCRequestData[] finalString = new RPCRequestData[networkEventGroup.Length];
                for (var index = 0; index < networkEventGroup.Length; index++)
                {
                    finalString[index] = CreateRPCRequestData(networkEventGroup[index], eventType);
                }
                return finalString;
            }

            return obj;
        }
        
        private INetworkEvent DeserializeNetworkEvent(RPCRequestData rpcRequestData)
        {
            var type = Type.GetType(rpcRequestData.lockstepType);
            if (type == null) return null;

            var instance = Activator.CreateInstance(type);
            
            var fieldInfos = instance.GetType().GetFields(DefaultFlags);
            var fieldObjects = new object[fieldInfos.Length];
            for (var i = 0; i < fieldInfos.Length; i++)
            {
                fieldObjects[i] = ConvertFromSerializable(_networkObjects, _prefabRegistry, fieldInfos[i].FieldType, rpcRequestData.Data[i]);
            }
            
            var propertyInfos = instance.GetType().GetProperties(DefaultFlags);
            var propertyObjects = new object[propertyInfos.Length];
            for (var i = 0; i < propertyInfos.Length; i++)
            {
                propertyObjects[i] = ConvertFromSerializable(_networkObjects, _prefabRegistry, propertyInfos[i].PropertyType, rpcRequestData.Data[i]);
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
        
        private object ConvertFromSerializable(Dictionary<string, NetworkObject> networkObjects, PrefabRegistry prefabRegistry, Type targetType, JToken obj)
        {
            if (typeof(NetworkObject).IsAssignableFrom(targetType))
            {
                var id = obj.ToObject<string>();

                if (networkObjects.TryGetValue(id, out NetworkObject networkObject))
                {
                    return networkObject;
                }
                
                if (prefabRegistry.TryGetPrefab(id, out networkObject))
                {
                    return networkObject;
                }
                
                Debug.LogWarning($"Couldn't identify any {targetType} with id {id}.");
                return null;
            }
            
            if (typeof(NetworkConnection).IsAssignableFrom(targetType))
            {
                return new NetworkConnection(obj.ToObject<string>());
            }

            if (typeof(INetworkEvent[]).IsAssignableFrom(targetType))
            {
                var elements = obj.ToObject<RPCRequestData[]>();
                
                var networkEvents = new INetworkEvent[elements.Length];
                for (var i = 0; i < elements.Length; i++)
                {
                    networkEvents[i] = DeserializeNetworkEvent(elements[i]);
                    networkEvents[i].PerformEvent();
                }

                return networkEvents;
            }

            return obj.ToObject(targetType);
        }

        #endregion
        
        private readonly struct NetworkEventGroup : INetworkEvent
        {
            [UsedImplicitly] private readonly INetworkEvent[] _networkEvents;
        
            public NetworkEventGroup(INetworkEvent networkEvent, params INetworkEvent[] stackingNetworkEvents)
            {
                _networkEvents = new INetworkEvent[stackingNetworkEvents.Length + 1];
                _networkEvents[0] = networkEvent;
                for (var i = 0; i < stackingNetworkEvents.Length; i++)
                {
                    _networkEvents[i + 1] = stackingNetworkEvents[i];
                }
            }
        
            public void PerformEvent() { }
        }
    }
}
