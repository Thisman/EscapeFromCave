using System;
using System.Collections.Generic;

public enum BattleResultStatus
{
    Defeat,
    Victory,
    Flee
}

public readonly struct BattleUnitsResult
{
    public BattleUnitsResult(
        IReadOnlyList<IReadOnlySquadModel> friendlyUnits,
        IReadOnlyList<IReadOnlySquadModel> enemyUnits)
    {
        FriendlyUnits = friendlyUnits ?? Array.Empty<IReadOnlySquadModel>();
        EnemyUnits = enemyUnits ?? Array.Empty<IReadOnlySquadModel>();
    }

    public IReadOnlyList<IReadOnlySquadModel> FriendlyUnits { get; }

    public IReadOnlyList<IReadOnlySquadModel> EnemyUnits { get; }
}

public readonly struct BattleResult
{
    public BattleResult(BattleResultStatus status, BattleUnitsResult battleUnitsResult)
    {
        Status = status;
        BattleUnitsResult = battleUnitsResult;
    }

    public BattleResultStatus Status { get; }

    public BattleUnitsResult BattleUnitsResult { get; }

    public bool IsVictory => Status == BattleResultStatus.Victory;

    public bool IsDefeat => Status == BattleResultStatus.Defeat;

    public bool IsFlee => Status == BattleResultStatus.Flee;
}
