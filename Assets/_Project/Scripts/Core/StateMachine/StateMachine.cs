using System;

namespace EscapeFromCave.Core.StateMachine
{
    public class StateMachine<TContext>
    {
        private readonly TContext _context;
        private IState<TContext> _currentState;

        public event Action<IState<TContext>, IState<TContext>, TContext> StateChanged;

        public StateMachine(TContext context)
        {
            _context = context;
        }

        public IState<TContext> CurrentState => _currentState;

        public TContext Context => _context;

        public void SetState(IState<TContext> newState)
        {
            if (newState == null)
            {
                throw new ArgumentNullException(nameof(newState));
            }

            if (ReferenceEquals(_currentState, newState))
            {
                return;
            }

            var previousState = _currentState;

            previousState?.Exit(_context);
            _currentState = newState;
            _currentState.Enter(_context);

            StateChanged?.Invoke(previousState, _currentState, _context);
        }

        public void Update()
        {
            _currentState?.Update(_context);
        }
    }
}
