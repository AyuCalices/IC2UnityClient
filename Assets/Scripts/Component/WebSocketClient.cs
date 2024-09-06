using System;
using System.Collections.Generic;
using UnityEngine;
using System.Net.WebSockets;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using SaveLoadSystem.Utility;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace LockstepNetworking
{
    public class WebSocketClient : MonoBehaviour
    {
        //write about cecil -> to complex, because of that -> delegate
        //a way to correctly identify every client -> test
        //NO host -> everyone needs to hold the data -> means, host migration is not needed as well. BUT: if a new client joins the room, he needs to fetch all data -> test
        //events aka RPC's is the main way for communication -> test
        //No predicition needed
        
        //TODO: various type support: Instantiation of Objects & it might be neccessary to keep references over the server?
        //TODO: reconnect
        
        [SerializeField] private double keepAliveInterval = 20;
        [SerializeField] private string lobbyName = "lobbyName";
        [SerializeField] private int lobbyCapacity = 4;

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationToken;
        
        //TODO: place somewhere else where multi scene setups can be supported
        private Dictionary<string, NetworkObject> _networkObjectLookup;

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

        private void Start()
        {
            _networkObjectLookup = new Dictionary<string, NetworkObject>();
            foreach (var networkObject in UnityUtility.FindObjectsOfTypeInScene<NetworkObject>(gameObject.scene, true))
            {
                Debug.Log("o/");
                _networkObjectLookup.Add(networkObject.SceneGuid, networkObject);
            }
        }

        [ContextMenu("Connect To Server")]
        public async void ConnectToServer()
        {
            _webSocket = new ClientWebSocket();
            _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(keepAliveInterval);
            _cancellationToken = new CancellationTokenSource();
            
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
                                break;
                            case FetchLobbyResponse:
                                Debug.Log($"{FetchLobbyResponse}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
                                break;
                            case CreateLobbyResponse:
                                Debug.Log($"{CreateLobbyResponse}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
                                break;
                            case JoinLobbyClientResponse:
                                Debug.Log($"{JoinLobbyClientResponse}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
                                var obj = JsonConvert.DeserializeObject<LobbyJoinedData>(receivedMessage.message);
                                foreach (var objLockstepEvent in obj.lobbyData)
                                {
                                    DeserializeLockstepEvent(JsonConvert.DeserializeObject<RPCRequest>(objLockstepEvent)).PerformEvent();
                                }
                                break;
                            case JoinLobbyBroadcastResponse:
                                Debug.Log($"{JoinLobbyBroadcastResponse}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
                                break;
                            case LeaveLobbyClientResponse:
                                Debug.Log($"{LeaveLobbyClientResponse}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
                                break;
                            case LeaveLobbyBroadcastResponse:
                                Debug.Log($"{LeaveLobbyBroadcastResponse}: {receivedMessage.type} - {receivedMessage.reason}: {receivedMessage.message}");
                                break;
                            case EventResponse:
                                var instance = DeserializeLockstepEvent(JsonConvert.DeserializeObject<RPCRequest>(receivedMessage.message));
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
        
        [ContextMenu("Raise Event")]
        private void RaiseEvent()
        {
            //var eventData = new EventData(UnityEngine.Random.Range(0, int.MaxValue), "hello");
            //RaiseLockstepEvent(eventData);
        }

        private object ConvertToSerializable(object obj)
        {
            if (obj is NetworkObject networkObject)
            {
                return networkObject.SceneGuid;
            }

            return obj;
        }
        
        public void RaiseLockstepEvent<T>(T lockstepEvent) where T : struct, ILockstepEvent
        {
            var fieldInfos = lockstepEvent.GetType().GetFields();
            var propertyInfos = lockstepEvent.GetType().GetProperties();
            
            var jObject = new JArray();
            
            foreach (var fieldInfo in fieldInfos)
            {
                jObject.Add(JToken.FromObject(ConvertToSerializable(fieldInfo.GetValue(lockstepEvent))));
            }
            
            foreach (var propertyInfo in propertyInfos)
            {
                jObject.Add(JToken.FromObject(ConvertToSerializable(propertyInfo.GetValue(lockstepEvent))));
            }
            
            var rpcRequest = new RPCRequest
            {
                lockstepType = lockstepEvent.GetType().AssemblyQualifiedName,
                Data = jObject
            };

            var message = new
            {
                type = EventRequest,
                data = rpcRequest
            };
            
            string jsonMessage = JsonConvert.SerializeObject(message);
            SendMessageToServer(jsonMessage);
        }
        
        private object ConvertFromSerializable(Type targetType, JToken obj)
        {
            if (targetType == typeof(NetworkObject))
            {
                return _networkObjectLookup.GetValueOrDefault(obj.ToObject<string>());
            }

            return obj.ToObject(targetType);
        }
        
        private ILockstepEvent DeserializeLockstepEvent(RPCRequest rpcRequest)
        {
            var type = Type.GetType(rpcRequest.lockstepType);
            if (type == null) return null;

            var instance = Activator.CreateInstance(type);
            
            var fieldInfos = instance.GetType().GetFields();
            var fieldObjects = new object[fieldInfos.Length];
            for (var i = 0; i < fieldInfos.Length; i++)
            {
                var fieldInfo = fieldInfos[i];
                
                var obj = ConvertFromSerializable(fieldInfo.FieldType, rpcRequest.Data[i]);
                if (obj == null)
                {
                    Debug.LogError("Error occured during deserialization!");
                    return null;
                }

                fieldObjects[i] = obj;
            }
            
            var propertyInfos = instance.GetType().GetProperties();
            var propertyObjects = new object[propertyInfos.Length];
            for (var i = 0; i < propertyInfos.Length; i++)
            {
                var propertyInfo = propertyInfos[i];
                
                var obj = ConvertFromSerializable(propertyInfo.PropertyType, rpcRequest.Data[i]);
                if (obj == null)
                {
                    Debug.LogError("Error occured during deserialization!");
                    return null;
                }

                propertyObjects[i] = obj;
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

            return instance as ILockstepEvent;
        }
    }

    [Serializable]
    public struct ReceivedMessage
    {
        public string type;    // 'success', 'error', 'info', 'data'
        public string reason;  // Specific reason (e.g., 'LOBBY_NOT_FOUND')
        public string message; // Human-readable message
    }
    
    [Serializable]
    public struct LobbyJoinedData
    {
        public string[] clientIDs;
        public string[] lobbyData;
    }
    
    [Serializable]
    public class RPCRequest
    {
        public string lockstepType;
        public JArray Data;
    }

    public interface ILockstepEvent
    {
        public void PerformEvent();
    }

    public readonly struct EventData : ILockstepEvent
    {
        public readonly NetworkObject _networkObject;

        public EventData(NetworkObject networkObject)
        {
            _networkObject = networkObject;
        }

        public void PerformEvent()
        {
            Debug.Log(_networkObject.GetComponent<IdentificationTest>().text);
        }
    }
}
