using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class BattleEffectDefinitionSO: ScriptableObject
{
    public string Name;

    public string Description;

    public Sprite Icon;

    public BattleEffectTrigger Trigger;
    
    public BattleEffectDurationMode DurationMode;

    public BattleEffectTargetKind TargetKind;

    public BattleEffectStackPolicy StackPolicy;

    public int MaxStacks;

    public int Duration;

    public virtual void OnAttach(BattleContext ctx) { }

    public virtual void OnApply(BattleContext ctx) { }

    public virtual void OnTick(BattleContext ctx) { }

    public virtual void OnBattleRoundState(BattleContext ctx) { }

    public virtual void OnRemove(BattleContext ctx) { }
}
