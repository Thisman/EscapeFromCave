using System;
using VContainer;

public class GameFlowService
{
    private GameMode _currentMode;
    private readonly InputRouter _inputRouter;

    public event Action<GameMode> ModeChanging;
    public event Action<GameMode> ModeChanged;

    [Inject]
    public GameFlowService(InputRouter inputRouter)
    {
        _inputRouter = inputRouter;
        ChangeMode(GameMode.Gameplay, force: true);
    }

    public GameMode CurrentMode => _currentMode;

    public void ChangeMode(GameMode newMode)
    {
        ChangeMode(newMode, force: false);
    }

    public void EnterBattle()
    {
        ChangeMode(GameMode.Battle);
    }

    public void EnterGameplay()
    {
        ChangeMode(GameMode.Gameplay);
    }

    public void EnterInventory()
    {
        ChangeMode(GameMode.Inventory);
    }

    public void EnterDialog() => ChangeMode(GameMode.Dialog);

    public void Pause() => ChangeMode(GameMode.Paused);

    private void ChangeMode(GameMode newMode, bool force)
    {
        if (!force && _currentMode == newMode)
            return;

        ModeChanging?.Invoke(newMode);
        _currentMode = newMode;
        RouteInput(newMode);
        ModeChanged?.Invoke(newMode);
    }

    private void RouteInput(GameMode mode)
    {
        if (_inputRouter == null)
            return;

        switch (mode)
        {
            case GameMode.Battle:
                _inputRouter.EnterBattle();
                break;
            case GameMode.Inventory:
                _inputRouter.EnterInventory();
                break;
            case GameMode.Dialog:
                _inputRouter.EnterDialog();
                break;
            case GameMode.Paused:
                _inputRouter.Pause();
                break;
            default:
                _inputRouter.EnterGameplay();
                break;
        }
    }
}
