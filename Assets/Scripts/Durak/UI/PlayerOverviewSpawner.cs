using System.Collections.Generic;
using Durak.Networking;
using UnityEngine;

namespace Durak.UI
{
    public class PlayerOverviewSpawner : MonoBehaviour
    {
        [SerializeField] private PlayerDataRuntimeSet playerDataRuntimeSet;
        [SerializeField] private PlayerOverviewElement playerOverviewElementPrefab;

        private readonly List<PlayerOverviewElement> _instantiatedPlayerOverviewElements = new();

        private void Awake()
        {
            playerDataRuntimeSet.OnItemAdded += InstantiatePlayerOverviewElement;
            playerDataRuntimeSet.OnItemRemoved += DestroyPlayerOverviewElement;
            playerDataRuntimeSet.OnItemsCleared += DestroyAll;
        }

        private void Update()
        {
            foreach (var instantiatedPlayerOverviewElement in _instantiatedPlayerOverviewElements)
            {
                instantiatedPlayerOverviewElement.UpdateUI();
            }
        }

        private void OnDestroy()
        {
            playerDataRuntimeSet.OnItemAdded -= InstantiatePlayerOverviewElement;
            playerDataRuntimeSet.OnItemRemoved -= DestroyPlayerOverviewElement;
            playerDataRuntimeSet.OnItemsCleared -= DestroyAll;
        }

        private void InstantiatePlayerOverviewElement(PlayerData playerData)
        {
            var element = Instantiate(playerOverviewElementPrefab, transform);
            element.PlayerData = playerData;
            _instantiatedPlayerOverviewElements.Add(element);
        }

        private void DestroyAll()
        {
            for (var i = _instantiatedPlayerOverviewElements.Count - 1; i >= 0; i--)
            {
                Destroy(_instantiatedPlayerOverviewElements[i].gameObject);
            }
            
            _instantiatedPlayerOverviewElements.Clear();
        }

        private void DestroyPlayerOverviewElement(PlayerData playerData)
        {
            var objectToDestroy = _instantiatedPlayerOverviewElements.Find(x => x.PlayerData == playerData);
            Destroy(objectToDestroy.gameObject);
        }
    }
}
