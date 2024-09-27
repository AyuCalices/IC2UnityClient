using System;

namespace Plugins.EventNetworking.Core
{
    [Serializable]
    public struct NetworkConnection : IEquatable<NetworkConnection>
    {
        public string ConnectionID => _connectionID;
        private string _connectionID;
        
        public bool IsValid => !string.IsNullOrEmpty(_connectionID);
        
        public NetworkConnection(string connectionID)
        {
            _connectionID = connectionID;
        }

        public bool Equals(NetworkConnection other)
        {
            return _connectionID == other._connectionID;
        }

        public override bool Equals(object obj)
        {
            return obj is NetworkConnection other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _connectionID != null ? _connectionID.GetHashCode() : 0;
        }
    }
}
