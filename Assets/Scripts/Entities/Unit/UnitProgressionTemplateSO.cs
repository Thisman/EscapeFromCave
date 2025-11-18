using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Unit Progression Template")]
public sealed class UnitProgressionTemplateSO : ScriptableObject
{
    [Min(1)] public float BaseHealth = 100f;
    [Range(0, 1)] public float BasePhysicalDefense = 2f;
    [Range(0, 1)] public float BaseMagicDefense = 0f;
    [Range(0, 1)] public float BaseAbsoluteDefense = 0f;
    [Min(0)] public float MinDamage = 10f;
    [Min(0)] public float MaxDamage = 20f;
    [Min(1)] public float Speed = 2f;
    [Range(0, 1)] public float BaseCritChance = 1f;
    [Min(1)] public float BaseCritMultiplier = 1.1f;
    [Range(0, 1)] public float BaseMissChance = 5f;
}
