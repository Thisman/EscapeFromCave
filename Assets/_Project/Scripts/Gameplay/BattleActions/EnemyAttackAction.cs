using System;
using UnityEngine;

public sealed class EnemyAttackAction : IBattleAction
{
    private readonly IBattleContext _context;
    private readonly IBattleDamageResolver _damageResolver;

    private bool _isResolving;
    private bool _resolved;
    private bool _isAwaitingAnimation;
    private BattleSquadAnimationController _activeAnimationController;
    private IActionTargetPicker _targetPicker;

    public EnemyAttackAction(IBattleContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _damageResolver = new DefaultBattleDamageResolver();
    }

    public event Action OnResolve;
    public event Action OnCancel;

    public void Resolve()
    {
        if (_resolved || _isResolving)
            return;

        _isResolving = true;

        var actorModel = _context.ActiveUnit;
        if (actorModel == null)
        {
            CancelResolution();
            return;
        }

        var actorController = FindController(actorModel);
        if (actorController == null)
        {
            CancelResolution();
            return;
        }

        _targetPicker = new AIActionTargetPicker(_context);
        _targetPicker.OnSelect += OnTargetSelected;
        _targetPicker.RequestTarget();
        if (!_isResolving)
            return;
    }

    private void OnTargetSelected(BattleSquadController targetController)
    {
        if (_targetPicker != null)
        {
            _targetPicker.OnSelect -= OnTargetSelected;
            _targetPicker.Dispose();
            _targetPicker = null;
        }

        if (targetController == null)
        {
            CancelResolution();
            return;
        }

        var actorModel = _context.ActiveUnit;
        if (actorModel == null)
        {
            CancelResolution();
            return;
        }

        var actorController = FindController(actorModel);
        if (actorController == null)
        {
            CancelResolution();
            return;
        }

        _damageResolver.ResolveDamage(actorController, targetController);

        TryResolveWithAnimation(targetController);
    }

    private void TryResolveWithAnimation(BattleSquadController target)
    {
        _isAwaitingAnimation = true;

        var animationController = target != null
            ? target.GetComponentInChildren<BattleSquadAnimationController>()
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

        _resolved = true;
        _isResolving = false;
        _isAwaitingAnimation = false;
        _activeAnimationController = null;
        OnResolve?.Invoke();
    }

    private void CancelResolution()
    {
        if (_resolved)
            return;

        _isResolving = false;
        if (_isAwaitingAnimation)
        {
            _isAwaitingAnimation = false;
            _activeAnimationController?.CancelDamageFlash();
            _activeAnimationController = null;
        }

        if (_targetPicker != null)
        {
            _targetPicker.OnSelect -= OnTargetSelected;
            _targetPicker.Dispose();
            _targetPicker = null;
        }

        OnCancel?.Invoke();
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
}
