using Durak;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardBehaviour : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CardHandManager handManager;

    private void Start()
    {
        handManager = GetComponentInParent<CardHandManager>(); // Get the hand manager from parent
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        handManager.OnCardHover(gameObject); // Notify the hand manager that this card is hovered
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        handManager.OnCardHoverEnd(gameObject); // Notify the hand manager that the hover ended
    }
}