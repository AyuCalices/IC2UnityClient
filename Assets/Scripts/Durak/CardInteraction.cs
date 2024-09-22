using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Durak
{
    public class CardInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float smoothSpeed = 15f;
        [SerializeField] private float grabOriginDistance = 9.35f;
        [SerializeField] private float scale = 0.01f;
    
        private CardHandManager _handManager;
        
        private bool _isDrag;
        private bool _isDropped;

        private void Start()
        {
            _handManager = GetComponentInParent<CardHandManager>(); // Get the hand manager from parent
        }

        private Vector3 GetMouseInWorldPosition()
        {
            Vector3 mousePosition = Mouse.current.position.ReadValue();
            mousePosition.z = grabOriginDistance; // 1 unit away from the camera
            return Camera.main.ScreenToWorldPoint(mousePosition);
        }
        
        private void Update()
        {
            if (!_isDrag) return;
            
            var mouseWorldPosition = GetMouseInWorldPosition();
            if (RayIntersectsPlane(Camera.main.transform.position, (mouseWorldPosition - Camera.main.transform.position), Vector3.up * 0.01f, Vector3.up, out Vector3 intersectionPoint))
            {
                transform.DOMove(intersectionPoint, 0.2f);
                transform.rotation = Quaternion.LookRotation(Vector3.up);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(-Camera.main.transform.forward);
                transform.position = mouseWorldPosition;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDropped) return;
            
            _handManager.OnCardHover(gameObject); // Notify the hand manager that this card is hovered
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isDropped) return;
            
            _handManager.OnCardHoverEnd(gameObject); // Notify the hand manager that the hover ended
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDrag = true;
            _isDropped = false;
            DOTween.Kill(transform);
            
            transform.SetParent(null);
            
            transform.localScale = Vector3.one * scale;
            transform.position = GetMouseInWorldPosition();
            
            _handManager.RemoveCard(gameObject);
            _handManager.CanHover = false;
        }

        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDrag = false;
            _handManager.CanHover = true;
            DOTween.Kill(transform);
            
            if (_isDropped) return;
            
            transform.localScale = Vector3.one;
            _handManager.AddCard(gameObject);
        }

        public void ConfirmDrop()
        {
            _isDropped = true;
        }
        
        private bool RayIntersectsPlane(Vector3 rayOrigin, Vector3 rayDirection, Vector3 planePoint, Vector3 planeNormal, out Vector3 intersectionPoint)
        {
            intersectionPoint = Vector3.zero;

            // Calculate the dot product of the plane's normal and the ray's direction
            float denominator = Vector3.Dot(planeNormal, rayDirection);

            // If the denominator is zero, the ray is parallel to the plane (no intersection)
            if (Mathf.Abs(denominator) < Mathf.Epsilon)
            {
                return false; // No intersection, ray is parallel to the plane
            }

            // Calculate the parameter t of the intersection point
            float t = Vector3.Dot(planeNormal, planePoint - rayOrigin) / denominator;

            // If t is negative, the intersection point is behind the ray's origin (no collision)
            if (t < 0)
            {
                return false; // No intersection in the forward direction
            }

            // Calculate the intersection point
            intersectionPoint = rayOrigin + t * rayDirection;
            return true; // There is an intersection
        }
    }
}