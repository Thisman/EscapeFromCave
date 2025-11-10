using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Unit")]
public sealed class UnitSO : ScriptableObject
{
    public Sprite Icon;

    public string UnitName = "UnitName";

    public UnitKind Kind = UnitKind.Neutral;

    public AttackKind AttackKind = AttackKind.Melee;

    public DamageType DamageType = DamageType.Physical;

    [Min(1)] public float BaseHealth = 100;

    [Range(0, 1)] public float BasePhysicalDefense = 2;

    [Range(0, 1)] public float BaseMagicDefense = 0;

    [Range(0, 1)] public float BaseAbsoluteDefense = 0;

    [Min(0)] public float MinDamage = 10;

    [Min(0)] public float MaxDamage = 20;

    [Min(1)] public float Speed = 2;

    [Range(0, 1)] public float BaseCritChance = 1;

    [Min(1)] public float BaseCritMultiplier = 1.1f;

    [Range(0, 1)] public float BaseMissChance = 5;

    public BattleAbilitySO[] Abilities;

    public (float min, float max) GetBaseDamageRange() => (MinDamage, MaxDamage);

    public bool IsFriendly () => Kind == UnitKind.Ally || Kind == UnitKind.Hero;

    public bool IsAlly () => Kind == UnitKind.Ally;

    public bool IsHero () => Kind == UnitKind.Hero;

    public bool IsEnemy () => Kind == UnitKind.Enemy;

    public bool IsNeutral () => Kind == UnitKind.Neutral;
}
