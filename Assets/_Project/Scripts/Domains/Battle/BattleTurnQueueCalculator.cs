using System;
using System.Collections.Generic;

public static class BattleTurnQueueCalculator
{
    private static readonly Random _random = new Random();

    public static Queue<BattleUnitModel> CreateQueue(BattleUnitModel[] units)
    {
        if (units == null)
            throw new ArgumentNullException(nameof(units));

        if (units.Length == 0)
            return new Queue<BattleUnitModel>();

        var rankedUnits = new List<(BattleUnitModel Unit, int Initiative, double Roll)>(units.Length);

        foreach (var unit in units)
        {
            if (unit == null)
                continue;

            var stats = unit.GetStats();
            int initiative = stats?.Initiative ?? 0;

            double roll;
            lock (_random)
            {
                roll = _random.NextDouble();
            }

            rankedUnits.Add((unit, initiative, roll));
        }

        rankedUnits.Sort((a, b) =>
        {
            int initiativeComparison = b.Initiative.CompareTo(a.Initiative);
            if (initiativeComparison != 0)
                return initiativeComparison;
            return a.Roll.CompareTo(b.Roll);
        });

        var queue = new Queue<BattleUnitModel>(rankedUnits.Count);
        foreach (var item in rankedUnits)
            queue.Enqueue(item.Unit);

        return queue;
    }
}
