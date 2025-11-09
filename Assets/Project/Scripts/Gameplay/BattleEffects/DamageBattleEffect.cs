using UnityEngine;

[CreateAssetMenu(fileName = "DamageBattleEffect", menuName = "Gameplay/Battle Effects/Damage Effect")]
public sealed class DamageBattleEffect : BattleEffectDefinitionSO, IBattleDamageSource
{
    [Min(0)]
    public int Damage;
    public DamageType DamageType = DamageType.Physical;

    public override void Apply(BattleContext ctx, BattleSquadEffectsController target)
    {
        if (target == null)
        {
            Debug.LogWarning($"{nameof(DamageBattleEffect)} '{name}' received a null target.");
            return;
        }

        var squadController = target.GetComponent<BattleSquadController>() ?? target.GetComponentInParent<BattleSquadController>();
        if (squadController == null)
        {
            Debug.LogWarning($"{nameof(DamageBattleEffect)} '{name}' could not find a {nameof(BattleSquadController)} on '{target.name}'.");
            return;
        }

        if (Damage <= 0)
        {
            Debug.Log($"{nameof(DamageBattleEffect)} '{name}' applied to '{target.name}' with non-positive damage value {Damage}. No damage dealt.");
            return;
        }

        Debug.Log($"{nameof(DamageBattleEffect)} '{name}' deals {Damage} {DamageType} damage to '{target.name}'.");
        var resolver = new DefaultBattleDamageResolver();
        _ = resolver.ResolveDamage(this, squadController);
    }

    public BattleDamageData ResolveDamage()
    {
        return new BattleDamageData(DamageType, Damage);
    }
}
