using System;

public class UnitStatsModel: IReadOnlyUnitStatsModel
{
    public int Level { get; }

    public int Health { get; }

    public int Damage { get; }

    public int Defense { get; }

    public int Initiative { get; }

    public float Speed { get; }

    public int XPToNext { get; }

    public UnitStatsModel(int level, UnitLevelDefintion stats)
    {
        Level = level;
        Health = stats.Health;
        Damage = stats.Damage;
        Defense = stats.Defense;
        Initiative = stats.Initiative;
        Speed = stats.Speed;
        XPToNext = stats.XPToNext;
    }
}
