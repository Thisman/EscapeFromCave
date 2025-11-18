using System;
using UnityEngine;

public sealed class BattleActionTargetResolverForAttack : IBattleActionTargetResolver
{
    private readonly BattleContext _context;

    public BattleActionTargetResolverForAttack(BattleContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public bool ResolveTarget(IReadOnlySquadModel actor, IReadOnlySquadModel target)
    {
        if (actor == null)
            throw new ArgumentNullException(nameof(actor));

        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (IsSameSide(actor.Kind, target.Kind))
            return false;

        if (actor.AttackKind == AttackKind.Range || actor.AttackKind == AttackKind.Magic)
            return true;

        var grid = _context.BattleGridController;
        if (grid == null)
            return false;

        if (!_context.TryGetSquadController(target, out var targetController) || targetController == null)
            return false;

        if (!TryGetRow(targetController.transform, out var targetRow))
            return false;

        if (targetRow == BattleGridRow.Front)
            return true;

        return !HasFrontlineUnits(target);
    }

    private bool HasFrontlineUnits(IReadOnlySquadModel target)
    {
        var units = _context.BattleUnits;

        foreach (var unit in units)
        {
            var model = unit.GetSquadModel();
            if (model == null || ReferenceEquals(model, target))
                continue;


            if (!IsSameSide(target.Kind, model.Kind))
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

    private static bool IsSameSide(UnitKind source, UnitKind target)
    {
        return source switch
        {
            UnitKind.Hero or UnitKind.Ally => target is UnitKind.Hero or UnitKind.Ally,
            UnitKind.Enemy => target == UnitKind.Enemy,
            UnitKind.Neutral => target == UnitKind.Neutral,
            _ => false,
        };
    }
}
 