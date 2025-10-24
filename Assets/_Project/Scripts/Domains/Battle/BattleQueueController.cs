using System;
using System.Collections.Generic;
using System.Linq;

public class BattleQueueController
{
    private Queue<IBattleEntityModel> _queue = new();

    public void Rebuild(IEnumerable<IBattleEntityModel> units)
    {
        if (units == null)
            throw new ArgumentNullException(nameof(units));

        _queue = new Queue<IBattleEntityModel>(
            units
                .Where(unit => unit != null)
                .OrderByDescending(unit => unit.GetInitiative())
                .ThenByDescending(IsFriendly));
    }

    public void AddLast(IBattleEntityModel unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        _queue.Enqueue(unit);
    }

    public void AddFirst(IBattleEntityModel unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        RebuildQueueWithInsertion(0, unit);
    }

    public bool AddAfter(IBattleEntityModel target, IBattleEntityModel unit)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        bool inserted = false;
        var buffer = new Queue<IBattleEntityModel>(_queue.Count + 1);

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

    public IReadOnlyList<IBattleEntityModel> GetQueue()
    {
        return _queue.ToArray();
    }

    public IBattleEntityModel NextTurn()
    {
        if (_queue.Count == 0)
            return null;

        return _queue.Dequeue();
    }

    public IBattleEntityModel GetFirst()
    {
        if (_queue.Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        return _queue.Peek();
    }

    public IBattleEntityModel GetAt(int index)
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

    public bool Remove(IBattleEntityModel unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        bool removed = false;
        var buffer = new Queue<IBattleEntityModel>(_queue.Count);

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

    private void RebuildQueueWithInsertion(int index, IBattleEntityModel unit)
    {
        var buffer = new Queue<IBattleEntityModel>(_queue.Count + 1);
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

    private static bool IsFriendly(IBattleEntityModel unit)
    {
        return unit.Definition.Type is UnitType.Hero or UnitType.Ally;
    }
}
