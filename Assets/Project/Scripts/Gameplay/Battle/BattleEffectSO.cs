using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum BattleEffectTrigger
{
    OnAttach,
    OnRoundStart,
    OnRoundEnd,
    OnAction,
    OnDefend,
    OnSkip,
    OnAttack,
    OnDealDamage,
    OnApplyDamage,
    OnAbility,
    OnTurnStart,
    OnTurnEnd,
}

public class BattleEffectSO: ScriptableObject
{
    public string Name;

    public string Description;

    public Sprite Icon;

    public BattleEffectTrigger Trigger;

    [Min(0)] public int MaxTick;

    public void OnAttach(BattleContext ctx, BattleSquadEffectsController target)
    {
        if (Trigger == BattleEffectTrigger.OnAttach)
        {
            Apply(ctx, target);
        }
    }

    public virtual void Apply(BattleContext ctx, BattleSquadEffectsController target)
    {
    }

    public void OnTick(BattleContext ctx, BattleSquadEffectsController target)
    {
        Apply(ctx, target);
    }

    public virtual void OnRemove(BattleContext ctx, BattleSquadEffectsController target)
    {
    }
}
