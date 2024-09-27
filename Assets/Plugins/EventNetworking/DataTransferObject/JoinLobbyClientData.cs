using System;

namespace Plugins.EventNetworking.DataTransferObject
{
    [Serializable]
    public struct JoinLobbyClientData
    {
        public string[] clientIDs;
        public string[] lobbyData;
    }
}
