namespace DataTypes.StateMachine
{
    public interface IState
    {
        void Enter();

        void Execute();

        void Exit();
    }
}