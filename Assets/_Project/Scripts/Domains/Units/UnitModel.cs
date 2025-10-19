using System;

public class UnitModel : IReadOnlyUnitModel
{
    public UnitDefinitionSO Definition { get; }

    public int Level => _level;

    public int Experience => _experience;

    private int _level;

    private int _experience;

    public UnitModel(UnitDefinitionSO definition, int startingLevel = 0, int startingExperience = 0)
    {
        Definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));

        _level = Math.Max(0, startingLevel);
        _experience = 0;

        if (startingExperience > 0)
            AddExperience(startingExperience);
    }

    public UnitStatsModel GetStats()
    {
        var statsLevel = Definition.GetStatsForLevel(_level);
        return new UnitStatsModel(_level, statsLevel);
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
