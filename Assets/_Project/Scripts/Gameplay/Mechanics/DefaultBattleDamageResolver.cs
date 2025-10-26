using System;
using System.Threading.Tasks;

public sealed class DefaultBattleDamageResolver : IBattleDamageResolver
{
    public async Task ResolveDamage(BattleSquadController actor, BattleSquadController target)
    {
        int damage = actor.ResolveDamage();
        target.ApplyDamage(damage);

        var animationController = target.GetComponentInChildren<BattleSquadAnimationController>();
        if (animationController == null)
            return;

        var completionSource = new TaskCompletionSource<bool>();
        animationController.PlayDamageFlash(() => completionSource.TrySetResult(true));
        await completionSource.Task;
    }
}
