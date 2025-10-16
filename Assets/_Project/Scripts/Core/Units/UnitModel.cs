using System;
using Game.Data;

public class UnitStatsModel
{
    public int Level { get; }
    public int Health { get; }
    public int Damage { get; }
    public int Defense { get; }
    public int Initiative { get; }
    public float Speed { get; }
    public int XPToNext { get; }

    public UnitStatsModel(int level, UnitStatsLevel stats)
    {
        if (stats == null)
            throw new ArgumentNullException(nameof(stats));

        Level = level;
        Health = stats.Health;
        Damage = stats.Damage;
        Defense = stats.Defense;
        Initiative = stats.Initiative;
        Speed = stats.Speed;
        XPToNext = stats.XPToNext;
    }
}

public class UnitModel
{
    public UnitDefinitionSO Definition { get; }
    public int Level => _level;
    public int Experience => _experience;

    private int _level;
    private int _experience;

    public UnitModel(UnitDefinitionSO definition, int startingLevel = 0, int startingExperience = 0)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));

        _level = Math.Max(0, startingLevel);
        _experience = 0;

        if (startingExperience > 0)
            AddExperience(startingExperience);
    }

    public UnitStatsModel GetStats()
    {
        var statsLevel = Definition.GetStatsForLevel(_level);
        return statsLevel != null ? new UnitStatsModel(_level, statsLevel) : null;
    }

    public bool AddExperience(int amount)
    {
        if (amount <= 0)
            return false;

        bool leveledUp = false;
        _experience += amount;

        while (true)
        {
            int xpToNext = Definition.GetXPForNextLevel(_level);
            if (xpToNext <= 0)
            {
                _experience = 0;
                break;
            }

            if (_experience < xpToNext)
                break;

            _experience -= xpToNext;
            _level++;
            leveledUp = true;
        }

        return leveledUp;
    }
}
