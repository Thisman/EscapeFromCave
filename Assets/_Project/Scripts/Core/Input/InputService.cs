using UnityEngine.InputSystem;
using UnityEngine;
using VContainer;

public class InputService
{
    [Inject] private readonly InputActionAsset _actions;

    public InputActionAsset Actions => _actions;

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
        ClearBindingMask();

        switch (mode)
        {
            case GameMode.Gameplay:
                EnableOnly("PlayerMove", "PlayerInteraction");
                break;
            case GameMode.Battle:
                EnableOnly("Battle");
                break;
            case GameMode.Menu:
                EnableOnly("Menu");
                break;
        }
    }
}
