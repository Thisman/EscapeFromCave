using System;
using System.Collections.Generic;
using System.Linq;

public class BattleQueueController
{
    private Queue<IReadOnlySquadModel> _queue = new();

    public void Rebuild(IEnumerable<IReadOnlySquadModel> units)
    {
        if (units == null)
            throw new ArgumentNullException(nameof(units));

        _queue = new Queue<IReadOnlySquadModel>(
            units
                .Where(unit => unit != null)
                .OrderByDescending(GetInitiative)
                .ThenByDescending(IsFriendly));
    }

    public void AddLast(IReadOnlySquadModel unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        _queue.Enqueue(unit);
    }

    public void AddFirst(IReadOnlySquadModel unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        RebuildQueueWithInsertion(0, unit);
    }

    public bool AddAfter(IReadOnlySquadModel target, IReadOnlySquadModel unit)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        bool inserted = false;
        var buffer = new Queue<IReadOnlySquadModel>(_queue.Count + 1);

        foreach (var existing in _queue)
        {
            buffer.Enqueue(existing);
            if (!inserted && ReferenceEquals(existing, target))
            {
                buffer.Enqueue(unit);
                inserted = true;
            }
        }

        if (!inserted)
            return false;

        _queue = buffer;
        return true;
    }

    public IReadOnlyList<IReadOnlySquadModel> GetQueue()
    {
        return _queue.ToArray();
    }

    public IReadOnlySquadModel NextTurn()
    {
        if (_queue.Count == 0)
            return null;

        return _queue.Dequeue();
    }

    public IReadOnlySquadModel GetFirst()
    {
        if (_queue.Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        return _queue.Peek();
    }

    public IReadOnlySquadModel GetAt(int index)
    {
        if (index < 0 || index >= _queue.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        int currentIndex = 0;
        foreach (var unit in _queue)
        {
            if (currentIndex == index)
                return unit;

            currentIndex++;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    public bool Remove(IReadOnlySquadModel unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        bool removed = false;
        var buffer = new Queue<IReadOnlySquadModel>(_queue.Count);

        foreach (var existing in _queue)
        {
            if (!removed && ReferenceEquals(existing, unit))
            {
                removed = true;
                continue;
            }

            buffer.Enqueue(existing);
        }

        if (!removed)
            return false;

        _queue = buffer;
        return true;
    }

    private void RebuildQueueWithInsertion(int index, IReadOnlySquadModel unit)
    {
        var buffer = new Queue<IReadOnlySquadModel>(_queue.Count + 1);
        int currentIndex = 0;

        foreach (var existing in _queue)
        {
            if (currentIndex == index)
                buffer.Enqueue(unit);

            buffer.Enqueue(existing);
            currentIndex++;
        }

        if (index >= currentIndex)
            buffer.Enqueue(unit);

        _queue = buffer;
    }

    private static bool IsFriendly(IReadOnlySquadModel unit)
    {
        return unit.Definition.Type is UnitType.Hero or UnitType.Ally;
    }

    private static int GetInitiative(IReadOnlySquadModel squad)
    {
        if (squad?.Definition == null)
            return 0;

        var stats = squad.Definition.GetStatsForLevel(1);
        return stats.Initiative;
    }
}
