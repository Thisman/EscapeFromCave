using System;
using UnityEngine;

public sealed class AIActionTargetPicker : IActionTargetPicker
{
    private readonly IBattleContext _context;
    private bool _disposed;
    private bool _selectionRequested;

    public event Action<BattleSquadController> OnSelect;

    public AIActionTargetPicker(IBattleContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void RequestTarget()
    {
        if (_disposed || _selectionRequested)
            return;

        _selectionRequested = true;

        var actorModel = _context.ActiveUnit;
        if (actorModel == null)
        {
            OnSelect?.Invoke(null);
            return;
        }

        var target = SelectTarget(actorModel);
        OnSelect?.Invoke(target);
    }

    private BattleSquadController SelectTarget(IReadOnlySquadModel actor)
    {
        var units = _context.BattleUnits;
        if (units == null)
            return null;

        var grid = _context.BattleGridController;
        if (grid == null)
            return null;

        var actorDefinition = actor?.Definition;
        var actorType = actorDefinition?.Type ?? UnitType.Enemy;

        BattleSquadController backlineCandidate = null;

        foreach (var unit in units)
        {
            if (unit == null)
                continue;

            var model = unit.GetSquadModel();
            if (model == null || model.IsEmpty)
                continue;

            var definition = model.Definition;
            if (definition == null)
                continue;

            if (!IsOpposingType(actorType, definition.Type))
                continue;

            if (!grid.TryGetSlotForOccupant(unit.transform, out var slot))
                continue;

            if (!grid.TryGetSlotRow(slot, out var row))
                continue;

            if (row == BattleGridRow.Front)
                return unit;

            backlineCandidate ??= unit;
        }

        return backlineCandidate;
    }

    private static bool IsOpposingType(UnitType source, UnitType target)
    {
        return source switch
        {
            UnitType.Hero or UnitType.Ally => target == UnitType.Enemy,
            UnitType.Enemy => target is UnitType.Hero or UnitType.Ally,
            _ => false,
        };
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
