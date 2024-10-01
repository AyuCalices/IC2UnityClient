using System;
using Newtonsoft.Json.Linq;

namespace Plugins.EventNetworking.DataTransferObject
{
    [Serializable]
    public struct RPCRequestData
    {
        public string lockstepType;
        public JArray Data;
    }
}
