using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AttackAction : IBattleAction, IDisposable
{
    private readonly IBattleContext _context;
    private readonly IBattleActionTargetResolver _targetResolver;
    private readonly IBattleDamageResolver _damageResolver;
    private bool _disposed;
    private bool _resolved;
    private bool _isActive;
    private bool _isAwaitingAnimation;
    private BattleSquadAnimationController _activeAnimationController;

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
        if (_disposed || _isActive)
            return;

        _isActive = true;
        InputSystem.onAfterUpdate += OnAfterInputUpdate;
    }

    private void OnAfterInputUpdate()
    {
        if (_disposed || _isAwaitingAnimation)
            return;

        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasReleasedThisFrame)
            return;

        if (!TryGetUnitUnderPointer(out var unit))
            return;

        if (!IsEnemyUnit(unit))
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

    private bool TryGetUnitUnderPointer(out BattleSquadController unit)
    {
        unit = null;

        var mouse = Mouse.current;
        if (mouse == null)
            return false;

        var camera = Camera.main;
        if (camera == null)
            return false;

        Vector2 screenPosition = mouse.position.ReadValue();
        Ray ray = camera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out var hitInfo))
        {
            unit = hitInfo.transform.GetComponentInParent<BattleSquadController>();
            if (unit != null)
                return true;
        }

        RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray);
        if (hit2D.transform != null)
        {
            unit = hit2D.transform.GetComponentInParent<BattleSquadController>();
            if (unit != null)
                return true;
        }

        return false;
    }

    private bool IsEnemyUnit(BattleSquadController unit)
    {
        if (unit == null)
            return false;

        var targetModel = unit.GetSquadModel();
        if (targetModel?.UnitDefinition == null)
            return false;

        var activeUnit = _context.ActiveUnit;
        if (activeUnit?.UnitDefinition == null)
            return targetModel.UnitDefinition.Type == UnitType.Enemy;

        return IsOpposingType(activeUnit.UnitDefinition.Type, targetModel.UnitDefinition.Type);
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

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_isActive)
        {
            InputSystem.onAfterUpdate -= OnAfterInputUpdate;
            _isActive = false;
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
