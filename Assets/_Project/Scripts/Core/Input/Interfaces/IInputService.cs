using UnityEngine;
using UnityEngine.InputSystem;

public interface IInputService
{
    InputActionAsset Actions { get; }

    void EnableOnly(params string[] maps);

    void SetBindingMask(string bindingGroupOrNull);

    void ClearBindingMask();

    void SetMode(GameMode mode);
}
