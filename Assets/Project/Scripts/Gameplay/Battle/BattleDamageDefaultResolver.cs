using System;
using System.Threading.Tasks;

public sealed class BattleDamageDefaultResolver : IBattleDamageResolver
{
    public async Task ResolveDamage(IBattleDamageSource actor, IBattleDamageReceiver target)
    {
        BattleDamageData damage = actor.ResolveDamage();
        
        await target.ApplyDamage(damage);
    }
}
