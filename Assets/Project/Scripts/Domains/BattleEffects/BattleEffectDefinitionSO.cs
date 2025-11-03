using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum BattleEffectTrigger { OnAttach, OnTick }

public class BattleEffectDefinitionSO: ScriptableObject
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
