using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;

namespace Plugins.EventNetworking.NetworkEvent
{
    public readonly struct SaveReleaseOwnershipEvent : INetworkEvent
    {
        private readonly NetworkObject _networkObject;

        public SaveReleaseOwnershipEvent(NetworkObject networkObject)
        {
            _networkObject = networkObject;
        }

        public void PerformEvent()
        {
            if (!_networkObject.HasOwner) return;

            var newConnection = new NetworkConnection();

            if (_networkObject.Owner.Equals(NetworkManager.Instance.LocalConnection))
            {
                _networkObject.OnBeforeLoseOwnership(_networkObject.Owner, newConnection);
            }
            
            _networkObject.Owner = newConnection;
        }
    }
}
