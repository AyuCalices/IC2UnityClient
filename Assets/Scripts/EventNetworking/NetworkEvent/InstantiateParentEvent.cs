using EventNetworking.Component;
using EventNetworking.Core;
using UnityEngine;

namespace EventNetworking.NetworkEvent
{
    // ReSharper disable MemberCanBePrivate.Global
    public readonly struct InstantiateParentEvent : INetworkEvent
    {
        public readonly NetworkObject OriginObj;
        public readonly string OriginID;
        public readonly string NewID;
        public readonly NetworkConnection NetworkConnection;
        public readonly NetworkObject Parent;

        public InstantiateParentEvent(NetworkObject originObj, string newID, NetworkConnection networkConnection, 
            NetworkObject parent)
        {
            OriginObj = originObj;
            OriginID = originObj.SceneGuid;
            NewID = newID;
            NetworkConnection = networkConnection;
            Parent = parent;
        }

        public bool ValidateRequest()
        {
            return true;
        }

        public void PerformEvent()
        {
            if (NetworkConnection.Equals(NetworkManager.Instance.LocalConnection)) return;
        
            OriginObj.SetSceneGuidGroup(NewID);
            var newNetworkObj = Object.Instantiate(OriginObj, Parent.transform);
            OriginObj.SetSceneGuidGroup(OriginID);
        
            newNetworkObj.OnNetworkInstantiate();
        }
    }
}
