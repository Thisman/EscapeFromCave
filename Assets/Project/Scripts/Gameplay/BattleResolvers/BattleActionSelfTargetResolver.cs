using System;

public sealed class BattleActionSelfTargetResolver : IBattleActionTargetResolver
{
    public bool ResolveTarget(IReadOnlySquadModel actor, IReadOnlySquadModel target)
    {
        if (actor == null)
            throw new ArgumentNullException(nameof(actor));

        return ReferenceEquals(actor, target);
    }
}
