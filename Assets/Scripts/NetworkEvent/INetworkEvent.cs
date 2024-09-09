namespace NetworkEvent
{
    public interface INetworkEvent
    {
        public bool ValidateRequest();
        public void PerformEvent();
    }
}
