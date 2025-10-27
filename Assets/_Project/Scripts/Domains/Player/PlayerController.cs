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
        return _squadModel.Definition.Speed;
    }

    public IReadOnlySquadModel GetPlayer()
    {
        return _squadModel;
    }
}
