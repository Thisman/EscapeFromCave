using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "DamageBattleEffect", menuName = "Gameplay/Battle Effects/Damage Effect")]
public sealed class BattleEffectDamageSO : BattleEffectSO, IBattleDamageSource
{
    [Min(0)]
    public int Damage;

    private const DamageType _effectDamageType = DamageType.Magical;

    public override async Task Apply(BattleContext ctx, BattleSquadEffectsController target)
    {
        BattleSquadController squadController = target.GetComponent<BattleSquadController>();
        if (squadController == null)
            return;

        Debug.Log($"[{nameof(BattleEffectDamageSO)}.{nameof(Apply)}] '{name}' deals {Damage} {_effectDamageType} damage to '{target.name}'.");
        await new BattleDamageDefaultResolver().ResolveDamage(this, squadController);
    }

    public BattleDamageData ResolveDamage()
    {
        return new BattleDamageData(_effectDamageType, Damage);
    }
}
