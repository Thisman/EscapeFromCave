using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Interactions/Interaction")]
public sealed class InteractionSO : ScriptableObject
{
    public float Cooldown;

    public InteractionEffectSO[] Effects;

    public InteractionConditionSO[] Conditions;

    public InteractionTargetResolverSO TargetResolver;
}
