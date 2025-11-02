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

public abstract class BattleAbilityDefinitionSO : ScriptableObject
{
    public string Id;

    public string AbilityName;

    public string Description;

    public Sprite Icon;

    public int Cooldown;

    public bool IsReady;

    public BattleAbilityType AbilityType;

    public BattleAbilityTargetType AbilityTargetType;

    public BattleEffectDefinitionSO[] Effects;

    public abstract void Apply(BattleContext ctx, IReadOnlySquadModel user, IReadOnlyList<IReadOnlySquadModel> targets);
}