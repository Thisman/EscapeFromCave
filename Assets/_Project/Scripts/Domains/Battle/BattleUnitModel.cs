using System;

public class BattleUnitModel : IReadOnlyBattleUnitModel
{
    public UnitDefinitionSO Definition { get; }

    public int Level { get; }

    public int Experience { get; }

    private readonly UnitStatsModel _stats;

    public BattleUnitModel(UnitModel sourceUnit)
    {
        if (sourceUnit == null)
            throw new ArgumentNullException(nameof(sourceUnit));

        Definition = sourceUnit.Definition;
        Level = sourceUnit.Level;
        Experience = sourceUnit.Experience;
        _stats = sourceUnit.GetStats();
    }

    public UnitStatsModel GetStats()
    {
        return _stats;
    }
}
