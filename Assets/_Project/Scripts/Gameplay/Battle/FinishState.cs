using UnityEngine;

public class FinishState : State<BattleContext>
{
    public override void Enter(BattleContext context)
    {
        context.PanelController?.Show(nameof(FinishState));
    }
}
