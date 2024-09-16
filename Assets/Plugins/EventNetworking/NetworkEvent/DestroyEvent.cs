using EventNetworking.Component;
using UnityEngine;

namespace EventNetworking.NetworkEvent
{
    // ReSharper disable MemberCanBePrivate.Global
    public readonly struct DestroyEvent : INetworkEvent
    {
        public readonly NetworkObject OriginObj;

        public DestroyEvent(NetworkObject originObj)
        {
            OriginObj = originObj;
        }

        public bool ValidateRequest()
        {
            return !OriginObj.HasOwner || OriginObj.Owner.Equals(NetworkManager.Instance.LocalConnection);
        }

        public void PerformEvent()
        {
            if (OriginObj.IsUnityNull()) return;
        
            Object.Destroy(OriginObj.gameObject);
        
            OriginObj.OnNetworkDestroy();
        }
    }
}
