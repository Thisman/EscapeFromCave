using System;
using UnityEngine;

public enum InteractionType
{
    Use,
    Pickup,
    Talk,
    Examine,
    Open,
    Activate,
    Custom
}

[CreateAssetMenu(menuName = "Gameplay/Interactable")]
public sealed class InteractationDefinitionSO : ScriptableObject
{
    public float Cooldown;

    public InteractionType Type;

    public float InteractionDistance;

    public EffectSO[] Effects;

    public ConditionSO[] Conditions;

    public TargetResolverSO TargetResolver;
}
