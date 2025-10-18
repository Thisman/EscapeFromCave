namespace EscapeFromCave.Core.StateMachine
{
    public interface IState<TContext>
    {
        void Enter(TContext context);

        void Update(TContext context);

        void Exit(TContext context);
    }
}
