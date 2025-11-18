using System;
using System.Threading.Tasks;

public sealed class BattleDamageDefaultResolver : IBattleDamageResolver
{
    public async Task ResolveDamage(IBattleDamageProvider actor, IBattleDamageReceiver target)
    {
        BattleDamageData damage = actor.CreateDamageData();
        
        await target.ApplyDamage(damage);
    }
}
