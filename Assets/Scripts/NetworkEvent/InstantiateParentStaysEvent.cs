using Component;
using Core;
using UnityEngine;

namespace NetworkEvent
{
    // ReSharper disable MemberCanBePrivate.Global
    public readonly struct InstantiateParentStaysEvent : INetworkEvent
    {
        public readonly NetworkObject OriginObj;
        public readonly string OriginID;
        public readonly string NewID;
        public readonly NetworkConnection NetworkConnection;
        public readonly NetworkObject Parent;
        public readonly bool WorldPositionStays;

        public InstantiateParentStaysEvent(NetworkObject originObj, string newID, NetworkConnection networkConnection,
            NetworkObject parent, bool worldPositionStays)
        {
            OriginObj = originObj;
            OriginID = originObj.SceneGuid;
            NewID = newID;
            NetworkConnection = networkConnection;
            Parent = parent;
            WorldPositionStays = worldPositionStays;
        }

        public bool ValidateRequest()
        {
            return true;
        }

        public void PerformEvent()
        {
            if (NetworkConnection.Equals(NetworkManager.Instance.LocalConnection)) return;

            OriginObj.SetSceneGuidGroup(NewID);
            var newNetworkObj = Object.Instantiate(OriginObj, Parent.transform, WorldPositionStays);
            OriginObj.SetSceneGuidGroup(OriginID);

            newNetworkObj.OnNetworkInstantiate();
        }
    }
}
