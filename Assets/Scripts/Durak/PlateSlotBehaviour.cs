using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Durak
{
    public class PlateSlotBehaviour : MonoBehaviour, IDropHandler
    {
        [SerializeField] private CardSlotBehaviour cardSlotBehaviourPrefab;
        [SerializeField] private Transform parent;

        private readonly List<CardSlotBehaviour> _instantiatedCardSlots = new ();
        
        public void OnDrop(PointerEventData eventData)
        {
            var cardSlotBehaviour = Instantiate(cardSlotBehaviourPrefab, parent);
            _instantiatedCardSlots.Add(cardSlotBehaviour);
            cardSlotBehaviour.Initialize(eventData.pointerDrag.GetComponent<CardController>());
        }
    }
}
