using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UICommon.Widgets
{
    public readonly struct UnitCardRenderData
    {
        public UnitCardRenderData(
            IReadOnlySquadModel squad,
            IReadOnlyList<string> infoKeys,
            BattleAbilitySO[] abilities = null,
            BattleEffectSO[] effects = null,
            string tooltip = null)
        {
            Squad = squad;
            InfoKeys = infoKeys ?? Array.Empty<string>();
            Abilities = abilities;
            Effects = effects;
            Tooltip = tooltip ?? squad?.UnitName ?? string.Empty;
        }

        public IReadOnlySquadModel Squad { get; }
        public IReadOnlyList<string> InfoKeys { get; }
        public BattleAbilitySO[] Abilities { get; }
        public BattleEffectSO[] Effects { get; }
        public string Tooltip { get; }
    }

    public sealed class UnitCardWidget
    {
        public const string BlockClassName = "unit-card";
        public const string SelectedModifierClassName = "unit-card--selected";
        public const string InfoLineClassName = "unit-card__info-line";
        public const string AbilityIconClassName = "ability-info";
        public const string EffectIconClassName = "effect-info";

        public static class InfoKeys
        {
            public const string Count = "count";
            public const string Health = "health";
            public const string Damage = "damage";
            public const string Initiative = "initiative";
            public const string AttackKind = "attack-kind";
            public const string DamageType = "damage-type";
            public const string PhysicalDefense = "physical-defense";
            public const string MagicDefense = "magic-defense";
            public const string AbsoluteDefense = "absolute-defense";
            public const string CritChance = "crit-chance";
            public const string CritMultiplier = "crit-multiplier";
            public const string MissChance = "miss-chance";
        }

        private readonly List<Label> _infoLabels = new();
        private readonly VisualElement _root;
        private readonly VisualElement _icon;
        private readonly Label _title;
        private readonly VisualElement _infoContainer;
        private readonly VisualElement _abilityList;
        private readonly VisualElement _effectsList;

        private static readonly IReadOnlyDictionary<string, string> EmptyInfoEntries =
            new Dictionary<string, string>();

        public UnitCardWidget(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _icon = _root.Q<VisualElement>("Icon");
            _title = _root.Q<Label>("Title");
            _infoContainer = _root.Q<VisualElement>("Info");
            _abilityList = _root.Q<VisualElement>("AbilityList");
            _effectsList = _root.Q<VisualElement>("EffectsList");

            _infoContainer?.Query<Label>().ForEach(label =>
            {
                if (label == null)
                    return;

                _infoLabels.Add(label);
            });
        }

        public VisualElement Root => _root;

        public void Render(UnitCardRenderData data)
        {
            ApplyUnit(data);
        }

        public void SetSelected(bool isSelected)
        {
            _root?.EnableInClassList(SelectedModifierClassName, isSelected);
        }

        public void SetEnabled(bool isEnabled)
        {
            _root?.SetEnabled(isEnabled);
        }

        private void ApplyUnit(UnitCardRenderData data)
        {
            IReadOnlySquadModel squad = data.Squad;
            Sprite icon = squad?.Icon;
            string title = squad?.UnitName ?? string.Empty;

            if (_icon != null)
            {
                if (icon != null)
                    _icon.style.backgroundImage = new StyleBackground(icon);
                else
                    _icon.style.backgroundImage = new StyleBackground();

                _icon.tooltip = data.Tooltip ?? string.Empty;
            }

            if (_title != null)
                _title.text = title;

            IReadOnlyDictionary<string, string> infoEntries = BuildSquadInfoEntries(squad);
            IReadOnlyList<string> infoKeys = data.InfoKeys ?? Array.Empty<string>();
            int infoCount = infoKeys.Count;
            EnsureInfoLabelCount(infoCount);

            for (int i = 0; i < _infoLabels.Count; i++)
            {
                Label label = _infoLabels[i];
                if (label == null)
                    continue;

                if (i < infoCount)
                {
                    string key = infoKeys[i];
                    label.text = TryGetInfoEntry(infoEntries, key);
                    label.style.display = string.IsNullOrEmpty(label.text) ? DisplayStyle.None : DisplayStyle.Flex;
                }
                else
                {
                    label.text = string.Empty;
                    label.style.display = DisplayStyle.None;
                }
            }

            _root?.EnableInClassList(SelectedModifierClassName, false);

            IReadOnlyList<UnitCardIconRenderData> abilityIcons = BuildAbilityIcons(data.Abilities ?? squad?.Abilities);
            IReadOnlyList<UnitCardIconRenderData> effectIcons = BuildEffectIcons(data.Effects);

            RenderIconList(_abilityList, abilityIcons, AbilityIconClassName);
            RenderIconList(_effectsList, effectIcons, EffectIconClassName);
        }

        private static string TryGetInfoEntry(IReadOnlyDictionary<string, string> entries, string key)
        {
            if (entries == null || string.IsNullOrEmpty(key))
                return string.Empty;

            return entries.TryGetValue(key, out string value) ? value ?? string.Empty : string.Empty;
        }

        private static IReadOnlyDictionary<string, string> BuildSquadInfoEntries(IReadOnlySquadModel squad)
        {
            if (squad == null)
                return EmptyInfoEntries;

            (float minDamage, float maxDamage) = squad.GetBaseDamageRange();
            Dictionary<string, string> entries = new()
            {
                [InfoKeys.Count] = $"Количество: {FormatValue(squad.Count)}",
                [InfoKeys.Health] = $"Здоровье: {FormatValue(squad.Health)}",
                [InfoKeys.Damage] = $"Урон: {FormatValue(minDamage)} - {FormatValue(maxDamage)}",
                [InfoKeys.Initiative] = $"Инициатива: {FormatValue(squad.Initiative)}",
                [InfoKeys.PhysicalDefense] =
                    $"Физическая защита: {FormatPercent(squad.PhysicalDefense)}",
                [InfoKeys.MagicDefense] = $"Магическая защита: {FormatPercent(squad.MagicDefense)}",
                [InfoKeys.AbsoluteDefense] =
                    $"Абсолютная защита: {FormatPercent(squad.AbsoluteDefense)}",
                [InfoKeys.AttackKind] = $"Тип атаки: {FormatAttackKind(squad.AttackKind)}",
                [InfoKeys.DamageType] = $"Тип урона: {FormatDamageType(squad.DamageType)}",
                [InfoKeys.CritChance] = $"Шанс критического удара: {FormatPercent(squad.CritChance)}",
                [InfoKeys.CritMultiplier] =
                    $"Критический множитель: {FormatValue(squad.CritMultiplier)}",
                [InfoKeys.MissChance] = $"Шанс промаха: {FormatPercent(squad.MissChance)}",
            };

            return entries;
        }

        private void EnsureInfoLabelCount(int targetCount)
        {
            if (_infoContainer == null)
                return;

            while (_infoLabels.Count < targetCount)
            {
                Label label = new();
                label.AddToClassList(InfoLineClassName);
                _infoContainer.Add(label);
                _infoLabels.Add(label);
            }
        }

        private static IReadOnlyList<UnitCardIconRenderData> BuildAbilityIcons(BattleAbilitySO[] abilities)
        {
            if (abilities == null || abilities.Length == 0)
                return Array.Empty<UnitCardIconRenderData>();

            List<UnitCardIconRenderData> result = new();

            foreach (BattleAbilitySO ability in abilities)
            {
                if (ability?.Icon == null)
                    continue;

                string tooltip = ability.AbilityName ?? string.Empty;
                if (!string.IsNullOrEmpty(ability.Description))
                {
                    tooltip = string.IsNullOrEmpty(tooltip)
                        ? ability.Description
                        : $"{tooltip}\n{ability.Description}";
                }

                result.Add(new UnitCardIconRenderData(ability.Icon, tooltip));
            }

            return result;
        }

        private static IReadOnlyList<UnitCardIconRenderData> BuildEffectIcons(BattleEffectSO[] effects)
        {
            if (effects == null || effects.Length == 0)
                return Array.Empty<UnitCardIconRenderData>();

            List<UnitCardIconRenderData> result = new();

            foreach (BattleEffectSO effect in effects)
            {
                if (effect?.Icon == null)
                    continue;

                string tooltip = effect.Name ?? string.Empty;
                if (!string.IsNullOrEmpty(effect.Description))
                {
                    tooltip = string.IsNullOrEmpty(tooltip)
                        ? effect.Description
                        : $"{tooltip}\n{effect.Description}";
                }

                result.Add(new UnitCardIconRenderData(effect.Icon, tooltip));
            }

            return result;
        }

        private static string FormatValue(float value)
        {
            return value.ToString("0.##");
        }

        private static string FormatPercent(float value)
        {
            return value.ToString("P0");
        }

        private static string FormatAttackKind(AttackKind attackKind)
        {
            return attackKind switch
            {
                AttackKind.Melee => "Ближняя",
                AttackKind.Range => "Дальняя",
                AttackKind.Magic => "Магическая",
                _ => attackKind.ToString()
            };
        }

        private static string FormatDamageType(DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Physical => "Физический",
                DamageType.Magical => "Магический",
                DamageType.Pure => "Чистый",
                _ => damageType.ToString()
            };
        }

        private readonly struct UnitCardIconRenderData
        {
            public UnitCardIconRenderData(Sprite icon, string tooltip = null)
            {
                Icon = icon;
                Tooltip = tooltip ?? string.Empty;
            }

            public Sprite Icon { get; }
            public string Tooltip { get; }
        }

        private static void RenderIconList(
            VisualElement container,
            IReadOnlyList<UnitCardIconRenderData> icons,
            string iconClassName)
        {
            if (container == null)
                return;

            container.Clear();

            if (icons != null)
            {
                foreach (UnitCardIconRenderData icon in icons)
                {
                    if (icon.Icon == null)
                        continue;

                    VisualElement iconElement = new();
                    iconElement.AddToClassList(iconClassName);
                    iconElement.style.backgroundImage = new StyleBackground(icon.Icon);
                    iconElement.tooltip = icon.Tooltip ?? string.Empty;
                    container.Add(iconElement);
                }
            }

            container.style.display = container.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
