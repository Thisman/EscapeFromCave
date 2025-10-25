using System;

public interface IBattleActionController
{
    void RequestAction(IBattleContext ctx, Action<IBattleAction> onActionReady);
}
