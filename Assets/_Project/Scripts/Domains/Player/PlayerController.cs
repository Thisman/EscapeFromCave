using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private SquadModel _squadModel;

    public void Initialize(SquadModel squadModel)
    {
        _squadModel = squadModel ?? throw new ArgumentNullException(nameof(squadModel));
    }

    public float GetMovementSpeed()
    {
        if (_squadModel == null)
            return 0f;

        var definition = _squadModel.UnitDefinition;
        if (definition == null)
            return 0f;

        var stats = definition.GetStatsForLevel(1);
        return stats.Speed;
    }

    public IReadOnlySquadModel GetPlayerSquad()
    {
        return _squadModel;
    }
}
