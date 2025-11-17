using System;
using System.Collections.Generic;
using System.Linq;

public class BattleQueueController
{
    private Queue<IReadOnlySquadModel> _queue = new();

    public void Build(IEnumerable<IReadOnlySquadModel> units)
    {
        if (units == null)
            throw new ArgumentNullException(nameof(units));

        _queue = new Queue<IReadOnlySquadModel>(
            units
                .Where(unit => unit != null)
                .OrderByDescending(GetInitiative)
                .ThenByDescending((unit) => unit.IsFriendly()));
    }

    public void AddLast(IReadOnlySquadModel unit)
    {
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        _queue.Enqueue(unit);
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

    private static int GetInitiative(IReadOnlySquadModel squad)
    {
        return (int)squad.Initiative;
    }
}
