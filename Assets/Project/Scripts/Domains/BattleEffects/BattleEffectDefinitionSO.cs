using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum BattleEffectTrigger { OnAttach, OnTick }

public enum BattleEffectDurationMode { Instant, TurnCount, RoundCount, UntilEvent, Infinite }

class BattleEffectDefinitionSO: ScriptableObject
{
    public string Name;

    public string Description;

    public Sprite Icon;

    public BattleEffectTrigger Trigger;

    public BattleEffectDurationMode DurationMode;

    public void OnAttach(BattleContext ctx, BattleSquadEffectsController target)
    {
        if (Trigger == BattleEffectTrigger.OnAttach)
        {
            Apply();
        }
    }

    public virtual void Apply()
    {

    }

    public void OnTick()
    {
        if (Trigger == BattleEffectTrigger.OnTick)
        {
            Apply();
        }
    }

    public virtual void OnRemove()
    {

    }
}
