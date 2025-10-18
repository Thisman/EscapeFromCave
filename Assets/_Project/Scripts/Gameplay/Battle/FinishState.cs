public class FinishState : BattleState
{
    public FinishState(PanelController panelController) : base(panelController)
    {
    }

    public override void Enter(BattleStateContext context)
    {
        base.Enter(context);
        ShowLayer(nameof(FinishState));
    }
}
