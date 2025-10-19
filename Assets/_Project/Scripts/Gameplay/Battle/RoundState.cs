using UnityEngine;

public class RoundState : State<BattleContext>
{
    public override void Enter(BattleContext context)
    {
        context.PanelController?.Show(nameof(RoundState));
    }
}
