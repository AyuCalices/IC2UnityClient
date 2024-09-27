namespace Plugins.EventNetworking.Core.Callbacks
{
    public interface IOnAfterAcquireOwnership
    {
        void OnAfterAcquireOwnership(NetworkConnection oldConnection, NetworkConnection newConnection);
    }
}
