using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Interactable")]
public sealed class InteractableDefinitionSO : ScriptableObject
{
    public string Id;
    public InteractableInfo Info;
    // TODO: move to ObjectDefinitionSO
    public Sprite Icon;
    public float Cooldown;
    public ConditionSO[] Conditions;
    public TargetResolverSO TargetResolver;
    public EffectSO[] Effects;
}
