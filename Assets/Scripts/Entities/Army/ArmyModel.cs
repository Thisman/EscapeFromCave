using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ArmyModel : IReadOnlyArmyModel
{
    [SerializeField, Min(1)] private int _maxSlots = 3;
    [SerializeField] private List<SquadModel> _slots;

    public event Action<IReadOnlyArmyModel> Changed;

    private readonly IReadOnlyList<IReadOnlySquadModel> _squads;

    public ArmyModel(int maxSlots = 3)
    {
        _maxSlots = Math.Max(1, maxSlots);
        _slots = new List<SquadModel>(_maxSlots);
        _squads = new ReadOnlySquadList(_slots);
        for (int i = 0; i < _maxSlots; i++)
            _slots.Add(null);
    }

    public IReadOnlyList<IReadOnlySquadModel> GetSquads() => _squads;

    public bool SetSlot(int index, SquadModel squad)
    {
        if (!IsValidSlotIndex(index)) return false;
        var normalized = (squad != null && squad.IsEmpty) ? null : squad;
        bool changed = AssignSlot(index, normalized);
        if (changed)
            Changed?.Invoke(this);
        return true;
    }

    public bool ClearSlot(int index)
    {
        if (!IsValidSlotIndex(index)) return false;
        bool changed = AssignSlot(index, null);
        if (changed)
            Changed?.Invoke(this);
        return true;
    }

    public bool TryAddSquad(UnitSO def, int amount)
    {
        if (!def || amount <= 0) return false;

        int emptyIndex = FindEmptySlot();
        if (emptyIndex >= 0)
        {
            var newSquad = new SquadModel(def, amount);
            AssignSlot(emptyIndex, newSquad);
            Changed?.Invoke(this);
            return true;
        }
        return false;
    }

    private int FindEmptySlot()
    {
        for (int i = 0; i < _slots.Count; i++)
            if (_slots[i] == null) return i;
        return -1;
    }

    private bool IsValidSlotIndex(int i) => i >= 0 && i < _slots.Count;

    private bool AssignSlot(int index, SquadModel squad)
    {
        var current = _slots[index];
        if (ReferenceEquals(current, squad))
            return false;

        if (current != null)
            current.Changed -= OnSquadChanged;

        _slots[index] = squad;

        if (squad != null)
            squad.Changed += OnSquadChanged;

        return true;
    }

    private void OnSquadChanged(IReadOnlySquadModel squad)
    {
        Changed?.Invoke(this);
    }

    private sealed class ReadOnlySquadList : IReadOnlyList<IReadOnlySquadModel>
    {
        private readonly List<SquadModel> _source;

        public ReadOnlySquadList(List<SquadModel> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public IReadOnlySquadModel this[int index] => _source[index];

        public int Count => _source.Count;

        public IEnumerator<IReadOnlySquadModel> GetEnumerator()
        {
            for (int i = 0; i < _source.Count; i++)
                yield return _source[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
