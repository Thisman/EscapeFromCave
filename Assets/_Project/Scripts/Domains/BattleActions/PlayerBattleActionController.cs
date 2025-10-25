using System;

public class PlayerBattleActionController : ITurnController
{
    public void RequestAction(IBattleContext ctx, Action<IBattleAction> onActionReady)
    {
    }
}
