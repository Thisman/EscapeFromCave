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

        if (_inputRouter == null)
        {
            Debug.LogError("[GameFlowService] InputRouter dependency was not provided. Game mode transitions will fail.");
            return;
        }

        EnterGameplay();
    }

    public GameMode CurrentMode => _currentMode;

    public void ChangeMode(GameMode newMode)
    {
        if (_currentMode == newMode)
            return;

        Debug.Log($"[GameFlowService] Changing mode from {_currentMode} to {newMode}.");
        _currentMode = newMode;
    }

    public void EnterBattle()
    {
        ChangeMode(GameMode.Battle);
        if (_inputRouter == null)
        {
            Debug.LogError("[GameFlowService] Cannot enter battle because InputRouter is null.");
            return;
        }

        _inputRouter.EnterBattle();
        Debug.Log("[GameFlowService] Entered battle mode.");
    }

    public void EnterGameplay()
    {
        ChangeMode(GameMode.Gameplay);
        if (_inputRouter == null)
        {
            Debug.LogError("[GameFlowService] Cannot enter gameplay because InputRouter is null.");
            return;
        }

        _inputRouter.EnterGameplay();
        Debug.Log("[GameFlowService] Entered gameplay mode.");
    }
}
