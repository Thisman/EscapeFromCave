using UnityEngine;
using System.Collections.Generic;

public class TacticState : State<BattleContext>
{
    public override void Enter(BattleContext context)
    {
        context.PanelController?.Show(nameof(TacticState));
        var enemies = new List<IReadOnlyUnitModel>();
        if (context.Payload.Enemy != null)
            enemies.Add(context.Payload.Enemy);

        context.BattleGridController?.Arrange(context.Payload.Hero, context.Payload.Army, enemies);
    }
}
