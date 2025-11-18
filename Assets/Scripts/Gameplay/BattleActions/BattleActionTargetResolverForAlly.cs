using System;

public sealed class BattleActionTargetResolverForAlly : IBattleActionTargetResolver
{
    public bool ResolveTarget(IReadOnlySquadModel actor, IReadOnlySquadModel target)
    {
        return IsSameSide(actor.Kind, target.Kind);
    }

    // TODO: вынести куда-то в утилиты или модель юнитов
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
