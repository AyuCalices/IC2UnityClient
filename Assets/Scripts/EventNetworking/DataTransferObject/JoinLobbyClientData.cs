using System;

namespace EventNetworking.DataTransferObject
{
    [Serializable]
    public struct JoinLobbyClientData
    {
        public string[] clientIDs;
        public string[] lobbyData;
    }
}
