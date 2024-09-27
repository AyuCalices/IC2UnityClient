using EventNetworking.Component;
using EventNetworking.Core;
using UnityEngine;

namespace EventNetworking.NetworkEvent
{
    public readonly struct InstantiatePosRotEvent : INetworkEvent
    {
        private readonly NetworkObject _originObj;
        private readonly string _originID;
        private readonly string _newID;
        private readonly NetworkConnection _networkConnection;
        private readonly Vector3 _position;
        private readonly Quaternion _rotation;

        public InstantiatePosRotEvent(NetworkObject originObj, string newID, NetworkConnection networkConnection, 
            Vector3 position, Quaternion rotation)
        {
            _originObj = originObj;
            _originID = originObj.SceneGuid;
            _newID = newID;
            _networkConnection = networkConnection;
            _position = position;
            _rotation = rotation;
        }

        public void PerformEvent()
        {
            if (_networkConnection.Equals(NetworkManager.Instance.LocalConnection)) return;
        
            _originObj.SetSceneGuidGroup(_newID);
            var newNetworkObj = Object.Instantiate(_originObj, _position, _rotation);
            _originObj.SetSceneGuidGroup(_originID);
        
            newNetworkObj.OnNetworkInstantiate();
        }
    }
}
