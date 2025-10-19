using System;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine<TContext>
{
    private readonly Dictionary<Type, IState<TContext>> _states = new Dictionary<Type, IState<TContext>>();
    private readonly TContext _context;
    private IState<TContext> _currentState;

    public event Action<IState<TContext>, IState<TContext>, TContext> StateChanged;

    public StateMachine(TContext context)
    {
        _context = context;
    }

    public IState<TContext> CurrentState => _currentState;

    public TContext Context => _context;

    public void RegisterState<TState>(TState state) where TState : class, IState<TContext>
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var stateType = typeof(TState);

        if (_states.ContainsKey(stateType))
        {
            throw new InvalidOperationException($"State '{stateType.Name}' is already registered.");
        }

        _states[stateType] = state;
    }

    public bool IsStateRegistered<TState>() where TState : class, IState<TContext>
    {
        return _states.ContainsKey(typeof(TState));
    }

    public void SetState<TState>() where TState : class, IState<TContext>
    {
        var stateType = typeof(TState);

        if (!_states.TryGetValue(stateType, out var newState))
        {
            throw new InvalidOperationException($"State '{stateType.Name}' is not registered.");
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
