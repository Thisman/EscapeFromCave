using System.Threading.Tasks;
using UnityEngine;

public enum BattleAbilityType
{
    Active,
    Passive,
}

public enum BattleAbilityTargetType
{
    SingleEnemy,
    AllEnemies,
    Self,
    Ally,
    AllAllies,
}

[CreateAssetMenu(menuName = "Gameplay/Battle Ability/Ability")]
public class BattleAbilitySO : ScriptableObject
{
    public string Id;

    public string AbilityName;

    public string Description;

    public Sprite Icon;

    public int Cooldown;

    public bool IsReady;

    public BattleAbilityType AbilityType;

    public BattleAbilityTargetType AbilityTargetType;

    public BattleEffectSO[] Effects;

    public async Task Apply(BattleContext ctx, BattleSquadController target)
    {
        if (ctx == null || target == null)
            return;

        var effectsManager = ctx.BattleEffectsManager;
        var effectsController = target.GetComponent<BattleSquadEffectsController>();
        if (effectsManager == null || effectsController == null)
            return;

        foreach (var effect in Effects)
        {
            if (effect == null)
                continue;

            await effectsManager.AddEffect(ctx, effect, effectsController);
        }
    }
}
