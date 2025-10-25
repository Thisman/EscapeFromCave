using System;

public sealed class DefaultBattleDamageResolver : IBattleDamageResolver
{
    public void ResolveDamage(BattleSquadController actor, BattleSquadController target)
    {
        if (actor == null)
            throw new ArgumentNullException(nameof(actor));
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        int damage = actor.ResolveDamage();
        target.ApplyDamage(damage);
    }
}
