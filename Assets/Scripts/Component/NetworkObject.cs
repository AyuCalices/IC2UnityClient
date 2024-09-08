using System;
using LockstepNetworking;
using SaveLoadSystem.Utility;
using UnityEditor;
using UnityEngine;

namespace Component
{
    public class NetworkObject : MonoBehaviour, ICreateGameObjectHierarchy, IChangeComponentProperties, IChangeGameObjectProperties, IChangeGameObjectStructure, IChangeGameObjectStructureHierarchy
    {
        [SerializeField] private string serializeFieldSceneGuid;
        private string _resetBufferSceneGuid;

        [SerializeField] private string prefabPath;

        public NetworkConnection Owner { get => owner; set => owner = value; }
        [SerializeField] private NetworkConnection owner;

        public string SceneGuid => serializeFieldSceneGuid;
        public string PrefabGuid => prefabPath;
        public bool HasOwner => Owner.IsValid;
        

        private void Reset()
        {
            ApplyResetBuffer();
        }
    
        private void Awake()
        {
            SetupSceneGuid();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            ApplyScriptReloadBuffer();
            SetupEditorAll();
        }
    
        public void OnCreateGameObjectHierarchy()
        {
            if (Application.isPlaying) return;
        
            SetupEditorAll();
        }
    
        public void OnChangeGameObjectStructure()
        {
            if (Application.isPlaying) return;
        
            SetupEditorAll();
        }
    
        public void OnChangeComponentProperties()
        {
            if (Application.isPlaying) return;
        
            SetupEditorAll();
        }

        public void OnChangeGameObjectProperties()
        {
            if (Application.isPlaying) return;
        
            SetupEditorAll();
        }
    
        public void OnChangeGameObjectStructureHierarchy()
        {
            if (Application.isPlaying) return;
        
            SetupEditorAll();
        }
    
        /// <summary>
        /// If a Component get's resetted, all Serialize Field values are lost. This method will reapply the lost values
        /// for the Serialize Fields with the Reset Buffer. This prevents loosing the original guid.
        /// </summary>
        private void ApplyResetBuffer()
        {
            serializeFieldSceneGuid = _resetBufferSceneGuid;
        }

        /// <summary>
        /// Serialize Fields will be serialized through script reloads and application restarts. The Reset Buffer values
        /// will be lost. This method will reapply the lost values for the Reset Buffer with the Serialize Fields. This
        /// prevents loosing the original guid.
        /// </summary>
        private void ApplyScriptReloadBuffer()
        {
            _resetBufferSceneGuid = serializeFieldSceneGuid;
        }
        
        private void SetupSceneGuid()
        {
            var websocketsClient = FindObjectOfType<WebSocketClient>(true);
            
            if (!string.IsNullOrEmpty(serializeFieldSceneGuid))
            {
                if (websocketsClient.NetworkObjects.TryGetValue(serializeFieldSceneGuid, out NetworkObject networkObject))
                {
                    if (networkObject != this)
                    {
                        SetSceneGuidGroup(Guid.NewGuid().ToString());
                        websocketsClient.NetworkObjects.Add(serializeFieldSceneGuid, this);
                    }
                }
                else
                {
                    websocketsClient.NetworkObjects.Add(serializeFieldSceneGuid, this);
                }
            }
            else
            {
                SetSceneGuidGroup(Guid.NewGuid().ToString());
            }
        }

        private void SetupEditorAll()
        {
            if (gameObject.scene.name != null)
            {
                SetupSceneGuid();
            }
            else
            {
                ResetSceneGuid();
            }
        
            SetDirty(this);
        }

        private void ResetSceneGuid()
        {
            SetSceneGuidGroup("");
        }

        public void SetSceneGuidGroup(string guid)
        {
            serializeFieldSceneGuid = guid;
            _resetBufferSceneGuid = guid;
        }
    
        public void SetPrefabPath(string newPrefabPath)
        {
            prefabPath = newPrefabPath;
        }
        
        public void SecureRequestOwnership()
        {
            var webSocketClient = UnityUtility.FindObjectOfTypeInScene<WebSocketClient>(gameObject.scene, true);
            var requestOwnershipEvent = new SaveRequestOwnershipEvent(this, webSocketClient.LocalConnection);
            webSocketClient.RequestRaiseEvent(requestOwnershipEvent);
        }

        public void SecureReleaseOwnership()
        {
            var releaseOwnershipEvent = new SaveReleaseOwnershipEvent(this);
            UnityUtility.FindObjectOfTypeInScene<WebSocketClient>(gameObject.scene, true).RequestRaiseEvent(releaseOwnershipEvent);
        }

        public void OnAfterAcquireOwnership(NetworkConnection oldConnection, NetworkConnection newConnection)
        {
            foreach (var onAfterAcquireOwnership in GetComponents<IOnAfterAcquireOwnership>())
            {
                onAfterAcquireOwnership.OnAfterAcquireOwnership(oldConnection, newConnection);
            }
        }

        public void OnBeforeLoseOwnership(NetworkConnection oldConnection, NetworkConnection newConnection)
        {
            foreach (var onBeforeLoseOwnership in GetComponents<IOnBeforeLoseOwnership>())
            {
                onBeforeLoseOwnership.OnBeforeLoseOwnership(oldConnection, newConnection);
            }
        }
    
        private void SetDirty(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(obj);
            }
#endif
        }
    }

    public interface IOnAfterAcquireOwnership
    {
        void OnAfterAcquireOwnership(NetworkConnection oldConnection, NetworkConnection newConnection);
    }
    
    public interface IOnBeforeLoseOwnership
    {
        void OnBeforeLoseOwnership(NetworkConnection oldConnection, NetworkConnection newConnection);
    }
    
    public readonly struct SaveRequestOwnershipEvent : INetworkEvent
    {
        public readonly NetworkObject NetworkObject;
        public readonly NetworkConnection NetworkConnection;

        public SaveRequestOwnershipEvent(NetworkObject networkObject, NetworkConnection networkConnection)
        {
            NetworkObject = networkObject;
            NetworkConnection = networkConnection;
        }

        public bool ValidateRequest()
        {
            return !NetworkObject.HasOwner;
        }

        public void PerformEvent()
        {
            if (NetworkObject.HasOwner) return;
            
            var oldConnection = NetworkObject.Owner;
            NetworkObject.Owner = NetworkConnection;
            NetworkObject.OnAfterAcquireOwnership(oldConnection, NetworkConnection);
        }
    }
    
    public readonly struct SaveReleaseOwnershipEvent : INetworkEvent
    {
        public readonly NetworkObject NetworkObject;

        public SaveReleaseOwnershipEvent(NetworkObject networkObject)
        {
            NetworkObject = networkObject;
        }

        private bool IsOwner => NetworkObject.Owner.Equals(UnityUtility.FindObjectOfTypeInScene<WebSocketClient>(NetworkObject.gameObject.scene, true).LocalConnection);

        public bool ValidateRequest()
        {
            return IsOwner;
        }

        public void PerformEvent()
        {
            if (!NetworkObject.HasOwner) return;

            var newConnection = new NetworkConnection();
            NetworkObject.OnBeforeLoseOwnership(NetworkObject.Owner, newConnection);
            NetworkObject.Owner = newConnection;
        }
    }
}
