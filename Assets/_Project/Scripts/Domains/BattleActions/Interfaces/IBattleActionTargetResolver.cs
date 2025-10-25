using Actor = BattleSquadController;

public interface IBattleActionTargetResolver
{
    bool ResolveTarget(Actor actor, IReadOnlySquadModel target);
}
