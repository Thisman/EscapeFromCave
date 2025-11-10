using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerBattleActionTargetPicker : IActionTargetPicker
{
    private readonly BattleContext _context;
    private readonly IBattleActionTargetResolver _targetResolver;

    private bool _isActive;
    private bool _disposed;

    public event Action<BattleSquadController> OnSelect;

    public PlayerBattleActionTargetPicker(BattleContext context, IBattleActionTargetResolver targetResolver)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _targetResolver = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));
    }

    public void RequestTarget()
    {
        if (_disposed || _isActive)
            return;

        _isActive = true;
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

        var actorModel = _context.ActiveUnit;
        var targetModel = unit.GetSquadModel();

        if (!_targetResolver.ResolveTarget(actorModel, targetModel))
            return;

        _isActive = false;
        InputSystem.onAfterUpdate -= OnAfterInputUpdate;

        OnSelect?.Invoke(unit);
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
    }
}
