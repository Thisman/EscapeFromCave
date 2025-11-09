using UnityEngine;

[CreateAssetMenu(fileName = "DamageBattleEffect", menuName = "Gameplay/Battle Effects/Damage Effect")]
public sealed class DamageBattleEffect : BattleEffectDefinitionSO, IBattleDamageSource
{
    [Min(0)]
    public int Damage;

    private const DamageType _effectDamageType = DamageType.Magical;

    public override void Apply(BattleContext ctx, BattleSquadEffectsController target)
    {
        if (target == null)
        {
            GameLogger.Warn($"{nameof(DamageBattleEffect)} '{name}' received a null target.");
            return;
        }

        var squadController = target.GetComponent<BattleSquadController>() ?? target.GetComponentInParent<BattleSquadController>();
        if (squadController == null)
        {
            GameLogger.Warn($"{nameof(DamageBattleEffect)} '{name}' could not find a {nameof(BattleSquadController)} on '{target.name}'.");
            return;
        }

        if (Damage <= 0)
        {
            GameLogger.Log($"{nameof(DamageBattleEffect)} '{name}' applied to '{target.name}' with non-positive damage value {Damage}. No damage dealt.");
            return;
        }

        GameLogger.Log($"{nameof(DamageBattleEffect)} '{name}' deals {Damage} {_effectDamageType} damage to '{target.name}'.");
        _ = new DefaultBattleDamageResolver().ResolveDamage(this, squadController);
    }

    public BattleDamageData ResolveDamage()
    {
        return new BattleDamageData(_effectDamageType, Damage);
    }
}
