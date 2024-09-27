using System.Collections.Generic;
using Plugins.EventNetworking.Component;
using UnityEngine;

namespace Plugins.EventNetworking.Identification
{
    public class PrefabRegistry : ScriptableObject
    {
        [SerializeField] private List<NetworkObject> networkObjects = new();

        public List<NetworkObject> NetworkObjects => networkObjects;

        internal void AddNetworkObjectPrefab(NetworkObject networkObject, string prefabGuid)
        {
            if (!networkObjects.Contains(networkObject))
            {
                networkObjects.Add(networkObject);
            }

            networkObject.SetPrefabPath(prefabGuid);
        }
        
        internal void RemoveNetworkObjectPrefab(string prefabGuid)
        {
            var networkObject = networkObjects.Find(x => x.PrefabGuid == prefabGuid);
            if (networkObject != null)
            {
                networkObject.SetPrefabPath(string.Empty);
                networkObjects.Remove(networkObject);
            }
        }

        internal void CleanupDestroyedObjects()
        {
            for (var i = networkObjects.Count - 1; i >= 0; i--)
            {
                if (networkObjects[i].IsUnityNull())
                {
                    networkObjects.RemoveAt(i);
                }
            }
        }
        
        internal void ChangeGuid(string oldGuid, string prefabGuid)
        {
            var networkObject = networkObjects.Find(x => x.PrefabGuid == oldGuid);
            if (networkObject != null)
            {
                networkObject.SetPrefabPath(prefabGuid);
            }
        }

        public bool ContainsPrefabGuid(string prefabGuid)
        {
            return networkObjects.Find(x => x.PrefabGuid == prefabGuid) != null;
        }
    
        public bool TryGetPrefab(string prefabGuid, out NetworkObject foundNetworkObject)
        {
            var networkObject = networkObjects.Find(x => x.PrefabGuid == prefabGuid);
            if (networkObject != null)
            {
                foundNetworkObject = networkObject;
                return true;
            }

            foundNetworkObject = null;
            return false;
        }
    }
}
