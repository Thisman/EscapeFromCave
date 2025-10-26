using System;
using UnityEngine;

public sealed class DefaultActionTargetResolver : IBattleActionTargetResolver
{
    private readonly IBattleContext _context;

    public DefaultActionTargetResolver(IBattleContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public bool ResolveTarget(IReadOnlySquadModel actor, IReadOnlySquadModel target)
    {
        _ = actor;

        if (target == null)
            throw new ArgumentNullException(nameof(target));

        var grid = _context.BattleGridController;
        if (grid == null)
            return false;

        var targetController = FindController(target);
        if (targetController == null)
            return false;

        if (!TryGetRow(targetController.transform, out var targetRow))
            return false;

        if (targetRow == BattleGridRow.Front)
            return true;

        return !HasFrontlineUnits(target);
    }

    private BattleSquadController FindController(IReadOnlySquadModel model)
    {
        if (model == null)
            return null;

        var units = _context.BattleUnits;
        if (units == null)
            return null;

        foreach (var unit in units)
        {
            if (unit?.GetSquadModel() == model)
                return unit;
        }

        return null;
    }

    private bool HasFrontlineUnits(IReadOnlySquadModel target)
    {
        var units = _context.BattleUnits;
        if (units == null)
            return false;

        var targetDefinition = target?.Definition;
        if (targetDefinition == null)
            return false;

        foreach (var unit in units)
        {
            if (unit == null)
                continue;

            var model = unit.GetSquadModel();
            if (model == null || ReferenceEquals(model, target))
                continue;

            var definition = model.Definition;
            if (definition == null)
                continue;

            if (!IsSameSide(targetDefinition.Type, definition.Type))
                continue;

            if (model.IsEmpty)
                continue;

            if (TryGetRow(unit.transform, out var row) && row == BattleGridRow.Front)
                return true;
        }

        return false;
    }

    private bool TryGetRow(Transform occupant, out BattleGridRow row)
    {
        row = default;

        var grid = _context.BattleGridController;
        if (grid == null || occupant == null)
            return false;

        if (!grid.TryGetSlotForOccupant(occupant, out var slot))
            return false;

        return grid.TryGetSlotRow(slot, out row);
    }

    private static bool IsSameSide(UnitType source, UnitType target)
    {
        return source switch
        {
            UnitType.Hero or UnitType.Ally => target is UnitType.Hero or UnitType.Ally,
            UnitType.Enemy => target == UnitType.Enemy,
            UnitType.Neutral => target == UnitType.Neutral,
            _ => false,
        };
    }
}
