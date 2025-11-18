using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct BattleSquadSetup
{
    public BattleSquadSetup(UnitSO definition, int count, float experience = 0f)
    {
        Definition = definition;
        Count = Math.Max(0, count);
        Experience = Mathf.Max(0f, experience);
    }

    public UnitSO Definition { get; }

    public int Count { get; }

    public float Experience { get; }

    public bool IsValid => Definition != null && Count > 0;
}

public sealed class BattleSceneData
{
    public BattleSceneData(
        BattleSquadSetup hero,
        IReadOnlyList<BattleSquadSetup> army,
        IReadOnlyList<BattleSquadSetup> enemies,
        GameObject heroSource,
        GameObject enemySource)
    {
        Hero = hero;
        Army = army;
        Enemies = enemies;
        HeroSource = heroSource;
        EnemySource = enemySource;
    }

    public BattleSquadSetup Hero { get; }

    public IReadOnlyList<BattleSquadSetup> Army { get; }

    public IReadOnlyList<BattleSquadSetup> Enemies { get; }

    public GameObject HeroSource { get; }

    public GameObject EnemySource { get; }
}

public sealed class BattleScenePayload : ISceneLoadingPayload<BattleSceneData>
{
    private readonly BattleSceneData _data;

    public BattleScenePayload(BattleSceneData data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public BattleSceneData GetData() => _data;
}
