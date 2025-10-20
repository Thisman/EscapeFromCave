using UnityEngine;
using VContainer;

public class InputRouter
{
    private readonly InputService _input;

    [Inject]
    public InputRouter(InputService input)
    {
        _input = input;

        if (_input == null)
        {
            Debug.LogError("[InputRouter] Input service dependency was not provided. Input routing is disabled.");
        }
    }

    public void EnterBattle() => SetMode(GameMode.Battle);

    public void EnterGameplay() => SetMode(GameMode.Gameplay);

    public void EnterMenu() => SetMode(GameMode.Menu);

    public void EnterDialog() => SetMode(GameMode.Dialog);

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
