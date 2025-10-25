using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AutoSkipTurnAction : IBattleAction, IDisposable
{
    private readonly float _triggerTime;
    private bool _disposed;
    private bool _resolved;

    public AutoSkipTurnAction(float delaySeconds)
    {
        if (delaySeconds < 0f)
            throw new ArgumentOutOfRangeException(nameof(delaySeconds));

        _triggerTime = Time.realtimeSinceStartup + delaySeconds;
        InputSystem.onAfterUpdate += OnAfterUpdate;
    }

    public event Action OnResolve;
    public event Action OnCancel;

    private void OnAfterUpdate()
    {
        if (_disposed || _resolved)
            return;

        if (Time.realtimeSinceStartup < _triggerTime)
            return;

        _resolved = true;
        OnResolve?.Invoke();
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        InputSystem.onAfterUpdate -= OnAfterUpdate;
        _disposed = true;

        if (!_resolved)
            OnCancel?.Invoke();
    }

    ~AutoSkipTurnAction()
    {
        Dispose();
    }
}
