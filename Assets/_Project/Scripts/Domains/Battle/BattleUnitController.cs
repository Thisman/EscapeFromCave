using System;
using UnityEngine;

public class BattleUnitController : MonoBehaviour
{
    private SquadModel _squadModel;

    public void Initialize(SquadModel squadModel)
    {
        if (squadModel == null)
            throw new ArgumentNullException(nameof(squadModel));

        _squadModel = squadModel;
    }

    public IReadOnlySquadModel GetSquadModel()
    {
        return _squadModel;
    }
}
