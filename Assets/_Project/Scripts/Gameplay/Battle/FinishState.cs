using UnityEngine;

public class FinishState : State<BattleStateContext>
{
    public override void Enter(BattleStateContext context)
    {
        context.PanelController?.Show(nameof(FinishState));
    }
}
