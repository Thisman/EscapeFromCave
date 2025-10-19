using UnityEngine;

public class BattleRoundState : State<BattleStateContext>
{
    public override void Enter(BattleStateContext context)
    {
        context.PanelController?.Show(nameof(BattleRoundState));
    }
}
