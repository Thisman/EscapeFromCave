using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "StatModifierBattleEffect", menuName = "Gameplay/Battle Effects/Stat Modifier Effect")]
public sealed class BattleEffectStatsModifierSO : BattleEffectSO
{
    public BattleSquadStatModifier[] StatsModifier = Array.Empty<BattleSquadStatModifier>();
    private const string PositiveColorHex = "#5DD68D";
    private const string NegativeColorHex = "#FF5C5C";

    public override Task Apply(BattleContext ctx, BattleSquadEffectsController target)
    {
        if (!TryResolveController(target, out var controller))
            return Task.CompletedTask;

        return controller.ApplyStatModifiers(this, StatsModifier ?? Array.Empty<BattleSquadStatModifier>());
    }

    public override void OnRemove(BattleContext ctx, BattleSquadEffectsController target)
    {
        if (!TryResolveController(target, out var controller))
            return;

        controller.RemoveStatModifiers(this);
    }

    private static bool TryResolveController(BattleSquadEffectsController target, out BattleSquadController controller)
    {
        controller = null;

        if (target == null)
            return false;

        controller = target.GetComponent<BattleSquadController>() ?? target.GetComponentInParent<BattleSquadController>();
        return controller != null;
    }

    public override string GetFormatedDescription()
    {
        if (StatsModifier == null || StatsModifier.Length == 0)
            return base.GetFormatedDescription();

        StringBuilder builder = new();
        builder.Append("Пока эффект активен изменяет характеристики:\n");

        for (int i = 0; i < StatsModifier.Length; i++)
        {
            BattleSquadStatModifier modifier = StatsModifier[i];
            builder.Append("• ");
            builder.Append(FormatModifier(modifier));

            if (i < StatsModifier.Length - 1)
                builder.Append('\n');
        }

        return builder.ToString();
    }

    private static string FormatModifier(BattleSquadStatModifier modifier)
    {
        string statName = GetStatDisplayName(modifier.Stat);
        string value = FormatSignedValue(modifier.Value);
        return $"{statName}: {value}";
    }

    private static string GetStatDisplayName(BattleSquadStat stat)
    {
        return stat switch
        {
            BattleSquadStat.Health => "Здоровье",
            BattleSquadStat.PhysicalDefense => "Физическая защита",
            BattleSquadStat.MagicDefense => "Магическая защита",
            BattleSquadStat.AbsoluteDefense => "Абсолютная защита",
            BattleSquadStat.MinDamage => "Минимальный урон",
            BattleSquadStat.MaxDamage => "Максимальный урон",
            BattleSquadStat.Speed => "Скорость",
            BattleSquadStat.Initiative => "Инициатива",
            BattleSquadStat.CritChance => "Шанс крит. удара",
            BattleSquadStat.CritMultiplier => "Множитель крита",
            BattleSquadStat.MissChance => "Шанс промаха",
            _ => stat.ToString()
        };
    }

    private static string FormatSignedValue(float value)
    {
        string color = value >= 0f ? PositiveColorHex : NegativeColorHex;
        string sign = value >= 0f ? "+" : "-";
        float absolute = Mathf.Abs(value);
        return $"<color={color}>{sign}<b>{absolute:0.##}</b></color>";
    }
}
