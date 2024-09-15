using System;

namespace EventNetworking.DataTransferObject
{
    [Serializable]
    public struct ReceivedMessage
    {
        public string type;    // 'success', 'error', 'info', 'data'
        public string reason;  // Specific reason (e.g., 'LOBBY_NOT_FOUND')
        public string message; // Human-readable message
    }
}
