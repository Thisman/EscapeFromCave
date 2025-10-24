using System;
using UnityEngine;

public class BattleUnitController : MonoBehaviour
{
    private BattleUnitModel _battleUnitModel;

    public void Initialize(UnitModel unitModel)
    {
        if (unitModel == null)
            throw new ArgumentNullException(nameof(unitModel));

        _battleUnitModel = new BattleUnitModel(unitModel);
    }

    public UnitStatsModel GetUnitStats()
    {
        return _battleUnitModel?.GetStats();
    }

    public IReadOnlyBattleUnitModel GetUnitModel()
    {
        return _battleUnitModel;
    }
}
