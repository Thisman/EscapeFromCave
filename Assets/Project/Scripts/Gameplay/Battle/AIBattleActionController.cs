using System;
using System.Threading.Tasks;

public class AIBattleActionController : IBattleActionController
{
    private readonly int _actionDelayMilliseconds;

    public AIBattleActionController(int actionDelayMilliseconds = 750)
    {
        _actionDelayMilliseconds = Math.Max(0, actionDelayMilliseconds);
    }

    public async void RequestAction(BattleContext ctx, Action<IBattleAction> onActionReady)
    {
        if (_actionDelayMilliseconds > 0)
        {
            await Task.Delay(_actionDelayMilliseconds);
        }

        var targetResolver = new BattleActionDefaultTargetResolver(ctx);
        var damageResolver = new BattleDamageDefaultResolver();
        var targetPicker = new AIActionTargetPicker(ctx);
        onActionReady?.Invoke(new BattleActionAttack(ctx, targetResolver, damageResolver, targetPicker));
    }
}
