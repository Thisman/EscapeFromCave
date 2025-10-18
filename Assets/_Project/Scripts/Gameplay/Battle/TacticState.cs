using UnityEngine;

public class TacticState : BattleState
{
    public TacticState(PanelController panelController) : base(panelController)
    {
    }

    public override void Enter(BattleStateContext context)
    {
        base.Enter(context);
        ShowLayer(nameof(TacticState));
        Debug.Log("TacticState Entered");
    }
}
