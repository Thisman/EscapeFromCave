namespace EscapeFromCave.Core.StateMachine
{
    public abstract class State<TContext> : IState<TContext>
    {
        public virtual void Enter(TContext context)
        {
        }

        public virtual void Update(TContext context)
        {
        }

        public virtual void Exit(TContext context)
        {
        }
    }
}
