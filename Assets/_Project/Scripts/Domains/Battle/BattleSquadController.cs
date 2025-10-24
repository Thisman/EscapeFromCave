using System;
using UnityEngine;

public class BattleSquadController : MonoBehaviour
{
    private BattleSquadModel _squadModel;

    public void Initialize(SquadModel squadModel)
    {
        if (squadModel == null)
            throw new ArgumentNullException(nameof(squadModel));

        _squadModel = new BattleSquadModel(squadModel);
    }

    public IBattleEntityModel GetSquadModel()
    {
        return _squadModel;
    }

    public BattleSquadModel GetBattleModel()
    {
        return _squadModel;
    }
}
