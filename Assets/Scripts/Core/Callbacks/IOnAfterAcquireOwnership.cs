namespace Core.Callbacks
{
    public interface IOnAfterAcquireOwnership
    {
        void OnAfterAcquireOwnership(NetworkConnection oldConnection, NetworkConnection newConnection);
    }
}
