using System;
using UnityEngine;

public class BattleUnitController : MonoBehaviour
{
    private BattleUnitModel _unitModel;

    public void Initialize(UnitModel unitModel)
    {
        if (unitModel == null)
            throw new ArgumentNullException(nameof(unitModel));

        _unitModel = new BattleUnitModel(unitModel);
    }

    public UnitStatsModel GetUnitStats()
    {
        return _unitModel?.GetStats();
    }

    public IBattleEntityModel GetUnitModel()
    {
        return _unitModel;
    }

    public BattleUnitModel GetBattleModel()
    {
        return _unitModel;
    }
}
