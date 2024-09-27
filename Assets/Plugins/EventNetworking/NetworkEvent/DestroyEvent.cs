using EventNetworking.Component;
using UnityEngine;

namespace EventNetworking.NetworkEvent
{
    public readonly struct DestroyEvent : INetworkEvent
    {
        private readonly NetworkObject _originObj;

        public DestroyEvent(NetworkObject originObj)
        {
            _originObj = originObj;
        }

        public void PerformEvent()
        {
            if (_originObj.IsUnityNull()) return;
        
            Object.Destroy(_originObj.gameObject);
        
            _originObj.OnNetworkDestroy();
        }
    }
}
