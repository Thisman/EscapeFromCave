using System;
using System.Threading.Tasks;

public sealed class BattleDamageDefaultResolver : IBattleDamageResolver
{
    public async Task ResolveDamage(IBattleDamageSource actor, IBattleDamageReceiver target)
    {
        if (actor == null)
            throw new ArgumentNullException(nameof(actor));

        if (target == null)
            throw new ArgumentNullException(nameof(target));

        var damage = actor.ResolveDamage();
        if (damage == null || damage.Value <= 0)
            return;

        await target.ApplyDamage(damage);
    }
}
