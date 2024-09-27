using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using UnityEngine;

namespace Plugins.EventNetworking.NetworkEvent
{
    public readonly struct InstantiateParentStaysEvent : INetworkEvent
    {
        private readonly NetworkObject _originObj;
        private readonly string _originID;
        private readonly string _newID;
        private readonly NetworkConnection _networkConnection;
        private readonly NetworkObject _parent;
        private readonly bool _worldPositionStays;

        public InstantiateParentStaysEvent(NetworkObject originObj, string newID, NetworkConnection networkConnection,
            NetworkObject parent, bool worldPositionStays)
        {
            _originObj = originObj;
            _originID = originObj.SceneGuid;
            _newID = newID;
            _networkConnection = networkConnection;
            _parent = parent;
            _worldPositionStays = worldPositionStays;
        }

        public void PerformEvent()
        {
            if (_networkConnection.Equals(NetworkManager.Instance.LocalConnection)) return;

            _originObj.SetSceneGuidGroup(_newID);
            var newNetworkObj = Object.Instantiate(_originObj, _parent.transform, _worldPositionStays);
            _originObj.SetSceneGuidGroup(_originID);

            newNetworkObj.OnNetworkInstantiate();
        }
    }
}
