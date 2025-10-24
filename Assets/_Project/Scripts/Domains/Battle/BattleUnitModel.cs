using System;

public sealed class BattleUnitModel : IReadOnlyBattleModel
{
    private readonly UnitModel _unitModel;

    public BattleUnitModel(UnitModel unitModel)
    {
        _unitModel = unitModel ?? throw new ArgumentNullException(nameof(unitModel));
    }

    public UnitDefinitionSO Definition => _unitModel.Definition;

    public int Level => _unitModel.Level;

    public int Experience => _unitModel.Experience;

    public UnitStatsModel GetStats()
    {
        return _unitModel.GetStats();
    }

    public int GetInitiative()
    {
        var stats = _unitModel.GetStats();
        return stats?.Initiative ?? 0;
    }

    public UnitModel GetBaseModel()
    {
        return _unitModel;
    }
}
