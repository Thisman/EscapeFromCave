using System;

public class AIBattleActionController : IBattleActionController
{
    public void RequestAction(BattleContext ctx, Action<IBattleAction> onActionReady)
    {
        var targetResolver = new BattleActionDefaultTargetResolver(ctx);
        var damageResolver = new BattleDamageDefaultResolver();
        var targetPicker = new AIActionTargetPicker(ctx);
        onActionReady?.Invoke(new BattleActionAttack(ctx, targetResolver, damageResolver, targetPicker));
    }
}
