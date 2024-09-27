using System;
using DataTypes.StateMachine;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Durak
{
    public class CardInteraction : MonoBehaviour, IStateManaged, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float smoothSpeed = 15f;
        [SerializeField] private float grabOriginDistance = 9.35f;
        [SerializeField] private float scale = 0.01f;

        public Type GetStateType() => _stateMachine.GetCurrentState().GetType();
        
        private CardHandManager _handManager;
        private StateMachine _stateMachine;
        private string _currentState;
        
        private bool _isDrag;
        private bool _isDropped;

        private void Awake()
        {
            _handManager = GetComponentInParent<CardHandManager>();
            _stateMachine = new StateMachine();
        }

        public void InitializeAsHandCard()
        {
            _stateMachine.Initialize(new HandState(transform, _handManager));
            _currentState = _stateMachine.GetCurrentState().ToString();
        }
        
        public void InitializeAsDroppedCard()
        {
            _stateMachine.Initialize(new DroppedState());
            _currentState = _stateMachine.GetCurrentState().ToString();
        }
        
        private void Update()
        {
            _stateMachine.Update();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_stateMachine.GetCurrentState() is HandState)
            {
                _handManager.OnCardHover(gameObject);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_stateMachine.GetCurrentState() is HandState)
            {
                _handManager.OnCardHoverEnd();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_stateMachine.GetCurrentState() is HandState)
            {
                RequestState(new DragState(transform, _handManager, scale, grabOriginDistance));
            }
        }

        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_stateMachine.GetCurrentState() is DragState)
            {
                RequestState(new HandState(transform, _handManager));
            }
        }

        public void ConfirmDrop()
        {
            if (_stateMachine.GetCurrentState() is DragState)
            {
                RequestState(new DroppedState());
            }
        }

        public void RequestState(IState requestedState)
        {
            _stateMachine.ChangeState(requestedState);
            _currentState = _stateMachine.GetCurrentState().ToString();
            Debug.Log(_currentState);
        }
    }
    
    public class HandState : IState
    {
        private readonly Transform _transform;
        private readonly CardHandManager _cardHandManager;

        public HandState(Transform transform, CardHandManager cardHandManager)
        {
            _transform = transform;
            _cardHandManager = cardHandManager;
        }
        
        public void Enter()
        {
            _transform.localScale = Vector3.one;
            _cardHandManager.AddCard(_transform.gameObject);
        }

        public void Execute()
        {
        }

        public void Exit()
        {
        }
    }

    public class DragState : IState
    {
        private readonly Transform _transform;
        private readonly CardHandManager _cardHandManager;
        private readonly float _scale;
        private readonly float _grabOriginDistance;

        public DragState(Transform transform, CardHandManager cardHandManager, float scale, float grabOriginDistance)
        {
            _transform = transform;
            _cardHandManager = cardHandManager;
            _scale = scale;
            _grabOriginDistance = grabOriginDistance;
        }
        
        public void Enter()
        {
            DOTween.Kill(_transform);
            
            _cardHandManager.RemoveCard(_transform.gameObject);
            _transform.localScale = Vector3.one * _scale;
            _transform.position = GetMouseInWorldPosition();
            
            _cardHandManager.CanHover = false;
        }

        public void Execute()
        {
            var mouseWorldPosition = GetMouseInWorldPosition();
            if (RayIntersectsPlane(Camera.main.transform.position, (mouseWorldPosition - Camera.main.transform.position), Vector3.up * 0.01f, Vector3.up, out Vector3 intersectionPoint))
            {
                _transform.DOMove(intersectionPoint, 0.2f);
                _transform.rotation = Quaternion.LookRotation(Vector3.up);
            }
            else
            {
                _transform.rotation = Quaternion.LookRotation(-Camera.main.transform.forward);
                _transform.position = mouseWorldPosition;
            }
        }

        public void Exit()
        {
            _cardHandManager.CanHover = true;
            DOTween.Kill(_transform);
        }
        
        private Vector3 GetMouseInWorldPosition()
        {
            Vector3 mousePosition = Mouse.current.position.ReadValue();
            mousePosition.z = _grabOriginDistance; // 1 unit away from the camera
            return Camera.main.ScreenToWorldPoint(mousePosition);
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
    
    public class DroppedState : IState
    {
        public void Enter() { }

        public void Execute() { }

        public void Exit() { }
    }
}