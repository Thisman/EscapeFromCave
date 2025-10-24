using System;
using System.Collections.Generic;
using System.Linq;

public class BattleQueueController
{
    private readonly List<IReadOnlyUnitModel> _queue;

    public BattleQueueController(IEnumerable<IReadOnlyUnitModel> units)
    {
        if (units == null)
            throw new ArgumentNullException(nameof(units));

        _queue = units
            .OrderByDescending(unit => unit?.GetStats().Initiative ?? 0)
            .ThenByDescending(unit => unit != null && IsFriendly(unit))
            .ToList();
    }

    public void AddLast(IReadOnlyUnitModel unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        _queue.Add(unit);
    }

    public void AddFirst(IReadOnlyUnitModel unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        _queue.Insert(0, unit);
    }

    public bool AddAfter(IReadOnlyUnitModel target, IReadOnlyUnitModel unit)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        int index = _queue.FindIndex(existing => ReferenceEquals(existing, target));
        if (index < 0)
            return false;

        _queue.Insert(index + 1, unit);
        return true;
    }

    public IReadOnlyList<IReadOnlyUnitModel> GetQueue()
    {
        return _queue;
    }

    public IReadOnlyUnitModel GetAt(int index)
    {
        if (index < 0 || index >= _queue.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        return _queue[index];
    }

    public bool Remove(IReadOnlyUnitModel unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        int index = _queue.FindIndex(existing => ReferenceEquals(existing, unit));
        if (index < 0)
            return false;

        _queue.RemoveAt(index);
        return true;
    }

    private static bool IsFriendly(IReadOnlyUnitModel unit)
    {
        return unit.Definition.Type is UnitType.Hero or UnitType.Ally;
    }
}
