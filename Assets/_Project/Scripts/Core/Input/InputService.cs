using UnityEngine.InputSystem;
using UnityEngine;

public class InputService : IInputService
{
    private readonly InputActionAsset _actions;

    public InputActionAsset Actions => _actions;
    public GameMode CurrentMode { get; private set; } = GameMode.Gameplay;

    public InputService(InputActionAsset actions)
    {
        _actions = actions;

        SetMode(GameMode.Gameplay);
    }

    public void EnableOnly(params string[] maps)
    {
        foreach (var m in _actions.actionMaps) m.Disable();
        foreach (var name in maps)
            _actions.FindActionMap(name, throwIfNotFound: true).Enable();
    }

    public void SetBindingMask(string bindingGroupOrNull)
    {
        _actions.bindingMask = string.IsNullOrEmpty(bindingGroupOrNull)
            ? (InputBinding?)null
            : InputBinding.MaskByGroup(bindingGroupOrNull);
    }

    public void ClearBindingMask() => _actions.bindingMask = null;

    public void SetMode(GameMode mode)
    {
        CurrentMode = mode;
        ClearBindingMask();

        switch (mode)
        {
            case GameMode.Gameplay:
                EnableOnly("Player", "Interact");
                break;
            case GameMode.Inventory:
            case GameMode.Dialog:
            case GameMode.Paused:
                EnableOnly("UI");
                break;
            case GameMode.Cutscene:
                // Ничего или минимум (Skip)
                EnableOnly("UI"); // если нужен Skip через Submit/Cancel
                break;
        }
    }
}
