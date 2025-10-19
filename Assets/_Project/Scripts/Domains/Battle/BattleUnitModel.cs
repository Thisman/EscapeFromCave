using System;

public class BattleUnitModel
{
    public BattleUnitModel(IReadOnlyUnitModel unitModel)
    {
        UnitModel = unitModel ?? throw new ArgumentNullException(nameof(unitModel));
    }

    public IReadOnlyUnitModel UnitModel { get; }

    public UnitStatsModel GetStats()
    {
        return UnitModel.GetStats();
    }
}
