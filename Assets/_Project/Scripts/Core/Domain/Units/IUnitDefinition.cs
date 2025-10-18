public interface IUnitDefinition
{
    string Id { get; }
    UnitStatsModel GetStatsForLevel(int level);
    int GetXPForNextLevel(int level);
}
