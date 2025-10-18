using System;
using System.Collections.Generic;

public class ArmyModel
{
    private readonly int _maxSlots;
    private readonly List<SquadModel> _slots;

    public int MaxSlots => _maxSlots;
    public IReadOnlyList<SquadModel> Slots => _slots;

    public event Action Changed;

    public ArmyModel(int maxSlots = 3)
    {
        _maxSlots = Math.Max(1, maxSlots);
        _slots = new List<SquadModel>(_maxSlots);
        for (int i = 0; i < _maxSlots; i++)
            _slots.Add(null);
    }

    public SquadModel GetSlot(int index) => IsValid(index) ? _slots[index] : null;

    public SquadModel[] GetAllSlots() => _slots.ToArray();

    public bool SetSlot(int index, SquadModel squad)
    {
        if (!IsValid(index)) return false;
        _slots[index] = (squad != null && squad.IsEmpty) ? null : squad;
        Changed?.Invoke();
        return true;
    }

    public bool ClearSlot(int index)
    {
        if (!IsValid(index)) return false;
        _slots[index] = null;
        Changed?.Invoke();
        return true;
    }

    public bool SwapSlots(int a, int b)
    {
        if (!IsValid(a) || !IsValid(b) || a == b) return false;
        (_slots[a], _slots[b]) = (_slots[b], _slots[a]);
        Changed?.Invoke();
        return true;
    }

    public bool TryAddUnits(IUnitDefinition def, int amount)
    {
        if (def == null || amount <= 0) return false;

        var squad = FindFirstSquad(def);
        if (squad != null)
        {
            var ok = squad.TryAdd(amount);
            if (ok) Changed?.Invoke();
            return ok;
        }

        int emptyIndex = FindEmptySlot();
        if (emptyIndex >= 0)
        {
            var newSquad = new SquadModel(def, amount);
            _slots[emptyIndex] = newSquad;
            Changed?.Invoke();
            return true;
        }
        return false;
    }

    public int RemoveUnits(IUnitDefinition def, int amount)
    {
        if (def == null || amount <= 0) return 0;
        int left = amount;
        for (int i = 0; i < _slots.Count && left > 0; i++)
        {
            var s = _slots[i];
            if (s == null || !ReferenceEquals(s.UnitDefinition, def) || s.IsEmpty) continue;

            int took = s.RemoveUpTo(left);
            left -= took;
            if (s.IsEmpty) _slots[i] = null;
        }
        int removed = amount - left;
        if (removed > 0) Changed?.Invoke();
        return removed;
    }

    public int GetTotalUnits(IUnitDefinition def)
    {
        if (def == null) return 0;
        int sum = 0;
        for (int i = 0; i < _slots.Count; i++)
            if (_slots[i] != null && ReferenceEquals(_slots[i].UnitDefinition, def))
                sum += _slots[i].Count;
        return sum;
    }

    public bool TrySplit(IUnitDefinition def, int amount)
    {
        if (def == null || amount <= 0) return false;
        var s = FindFirstSquad(def);
        if (s == null || s.Count <= amount) return false;

        int empty = FindEmptySlot();
        if (empty < 0) return false;

        var newSquad = s.Split(amount);
        _slots[empty] = newSquad;
        Changed?.Invoke();
        return true;
    }

    public bool TryMerge(int fromIndex, int toIndex)
    {
        if (!IsValid(fromIndex) || !IsValid(toIndex) || fromIndex == toIndex) return false;
        var from = _slots[fromIndex];
        var to = _slots[toIndex];
        if (from == null) return false;

        if (to == null)
        {
            _slots[toIndex] = from;
            _slots[fromIndex] = null;
            Changed?.Invoke();
            return true;
        }

        if (!ReferenceEquals(from.UnitDefinition, to.UnitDefinition)) return false;

        to.MergeFrom(from);
        _slots[fromIndex] = null;
        Changed?.Invoke();
        return true;
    }

    private SquadModel FindFirstSquad(IUnitDefinition def)
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            var s = _slots[i];
            if (s != null && ReferenceEquals(s.UnitDefinition, def)) return s;
        }
        return null;
    }

    private int FindEmptySlot()
    {
        for (int i = 0; i < _slots.Count; i++)
            if (_slots[i] == null) return i;
        return -1;
    }

    private bool IsValid(int i) => i >= 0 && i < _slots.Count;
}
