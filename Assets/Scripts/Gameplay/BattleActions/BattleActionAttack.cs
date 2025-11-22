using System;

public sealed class BattleActionAttack : IBattleAction, IDisposable, IBattleActionTargetResolverProvider
{
    private readonly BattleContext _context;
    private readonly IBattleActionTargetResolver _targetResolver;
    private readonly BattleDamageResolverByDefault _damageResolver;
    private bool _disposed;
    private bool _resolved;
    private bool _targetRequested;
    private IBattleActionTargetPicker _targetPicker;

    public event Action OnResolve;
    public event Action OnCancel;

    public IBattleActionTargetResolver TargetResolver => _targetResolver;

    public BattleActionAttack(
        BattleContext context,
        IBattleActionTargetResolver targetResolver,
        BattleDamageResolverByDefault damageResolver,
        IBattleActionTargetPicker targetPicker)
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

        if (!_context.TryGetSquadController(actorModel, out var actorController) || actorController == null)
        {
            CompleteResolve();
            return;
        }

        await _damageResolver.ResolveDamage(actorController, unit);

        CompleteResolve();
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

    ~BattleActionAttack()
    {
        Dispose();
    }
}
