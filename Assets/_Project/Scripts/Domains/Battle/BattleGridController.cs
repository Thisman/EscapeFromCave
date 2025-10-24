using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BattleGridSlotSide
{
    Ally,
    Enemy
}

public enum BattleGridRow
{
    Back,
    Front
}

public sealed class BattleGridController : MonoBehaviour
{
    [SerializeField] private Transform[] _allySlots = Array.Empty<Transform>();
    [SerializeField] private Transform[] _enemySlots = Array.Empty<Transform>();

    private readonly Dictionary<Transform, Transform> _slotOccupants = new();
    private readonly Dictionary<Transform, Transform> _occupantSlots = new();
    private readonly Dictionary<Transform, SlotVisualState> _slotVisuals = new();

    private void Awake()
    {
        InitializeSlots(_allySlots);
        InitializeSlots(_enemySlots);
    }

    public IReadOnlyList<Transform> AllySlots => _allySlots;

    public IReadOnlyList<Transform> EnemySlots => _enemySlots;

    public bool TryAttachToSlot(Transform slot, Transform unit, bool keepWorldPosition = false)
    {
        if (!TryResolveSlot(slot, out var resolvedSlot) || unit == null)
            return false;

        if (!IsSlotEmpty(resolvedSlot))
            return false;

        if (_occupantSlots.TryGetValue(unit, out var previousSlot) && previousSlot != resolvedSlot)
        {
            _slotOccupants[previousSlot] = null;
        }

        unit.SetParent(resolvedSlot, keepWorldPosition);

        if (!keepWorldPosition)
        {
            unit.localPosition = Vector3.zero;
            unit.localRotation = Quaternion.identity;
        }

        _slotOccupants[resolvedSlot] = unit;
        _occupantSlots[unit] = resolvedSlot;
        return true;
    }

    public bool IsSlotEmpty(Transform slot)
    {
        if (!TryResolveSlot(slot, out var resolvedSlot))
            return false;

        if (_slotOccupants.TryGetValue(resolvedSlot, out var occupant) && occupant != null)
        {
            return false;
        }

        Transform detectedOccupant = DetectOccupant(resolvedSlot);
        if (detectedOccupant != null)
        {
            _slotOccupants[resolvedSlot] = detectedOccupant;
            _occupantSlots[detectedOccupant] = resolvedSlot;
            return false;
        }

        return true;
    }

    public bool IsSlotOccupied(Transform slot)
    {
        return !IsSlotEmpty(slot);
    }

    public void HighlightSlot(Transform slot, Color color)
    {
        if (!TryResolveSlot(slot, out var resolvedSlot))
            return;

        if (!TryGetVisualState(resolvedSlot, out var visualState))
            return;

        visualState.Setter?.Invoke(color);
    }

    public void ResetSlotHighlight(Transform slot)
    {
        if (!TryResolveSlot(slot, out var resolvedSlot))
            return;

        if (!TryGetVisualState(resolvedSlot, out var visualState))
            return;

        visualState.Setter?.Invoke(visualState.OriginalColor);
    }

    public bool TryGetSlotSide(Transform slot, out BattleGridSlotSide side)
    {
        if (!TryResolveSlot(slot, out var resolvedSlot))
        {
            side = default;
            return false;
        }

        if (Array.IndexOf(_allySlots, resolvedSlot) >= 0)
        {
            side = BattleGridSlotSide.Ally;
            return true;
        }

        if (Array.IndexOf(_enemySlots, resolvedSlot) >= 0)
        {
            side = BattleGridSlotSide.Enemy;
            return true;
        }

        side = default;
        return false;
    }

    public bool TryGetSlotRow(Transform slot, out BattleGridRow row)
    {
        if (!TryResolveSlot(slot, out var resolvedSlot))
        {
            row = default;
            return false;
        }

        int index = Array.IndexOf(_allySlots, resolvedSlot);
        if (index >= 0)
        {
            row = index < 3 ? BattleGridRow.Back : BattleGridRow.Front;
            return true;
        }

        index = Array.IndexOf(_enemySlots, resolvedSlot);
        if (index >= 0)
        {
            row = index < 3 ? BattleGridRow.Back : BattleGridRow.Front;
            return true;
        }

        row = default;
        return false;
    }

    public bool TryGetSlotForOccupant(Transform occupant, out Transform slot)
    {
        if (occupant == null)
        {
            slot = null;
            return false;
        }

        if (_occupantSlots.TryGetValue(occupant, out slot) && slot != null)
            return true;

        foreach (var registeredSlot in EnumerateSlots())
        {
            if (registeredSlot == null)
                continue;

            for (Transform current = occupant; current != null; current = current.parent)
            {
                if (current == registeredSlot)
                {
                    EnsureSlotRegistered(registeredSlot);
                    if (_slotOccupants.TryGetValue(registeredSlot, out var trackedOccupant) && trackedOccupant == occupant)
                    {
                        slot = registeredSlot;
                        return true;
                    }
                }
            }
        }

        slot = null;
        return false;
    }

    public bool TryRemoveOccupant(Transform occupant, out Transform slot)
    {
        if (occupant == null)
        {
            slot = null;
            return false;
        }

        if (_occupantSlots.TryGetValue(occupant, out slot))
        {
            _occupantSlots.Remove(occupant);
            if (slot != null && _slotOccupants.TryGetValue(slot, out var storedOccupant) && storedOccupant == occupant)
            {
                _slotOccupants[slot] = null;
            }
            return true;
        }

        if (TryGetSlotForOccupant(occupant, out slot))
        {
            if (slot != null && _slotOccupants.TryGetValue(slot, out var storedOccupant) && storedOccupant == occupant)
            {
                _slotOccupants[slot] = null;
            }
            return true;
        }

        slot = null;
        return false;
    }

    public bool TryPlaceUnit(IReadOnlyBattleUnitModel[] unitModels, Transform[] units)
    {
        if (unitModels == null || units == null)
            return false;

        if (unitModels.Length != units.Length)
            return false;

        if (unitModels.Length == 0)
            return true;

        var allyFrontSlots = new List<Transform>();
        var allyBackSlots = new List<Transform>();
        var allyAnySlots = new List<Transform>();
        var enemySlots = new List<Transform>();

        CollectAvailableSlots(allyFrontSlots, allyBackSlots, allyAnySlots, enemySlots);

        var allPools = new List<List<Transform>>
        {
            allyFrontSlots,
            allyBackSlots,
            allyAnySlots,
            enemySlots
        };

        var assignments = new Transform[unitModels.Length];
        var originalParents = new Transform[units.Length];
        var originalLocalPositions = new Vector3[units.Length];
        var originalLocalRotations = new Quaternion[units.Length];
        var originalLocalScales = new Vector3[units.Length];
        var originalSlots = new Transform[units.Length];

        for (int i = 0; i < unitModels.Length; i++)
        {
            var model = unitModels[i];
            var unit = units[i];

            if (model == null || model.Definition == null || unit == null)
                return false;

            originalParents[i] = unit.parent;
            originalLocalPositions[i] = unit.localPosition;
            originalLocalRotations[i] = unit.localRotation;
            originalLocalScales[i] = unit.localScale;

            if (TryGetSlotForOccupant(unit, out var previousSlot))
                originalSlots[i] = previousSlot;

            Transform slot = null;

            switch (model.Definition.Type)
            {
                case UnitType.Hero:
                    slot = AllocateSlot(allPools, allyBackSlots);
                    break;
                case UnitType.Ally:
                    slot = AllocateSlot(allPools, allyFrontSlots, allyBackSlots, allyAnySlots);
                    break;
                case UnitType.Enemy:
                    slot = AllocateSlot(allPools, enemySlots);
                    break;
                default:
                    slot = AllocateSlot(allPools, allyFrontSlots, allyBackSlots, allyAnySlots, enemySlots);
                    break;
            }

            if (slot == null)
                return false;

            assignments[i] = slot;
        }

        for (int i = 0; i < assignments.Length; i++)
        {
            if (TryAttachToSlot(assignments[i], units[i]))
                continue;

            for (int revertIndex = 0; revertIndex < i; revertIndex++)
            {
                var revertUnit = units[revertIndex];
                if (revertUnit == null)
                    continue;

                TryRemoveOccupant(revertUnit, out _);

                var originalSlot = originalSlots[revertIndex];
                if (originalSlot != null)
                {
                    TryAttachToSlot(originalSlot, revertUnit);
                    continue;
                }

                revertUnit.SetParent(originalParents[revertIndex], false);
                revertUnit.localPosition = originalLocalPositions[revertIndex];
                revertUnit.localRotation = originalLocalRotations[revertIndex];
                revertUnit.localScale = originalLocalScales[revertIndex];
            }

            return false;
        }

        return true;
    }

    public bool TryResolveSlot(Transform candidate, out Transform slot)
    {
        slot = null;
        if (candidate == null)
            return false;

        foreach (var registeredSlot in EnumerateSlots())
        {
            if (registeredSlot == null)
                continue;

            if (IsSameOrParent(candidate, registeredSlot))
            {
                slot = registeredSlot;
                EnsureSlotRegistered(slot);
                return true;
            }
        }

        return false;
    }

    private void CollectAvailableSlots(List<Transform> allyFrontSlots, List<Transform> allyBackSlots, List<Transform> allyAnySlots, List<Transform> enemySlots)
    {
        if (allyFrontSlots == null || allyBackSlots == null || allyAnySlots == null || enemySlots == null)
            return;

        FillAllySlots(allyFrontSlots, allyBackSlots, allyAnySlots);
        FillEnemySlots(enemySlots);
    }

    private void FillAllySlots(List<Transform> allyFrontSlots, List<Transform> allyBackSlots, List<Transform> allyAnySlots)
    {
        if (_allySlots == null)
            return;

        foreach (var slot in _allySlots)
        {
            if (slot == null)
                continue;

            if (!IsSlotEmpty(slot))
                continue;

            allyAnySlots.Add(slot);

            if (!TryGetSlotRow(slot, out var row))
                continue;

            if (row == BattleGridRow.Front)
            {
                allyFrontSlots.Add(slot);
            }
            else if (row == BattleGridRow.Back)
            {
                allyBackSlots.Add(slot);
            }
        }
    }

    private void FillEnemySlots(List<Transform> enemySlots)
    {
        if (_enemySlots == null)
            return;

        foreach (var slot in _enemySlots)
        {
            if (slot == null)
                continue;

            if (!IsSlotEmpty(slot))
                continue;

            enemySlots.Add(slot);
        }
    }

    private Transform AllocateSlot(List<List<Transform>> allPools, params List<Transform>[] priorityOrder)
    {
        if (priorityOrder == null || priorityOrder.Length == 0)
            return null;

        foreach (var pool in priorityOrder)
        {
            if (TryDrawFromPool(pool, allPools, out var slot))
                return slot;
        }

        return null;
    }

    private bool TryDrawFromPool(List<Transform> pool, List<List<Transform>> allPools, out Transform slot)
    {
        slot = null;

        if (pool == null || pool.Count == 0)
            return false;

        int index = UnityEngine.Random.Range(0, pool.Count);
        slot = pool[index];
        pool.RemoveAt(index);

        if (allPools != null)
        {
            foreach (var otherPool in allPools)
            {
                if (otherPool == null || otherPool == pool)
                    continue;

                otherPool.Remove(slot);
            }
        }

        return true;
    }

    private void InitializeSlots(IEnumerable<Transform> slots)
    {
        if (slots == null)
            return;

        foreach (var slot in slots)
        {
            if (slot == null)
                continue;

            EnsureSlotRegistered(slot);
        }
    }

    private void EnsureSlotRegistered(Transform slot)
    {
        if (slot == null || _slotOccupants.ContainsKey(slot))
            return;

        var occupant = DetectOccupant(slot);
        _slotOccupants[slot] = occupant;
        if (occupant != null)
        {
            _occupantSlots[occupant] = slot;
        }
    }

    private Transform DetectOccupant(Transform slot)
    {
        if (slot == null)
            return null;

        for (int i = 0; i < slot.childCount; i++)
        {
            var child = slot.GetChild(i);
            if (child == null)
                continue;

            if (child.GetComponent<BattleUnitController>() != null)
                return child;

            if (child.GetComponent<UnitController>() != null)
                return child;
        }

        return null;
    }

    private IEnumerable<Transform> EnumerateSlots()
    {
        foreach (var slot in _allySlots)
            yield return slot;

        foreach (var slot in _enemySlots)
            yield return slot;
    }

    private bool TryGetVisualState(Transform slot, out SlotVisualState state)
    {
        if (slot == null)
        {
            state = null;
            return false;
        }

        if (_slotVisuals.TryGetValue(slot, out state))
            return true;

        if (TryCreateVisualState(slot, out state))
        {
            _slotVisuals[slot] = state;
            return true;
        }

        state = null;
        return false;
    }

    private bool TryCreateVisualState(Transform slot, out SlotVisualState state)
    {
        if (slot.TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            state = new SlotVisualState(spriteRenderer.color, c => spriteRenderer.color = c);
            return true;
        }

        if (slot.TryGetComponent(out Graphic graphic))
        {
            state = new SlotVisualState(graphic.color, c => graphic.color = c);
            return true;
        }

        if (slot.TryGetComponent(out Renderer renderer) && renderer.material != null)
        {
            state = new SlotVisualState(renderer.material.color, c => renderer.material.color = c);
            return true;
        }

        state = null;
        return false;
    }

    private static bool IsSameOrParent(Transform candidate, Transform potentialChild)
    {
        if (candidate == null || potentialChild == null)
            return false;

        for (Transform current = candidate; current != null; current = current.parent)
        {
            if (current == potentialChild)
                return true;
        }

        for (Transform current = potentialChild; current != null; current = current.parent)
        {
            if (current == candidate)
                return true;
        }

        return false;
    }

    private sealed class SlotVisualState
    {
        public readonly Color OriginalColor;
        public readonly Action<Color> Setter;

        public SlotVisualState(Color originalColor, Action<Color> setter)
        {
            OriginalColor = originalColor;
            Setter = setter;
        }
    }
}
