using EventNetworking.Component;
using EventNetworking.Core;
using UnityEngine;

namespace EventNetworking.NetworkEvent
{
    // ReSharper disable MemberCanBePrivate.Global
    public readonly struct InstantiatePosRotParentEvent : INetworkEvent
    {
        public readonly NetworkObject OriginObj;
        public readonly string OriginID;
        public readonly string NewID;
        public readonly NetworkConnection NetworkConnection;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly NetworkObject Parent;

        public InstantiatePosRotParentEvent(NetworkObject originObj, string newID, NetworkConnection networkConnection, 
            Vector3 position, Quaternion rotation, NetworkObject parent)
        {
            OriginObj = originObj;
            OriginID = originObj.SceneGuid;
            NewID = newID;
            NetworkConnection = networkConnection;
            Position = position;
            Rotation = rotation;
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
            var newNetworkObj = Object.Instantiate(OriginObj, Position, Rotation, Parent.transform);
            OriginObj.SetSceneGuidGroup(OriginID);
        
            newNetworkObj.OnNetworkInstantiate();
        }
    }
}
