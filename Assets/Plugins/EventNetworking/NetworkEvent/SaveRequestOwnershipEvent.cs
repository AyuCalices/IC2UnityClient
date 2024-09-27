using EventNetworking.Component;
using EventNetworking.Core;

namespace EventNetworking.NetworkEvent
{
    public readonly struct SaveRequestOwnershipEvent : INetworkEvent
    {
        private readonly NetworkObject _networkObject;
        private readonly NetworkConnection _networkConnection;

        public SaveRequestOwnershipEvent(NetworkObject networkObject, NetworkConnection networkConnection)
        {
            _networkObject = networkObject;
            _networkConnection = networkConnection;
        }

        public void PerformEvent()
        {
            if (_networkObject.HasOwner) return;
            
            var oldConnection = _networkObject.Owner;
            _networkObject.Owner = _networkConnection;

            if (_networkConnection.Equals(NetworkManager.Instance.LocalConnection))
            {
                _networkObject.OnAfterAcquireOwnership(oldConnection, _networkConnection);
            }
        }
    }
}
