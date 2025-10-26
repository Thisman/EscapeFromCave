using System;
using System.Threading.Tasks;

public sealed class DefaultBattleDamageResolver : IBattleDamageResolver
{
    public async Task ResolveDamage(BattleSquadController actor, BattleSquadController target)
    {
        if (actor == null)
            throw new ArgumentNullException(nameof(actor));
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        int damage = actor.ResolveDamage();
        await target.ApplyDamage(damage);
    }
}
