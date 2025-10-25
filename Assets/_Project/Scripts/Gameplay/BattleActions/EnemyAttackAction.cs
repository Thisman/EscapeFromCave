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

        var targetController = SelectTarget(actorModel);
        if (targetController == null)
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

        OnCancel?.Invoke();
    }

    private BattleSquadController SelectTarget(IReadOnlySquadModel actor)
    {
        var units = _context.BattleUnits;
        if (units == null)
            return null;

        var grid = _context.BattleGridController;
        if (grid == null)
            return null;

        var actorDefinition = actor?.UnitDefinition;
        var actorType = actorDefinition?.Type ?? UnitType.Enemy;

        BattleSquadController backlineCandidate = null;

        foreach (var unit in units)
        {
            if (unit == null)
                continue;

            var model = unit.GetSquadModel();
            if (model == null || model.IsEmpty)
                continue;

            var definition = model.UnitDefinition;
            if (definition == null)
                continue;

            if (!IsOpposingType(actorType, definition.Type))
                continue;

            if (!grid.TryGetSlotForOccupant(unit.transform, out var slot))
                continue;

            if (!grid.TryGetSlotRow(slot, out var row))
                continue;

            if (row == BattleGridRow.Front)
                return unit;

            backlineCandidate ??= unit;
        }

        return backlineCandidate;
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

    private static bool IsOpposingType(UnitType source, UnitType target)
    {
        return source switch
        {
            UnitType.Hero or UnitType.Ally => target == UnitType.Enemy,
            UnitType.Enemy => target is UnitType.Hero or UnitType.Ally,
            _ => false,
        };
    }
}
