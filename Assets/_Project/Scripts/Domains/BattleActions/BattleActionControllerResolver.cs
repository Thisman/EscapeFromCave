using System;

public sealed class BattleActionControllerResolver
{
    private readonly ITurnController _playerController;
    private readonly ITurnController _enemyController;

    public BattleActionControllerResolver(ITurnController player, ITurnController enemy)
    {
        _playerController = player ?? throw new ArgumentNullException(nameof(player));
        _enemyController = enemy ?? throw new ArgumentNullException(nameof(enemy));
    }

    public IBattleActionController ResolveFor(IReadOnlySquadModel unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        if (unit.UnitDefinition != null && (unit.UnitDefinition.Type == UnitType.Hero || unit.UnitDefinition.Type == UnitType.Ally))
            return _playerController;

        return _enemyController;
    }
}
