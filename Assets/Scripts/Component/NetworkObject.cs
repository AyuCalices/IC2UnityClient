using System;
using Core;
using Core.Callbacks;
using Identification;
using NetworkEvent;
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

        
        #region Unity Lifecycle

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
        
        private void SetupSceneGuid()
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

        #endregion

        #region Public

        public void SecureRequestOwnership()
        {
            var networkManager = NetworkManager.Instance;
            var requestOwnershipEvent = new SaveRequestOwnershipEvent(this, networkManager.LocalConnection);
            networkManager.RequestRaiseEvent(requestOwnershipEvent);
        }

        public void SecureReleaseOwnership()
        {
            NetworkManager.Instance.RequestRaiseEvent(new SaveReleaseOwnershipEvent(this));
        }

        #endregion
    }
}
