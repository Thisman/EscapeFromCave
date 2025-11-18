using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    private readonly IReadOnlyDictionary<IReadOnlySquadModel, int> _initialSquadCounts;

    public BattleResult(
        bool playerRequestedFlee,
        IReadOnlyCollection<IReadOnlySquadModel> friendlySquads,
        IReadOnlyCollection<IReadOnlySquadModel> enemySquads,
        IReadOnlyDictionary<IReadOnlySquadModel, int> initialSquadCounts)
    {
        BattleUnitsResult = BuildUnitsResult(friendlySquads, enemySquads);
        _initialSquadCounts = initialSquadCounts != null
            ? new Dictionary<IReadOnlySquadModel, int>(initialSquadCounts)
            : new Dictionary<IReadOnlySquadModel, int>();
        Status = DetermineBattleStatus(playerRequestedFlee);
        ExperienceReward = CalculateExperienceReward(BattleUnitsResult.EnemyUnits);
    }

    public BattleResultStatus Status { get; }

    public BattleUnitsResult BattleUnitsResult { get; }

    public float ExperienceReward { get; }

    public IReadOnlyDictionary<IReadOnlySquadModel, int> InitialSquadCounts => _initialSquadCounts;

    public bool IsVictory => Status == BattleResultStatus.Victory;

    public bool IsDefeat => Status == BattleResultStatus.Defeat;

    public bool IsFlee => Status == BattleResultStatus.Flee;

    public int GetInitialCount(IReadOnlySquadModel squad)
    {
        if (squad == null)
            return 0;

        if (_initialSquadCounts != null && _initialSquadCounts.TryGetValue(squad, out int count))
            return count;

        return Mathf.Max(0, squad.Count);
    }

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

    private float CalculateExperienceReward(IReadOnlyList<IReadOnlySquadModel> enemySquads)
    {
        if (enemySquads == null || enemySquads.Count == 0)
            return 0f;

        float totalExperience = 0f;

        foreach (var squad in enemySquads)
        {
            if (squad == null)
                continue;

            int initialCount = Mathf.Max(0, GetInitialCount(squad));
            if (initialCount <= 0)
                continue;

            float health = Mathf.Max(0f, squad.Health);
            if (Mathf.Approximately(health, 0f))
                continue;

            totalExperience += initialCount * health / 10f;
        }

        return Mathf.Max(0f, totalExperience);
    }
}
