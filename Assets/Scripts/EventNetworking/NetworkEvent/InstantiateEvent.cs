using EventNetworking.Component;
using EventNetworking.Core;
using UnityEngine;

namespace EventNetworking.NetworkEvent
{
    // ReSharper disable MemberCanBePrivate.Global
    public readonly struct InstantiateEvent : INetworkEvent
    {
        public readonly NetworkObject OriginObj;
        public readonly string OriginID;
        public readonly string NewID;
        public readonly NetworkConnection NetworkConnection;

        public InstantiateEvent(NetworkObject originObj, string newID, NetworkConnection networkConnection)
        {
            OriginObj = originObj;
            OriginID = originObj.SceneGuid;
            NewID = newID;
            NetworkConnection = networkConnection;
        }

        public bool ValidateRequest()
        {
            return true;
        }

        public void PerformEvent()
        {
            if (NetworkConnection.Equals(NetworkManager.Instance.LocalConnection)) return;
        
            OriginObj.SetSceneGuidGroup(NewID);
            var newNetworkObj = Object.Instantiate(OriginObj);
            OriginObj.SetSceneGuidGroup(OriginID);
        
            newNetworkObj.OnNetworkInstantiate();
        }
    }
}
