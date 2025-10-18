public class TacticState : State<BattleStateContext>
{
    private readonly PanelController _panelController;

    public TacticState(PanelController panelController)
    {
        _panelController = panelController;
    }

    public override void Enter(BattleStateContext context)
    {
        _panelController?.Show(nameof(TacticState));
    }
}
