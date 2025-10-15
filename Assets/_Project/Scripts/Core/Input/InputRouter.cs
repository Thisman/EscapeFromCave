using VContainer;

public class InputRouter
{
    private readonly IInputService _input;

    [Inject]
    public InputRouter(IInputService input) => _input = input;

    public void EnterGameplay() => _input.SetMode(GameMode.Gameplay);
    public void EnterInventory() => _input.SetMode(GameMode.Inventory);
    public void EnterDialog() => _input.SetMode(GameMode.Dialog);
    public void Pause() => _input.SetMode(GameMode.Paused);
    public void Cutscene() => _input.SetMode(GameMode.Cutscene);
}
