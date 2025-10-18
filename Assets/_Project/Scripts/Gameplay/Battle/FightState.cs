public class FightState : State<BattleStateContext>
{
    private readonly PanelController _panelController;

    public FightState(PanelController panelController)
    {
        _panelController = panelController;
    }

    public override void Enter(BattleStateContext context)
    {
        _panelController?.Show(nameof(FightState));
    }
}
