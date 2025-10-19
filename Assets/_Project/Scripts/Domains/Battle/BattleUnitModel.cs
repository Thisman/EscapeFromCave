using System;

public class BattleUnitModel
{
    public BattleUnitModel(UnitModel unitModel)
    {
        UnitModel = unitModel ?? throw new ArgumentNullException(nameof(unitModel));
    }

    public UnitModel UnitModel { get; }

    public UnitStatsModel GetStats()
    {
        return UnitModel.GetStats();
    }
}
