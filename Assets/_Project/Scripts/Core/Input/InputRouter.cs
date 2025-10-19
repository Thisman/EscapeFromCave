using UnityEngine;
using VContainer;

public class InputRouter
{
    private readonly IInputService _input;

    [Inject]
    public InputRouter(IInputService input)
    {
        _input = input;

        if (_input == null)
        {
            Debug.LogError("[InputRouter] Input service dependency was not provided. Input routing is disabled.");
        }
    }

    public void EnterBattle() => SetMode(GameMode.Battle);

    public void EnterGameplay() => SetMode(GameMode.Gameplay);

    public void EnterInventory() => SetMode(GameMode.Inventory);

    public void EnterDialog() => SetMode(GameMode.Dialog);

    public void Pause() => SetMode(GameMode.Paused);

    private void SetMode(GameMode mode)
    {
        if (_input == null)
        {
            Debug.LogError($"[InputRouter] Cannot set input mode to {mode} because input service is null.");
            return;
        }

        _input.SetMode(mode);
        Debug.Log($"[InputRouter] Input mode set to {mode}.");
    }
}
