using UnityEngine;
using UnityEngine.InputSystem;

public enum GameMode { Gameplay, Inventory, Dialog, Paused }

public class InputRouter : MonoBehaviour
{
    [SerializeField] private InputActionAsset _actions;

    void Start() => SetMode(GameMode.Gameplay);

    public void SetMode(GameMode mode)
    {
        _actions.bindingMask = null;

        foreach (var m in _actions.actionMaps) m.Disable();

        switch (mode)
        {
            case GameMode.Gameplay:
                _actions.FindActionMap("PlayerMove").Enable();
                _actions.FindActionMap("PlayerInteraction").Enable();
                break;
            case GameMode.Inventory:
            case GameMode.Dialog:
            case GameMode.Paused:
                _actions.FindActionMap("UI").Enable();
                break;
        }
    }
}
