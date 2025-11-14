using System;

public sealed class BattleActionSelfTargetResolver : IBattleActionTargetResolver
{
    public bool ResolveTarget(IReadOnlySquadModel actor, IReadOnlySquadModel target)
    {
        return ReferenceEquals(actor, target);
    }
}
