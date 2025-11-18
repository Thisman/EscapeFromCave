using System;
using System.Collections.Generic;
using UnityEngine;

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

public enum BattleGridSlotHighlightMode
{
    None,
    Available,
    Unavailable,
    Active
}

public sealed class BattleGridController : MonoBehaviour
{
    [SerializeField] private Transform[] _allySlots = Array.Empty<Transform>();
    [SerializeField] private Transform[] _enemySlots = Array.Empty<Transform>();
    [SerializeField] private Color _availableHighlightColor = new(0.35f, 0.8f, 0.4f, 0.35f);
    [SerializeField] private Color _unavailableHighlightColor = new(0.85f, 0.2f, 0.2f, 0.35f);
    [SerializeField] private Color _activeHighlightColor = new(1f, 0.92f, 0.016f, 0.35f);

    private readonly Dictionary<Transform, Transform> _slotOccupants = new();
    private readonly Dictionary<Transform, Transform> _occupantSlots = new();
    private readonly Dictionary<Transform, SlotVisualState> _slotVisuals = new();
    private Transform _activeSlot;

    public Transform ActiveSlot => _activeSlot;

    private void Awake()
    {
        InitializeSlots(_allySlots);
        InitializeSlots(_enemySlots);
    }

    public void DisableSlotsCollider()
    {
        foreach (var slot in EnumerateSlots())
        {
            slot.gameObject.GetComponent<Collider2D>().enabled = false;
        }
    }

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
        unit.localRotation = Quaternion.identity;

        if (!keepWorldPosition)
        {
            unit.localPosition = Vector3.zero;
        }

        if (TryGetSlotSide(resolvedSlot, out var slotSide))
        {
            var animationController = unit.GetComponentInChildren<BattleSquadAnimationController>();
            animationController.SetFlipX(slotSide == BattleGridSlotSide.Ally);
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

    public void HighlightSlot(Transform slot, BattleGridSlotHighlightMode mode)
    {
        if (!TryResolveSlot(slot, out var resolvedSlot))
            return;

        if (!TryGetVisualState(resolvedSlot, out var visualState))
            return;

        if (mode == BattleGridSlotHighlightMode.None)
        {
            ResetSlotHighlight(resolvedSlot, keepActiveHighlight: false);
            return;
        }

        visualState.Setter?.Invoke(GetColorForMode(mode));
    }

    public void HighlightSlots(IEnumerable<Transform> slots, BattleGridSlotHighlightMode mode)
    {
        if (slots == null)
            return;

        foreach (var slot in slots)
        {
            HighlightSlot(slot, mode);
        }
    }

    public void ResetAllSlotHighlights(bool keepActiveHighlight = true)
    {
        foreach (var slot in EnumerateSlots())
        {
            if (slot == null)
                continue;

            ResetSlotHighlight(slot, keepActiveHighlight);
        }
    }

    public void ResetSlotHighlight(Transform slot, bool keepActiveHighlight = true)
    {
        if (!TryResolveSlot(slot, out var resolvedSlot))
            return;

        if (!TryGetVisualState(resolvedSlot, out var visualState))
            return;

        if (keepActiveHighlight && resolvedSlot == _activeSlot)
        {
            HighlightSlot(resolvedSlot, BattleGridSlotHighlightMode.Active);
            return;
        }

        visualState.Setter?.Invoke(visualState.OriginalColor);
    }

    public void SetActiveSlot(Transform slot)
    {
        if (!TryResolveSlot(slot, out var resolvedSlot))
            return;

        if (_activeSlot == resolvedSlot)
        {
            HighlightSlot(resolvedSlot, BattleGridSlotHighlightMode.Active);
            return;
        }

        ClearActiveSlot();

        _activeSlot = resolvedSlot;
        HighlightSlot(resolvedSlot, BattleGridSlotHighlightMode.Active);
    }

    public void ClearActiveSlot()
    {
        if (_activeSlot == null)
            return;

        ResetSlotHighlight(_activeSlot, keepActiveHighlight: false);
        _activeSlot = null;
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

    public bool TryPlaceUnits(IReadOnlyList<BattleSquadController> unitControllers)
    {
        if (unitControllers == null)
            return false;

        if (unitControllers.Count == 0)
            return true;

        var models = new List<IReadOnlySquadModel>(unitControllers.Count);
        var transforms = new Transform[unitControllers.Count];
        var originalParents = new Transform[unitControllers.Count];
        var originalPositions = new Vector3[unitControllers.Count];
        var originalRotations = new Quaternion[unitControllers.Count];
        var originalScales = new Vector3[unitControllers.Count];

        for (int i = 0; i < unitControllers.Count; i++)
        {
            var controller = unitControllers[i];
            if (controller == null)
                return false;

            var model = controller.GetSquadModel();

            var unitTransform = controller.transform;

            transforms[i] = unitTransform;
            models.Add(model);
            originalParents[i] = unitTransform.parent;
            originalPositions[i] = unitTransform.localPosition;
            originalRotations[i] = unitTransform.localRotation;
            originalScales[i] = unitTransform.localScale;
        }

        if (!TryAllocateSlots(models, out var assignments))
            return false;

        for (int i = 0; i < assignments.Length; i++)
        {
            if (TryAttachToSlot(assignments[i], transforms[i]))
                continue;

            for (int revertIndex = 0; revertIndex < i; revertIndex++)
            {
                var revertTransform = transforms[revertIndex];
                if (revertTransform == null)
                    continue;

                TryRemoveOccupant(revertTransform, out _);
                var originalParent = originalParents[revertIndex];
                revertTransform.SetParent(originalParent, false);
                revertTransform.localPosition = originalPositions[revertIndex];
                revertTransform.localRotation = originalRotations[revertIndex];
                revertTransform.localScale = originalScales[revertIndex];
            }

            return false;
        }

        return true;
    }

    private bool TryAllocateSlots(IReadOnlyList<IReadOnlySquadModel> models, out Transform[] assignments)
    {
        assignments = null;

        if (models == null)
            return false;

        int count = models.Count;
        if (count == 0)
        {
            assignments = Array.Empty<Transform>();
            return true;
        }

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

        assignments = new Transform[count];

        for (int i = 0; i < count; i++)
        {
            var model = models[i];

            Transform slot = model.Kind switch
            {
                UnitKind.Hero => AllocateSlot(allPools, allyBackSlots),
                UnitKind.Ally => AllocateSlot(allPools, allyFrontSlots, allyBackSlots, allyAnySlots),
                UnitKind.Enemy => AllocateSlot(allPools, enemySlots),
                _ => AllocateSlot(allPools, allyFrontSlots, allyBackSlots, allyAnySlots, enemySlots)
            };

            if (slot == null)
                return false;

            assignments[i] = slot;
        }

        return true;
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

            if (child.GetComponent<BattleSquadController>() != null)
                return child;

            if (child.GetComponent<SquadController>() != null)
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

        state = null;
        return false;
    }

    private Color GetColorForMode(BattleGridSlotHighlightMode mode)
    {
        return mode switch
        {
            BattleGridSlotHighlightMode.Active => _activeHighlightColor,
            BattleGridSlotHighlightMode.Available => _availableHighlightColor,
            BattleGridSlotHighlightMode.Unavailable => _unavailableHighlightColor,
            _ => _availableHighlightColor
        };
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
