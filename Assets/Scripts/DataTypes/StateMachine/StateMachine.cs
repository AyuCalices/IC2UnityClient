namespace DataTypes.StateMachine
{
    public class StateMachine
    {
        private IState _currentState;
        private IState _previousState;

        public void Initialize(IState startingState)
        {
            _currentState = startingState;
            startingState.Enter();
        }

        public void ChangeState(IState newState)
        {
            _currentState?.Exit();
            _previousState = _currentState;
            _currentState = newState;
            _currentState.Enter();
        }

        public IState GetCurrentState()
        {
            return _currentState;
        }

        public void Update()
        {
            _currentState?.Execute();
        }

        public void SwitchToPreviousState()
        {
            _currentState.Exit();
            _currentState = _previousState;
            _currentState.Enter();
        }
    }
}