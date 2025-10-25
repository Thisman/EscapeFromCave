using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AutoSkipTurnAction : IBattleAction, IDisposable
{
    private readonly float _triggerTime;
    private bool _disposed;
    private bool _resolved;
    private bool _isActive;

    public AutoSkipTurnAction(float delaySeconds)
    {
        if (delaySeconds < 0f)
            throw new ArgumentOutOfRangeException(nameof(delaySeconds));

        _triggerTime = Time.realtimeSinceStartup + delaySeconds;
    }

    public event Action OnResolve;
    public event Action OnCancel;

    public void Resolve()
    {
        if (_disposed || _resolved || _isActive)
            return;

        _isActive = true;
        InputSystem.onAfterUpdate += OnAfterUpdate;
    }

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

        if (_isActive)
        {
            InputSystem.onAfterUpdate -= OnAfterUpdate;
            _isActive = false;
        }
        _disposed = true;

        if (!_resolved)
            OnCancel?.Invoke();
    }

    ~AutoSkipTurnAction()
    {
        Dispose();
    }
}
