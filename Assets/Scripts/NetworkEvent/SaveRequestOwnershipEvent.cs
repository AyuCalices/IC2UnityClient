using Component;
using Core;

namespace NetworkEvent
{
    public readonly struct SaveRequestOwnershipEvent : INetworkEvent
    {
        public readonly NetworkObject NetworkObject;
        public readonly NetworkConnection NetworkConnection;

        public SaveRequestOwnershipEvent(NetworkObject networkObject, NetworkConnection networkConnection)
        {
            NetworkObject = networkObject;
            NetworkConnection = networkConnection;
        }

        public bool ValidateRequest()
        {
            return !NetworkObject.HasOwner;
        }

        public void PerformEvent()
        {
            if (NetworkObject.HasOwner) return;
            
            var oldConnection = NetworkObject.Owner;
            NetworkObject.Owner = NetworkConnection;
            NetworkObject.OnAfterAcquireOwnership(oldConnection, NetworkConnection);
        }
    }
}
