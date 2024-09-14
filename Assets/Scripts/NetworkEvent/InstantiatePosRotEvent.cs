using Component;
using Core;
using UnityEngine;

namespace NetworkEvent
{
    // ReSharper disable MemberCanBePrivate.Global
    public readonly struct InstantiatePosRotEvent : INetworkEvent
    {
        public readonly NetworkObject OriginObj;
        public readonly string OriginID;
        public readonly string NewID;
        public readonly NetworkConnection NetworkConnection;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public InstantiatePosRotEvent(NetworkObject originObj, string newID, NetworkConnection networkConnection, 
            Vector3 position, Quaternion rotation)
        {
            OriginObj = originObj;
            OriginID = originObj.SceneGuid;
            NewID = newID;
            NetworkConnection = networkConnection;
            Position = position;
            Rotation = rotation;
        }

        public bool ValidateRequest()
        {
            return true;
        }

        public void PerformEvent()
        {
            if (NetworkConnection.Equals(NetworkManager.Instance.LocalConnection)) return;
        
            OriginObj.SetSceneGuidGroup(NewID);
            var newNetworkObj = Object.Instantiate(OriginObj, Position, Rotation);
            OriginObj.SetSceneGuidGroup(OriginID);
        
            newNetworkObj.OnNetworkInstantiate();
        }
    }
}
