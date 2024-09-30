using System;
using System.Linq;
using Plugins.EventNetworking.Core;
using Plugins.EventNetworking.Core.Callbacks;
using Plugins.EventNetworking.DataTransferObject;
using Plugins.EventNetworking.Identification;
using Plugins.EventNetworking.NetworkEvent;
using UnityEditor;
using UnityEngine;

namespace Plugins.EventNetworking.Component
{
    public class NetworkObject : MonoBehaviour, ICreateGameObjectHierarchy, IChangeComponentProperties, IChangeGameObjectProperties, IChangeGameObjectStructure, IChangeGameObjectStructureHierarchy
    {
        [SerializeField] private string serializeFieldSceneGuid;
        private string _resetBufferSceneGuid;

        [SerializeField] private string prefabPath;

        [SerializeField] private bool migrateOwnerOnLeaveLobby;

        public NetworkConnection Owner { get => owner; set => owner = value; }
        [SerializeField] private NetworkConnection owner;

        public string SceneGuid => serializeFieldSceneGuid;
        public string PrefabGuid => prefabPath;
        public bool HasOwner => Owner.IsValid;

        
        #region Unity Lifecycle

        protected virtual void Reset()
        {
            ApplyResetBuffer();
        }
    
        protected virtual void Awake()
        {
            SetupSceneGuid(true);
        }

        protected virtual void OnDestroy()
        {
            var networkManager = NetworkManager.Instance;
            if (networkManager.NetworkObjects.TryGetValue(serializeFieldSceneGuid, out NetworkObject networkObject))
            {
                if (networkObject == this)
                {
                    networkManager.NetworkObjects.Remove(serializeFieldSceneGuid);
                }
            }
        }

        protected virtual void OnValidate()
        {
            if (Application.isPlaying) return;

            ApplyScriptReloadBuffer();
            SetupEditorAll();
        }

        #endregion

        #region Internal Interfaces

        void ICreateGameObjectHierarchy.OnCreateGameObjectHierarchy()
        {
            if (Application.isPlaying) return;
        
            SetupEditorAll();
        }
    
        void IChangeGameObjectStructure.OnChangeGameObjectStructure()
        {
            if (Application.isPlaying) return;
        
            SetupEditorAll();
        }
    
        void IChangeComponentProperties.OnChangeComponentProperties()
        {
            if (Application.isPlaying) return;
        
            SetupEditorAll();
        }

        void IChangeGameObjectProperties.OnChangeGameObjectProperties()
        {
            if (Application.isPlaying) return;
        
            SetupEditorAll();
        }
    
        void IChangeGameObjectStructureHierarchy.OnChangeGameObjectStructureHierarchy()
        {
            if (Application.isPlaying) return;
        
            SetupEditorAll();
        }

        #endregion

        #region Private

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

        private void SetupEditorAll()
        {
            if (gameObject.scene.name != null)
            {
                SetupSceneGuid(false);
            }
            else
            {
                ResetSceneGuid();
            }
        
            SetDirty(this);
        }
        
        private void SetupSceneGuid(bool isRuntime)
        {
            var networkManager = NetworkManager.Instance;
            
            if (!string.IsNullOrEmpty(serializeFieldSceneGuid))
            {
                if (networkManager.NetworkObjects.TryGetValue(serializeFieldSceneGuid, out NetworkObject networkObject))
                {
                    if (networkObject != this)
                    {
                        SetSceneGuidGroup(Guid.NewGuid().ToString());
                        networkManager.NetworkObjects.Add(serializeFieldSceneGuid, this);
                    }
                }
                else
                {
                    networkManager.NetworkObjects.Add(serializeFieldSceneGuid, this);
                }
            }
            else
            {
                SetSceneGuidGroup(Guid.NewGuid().ToString());
                if (isRuntime)
                {
                    networkManager.NetworkObjects.Add(serializeFieldSceneGuid, this);
                }
            }
        }

        private void ResetSceneGuid()
        {
            SetSceneGuidGroup("");
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

        #endregion

        #region Internal

        internal void SetSceneGuidGroup(string guid)
        {
            serializeFieldSceneGuid = guid;
            _resetBufferSceneGuid = guid;
        }
    
        internal void SetPrefabPath(string newPrefabPath)
        {
            prefabPath = newPrefabPath;
        }
        
        internal void OnAfterAcquireOwnership(NetworkConnection oldConnection, NetworkConnection newConnection)
        {
            foreach (var onAfterAcquireOwnership in GetComponents<IOnAfterAcquireOwnership>())
            {
                onAfterAcquireOwnership.OnAfterAcquireOwnership(oldConnection, newConnection);
            }
        }

        internal void OnBeforeLoseOwnership(NetworkConnection oldConnection, NetworkConnection newConnection)
        {
            foreach (var onBeforeLoseOwnership in GetComponents<IOnBeforeLoseOwnership>())
            {
                onBeforeLoseOwnership.OnBeforeLoseOwnership(oldConnection, newConnection);
            }
        }

        internal void OnNetworkInstantiate()
        {
            foreach (var onNetworkInstantiate in GetComponents<IOnNetworkInstantiate>())
            {
                onNetworkInstantiate.OnNetworkInstantiate();
            }
        }
        
        internal void OnNetworkDestroy()
        {
            foreach (var onNetworkDestroy in GetComponents<IOnNetworkDestroy>())
            {
                onNetworkDestroy.OnNetworkDestroy();
            }
        }
        
        internal void InternalOnClientLeftLobby(NetworkConnection disconnectedClient)
        {
            //state migration
            if (migrateOwnerOnLeaveLobby && HasOwner && Owner.Equals(disconnectedClient) && NetworkManager.Instance.LobbyConnections.Count > 0)
            {
                Owner = NetworkManager.Instance.LobbyConnections.FirstOrDefault();
            }

            OnClientLeftLobby(disconnectedClient);
        }

        #endregion

        #region Public

        public void SecureRequestOwnership()
        {
            if (HasOwner) return;
            
            var networkManager = NetworkManager.Instance;
            var requestOwnershipEvent = new SaveRequestOwnershipEvent(this, networkManager.LocalConnection);
            networkManager.RequestRaiseEventCached(requestOwnershipEvent);
        }

        public void SecureReleaseOwnership()
        {
            if (!HasOwner) return;
            if (!Owner.Equals(NetworkManager.Instance.LocalConnection)) return;
            
            NetworkManager.Instance.RequestRaiseEventCached(new SaveReleaseOwnershipEvent(this));
        }

        #endregion

        #region Callback

        public virtual void OnError(ErrorType errorType) { }

        public virtual void OnConnected(NetworkConnection ownConnection) { }

        public virtual void OnLobbiesFetched(LobbiesData[] lobbiesData) { }

        public virtual void OnLobbyCreated() { }

        public virtual void OnLobbyJoining(JoinLobbyClientData joinLobbyClientData) { }

        public virtual void OnLobbyJoined(JoinLobbyClientData joinLobbyClientData) { }

        public virtual void OnClientJoinedLobby(NetworkConnection joinedClient) { }

        public virtual void OnLeaveLobby() { }

        public virtual void OnClientLeftLobby(NetworkConnection disconnectedClient) { }
        
        public virtual void OnDisconnected() { }

        #endregion
    }
}
