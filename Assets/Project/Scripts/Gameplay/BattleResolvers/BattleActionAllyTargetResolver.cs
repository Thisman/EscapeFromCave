using System;

public sealed class BattleActionAllyTargetResolver : IBattleActionTargetResolver
{
    public bool ResolveTarget(IReadOnlySquadModel actor, IReadOnlySquadModel target)
    {
        if (actor == null)
            throw new ArgumentNullException(nameof(actor));

        if (target == null)
            return false;

        return IsSameSide(actor.Kind, target.Kind);
    }

    private static bool IsSameSide(UnitKind actorKind, UnitKind targetKind)
    {
        return actorKind switch
        {
            UnitKind.Hero or UnitKind.Ally => targetKind is UnitKind.Hero or UnitKind.Ally,
            UnitKind.Enemy => targetKind == UnitKind.Enemy,
            UnitKind.Neutral => targetKind == UnitKind.Neutral,
            _ => false,
        };
    }
}
