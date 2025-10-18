public class FinishState : State<BattleStateContext>
{
    private readonly PanelController _panelController;

    public FinishState(PanelController panelController)
    {
        _panelController = panelController;
    }

    public override void Enter(BattleStateContext context)
    {
        _panelController?.Show(nameof(FinishState));
    }
}
