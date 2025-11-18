using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public enum BattleAbilityType
{
    Active,
    Passive,
}

public enum BattleAbilityTargetType
{
    SingleEnemy,
    AllEnemies,
    Self,
    SingleAlly,
    AllAllies,
}

[CreateAssetMenu(menuName = "Gameplay/Battle Ability/Ability")]
public class BattleAbilitySO : ScriptableObject
{
    public string Id;

    public string AbilityName;

    public string Description;

    public Sprite Icon;

    public int Cooldown;

    public bool IsReady;

    public BattleAbilityType AbilityType;

    public BattleAbilityTargetType AbilityTargetType;

    public BattleEffectSO[] Effects;

    public async Task Apply(BattleContext ctx, BattleSquadController target)
    {
        if (ctx == null || target == null)
            return;

        var effectsManager = ctx.BattleEffectsManager;
        var effectsController = target.GetComponent<BattleSquadEffectsController>();
        if (effectsManager == null || effectsController == null)
            return;

        foreach (var effect in Effects)
        {
            if (effect == null)
                continue;

            await effectsManager.AddEffect(ctx, effect, effectsController);
        }
    }

    public virtual string GetFormatedDescription()
    {
        StringBuilder builder = new();
        string title = string.IsNullOrWhiteSpace(AbilityName) ? name : AbilityName;

        if (!string.IsNullOrWhiteSpace(title))
            builder.AppendLine($"<b>{title}</b>");

        if (!string.IsNullOrWhiteSpace(Description))
            builder.AppendLine(Description);

        builder.AppendLine($"Тип: {FormatHighlight(FormatAbilityType(AbilityType))}");
        builder.AppendLine($"Цель: {FormatHighlight(FormatTargetType(AbilityTargetType))}");
        builder.AppendLine($"Кулдаун: {FormatCooldown(Cooldown)}");

        IReadOnlyList<BattleEffectSO> effectList = Effects ?? Array.Empty<BattleEffectSO>();
        bool hasEffect = false;
        foreach (BattleEffectSO effect in effectList)
        {
            if (effect == null)
                continue;

            if (!hasEffect)
            {
                builder.AppendLine("Эффекты:");
                hasEffect = true;
            }

            builder.Append("• ");
            builder.Append(effect.GetFormatedDescription());
            builder.Append('\n');
        }

        if (!hasEffect)
        {
            builder.AppendLine("Эффекты:");
            builder.Append("• <color=grey>Эффектов нет</color>\n");
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatHighlight(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "<b>—</b>";

        return $"<b>{value}</b>";
    }

    private static string FormatAbilityType(BattleAbilityType abilityType)
    {
        return abilityType switch
        {
            BattleAbilityType.Active => "Активная",
            BattleAbilityType.Passive => "Пассивная",
            _ => abilityType.ToString()
        };
    }

    private static string FormatTargetType(BattleAbilityTargetType targetType)
    {
        return targetType switch
        {
            BattleAbilityTargetType.SingleEnemy => "Один враг",
            BattleAbilityTargetType.AllEnemies => "Все враги",
            BattleAbilityTargetType.Self => "Сам себя",
            BattleAbilityTargetType.SingleAlly => "Союзник",
            BattleAbilityTargetType.AllAllies => "Все союзники",
            _ => targetType.ToString()
        };
    }

    private static string FormatCooldown(int cooldown)
    {
        if (cooldown <= 0)
            return "<b>0</b>";

        return $"<color={NegativeColorHex}><b>{cooldown}</b></color> хода";
    }

    private const string NegativeColorHex = "#FF5C5C";
}
