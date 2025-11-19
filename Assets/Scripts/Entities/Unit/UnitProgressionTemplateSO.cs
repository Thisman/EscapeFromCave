using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Unit Progression Template")]
public sealed class UnitProgressionTemplateSO : ScriptableObject
{
    [Min(1)] public float BaseHealth = 100f;
    [Min(0)] public float BasePhysicalDefense = 2f;
    [Min(0)] public float BaseMagicDefense = 0f;
    [Min(0)] public float BaseAbsoluteDefense = 0f;
    [Min(0)] public float MinDamage = 10f;
    [Min(0)] public float MaxDamage = 20f;
    [Min(0)] public float Speed = 2f;
    public float BaseCritChance = 1f;
    [Min(0)] public float BaseCritMultiplier = 1.1f;
    public float BaseMissChance = 5f;
}
