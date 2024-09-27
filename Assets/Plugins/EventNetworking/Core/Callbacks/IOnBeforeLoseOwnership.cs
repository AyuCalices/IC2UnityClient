namespace Plugins.EventNetworking.Core.Callbacks
{
    public interface IOnBeforeLoseOwnership
    {
        void OnBeforeLoseOwnership(NetworkConnection oldConnection, NetworkConnection newConnection);
    }
}
