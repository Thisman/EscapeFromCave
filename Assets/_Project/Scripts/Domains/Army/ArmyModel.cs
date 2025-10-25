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

    private IReadOnlyList<IReadOnlySquadModel> _readOnlySlots;

    public int MaxSlots => _maxSlots;

    public IReadOnlyList<IReadOnlySquadModel> Slots => _readOnlySlots;

    public ArmyModel(int maxSlots = 3)
    {
        _maxSlots = Math.Max(1, maxSlots);
        _slots = new List<SquadModel>(_maxSlots);
        _readOnlySlots = new ReadOnlySquadList(_slots);
        for (int i = 0; i < _maxSlots; i++)
            _slots.Add(null);
    }

    public IReadOnlySquadModel GetSlot(int index) => IsValidSlotIndex(index) ? _slots[index] : null;

    public IReadOnlyList<IReadOnlySquadModel> GetAllSlots() => _readOnlySlots;

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

    public bool SwapSlots(int a, int b)
    {
        if (!IsValidSlotIndex(a) || !IsValidSlotIndex(b) || a == b) return false;
        var slotA = _slots[a];
        var slotB = _slots[b];
        bool changed = AssignSlot(a, slotB);
        changed |= AssignSlot(b, slotA);
        if (changed)
            Changed?.Invoke(this);
        return true;
    }

    public bool TryAddUnits(UnitDefinitionSO def, int amount)
    {
        if (!def || amount <= 0) return false;

        var squad = FindFirstSquad(def);
        if (squad != null)
        {
            var ok = squad.TryAdd(amount);
            return ok;
        }

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

    public int RemoveUnits(UnitDefinitionSO def, int amount)
    {
        if (!def || amount <= 0) return 0;
        int left = amount;
        bool structuralChange = false;
        for (int i = 0; i < _slots.Count && left > 0; i++)
        {
            var s = _slots[i];
            if (s == null || s.UnitDefinition != def || s.IsEmpty) continue;

            int took = s.RemoveUpTo(left);
            left -= took;
            if (s.IsEmpty)
                structuralChange |= AssignSlot(i, null);
        }
        int removed = amount - left;
        if (removed > 0 && structuralChange)
            Changed?.Invoke(this);
        return removed;
    }

    public int GetTotalUnits(UnitDefinitionSO def)
    {
        if (!def) return 0;
        int sum = 0;
        for (int i = 0; i < _slots.Count; i++)
            if (_slots[i] != null && _slots[i].UnitDefinition == def)
                sum += _slots[i].Count;
        return sum;
    }

    public bool TrySplit(UnitDefinitionSO def, int amount)
    {
        if (!def || amount <= 0) return false;
        var s = FindFirstSquad(def);
        if (s == null || s.Count <= amount) return false;

        int empty = FindEmptySlot();
        if (empty < 0) return false;

        var newSquad = s.Split(amount);
        AssignSlot(empty, newSquad);
        Changed?.Invoke(this);
        return true;
    }

    public bool TryMerge(int fromIndex, int toIndex)
    {
        if (!IsValidSlotIndex(fromIndex) || !IsValidSlotIndex(toIndex) || fromIndex == toIndex) return false;
        var from = _slots[fromIndex];
        var to = _slots[toIndex];
        if (from == null) return false;

        if (to == null)
        {
            AssignSlot(toIndex, from);
            AssignSlot(fromIndex, null);
            Changed?.Invoke(this);
            return true;
        }

        if (from.UnitDefinition != to.UnitDefinition) return false;

        to.MergeFrom(from);
        AssignSlot(fromIndex, null);
        Changed?.Invoke(this);
        return true;
    }

    private SquadModel FindFirstSquad(UnitDefinitionSO def)
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            var s = _slots[i];
            if (s != null && s.UnitDefinition == def) return s;
        }
        return null;
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
