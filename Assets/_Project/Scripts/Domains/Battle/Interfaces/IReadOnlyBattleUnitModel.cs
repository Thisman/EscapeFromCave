public interface IReadOnlyBattleUnitModel
{
    UnitDefinitionSO Definition { get; }

    int Level { get; }

    int Experience { get; }

    UnitStatsModel GetStats();
}
