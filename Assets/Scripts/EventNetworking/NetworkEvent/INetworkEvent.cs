namespace EventNetworking.NetworkEvent
{
    public interface INetworkEvent
    {
        public bool ValidateRequest();
        public void PerformEvent();
    }
}
