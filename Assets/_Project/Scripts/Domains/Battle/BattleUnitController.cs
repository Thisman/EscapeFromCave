using System;
using UnityEngine;

public class BattleUnitController : MonoBehaviour
{
    private UnitModel _unitModel;

    public void Initialize(UnitModel unitModel)
    {
        if (unitModel == null)
            throw new ArgumentNullException(nameof(unitModel));

        _unitModel = unitModel;
    }

    public UnitStatsModel GetUnitStats()
    {
        return _unitModel?.GetStats();
    }

    public UnitModel GetUnitModel()
    {
        return _unitModel;
    }
}
