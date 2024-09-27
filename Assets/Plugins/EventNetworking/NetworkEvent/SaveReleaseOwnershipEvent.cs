using EventNetworking.Component;
using EventNetworking.Core;

namespace EventNetworking.NetworkEvent
{
    public readonly struct SaveReleaseOwnershipEvent : INetworkEvent
    {
        private readonly NetworkObject _networkObject;

        public SaveReleaseOwnershipEvent(NetworkObject networkObject)
        {
            _networkObject = networkObject;
        }

        private bool IsOwner => _networkObject.Owner.Equals(NetworkManager.Instance.LocalConnection);

        public void PerformEvent()
        {
            if (!_networkObject.HasOwner) return;

            var newConnection = new NetworkConnection();

            if (IsOwner)
            {
                _networkObject.OnBeforeLoseOwnership(_networkObject.Owner, newConnection);
            }
            
            _networkObject.Owner = newConnection;
        }
    }
}
