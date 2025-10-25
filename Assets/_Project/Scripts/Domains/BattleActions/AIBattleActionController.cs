using System;

public class AIBattleActionController : IBattleActionController
{
    public void RequestAction(IBattleContext ctx, Action<IBattleAction> onActionReady)
    {
        onActionReady.Invoke(new AutoSkipTurnAction(1.5f));
    }
}
