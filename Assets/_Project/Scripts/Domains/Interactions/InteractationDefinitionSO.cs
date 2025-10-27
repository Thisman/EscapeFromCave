using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Interactable")]
public sealed class InteractationDefinitionSO : ScriptableObject
{
    public float Cooldown;

    public float InteractionDistance;

    public EffectDefinitionSO[] Effects;

    public ConditionDefinitionSO[] Conditions;

    public TargetResolverDefinitionSO TargetResolver;
}
