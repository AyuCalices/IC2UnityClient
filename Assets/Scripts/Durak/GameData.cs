using System;
using System.Collections.Generic;
using Durak.States;
using Plugins.EventNetworking.Core;
using UnityEngine;

namespace Durak
{
    [CreateAssetMenu]
    public class GameData : ScriptableObject
    {
        [field: SerializeField] public NetworkConnection DefenderNetworkConnection { get; set; }
        [field: SerializeField] public NetworkConnection FirstAttackerNetworkConnection { get; set; }
        [field: SerializeField] public int DefenderLobbyConnectionsIndex { get; set; }
        [field: SerializeField] public PlayerRoleType PlayerRoleType { get; set; }

        [field: SerializeField] public List<Card> DestroyedCards { get; set; }

        private void OnEnable()
        {
            //reset
            DefenderNetworkConnection = new NetworkConnection();
            FirstAttackerNetworkConnection = new NetworkConnection();
            DefenderLobbyConnectionsIndex = 0;
            PlayerRoleType = PlayerRoleType.Defender;
            DestroyedCards.Clear();
        }
    }
}
