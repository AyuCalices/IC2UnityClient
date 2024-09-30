using System;
using System.Collections.Generic;
using System.Linq;
using Durak.Networking;
using Durak.States;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using UnityEngine;

namespace Durak
{
    [CreateAssetMenu]
    public class PlayerDataRuntimeSet : RuntimeSet<PlayerData>
    {
        public List<PlayerData> disconnectedPlayers = new();
        
        private void OnEnable()
        {
            Restore();
        }

        public void SetToDisconnectedPlayers(PlayerData playerData)
        {
            if (items.Contains(playerData))
            {
                items.Remove(playerData);
                disconnectedPlayers.Add(playerData);
            }
        }

        public PlayerData GetLocalPlayerData()
        {
            return GetPlayerData(NetworkManager.Instance.LocalConnection);
        }
        
        public PlayerData GetDefenderPlayerData()
        {
            return items.First(x => x.RoleType == PlayerRoleType.Defender);
        }
        
        public PlayerData GetFirstAttackerPlayer()
        {
            return items.First(x => x.RoleType == PlayerRoleType.Defender);
        }
        
        public List<Card> GetCards(NetworkConnection networkConnection)
        {
            return GetPlayerData(networkConnection).Cards;
        }
        
        public Card GetCard(NetworkConnection networkConnection, int index)
        {
            return GetPlayerData(networkConnection).Cards[index];
        }

        public int GetCardIndex(NetworkConnection networkConnection, Card card)
        {
            return GetPlayerData(networkConnection).Cards.FindIndex(x => x == card);
        }

        public PlayerData GetPlayerData(NetworkConnection networkConnection)
        {
            var playerData = items.Find(x => x.Connection.Equals(networkConnection));

            if (playerData == null)
            {
                Debug.LogError($"Couldn't find: {typeof(PlayerData)}");
            }

            return playerData;
        }
    }
}
