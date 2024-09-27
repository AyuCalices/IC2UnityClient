using System;

namespace Plugins.EventNetworking.DataTransferObject
{
    [Serializable]
    public struct LobbiesData
    {
        public string name;
        public int playerCount;
        public int capacity;
        public bool requiresPassword;
    }
}
