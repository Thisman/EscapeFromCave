using System;

public sealed class AttackAction : IBattleAction, IDisposable
{
    private readonly BattleContext _context;
    private readonly IBattleActionTargetResolver _targetResolver;
    private readonly IBattleDamageResolver _damageResolver;
    private bool _disposed;
    private bool _resolved;
    private bool _targetRequested;
    private IActionTargetPicker _targetPicker;

    public event Action OnResolve;
    public event Action OnCancel;

    public AttackAction(
        BattleContext context,
        IBattleActionTargetResolver targetResolver,
        IBattleDamageResolver damageResolver,
        IActionTargetPicker targetPicker)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _targetResolver = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));
        _damageResolver = damageResolver ?? throw new ArgumentNullException(nameof(damageResolver));
        _targetPicker = targetPicker ?? throw new ArgumentNullException(nameof(targetPicker));
    }

    public void Resolve()
    {
        if (_disposed || _resolved || _targetRequested || _targetPicker == null)
            return;

        _targetRequested = true;
        _targetPicker.OnSelect += OnTargetSelected;
        _targetPicker.RequestTarget();
    }

    private async void OnTargetSelected(BattleSquadController unit)
    {
        if (_disposed || _resolved)
            return;

        if (_targetPicker != null)
        {
            _targetPicker.OnSelect -= OnTargetSelected;
            _targetPicker.Dispose();
            _targetPicker = null;
        }

        _targetRequested = false;

        if (unit == null)
        {
            CompleteResolve();
            return;
        }

        var actorModel = _context.ActiveUnit;
        var targetModel = unit.GetSquadModel();
        if (actorModel == null || targetModel == null)
        {
            CompleteResolve();
            return;
        }

        if (!_targetResolver.ResolveTarget(actorModel, targetModel))
        {
            CompleteResolve();
            return;
        }

        var actorController = FindController(actorModel);
        if (actorController == null)
        {
            CompleteResolve();
            return;
        }

        await _damageResolver.ResolveDamage(actorController, unit);

        CompleteResolve();
    }

    private BattleSquadController FindController(IReadOnlySquadModel model)
    {
        if (model == null)
            return null;

        var units = _context.BattleUnits;
        if (units == null)
            return null;

        foreach (var squad in units)
        {
            if (squad?.GetSquadModel() == model)
                return squad;
        }

        return null;
    }

    private void CompleteResolve()
    {
        if (_resolved)
            return;

        if (_disposed)
        {
            return;
        }

        _resolved = true;
        OnResolve?.Invoke();
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_targetPicker != null)
        {
            _targetPicker.OnSelect -= OnTargetSelected;
            _targetPicker.Dispose();
            _targetPicker = null;
        }
        _disposed = true;
        _targetRequested = false;

        if (!_resolved)
            OnCancel?.Invoke();
    }

    ~AttackAction()
    {
        Dispose();
    }
}
