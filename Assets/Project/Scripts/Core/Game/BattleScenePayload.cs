using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct AdditionalSquadSetup
{
    public AdditionalSquadSetup(UnitDefinitionSO definition, int count)
    {
        Definition = definition;
        Count = Math.Max(0, count);
    }

    public UnitDefinitionSO Definition { get; }

    public int Count { get; }

    public bool IsValid => Definition != null && Count > 0;
}

public readonly struct BattleSquadSetup
{
    private const int MaxAdditionalUnits = 5;

    private static readonly AdditionalSquadSetup[] EmptyAdditionalUnits = Array.Empty<AdditionalSquadSetup>();

    public BattleSquadSetup(UnitDefinitionSO definition, int count, IReadOnlyList<AdditionalSquadSetup> additionalUnits = null)
    {
        Definition = definition;
        Count = Math.Max(0, count);
        AdditionalUnits = PrepareAdditionalUnits(additionalUnits);
    }

    public UnitDefinitionSO Definition { get; }

    public int Count { get; }

    public IReadOnlyList<AdditionalSquadSetup> AdditionalUnits { get; }

    public bool IsValid => Definition != null && Count > 0;

    public bool HasAdditionalUnits => AdditionalUnits.Count > 0;

    public bool HasAnyUnits => IsValid || HasAdditionalUnits;

    private static IReadOnlyList<AdditionalSquadSetup> PrepareAdditionalUnits(IReadOnlyList<AdditionalSquadSetup> additionalUnits)
    {
        if (additionalUnits == null || additionalUnits.Count == 0)
            return EmptyAdditionalUnits;

        var buffer = new List<AdditionalSquadSetup>(Math.Min(additionalUnits.Count, MaxAdditionalUnits));

        for (int i = 0; i < additionalUnits.Count && buffer.Count < MaxAdditionalUnits; i++)
        {
            var candidate = additionalUnits[i];
            if (candidate.IsValid)
                buffer.Add(candidate);
        }

        return buffer.Count > 0 ? buffer.ToArray() : EmptyAdditionalUnits;
    }
}

public sealed class BattleSceneData
{
    public BattleSceneData(
        BattleSquadSetup hero,
        IReadOnlyList<BattleSquadSetup> army,
        BattleSquadSetup enemy,
        GameObject heroSource,
        GameObject enemySource)
    {
        Hero = hero;
        Army = army != null ? new List<BattleSquadSetup>(army) : Array.Empty<BattleSquadSetup>();
        Enemy = enemy;
        HeroSource = heroSource;
        EnemySource = enemySource;
    }

    public BattleSquadSetup Hero { get; }

    public IReadOnlyList<BattleSquadSetup> Army { get; }

    public BattleSquadSetup Enemy { get; }

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
