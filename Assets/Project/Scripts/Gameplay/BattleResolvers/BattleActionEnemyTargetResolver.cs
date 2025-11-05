using System;

public sealed class BattleActionEnemyTargetResolver : IBattleActionTargetResolver
{
    public bool ResolveTarget(IReadOnlySquadModel actor, IReadOnlySquadModel target)
    {
        if (actor == null)
            throw new ArgumentNullException(nameof(actor));

        if (target == null)
            return false;

        return IsEnemy(actor.Kind, target.Kind);
    }

    private static bool IsEnemy(UnitKind actorKind, UnitKind targetKind)
    {
        return actorKind switch
        {
            UnitKind.Hero or UnitKind.Ally => targetKind == UnitKind.Enemy,
            UnitKind.Enemy => targetKind is UnitKind.Hero or UnitKind.Ally,
            UnitKind.Neutral => false,
            _ => false,
        };
    }
}
