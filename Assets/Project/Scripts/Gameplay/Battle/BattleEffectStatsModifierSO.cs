using System;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "StatModifierBattleEffect", menuName = "Gameplay/Battle Effects/Stat Modifier Effect")]
public sealed class BattleEffectStatsModifierSO : BattleEffectSO
{
    public BattleStatModifier[] StatsModifier = Array.Empty<BattleStatModifier>();

    public override Task Apply(BattleContext ctx, BattleSquadEffectsController target)
    {
        if (!TryResolveModel(target, out var model))
            return Task.CompletedTask;

        model.SetStatModifiers(this, StatsModifier ?? Array.Empty<BattleStatModifier>());
        return Task.CompletedTask;
    }

    public override void OnRemove(BattleContext ctx, BattleSquadEffectsController target)
    {
        if (!TryResolveModel(target, out var model))
            return;

        model.RemoveStatModifiers(this);
    }

    private bool TryResolveModel(BattleSquadEffectsController target, out BattleSquadModel model)
    {
        BattleSquadController squadController = target.GetComponent<BattleSquadController>() ?? target.GetComponentInParent<BattleSquadController>();

        model = squadController.GetSquadModel() as BattleSquadModel;

        return true;
    }
}
