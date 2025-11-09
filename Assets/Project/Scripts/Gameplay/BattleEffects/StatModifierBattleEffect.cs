using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StatModifierBattleEffect", menuName = "Gameplay/Battle Effects/Stat Modifier Effect")]
public sealed class StatModifierBattleEffect : BattleEffectDefinitionSO
{
    [SerializeField]
    public BattleStatModifier[] _statModifiers = Array.Empty<BattleStatModifier>();

    public override void Apply(BattleContext ctx, BattleSquadEffectsController target)
    {
        if (!TryResolveModel(target, out var model))
            return;

        model.SetStatModifiers(this, _statModifiers ?? Array.Empty<BattleStatModifier>());
    }

    public override void OnRemove(BattleContext ctx, BattleSquadEffectsController target)
    {
        if (target == null)
            return;

        if (!TryResolveModel(target, out var model))
            return;

        model.RemoveStatModifiers(this);
    }

    private bool TryResolveModel(BattleSquadEffectsController target, out BattleSquadModel model)
    {
        model = null;

        if (target == null)
        {
            GameLogger.Warn($"{nameof(StatModifierBattleEffect)} '{name}' received a null target.");
            return false;
        }

        var squadController = target.GetComponent<BattleSquadController>() ?? target.GetComponentInParent<BattleSquadController>();
        if (squadController == null)
        {
            GameLogger.Warn($"{nameof(StatModifierBattleEffect)} '{name}' could not find a {nameof(BattleSquadController)} on '{target.name}'.");
            return false;
        }

        model = squadController.GetSquadModel() as BattleSquadModel;
        if (model == null)
        {
            GameLogger.Warn($"{nameof(StatModifierBattleEffect)} '{name}' requires a {nameof(BattleSquadModel)} on '{target.name}'.");
            return false;
        }

        return true;
    }
}
