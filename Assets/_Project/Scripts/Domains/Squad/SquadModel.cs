using System;
using UnityEngine;

[Serializable]
public class SquadModel : IReadOnlySquadModel
{
    [SerializeField] private UnitDefinitionSO _unitDefinition;
    [SerializeField, Min(0)] private int _count;

    public UnitDefinitionSO Definition => _unitDefinition;

    public int Count => _count;

    public bool IsEmpty => _count <= 0;

    public event Action<IReadOnlySquadModel> Changed;

    public SquadModel(UnitDefinitionSO definition, int initialCount = 0)
    {
        if (!definition) throw new ArgumentNullException(nameof(definition));
        if (initialCount < 0) throw new ArgumentOutOfRangeException(nameof(initialCount));

        _unitDefinition = definition;
        _count = initialCount;
    }

    public bool TryAdd(int amount)
    {
        if (amount <= 0) return false;

        _count += amount;
        NotifyChanged();
        return true;
    }

    public void Clear()
    {
        if (_count == 0) return;
        _count = 0;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        Changed?.Invoke(this);
    }
}
