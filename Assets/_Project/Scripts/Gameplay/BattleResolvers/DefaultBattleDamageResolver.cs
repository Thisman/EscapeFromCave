using System;
using System.Threading.Tasks;

public sealed class DefaultBattleDamageResolver : IBattleDamageResolver
{
    public async Task ResolveDamage(BattleSquadController actor, BattleSquadController target)
    {
        int damage = actor.ResolveDamage();
        await target.ApplyDamage(damage);
    }

    public async Task ResolveDamage(int damage, BattleSquadController target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        damage = Math.Max(0, damage);
        if (damage == 0)
            return;

        await target.ApplyDamage(damage);
    }
}
