using System;

public sealed class BattleActionDefend : IBattleAction, IDisposable
{
    private bool _disposed;
    private bool _resolved;

    public event Action OnResolve;
    public event Action OnCancel;

    public void Resolve()
    {
        if (_disposed || _resolved)
            return;

        _resolved = true;
        OnResolve?.Invoke();
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (!_resolved)
            OnCancel?.Invoke();
    }
}
