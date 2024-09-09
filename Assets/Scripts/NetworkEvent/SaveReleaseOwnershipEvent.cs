using Component;
using Core;

namespace NetworkEvent
{
    public readonly struct SaveReleaseOwnershipEvent : INetworkEvent
    {
        public readonly NetworkObject NetworkObject;

        public SaveReleaseOwnershipEvent(NetworkObject networkObject)
        {
            NetworkObject = networkObject;
        }

        private bool IsOwner => NetworkObject.Owner.Equals(NetworkManager.Instance.LocalConnection);

        public bool ValidateRequest()
        {
            return IsOwner;
        }

        public void PerformEvent()
        {
            if (!NetworkObject.HasOwner) return;

            var newConnection = new NetworkConnection();
            NetworkObject.OnBeforeLoseOwnership(NetworkObject.Owner, newConnection);
            NetworkObject.Owner = newConnection;
        }
    }
}
