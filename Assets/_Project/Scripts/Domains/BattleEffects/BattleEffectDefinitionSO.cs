using System;
using System.Collections.Generic;
using System.Text;

public class BattleEffectDefinitionSO
{
    public string Name;

    public string Description;

    public virtual void OnAttach(BattleContext ctx) { }

    public virtual void OnApply(BattleContext ctx) { }

    public virtual void OnTick(BattleContext ctx) { }

    public virtual void OnBattleRoundState(BattleContext ctx) { }

    public virtual void OnRemove(BattleContext ctx) { }
}
