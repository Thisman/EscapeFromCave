using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "DamageBattleEffect", menuName = "Gameplay/Battle Effects/Damage Effect")]
public sealed class BattleEffectDamageSO : BattleEffectSO, IBattleDamageProvider
{
    [Min(0)]
    public int Damage;

    private const DamageType _effectDamageType = DamageType.Magical;
    private const string NegativeColorHex = "#FF5C5C";

    public override async Task Apply(BattleContext ctx, BattleSquadEffectsController target)
    {
        BattleSquadController squadController = target.GetComponent<BattleSquadController>();
        if (squadController == null)
            return;

        Debug.Log($"[{nameof(BattleEffectDamageSO)}.{nameof(Apply)}] '{name}' deals {Damage} {_effectDamageType} damage to '{target.name}'.");
        await new BattleDamageResolverByDefault().ResolveDamage(this, squadController);
    }

    public BattleDamageData CreateDamageData()
    {
        return new BattleDamageData(_effectDamageType, Damage);
    }

    public override string GetFormatedDescription()
    {
        string triggerLabel = GetTriggerLabel(Trigger);
        string tickTriggerLabel = GetTriggerLabel(TickTrigger);
        string damageValue = FormatNegativeValue(Damage);
        string damageType = FormatDamageType(_effectDamageType);

        if (MaxTick > 1)
        {
            string tickLimit = MaxTick > 0 ? $" (до <b>{MaxTick}</b> тиков)" : string.Empty;
            return $"При {triggerLabel} наносит {damageValue} {damageType} урона. Количество тиков обновляется при {tickTriggerLabel}{tickLimit}.";
        }

        return $"При {triggerLabel} наносит {damageValue} {damageType} урона.";
    }

    private static string FormatNegativeValue(int value)
    {
        return $"<color={NegativeColorHex}><b>{Mathf.Abs(value)}</b></color>";
    }

    private static string FormatDamageType(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Physical => "физического",
            DamageType.Magical => "магического",
            DamageType.Pure => "чистого",
            _ => damageType.ToString().ToLowerInvariant()
        };
    }

    private static string GetTriggerLabel(BattleEffectTrigger trigger)
    {
        return trigger switch
        {
            BattleEffectTrigger.OnAttach => "применении",
            BattleEffectTrigger.OnRoundStart => "начале раунда",
            BattleEffectTrigger.OnRoundEnd => "конце раунда",
            BattleEffectTrigger.OnAction => "совершении действия",
            BattleEffectTrigger.OnDefend => "защите",
            BattleEffectTrigger.OnSkip => "пропуске хода",
            BattleEffectTrigger.OnAttack => "атаке",
            BattleEffectTrigger.OnDealDamage => "нанесении урона",
            BattleEffectTrigger.OnApplyDamage => "получении урона",
            BattleEffectTrigger.OnAbility => "активации способности",
            BattleEffectTrigger.OnTurnStart => "начале хода",
            BattleEffectTrigger.OnTurnEnd => "конце хода",
            _ => "срабатывании"
        };
    }
}
