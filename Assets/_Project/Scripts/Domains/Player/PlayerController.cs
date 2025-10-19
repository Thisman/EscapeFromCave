using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private UnitModel _unitModel;

    public void Initialize(UnitModel unitModel)
    {
        _unitModel = unitModel ?? throw new ArgumentNullException(nameof(unitModel));
    }

    public UnitStatsModel GetPlayerStats()
    {
        if (_unitModel == null)
            return null;

        return _unitModel.GetStats();
    }

    public IReadOnlyUnitModel GetPlayerModel()
    {
        if (_unitModel == null)
            return null;

        return _unitModel;
    }
}
