public interface IBattleActionTargetResolver
{
    bool ResolveTarget(IReadOnlySquadModel actor, IReadOnlySquadModel target);
}
