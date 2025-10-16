using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Interactable")]
public sealed class InteractableDefinition : ScriptableObject
{
    public string Id;
    public InteractableInfo Info;
    public float Cooldown;
    public ConditionSO[] Conditions;
    public TargetResolverSO TargetResolver;
    public EffectSO[] Effects;
}
