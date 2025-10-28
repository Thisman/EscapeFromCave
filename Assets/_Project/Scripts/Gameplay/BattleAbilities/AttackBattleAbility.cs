using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Battle Ability/Attack Ability")]
public class AttackBattleAbility : BattleAbilityDefinitionSO
{
    public override void Apply(BattleContext ctx, IReadOnlySquadModel user, IReadOnlyList<IReadOnlySquadModel> targets)
    {
        Debug.Log(AbilityName);
    }
}