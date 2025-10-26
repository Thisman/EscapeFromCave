using System;

public class AIBattleActionController : IBattleActionController
{
    public void RequestAction(BattleContext ctx, Action<IBattleAction> onActionReady)
    {
        var targetResolver = new DefaultActionTargetResolver(ctx);
        var damageResolver = new DefaultBattleDamageResolver();
        var targetPicker = new AIActionTargetPicker(ctx);
        onActionReady?.Invoke(new AttackAction(ctx, targetResolver, damageResolver, targetPicker));
    }
}
