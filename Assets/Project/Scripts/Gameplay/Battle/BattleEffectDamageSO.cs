using UnityEngine;

[CreateAssetMenu(fileName = "DamageBattleEffect", menuName = "Gameplay/Battle Effects/Damage Effect")]
public sealed class BattleEffectDamageSO : BattleEffectSO, IBattleDamageSource
{
    [Min(0)]
    public int Damage;

    private const DamageType _effectDamageType = DamageType.Magical;

    public override void Apply(BattleContext ctx, BattleSquadEffectsController target)
    {
        BattleSquadController squadController = target.GetComponent<BattleSquadController>();

        Debug.Log($"[{nameof(BattleEffectDamageSO)}.{nameof(Apply)}] '{name}' deals {Damage} {_effectDamageType} damage to '{target.name}'.");
        _ = new BattleDamageDefaultResolver().ResolveDamage(this, squadController);
    }

    public BattleDamageData ResolveDamage()
    {
        return new BattleDamageData(_effectDamageType, Damage);
    }
}
