using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Interactable")]
public sealed class InteractionDefinitionSO : ScriptableObject
{
    public float Cooldown;

    public float InteractionDistance;

    public InteractionEffectDefinitionSO[] Effects;

    public InteractionConditionDefinitionSO[] Conditions;

    public InteractionTargetResolverDefinitionSO TargetResolver;
}
