using System;
using UnityEngine;

public sealed class AIActionTargetPicker : IActionTargetPicker
{
    private readonly BattleContext _context;
    private bool _disposed;
    private bool _selectionRequested;

    public event Action<BattleSquadController> OnSelect;

    public AIActionTargetPicker(BattleContext context)
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
        if (actor == null)
            return null;

        if (actor.AttackKind == AttackKind.Melee)
            return SelectFrontlineTarget(actor);

        return SelectTargetWithHighestInitiative(actor);
    }
    private BattleSquadController SelectFrontlineTarget(IReadOnlySquadModel actor)
    {
        var units = _context.BattleUnits;
        var grid = _context.BattleGridController;

        BattleSquadController backlineCandidate = null;

        foreach (var unit in units)
        {
            var model = unit.GetSquadModel();

            if (!IsOpposingType(actor.Kind, model.Kind))
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

    private BattleSquadController SelectTargetWithHighestInitiative(IReadOnlySquadModel actor)
    {
        var units = _context.BattleUnits;
        var actorType = actor.Kind;

        BattleSquadController bestTarget = null;
        float bestInitiative = float.MinValue;

        foreach (var unit in units)
        {
            var model = unit.GetSquadModel();

            if (!IsOpposingType(actor.Kind, model.Kind))
                continue;

            var initiative = actor.Initiative;

            if (initiative <= bestInitiative)
                continue;

            bestInitiative = initiative;
            bestTarget = unit;
        }

        return bestTarget;
    }

    private static bool IsOpposingType(UnitKind source, UnitKind target)
    {
        return source switch
        {
            UnitKind.Hero or UnitKind.Ally => target == UnitKind.Enemy,
            UnitKind.Enemy => target is UnitKind.Hero or UnitKind.Ally,
            _ => false,
        };
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
