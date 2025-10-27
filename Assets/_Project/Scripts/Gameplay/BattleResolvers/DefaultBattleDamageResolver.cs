using System;
using System.Threading.Tasks;

public sealed class DefaultBattleDamageResolver : IBattleDamageResolver
{
    public async Task ResolveDamage(BattleSquadController actor, BattleSquadController target)
    {
        int damage = actor.ResolveDamage();
        await target.ApplyDamage(damage);
    }
}
