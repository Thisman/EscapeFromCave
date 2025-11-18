using System;
using System.Collections.Generic;
using System.Linq;

public enum BattleResultStatus
{
    Defeat,
    Victory,
    Flee
}

// TODO: убрать структуру, поля перенести в BattleResult
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

public sealed class BattleResult
{
    public BattleResult(
        bool playerRequestedFlee,
        IReadOnlyCollection<IReadOnlySquadModel> friendlySquads,
        IReadOnlyCollection<IReadOnlySquadModel> enemySquads)
    {
        BattleUnitsResult = BuildUnitsResult(friendlySquads, enemySquads);
        Status = DetermineBattleStatus(playerRequestedFlee);
    }

    public BattleResultStatus Status { get; }

    public BattleUnitsResult BattleUnitsResult { get; }

    public bool IsVictory => Status == BattleResultStatus.Victory;

    public bool IsDefeat => Status == BattleResultStatus.Defeat;

    public bool IsFlee => Status == BattleResultStatus.Flee;

    public static bool CheckForBattleCompletion(bool battleFinished, IReadOnlyList<BattleSquadController> units)
    {
        if (battleFinished)
            return true;

        if (units == null || units.Count == 0)
            return true;

        bool heroInQueue = units.Any(unit => unit?.GetSquadModel()?.IsHero() == true);

        if (!heroInQueue)
            return true;

        bool hasFriendlyUnits = false;
        bool hasEnemyUnits = false;

        foreach (var unitController in units)
        {
            if (unitController == null)
                continue;

            var model = unitController.GetSquadModel();

            if (model == null || model.Count <= 0)
                continue;

            if (model.IsFriendly())
            {
                hasFriendlyUnits = true;
            }
            else
            {
                hasEnemyUnits = true;
            }

            if (hasFriendlyUnits && hasEnemyUnits)
                return false;
        }

        if (!hasFriendlyUnits && !hasEnemyUnits)
            return true;

        if (hasFriendlyUnits == hasEnemyUnits)
            return false;

        return true;
    }

    private BattleUnitsResult BuildUnitsResult(
        IReadOnlyCollection<IReadOnlySquadModel> friendlySquads,
        IReadOnlyCollection<IReadOnlySquadModel> enemySquads)
    {
        IReadOnlySquadModel[] friendlyUnits = friendlySquads != null && friendlySquads.Count > 0
            ? friendlySquads.ToArray()
            : Array.Empty<IReadOnlySquadModel>();

        IReadOnlySquadModel[] enemyUnits = enemySquads != null && enemySquads.Count > 0
            ? enemySquads.ToArray()
            : Array.Empty<IReadOnlySquadModel>();

        return new BattleUnitsResult(friendlyUnits, enemyUnits);
    }

    private BattleResultStatus DetermineBattleStatus(bool playerRequestedFlee)
    {
        if (playerRequestedFlee)
            return BattleResultStatus.Flee;

        bool heroAlive = BattleUnitsResult.FriendlyUnits.Any(model => model.IsHero() && model.Count > 0);
        bool hasAliveFriendlies = BattleUnitsResult.FriendlyUnits.Any(model => model.Count > 0);
        bool hasAliveEnemies = BattleUnitsResult.EnemyUnits.Any(model => model.Count > 0);

        if (!heroAlive)
            return BattleResultStatus.Defeat;

        if (hasAliveFriendlies && !hasAliveEnemies)
            return BattleResultStatus.Victory;

        if (hasAliveFriendlies)
            return BattleResultStatus.Victory;

        return BattleResultStatus.Defeat;
    }
}
