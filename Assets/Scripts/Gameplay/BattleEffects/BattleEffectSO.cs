using System;
using System.Threading.Tasks;
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

    public virtual Task OnAttach(BattleContext ctx, BattleSquadEffectsController target)
    {
        if (Trigger == BattleEffectTrigger.OnAttach)
        {
            return Apply(ctx, target);
        }

        return Task.CompletedTask;
    }

    public virtual Task Apply(BattleContext ctx, BattleSquadEffectsController target)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnTick(BattleContext ctx, BattleSquadEffectsController target)
    {
        return Apply(ctx, target);
    }

    public virtual void OnRemove(BattleContext ctx, BattleSquadEffectsController target)
    {
    }

    public virtual string GetFormatedDescription()
    {
        if (!string.IsNullOrWhiteSpace(Description))
            return Description;

        return string.IsNullOrWhiteSpace(Name) ? string.Empty : Name;
    }
}
