using System;
using System.Threading.Tasks;

public class AIBattleActionController : IBattleActionController
{
    private const int MinActionDelayMilliseconds = 500;
    private const int MaxActionDelayMilliseconds = 1250;
    private static readonly Random DelayRandomizer = new();

    public async void RequestAction(BattleContext ctx, Action<IBattleAction> onActionReady)
    {
        int delay = GetRandomActionDelay();
        await Task.Delay(delay);

        var targetResolver = new BattleActionTargetResolverForAttack(ctx);
        var damageResolver = new BattleDamageResolverByDefault();
        var targetPicker = new BattlActionTargetPickerForAI(ctx);
        onActionReady?.Invoke(new BattleActionAttack(ctx, targetResolver, damageResolver, targetPicker));
    }

    private static int GetRandomActionDelay()
    {
        int min = Math.Max(0, MinActionDelayMilliseconds);
        int max = Math.Max(min, MaxActionDelayMilliseconds);

        lock (DelayRandomizer)
        {
            // Random.Next upper bound is exclusive, so add 1 to make it inclusive.
            return DelayRandomizer.Next(min, max + 1);
        }
    }
}
