using System.Collections.Generic;
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

    public void Apply(BattleContext ctx, BattleSquadController target)
    {
        foreach(var effect in Effects)
        {
            ctx.BattleEffectsManager.AddEffect(ctx, effect, target.GetComponent<BattleSquadEffectsController>());
        }
    }
}