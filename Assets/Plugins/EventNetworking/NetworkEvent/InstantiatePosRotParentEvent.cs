using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using UnityEngine;

namespace Plugins.EventNetworking.NetworkEvent
{
    public readonly struct InstantiatePosRotParentEvent : INetworkEvent
    {
        private readonly NetworkObject _originObj;
        private readonly string _originID;
        private readonly string _newID;
        private readonly NetworkConnection _networkConnection;
        private readonly (float x, float y, float z) _position;
        private readonly (float x, float y, float z, float w) _rotation;
        private readonly NetworkObject _parent;

        public InstantiatePosRotParentEvent(NetworkObject originObj, string newID, NetworkConnection networkConnection, 
            Vector3 position, Quaternion rotation, NetworkObject parent)
        {
            _originObj = originObj;
            _originID = originObj.SceneGuid;
            _newID = newID;
            _networkConnection = networkConnection;
            _position = (position.x, position.y, position.z);
            _rotation = (rotation.x, rotation.y, rotation.z, rotation.w);
            _parent = parent;
        }

        public void PerformEvent()
        {
            if (_networkConnection.Equals(NetworkManager.Instance.LocalConnection)) return;
        
            _originObj.SetSceneGuidGroup(_newID);
            var position = new Vector3(_position.x, _position.y, _position.z);
            var rotation = new Quaternion(_rotation.x, _rotation.y, _rotation.z, _rotation.w);
            var newNetworkObj = Object.Instantiate(_originObj, position, rotation, _parent.transform);
            _originObj.SetSceneGuidGroup(_originID);
        
            newNetworkObj.OnNetworkInstantiate();
        }
    }
}
