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

        var grid = _context.BattleGridController;
        if (grid == null)
            return false;

        if (actor.AttackKind == AttackKind.Range || actor.AttackKind == AttackKind.Magic)
            return true;

        if (!_context.TryGetSquadController(target, out var targetController) || targetController == null)
            return false;

        if (!grid.TryGetSlotForOccupant(targetController.transform, out var targetSlot))
            return false;

        if (!grid.TryGetSlotRow(targetSlot, out var targetRow))
            return false;

        if (targetRow == BattleGridRow.Front)
            return true;

        return !HasBlockingFrontlineUnit(targetSlot);
    }

    private bool HasBlockingFrontlineUnit(Transform targetSlot)
    {
        var grid = _context.BattleGridController;
        if (grid == null)
            return false;

        if (!grid.TryGetFrontSlotFor(targetSlot, out var frontSlot))
            return false;

        if (!grid.TryGetSlotOccupant(frontSlot, out var occupant) || occupant == null)
            return false;

        var modelProvider = occupant.GetComponent<ISquadModelProvider>();
        var model = modelProvider?.GetSquadModel();

        if (model == null || model.IsEmpty)
            return false;

        return true;
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
 