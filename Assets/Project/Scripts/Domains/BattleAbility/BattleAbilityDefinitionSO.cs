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
public class BattleAbilityDefinitionSO : ScriptableObject
{
    public string Id;

    public string AbilityName;

    public string Description;

    public Sprite Icon;

    public int Cooldown;

    public bool IsReady;

    public BattleAbilityType AbilityType;

    public BattleAbilityTargetType AbilityTargetType;

    public void Apply(BattleContext ctx, IReadOnlySquadModel actor, IReadOnlyList<IReadOnlySquadModel> targets)
    {
        Debug.Log("Apply ability: " + AbilityName);
    }
}