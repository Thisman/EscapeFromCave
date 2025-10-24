using System;
using UnityEngine;

public sealed class BattleScenePayload : ISceneLoadingPayload<BattleSceneData>
{
    private readonly BattleSceneData _data;

    public BattleScenePayload(GameObject player, IReadOnlyArmyModel playerArmy, GameObject enemy)
    {
        _data = new BattleSceneData(player, playerArmy, enemy);
    }

    public BattleSceneData GetData() => _data;
}

public sealed class BattleSceneData
{
    public BattleSceneData(GameObject player, IReadOnlyArmyModel playerArmy, GameObject enemy)
    {
        Player = player ? player : throw new ArgumentNullException(nameof(player));
        PlayerArmy = playerArmy ?? throw new ArgumentNullException(nameof(playerArmy));
        Enemy = enemy ? enemy : throw new ArgumentNullException(nameof(enemy));
    }

    public GameObject Player { get; }

    public IReadOnlyArmyModel PlayerArmy { get; }

    public GameObject Enemy { get; }
}
