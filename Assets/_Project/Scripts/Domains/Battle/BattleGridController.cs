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
