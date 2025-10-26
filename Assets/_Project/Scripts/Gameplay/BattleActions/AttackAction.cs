using System;
using UnityEngine;

public sealed class AttackAction : IBattleAction, IDisposable
{
    private readonly IBattleContext _context;
    private readonly IBattleActionTargetResolver _targetResolver;
    private readonly IBattleDamageResolver _damageResolver;
    private bool _disposed;
    private bool _resolved;
    private bool _isAwaitingAnimation;
    private BattleSquadAnimationController _activeAnimationController;
    private IActionTargetPicker _targetPicker;

    public event Action OnResolve;
    public event Action OnCancel;

    public AttackAction(IBattleContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _targetResolver = new DefaultActionTargetResolver(_context);
        _damageResolver = new DefaultBattleDamageResolver();
    }

    public void Resolve()
    {
        if (_disposed || _resolved)
            return;

        _targetPicker = new PlayerActionTargetPicker(_context, _targetResolver);
        _targetPicker.OnSelect += OnTargetSelected;
        _targetPicker.RequestTarget();
    }

    private void OnTargetSelected(BattleSquadController unit)
    {
        if (_disposed || _resolved || _isAwaitingAnimation)
            return;

        if (_targetPicker != null)
        {
            _targetPicker.OnSelect -= OnTargetSelected;
            _targetPicker.Dispose();
            _targetPicker = null;
        }

        if (unit == null)
            return;

        var actorModel = _context.ActiveUnit;
        var targetModel = unit.GetSquadModel();
        if (actorModel == null || targetModel == null)
            return;

        if (!_targetResolver.ResolveTarget(actorModel, targetModel))
            return;

        var actorController = FindController(actorModel);
        if (actorController != null)
            _damageResolver.ResolveDamage(actorController, unit);

        TryResolveWithAnimation(unit);
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

    private void TryResolveWithAnimation(BattleSquadController unit)
    {
        _isAwaitingAnimation = true;

        var animationController = unit != null
            ? unit.GetComponentInChildren<BattleSquadAnimationController>()
            : null;

        if (animationController == null)
        {
            CompleteResolve();
            return;
        }

        _activeAnimationController = animationController;
        animationController.PlayDamageFlash(CompleteResolve);
    }

    private void CompleteResolve()
    {
        if (_resolved)
            return;

        if (_disposed)
        {
            _isAwaitingAnimation = false;
            return;
        }

        _resolved = true;
        _isAwaitingAnimation = false;
        _activeAnimationController = null;
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
        _isAwaitingAnimation = false;
        _activeAnimationController?.CancelDamageFlash();
        _activeAnimationController = null;

        if (!_resolved)
            OnCancel?.Invoke();
    }

    ~AttackAction()
    {
        Dispose();
    }
}
