using EventNetworking.Component;
using EventNetworking.Core;
using UnityEngine;

namespace EventNetworking.NetworkEvent
{
    public readonly struct InstantiateParentEvent : INetworkEvent
    {
        private readonly NetworkObject _originObj;
        private readonly string _originID;
        private readonly string _newID;
        private readonly NetworkConnection _networkConnection;
        private readonly NetworkObject _parent;

        public InstantiateParentEvent(NetworkObject originObj, string newID, NetworkConnection networkConnection, 
            NetworkObject parent)
        {
            _originObj = originObj;
            _originID = originObj.SceneGuid;
            _newID = newID;
            _networkConnection = networkConnection;
            _parent = parent;
        }

        public void PerformEvent()
        {
            if (_networkConnection.Equals(NetworkManager.Instance.LocalConnection)) return;
        
            _originObj.SetSceneGuidGroup(_newID);
            var newNetworkObj = Object.Instantiate(_originObj, _parent.transform);
            _originObj.SetSceneGuidGroup(_originID);
        
            newNetworkObj.OnNetworkInstantiate();
        }
    }
}
