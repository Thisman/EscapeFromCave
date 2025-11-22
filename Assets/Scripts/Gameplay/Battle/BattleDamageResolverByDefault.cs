using System;
using System.Threading.Tasks;

public sealed class BattleDamageResolverByDefault
{
    public async Task ResolveDamage(IBattleDamageProvider actor, IBattleDamageReceiver target)
    {
        BattleDamageData damage = actor.CreateDamageData();
        
        await target.ApplyDamage(damage);
    }
}
