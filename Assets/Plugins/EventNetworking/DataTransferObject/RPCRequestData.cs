using System;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace EventNetworking.DataTransferObject
{
    [Serializable]
    public struct RPCRequestData
    {
        public string lockstepType;
        public JArray Data;
    }
}
