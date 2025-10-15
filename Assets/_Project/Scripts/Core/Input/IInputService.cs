using UnityEngine;
using UnityEngine.InputSystem;

public enum GameMode { Gameplay, Inventory, Dialog, Paused, Cutscene }

public interface IInputService
{
    InputActionAsset Actions { get; }
    void EnableOnly(params string[] maps);
    void SetBindingMask(string bindingGroupOrNull);
    void ClearBindingMask();

    void SetMode(GameMode mode);
    GameMode CurrentMode { get; }
}
