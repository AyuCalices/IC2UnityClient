using System;

namespace DataTransferObject
{
    [Serializable]
    public struct JoinLobbyClientData
    {
        public string[] clientIDs;
        public string[] lobbyData;
    }
}
