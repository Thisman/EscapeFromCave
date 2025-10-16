using System;
using UnityEngine;

[Serializable]
public class SquadModel
{
    public UnitDefinitionSO Definition { get; private set; }

    [SerializeField, Min(0)]
    private int _count;

    public int Count => _count;
    public bool IsEmpty => _count <= 0;

    public event Action<SquadModel> Changed;

    public SquadModel(UnitDefinitionSO definition, int initialCount = 0)
    {
        if (!definition) throw new ArgumentNullException(nameof(definition));
        if (initialCount < 0) throw new ArgumentOutOfRangeException(nameof(initialCount));
        Definition = definition;
        _count = initialCount;
    }

    public bool TryAdd(int amount)
    {
        if (amount <= 0) return false;
        _count += amount;
        Changed?.Invoke(this);
        return true;
    }

    public bool TryRemove(int amount)
    {
        if (amount <= 0) return false;
        if (_count < amount) return false;
        _count -= amount;
        Changed?.Invoke(this);
        return true;
    }

    public int RemoveUpTo(int amount)
    {
        if (amount <= 0 || _count <= 0) return 0;
        int take = Math.Min(amount, _count);
        _count -= take;
        if (take > 0) Changed?.Invoke(this);
        return take;
    }

    public void MergeFrom(SquadModel source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (source.Definition != Definition)
            throw new InvalidOperationException("Cannot merge squads of different unit types.");

        if (source._count <= 0) return;
        _count += source._count;
        source._count = 0;

        Changed?.Invoke(this);
        source.Changed?.Invoke(source);
    }

    public SquadModel Split(int amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (_count < amount) throw new InvalidOperationException("Not enough units to split.");

        _count -= amount;
        var newSquad = new SquadModel(Definition, amount);

        Changed?.Invoke(this);
        newSquad.Changed?.Invoke(newSquad);
        return newSquad;
    }

    public void Clear()
    {
        if (_count == 0) return;
        _count = 0;
        Changed?.Invoke(this);
    }
}
