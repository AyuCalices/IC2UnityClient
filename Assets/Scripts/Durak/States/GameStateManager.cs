using System;
using DataTypes.StateMachine;
using Plugins.EventNetworking.Component;
using Plugins.EventNetworking.Core;
using Plugins.EventNetworking.Core.Callbacks;

namespace Durak.States
{
    public class GameStateManager : NetworkObject, IStateManaged, IOnAfterAcquireOwnership, IOnBeforeLoseOwnership
    {
        
        
        public event Action OnStateAuthorityAcquired;
        public event Action OnStateAuthorityLost;

        public IState CurrentState => _stateMachine.GetCurrentState();

        private StateMachine _stateMachine;
        private string _currentState;

        protected override void Awake()
        {
            base.Awake();
            
            _stateMachine = new StateMachine();
            _stateMachine.Initialize(new IdleState());
            _currentState = _stateMachine.GetCurrentState().ToString();
        }

        private void Update()
        {
            _stateMachine.Update();
        }

        public void RequestStateAuthority()
        {
            SecureRequestOwnership();
        }

        public void ReleaseStateAuthority()
        {
            SecureReleaseOwnership();
        }
        
        public void OnAfterAcquireOwnership(NetworkConnection oldConnection, NetworkConnection newConnection)
        {
            OnStateAuthorityAcquired?.Invoke();
        }
        
        public void OnBeforeLoseOwnership(NetworkConnection oldConnection, NetworkConnection newConnection)
        {
            OnStateAuthorityLost?.Invoke();
        }

        public void RequestState(IState requestedState)
        {
            _stateMachine.ChangeState(requestedState);
            _currentState = _stateMachine.GetCurrentState().ToString();
        }
    }

    [Serializable]
    public class IdleState : IState
    {
        public void Enter() { }

        public void Execute() { }

        public void Exit() { }
    }
    
    public enum PlayerRoleType { None, Defender, FirstAttacker, Attacker }
}
