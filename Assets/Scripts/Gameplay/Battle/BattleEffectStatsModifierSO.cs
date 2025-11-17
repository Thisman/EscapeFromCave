using System;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "StatModifierBattleEffect", menuName = "Gameplay/Battle Effects/Stat Modifier Effect")]
public sealed class BattleEffectStatsModifierSO : BattleEffectSO
{
    public BattleSquadStatModifier[] StatsModifier = Array.Empty<BattleSquadStatModifier>();

    public override Task Apply(BattleContext ctx, BattleSquadEffectsController target)
    {
        if (!TryResolveController(target, out var controller))
            return Task.CompletedTask;

        return controller.ApplyStatModifiers(this, StatsModifier ?? Array.Empty<BattleSquadStatModifier>());
    }

    public override void OnRemove(BattleContext ctx, BattleSquadEffectsController target)
    {
        if (!TryResolveController(target, out var controller))
            return;

        controller.RemoveStatModifiers(this);
    }

    private static bool TryResolveController(BattleSquadEffectsController target, out BattleSquadController controller)
    {
        controller = null;

        if (target == null)
            return false;

        controller = target.GetComponent<BattleSquadController>() ?? target.GetComponentInParent<BattleSquadController>();
        return controller != null;
    }
}
