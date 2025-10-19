using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleGridModel
{
    public const int SlotsPerSide = 6;

    private readonly GameObject[] _friendlySlots;
    private readonly GameObject[] _enemySlots;

    private readonly GameObject[] _friendlyUnits;
    private readonly GameObject[] _enemyUnits;

    public BattleGridModel(IReadOnlyList<GameObject> friendlySlots, IReadOnlyList<GameObject> enemySlots)
    {
        _friendlySlots = CopySlots(friendlySlots, nameof(friendlySlots));
        _enemySlots = CopySlots(enemySlots, nameof(enemySlots));

        _friendlyUnits = new GameObject[SlotsPerSide];
        _enemyUnits = new GameObject[SlotsPerSide];
    }

    public GameObject GetFriendlyUnit(int index) => GetUnit(_friendlyUnits, index);

    public GameObject GetEnemyUnit(int index) => GetUnit(_enemyUnits, index);

    public GameObject GetFriendlySlot(int index) => GetUnit(_friendlySlots, index);

    public GameObject GetEnemySlot(int index) => GetUnit(_enemySlots, index);

    public bool TryPlaceFriendly(int index, GameObject unit, bool keepWorldPosition = false)
    {
        return TryPlaceUnit(_friendlySlots, _friendlyUnits, _enemyUnits, index, unit, keepWorldPosition);
    }

    public bool TryPlaceEnemy(int index, GameObject unit, bool keepWorldPosition = false)
    {
        return TryPlaceUnit(_enemySlots, _enemyUnits, _friendlyUnits, index, unit, keepWorldPosition);
    }

    public bool TryClearFriendly(int index, bool keepWorldPosition = false)
    {
        return TryClearSlot(_friendlySlots, _friendlyUnits, index, keepWorldPosition);
    }

    public bool TryClearEnemy(int index, bool keepWorldPosition = false)
    {
        return TryClearSlot(_enemySlots, _enemyUnits, index, keepWorldPosition);
    }

    public bool TryMoveFriendly(int fromIndex, int toIndex, bool allowSwap = false, bool keepWorldPosition = false)
    {
        return TryMoveUnit(_friendlySlots, _friendlyUnits, fromIndex, toIndex, allowSwap, keepWorldPosition);
    }

    public bool TryMoveEnemy(int fromIndex, int toIndex, bool allowSwap = false, bool keepWorldPosition = false)
    {
        return TryMoveUnit(_enemySlots, _enemyUnits, fromIndex, toIndex, allowSwap, keepWorldPosition);
    }

    public bool TrySwapFriendly(int indexA, int indexB, bool keepWorldPosition = false)
    {
        return TrySwapUnits(_friendlySlots, _friendlyUnits, indexA, indexB, keepWorldPosition);
    }

    public bool TrySwapEnemy(int indexA, int indexB, bool keepWorldPosition = false)
    {
        return TrySwapUnits(_enemySlots, _enemyUnits, indexA, indexB, keepWorldPosition);
    }

    private static GameObject[] CopySlots(IReadOnlyList<GameObject> slots, string parameterName)
    {
        if (slots == null)
            throw new ArgumentNullException(parameterName);
        if (slots.Count != SlotsPerSide)
            throw new ArgumentException($"Exactly {SlotsPerSide} slots are required.", parameterName);

        var result = new GameObject[SlotsPerSide];
        for (int i = 0; i < SlotsPerSide; i++)
        {
            if (!slots[i])
                throw new ArgumentException($"Slot at index {i} is null.", parameterName);
            result[i] = slots[i];
        }
        return result;
    }

    private static GameObject GetUnit(IReadOnlyList<GameObject> units, int index)
    {
        return IsValidIndex(index) ? units[index] : null;
    }

    private static bool TryPlaceUnit(GameObject[] slots, GameObject[] units, GameObject[] oppositeUnits, int index, GameObject unit, bool keepWorldPosition)
    {
        if (!IsValidIndex(index) || unit == null)
            return false;
        if (Array.IndexOf(oppositeUnits, unit) >= 0)
            return false;

        var slot = slots[index];
        if (slot == null)
            return false;

        int currentIndex = Array.IndexOf(units, unit);
        if (currentIndex == index)
            return true;

        if (units[index] != null && units[index] != unit)
            return false;

        if (currentIndex >= 0)
            units[currentIndex] = null;

        units[index] = unit;
        AttachToSlot(unit, slot.transform, keepWorldPosition);
        return true;
    }

    private static bool TryClearSlot(GameObject[] slots, GameObject[] units, int index, bool keepWorldPosition)
    {
        if (!IsValidIndex(index))
            return false;
        var unit = units[index];
        if (unit == null)
            return false;

        units[index] = null;
        AttachToSlot(unit, null, keepWorldPosition);
        return true;
    }

    private static bool TryMoveUnit(GameObject[] slots, GameObject[] units, int fromIndex, int toIndex, bool allowSwap, bool keepWorldPosition)
    {
        if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex)
            return false;

        var unit = units[fromIndex];
        if (unit == null)
            return false;

        if (units[toIndex] != null)
        {
            if (!allowSwap)
                return false;
            return TrySwapUnits(slots, units, fromIndex, toIndex, keepWorldPosition);
        }

        units[fromIndex] = null;
        units[toIndex] = unit;
        AttachToSlot(unit, slots[toIndex].transform, keepWorldPosition);
        return true;
    }

    private static bool TrySwapUnits(GameObject[] slots, GameObject[] units, int indexA, int indexB, bool keepWorldPosition)
    {
        if (!IsValidIndex(indexA) || !IsValidIndex(indexB) || indexA == indexB)
            return false;

        var slotA = slots[indexA];
        var slotB = slots[indexB];
        if (slotA == null || slotB == null)
            return false;

        var unitA = units[indexA];
        var unitB = units[indexB];

        units[indexA] = unitB;
        units[indexB] = unitA;

        if (unitA != null)
            AttachToSlot(unitA, slotB.transform, keepWorldPosition);
        if (unitB != null)
            AttachToSlot(unitB, slotA.transform, keepWorldPosition);

        return true;
    }

    private static void AttachToSlot(GameObject unit, Transform parent, bool keepWorldPosition)
    {
        if (unit == null)
            return;

        unit.transform.SetParent(parent, keepWorldPosition);
        if (parent != null && !keepWorldPosition)
        {
            unit.transform.localPosition = Vector3.zero;
            unit.transform.localRotation = Quaternion.identity;
        }
    }

    private static bool IsValidIndex(int index) => index >= 0 && index < SlotsPerSide;
}
