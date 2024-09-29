using System;
using System.Collections.Generic;
using Durak.States;
using Plugins.EventNetworking.Core;
using UnityEngine;

namespace Durak.Networking
{
    //UI: Card Deck Count, Trump Card
    //Lobby: Player Names, IsReady
    
    [Serializable]
    public class PlayerData
    {
        [field: SerializeField] public NetworkConnection Connection { get; private set; }
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public PlayerRoleType RoleType { get; set; }
        [field: SerializeField] public List<Card> Cards { get; private set; }
        
        public PlayerData(NetworkConnection connection)
        {
            Connection = connection;
            RoleType = PlayerRoleType.None;
            Cards = new List<Card>();
        }
    }
}
