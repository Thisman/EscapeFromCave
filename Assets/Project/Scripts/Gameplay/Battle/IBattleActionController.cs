using System;

public interface IBattleActionController
{
    void RequestAction(BattleContext ctx, Action<IBattleAction> onActionReady);
}
