public interface IReadOnlyUnitModel
{
    UnitDefinitionSO Definition { get; }
    int Level { get; }
    int Experience { get; }

    UnitStatsModel GetStats();
}
