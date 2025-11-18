using System;

public sealed class BattleActionTargetResolverForSelf : IBattleActionTargetResolver
{
    public bool ResolveTarget(IReadOnlySquadModel actor, IReadOnlySquadModel target)
    {
        return ReferenceEquals(actor, target);
    }
}
