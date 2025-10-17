using UnityEngine;
using VContainer;

public enum GameMode { Gameplay, Inventory, Dialog, Paused, Battle }

public class GameFlowService
{
    private GameMode _currentMode;
    private InputRouter _inputRouter;

    [Inject]
    public GameFlowService(InputRouter inputRouter)
    {
        _inputRouter = inputRouter;
        _currentMode = GameMode.Gameplay;

        EnterGameplay();
    }

    public GameMode CurrentMode => _currentMode;

    public void ChangeMode(GameMode newMode)
    {
        if (_currentMode == newMode) return;
        _currentMode = newMode;
    }

    public void EnterBattle()
    {
        ChangeMode(GameMode.Battle);
        _inputRouter.EnterBattle();
    }

    public void EnterGameplay()
    {
        ChangeMode(GameMode.Gameplay);
        _inputRouter.EnterGameplay();
    }
}
