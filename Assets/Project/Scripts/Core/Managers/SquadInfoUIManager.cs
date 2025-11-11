using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class SquadInfoUIManager : IDisposable
{
    private readonly SquadInfoUIController _uiController;

    private bool _isEnabled;
    private ISquadModelProvider _currentHovered;

    public SquadInfoUIManager(SquadInfoUIController uiController)
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

        if (_currentHovered != null)
        {
            var currentModel = _currentHovered.GetSquadModel();
            if (currentModel == null)
            {
                _currentHovered = null;
                _uiController.Hide();
            }
        }

        var hovered = FindProviderUnderPointer();
        if (ReferenceEquals(hovered, _currentHovered))
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

    private static ISquadModelProvider FindProviderUnderPointer()
    {
        if (!TryGetPointerScreenPosition(out var screenPosition))
            return null;

        var uiProvider = FindProviderOnUI(screenPosition);
        if (uiProvider != null)
            return uiProvider;

        return FindProviderInWorld(screenPosition);
    }

    private static ISquadModelProvider FindProviderOnUI(Vector2 screenPosition)
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
            return null;

        var pointerData = new PointerEventData(eventSystem)
        {
            position = screenPosition,
        };

        var raycastResults = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, raycastResults);
        foreach (var result in raycastResults)
        {
            if (result.gameObject == null)
                continue;

            var provider = result.gameObject.GetComponentInParent<ISquadModelProvider>();
            if (provider != null)
                return provider;
        }

        return null;
    }

    private static ISquadModelProvider FindProviderInWorld(Vector2 screenPosition)
    {
        var camera = Camera.main;
        if (camera == null)
            return null;

        var ray = camera.ScreenPointToRay(screenPosition);
        var provider = FindProviderInWorld2D(ray);
        if (provider != null)
            return provider;

        return FindProviderInWorld3D(ray);
    }

    private static ISquadModelProvider FindProviderInWorld2D(Ray ray)
    {
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray, 1000f);
        for (int i = 0; i < hits.Length; i++)
        {
            var transform = hits[i].transform;
            if (transform == null)
                continue;

            var provider = transform.GetComponentInParent<ISquadModelProvider>();
            if (provider != null)
                return provider;
        }

        return null;
    }

    private static ISquadModelProvider FindProviderInWorld3D(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
        for (int i = 0; i < hits.Length; i++)
        {
            var transform = hits[i].transform;
            if (transform == null)
                continue;

            var provider = transform.GetComponentInParent<ISquadModelProvider>();
            if (provider != null)
                return provider;
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
