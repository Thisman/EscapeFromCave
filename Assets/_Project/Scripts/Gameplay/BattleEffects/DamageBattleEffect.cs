using System;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Battle Effects/Damage Effect")]
public class DamageBattleEffect : BattleEffectDefinitionSO
{
    public int Damage;

    public override void OnApply(BattleContext ctx)
    {
        if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

        _ = ApplyDamageAsync(ctx);
    }

    private async Task ApplyDamageAsync(BattleContext ctx)
    {
        try
        {
            if (ctx == null)
                return;

            if (Damage <= 0)
                return;

            var targetModel = ctx.CurrentEffectTarget;
            if (targetModel == null)
                return;

            var targetController = FindController(ctx, targetModel);
            if (targetController == null)
                return;

            var resolver = new DefaultBattleDamageResolver();
            await resolver.ResolveDamage(Damage, targetController);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private static BattleSquadController FindController(BattleContext ctx, IReadOnlySquadModel target)
    {
        if (ctx?.BattleUnits == null || target == null)
            return null;

        foreach (var unit in ctx.BattleUnits)
        {
            if (unit?.GetSquadModel() == target)
                return unit;
        }

        return null;
    }
}
