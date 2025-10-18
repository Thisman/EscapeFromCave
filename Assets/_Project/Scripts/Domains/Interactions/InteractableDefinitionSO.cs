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
public sealed class InteractableDefinitionSO : ScriptableObject
{
    public string Id;

    public InteractableInfoDefinition Info;

    // TODO: move to ObjectDefinitionSO
    public Sprite Icon;

    public float Cooldown;

    public ConditionSO[] Conditions;

    public TargetResolverSO TargetResolver;

    public EffectSO[] Effects;
}

[Serializable]
public struct InteractableInfoDefinition
{
    public string DisplayName;

    public string Description;

    public InteractionType Type;

    public float InteractionDistance;
}
