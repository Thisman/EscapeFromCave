using System;

public sealed class BattleActionControllerResolver
{
    private readonly IBattleActionController _playerController;
    private readonly IBattleActionController _enemyController;

    public BattleActionControllerResolver(IBattleActionController player, IBattleActionController enemy)
    {
        _playerController = player;
        _enemyController = enemy;
    }

    public IBattleActionController ResolveFor(IReadOnlySquadModel unit)
    {
        if (unit.Definition.IsFriendly())
            return _playerController;

        return _enemyController;
    }
}
