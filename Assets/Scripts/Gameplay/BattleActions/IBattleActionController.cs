using System;

public interface IBattleActionController
{
    void RequestAction(BattleContext ctx, int actionId);
}
