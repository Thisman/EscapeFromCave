using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AttackAction : IBattleAction, IDisposable
{
    private readonly IBattleContext _context;
    private bool _disposed;
    private bool _resolved;

    public event Action OnResolve;
    public event Action OnCancel;

    public AttackAction(IBattleContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        InputSystem.onAfterUpdate += OnAfterInputUpdate;
    }

    private void OnAfterInputUpdate()
    {
        if (_disposed)
            return;

        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasReleasedThisFrame)
            return;

        if (!TryGetUnitUnderPointer(out var unit))
            return;

        if (!IsEnemyUnit(unit))
            return;

        _resolved = true;
        OnResolve?.Invoke();
        Dispose();
    }

    private bool TryGetUnitUnderPointer(out BattleUnitController unit)
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
            unit = hitInfo.transform.GetComponentInParent<BattleUnitController>();
            if (unit != null)
                return true;
        }

        RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray);
        if (hit2D.transform != null)
        {
            unit = hit2D.transform.GetComponentInParent<BattleUnitController>();
            if (unit != null)
                return true;
        }

        return false;
    }

    private bool IsEnemyUnit(BattleUnitController unit)
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

        InputSystem.onAfterUpdate -= OnAfterInputUpdate;
        _disposed = true;

        if (!_resolved)
            OnCancel?.Invoke();
    }

    ~AttackAction()
    {
        Dispose();
    }
}
