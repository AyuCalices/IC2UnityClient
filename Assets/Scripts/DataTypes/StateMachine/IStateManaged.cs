
namespace DataTypes.StateMachine
{
    public interface IStateManaged
    {
        void RequestState(IState requestedState);
    }
}
