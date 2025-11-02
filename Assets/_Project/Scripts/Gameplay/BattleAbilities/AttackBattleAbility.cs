using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Battle Ability/Attack Ability")]
public class AttackBattleAbility : BattleAbilityDefinitionSO
{
    public override void Apply(BattleContext ctx, IReadOnlySquadModel user, IReadOnlyList<IReadOnlySquadModel> targets)
    {
        if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

        if (targets == null)
            throw new ArgumentNullException(nameof(targets));

        var effectsManager = ctx.BattleEffectsManager;
        if (effectsManager == null)
            return;

        var effects = Effects;
        if (effects == null || effects.Length == 0)
            return;

        foreach (var target in targets)
        {
            if (target == null)
                continue;

            foreach (var effect in effects)
            {
                if (effect == null)
                    continue;

                effectsManager.ApplyEffect(ctx, target, effect);
            }
        }
    }
}