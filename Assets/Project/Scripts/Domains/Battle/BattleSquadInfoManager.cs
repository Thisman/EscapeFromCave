using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BattleSquadInfoManager : IDisposable
{
    private readonly BattleSquadInfoUIController _uiController;

    private bool _isEnabled;
    private BattleSquadController _currentHovered;

    public BattleSquadInfoManager(BattleSquadInfoUIController uiController)
    {
        _uiController = uiController ?? throw new ArgumentNullException(nameof(uiController));
    }

    public void Enable()
    {
        if (_isEnabled)
            return;

        _isEnabled = true;
        InputSystem.onAfterUpdate += HandleAfterInputUpdate;
    }

    public void Disable()
    {
        if (!_isEnabled)
            return;

        _isEnabled = false;
        InputSystem.onAfterUpdate -= HandleAfterInputUpdate;
        _currentHovered = null;
        _uiController.Hide();
    }

    public void Dispose()
    {
        Disable();
    }

    private void HandleAfterInputUpdate()
    {
        if (!_isEnabled)
            return;

        var hovered = FindUnitUnderPointer();
        if (hovered == _currentHovered)
            return;

        _currentHovered = hovered;
        if (_currentHovered == null)
        {
            _uiController.Hide();
            return;
        }

        var model = _currentHovered.GetSquadModel();
        if (model == null)
        {
            _uiController.Hide();
            return;
        }

        _uiController.Render(model);
    }

    private static BattleSquadController FindUnitUnderPointer()
    {
        if (!TryGetPointerScreenPosition(out var screenPosition))
            return null;

        var camera = Camera.main;
        if (camera == null)
            return null;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        int mask = LayerMask.GetMask("Units");
        RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray, 1000f, mask);
        if (hit2D.transform != null)
        {
            var unit2D = hit2D.transform.GetComponentInParent<BattleSquadController>();
            if (unit2D != null)
                return unit2D;
        }

        return null;
    }

    private static bool TryGetPointerScreenPosition(out Vector2 position)
    {
        var mouse = Mouse.current;
        if (mouse == null)
        {
            position = default;
            return false;
        }

        position = mouse.position.ReadValue();
        return true;
    }
}
