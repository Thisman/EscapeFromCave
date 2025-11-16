using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UICommon.Widgets
{
    public enum UnitCardInfoKey
    {
        Count,
        Health,
        DamageRange,
        Initiative,
        AttackKind,
        DamageType,
        PhysicalDefense,
        MagicDefense,
        AbsoluteDefense,
        CritChance,
        CritMultiplier,
        MissChance
    }

    public sealed class UnitCardWidget
    {
        public const string BlockClassName = "unit-card";
        public const string SelectedModifierClassName = "unit-card--selected";
        public const string InfoLineClassName = "unit-card__info-line";
        private const string AbilityInfoClassName = "ability-info";
        private const string EffectInfoClassName = "effect-info";

        private const int DefaultDefinitionCount = 1;

        private readonly List<Label> _infoLabels = new();
        private readonly List<VisualElement> _abilityElements = new();
        private readonly List<VisualElement> _effectElements = new();
        private readonly List<UnitAbilityRenderData> _abilityBuffer = new();
        private readonly List<UnitEffectRenderData> _effectBuffer = new();
        private readonly VisualElement _root;
        private readonly VisualElement _icon;
        private readonly Label _title;
        private readonly VisualElement _infoContainer;
        private readonly VisualElement _abilitiesContainer;
        private readonly VisualElement _effectsContainer;

        private static readonly Dictionary<UnitCardInfoKey, Func<UnitController, string>> InfoFormatters = new()
        {
            [UnitCardInfoKey.Count] = unit =>
                $"Количество: {FormatValue(unit?.GetCount(DefaultDefinitionCount) ?? DefaultDefinitionCount)}",
            [UnitCardInfoKey.Health] = unit =>
                $"Здоровье: {FormatValue(unit?.GetHealth() ?? 0f)}",
            [UnitCardInfoKey.DamageRange] = unit =>
            {
                (float min, float max) = unit?.GetDamageRange() ?? (0f, 0f);
                return $"Урон: {FormatValue(min)} - {FormatValue(max)}";
            },
            [UnitCardInfoKey.Initiative] = unit =>
                $"Инициатива: {FormatValue(unit?.GetInitiative() ?? 0f)}",
            [UnitCardInfoKey.AttackKind] = unit =>
                $"Тип атаки: {FormatAttackKind(unit?.GetAttackKind() ?? AttackKind.Melee)}",
            [UnitCardInfoKey.DamageType] = unit =>
                $"Тип урона: {FormatDamageType(unit?.GetDamageType() ?? DamageType.Physical)}",
            [UnitCardInfoKey.PhysicalDefense] = unit =>
                $"Физическая защита: {FormatPercent(unit?.GetPhysicalDefense() ?? 0f)}",
            [UnitCardInfoKey.MagicDefense] = unit =>
                $"Магическая защита: {FormatPercent(unit?.GetMagicDefense() ?? 0f)}",
            [UnitCardInfoKey.AbsoluteDefense] = unit =>
                $"Абсолютная защита: {FormatPercent(unit?.GetAbsoluteDefense() ?? 0f)}",
            [UnitCardInfoKey.CritChance] = unit =>
                $"Шанс критического удара: {FormatPercent(unit?.GetCritChance() ?? 0f)}",
            [UnitCardInfoKey.CritMultiplier] = unit =>
                $"Критический множитель: {FormatValue(unit?.GetCritMultiplier() ?? 0f)}",
            [UnitCardInfoKey.MissChance] = unit =>
                $"Шанс промаха: {FormatPercent(unit?.GetMissChance() ?? 0f)}"
        };

        public UnitCardWidget(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _icon = _root.Q<VisualElement>("Icon");
            _title = _root.Q<Label>("Title");
            _infoContainer = _root.Q<VisualElement>("Info");
            _abilitiesContainer = _root.Q<VisualElement>("AbilityList");
            _effectsContainer = _root.Q<VisualElement>("EffectsList");

            _infoContainer?.Query<Label>().ForEach(label =>
            {
                if (label == null)
                    return;

                _infoLabels.Add(label);
            });
        }

        public VisualElement Root => _root;

        public void Render(UnitController unit, IReadOnlyList<UnitCardInfoKey> infoKeys)
        {
            ApplyUnit(unit, infoKeys);
        }

        public void SetSelected(bool isSelected)
        {
            _root?.EnableInClassList(SelectedModifierClassName, isSelected);
        }

        public void SetEnabled(bool isEnabled)
        {
            _root?.SetEnabled(isEnabled);
        }

        private void ApplyUnit(UnitController unit, IReadOnlyList<UnitCardInfoKey> infoKeys)
        {
            ApplyIcon(unit);
            ApplyTitle(unit);
            ApplyInfo(unit, infoKeys);
            RenderAbilityList(unit);
            RenderEffectList(unit);

            _root?.EnableInClassList(SelectedModifierClassName, false);
        }

        private void ApplyIcon(UnitController unit)
        {
            if (_icon == null)
                return;

            Sprite icon = unit?.Icon;
            if (icon != null)
                _icon.style.backgroundImage = new StyleBackground(icon);
            else
                _icon.style.backgroundImage = new StyleBackground();

            _icon.tooltip = unit?.Title ?? string.Empty;
        }

        private void ApplyTitle(UnitController unit)
        {
            if (_title == null)
                return;

            _title.text = unit?.Title ?? string.Empty;
        }

        private void ApplyInfo(UnitController unit, IReadOnlyList<UnitCardInfoKey> infoKeys)
        {
            IReadOnlyList<string> entries = BuildInfoEntries(unit, infoKeys);
            int statsCount = entries.Count;
            EnsureInfoLabelCount(statsCount);

            for (int i = 0; i < _infoLabels.Count; i++)
            {
                Label label = _infoLabels[i];
                if (label == null)
                    continue;

                if (i < statsCount)
                {
                    label.text = entries[i];
                    label.style.display = DisplayStyle.Flex;
                }
                else
                {
                    label.text = string.Empty;
                    label.style.display = DisplayStyle.None;
                }
            }
        }

        private static IReadOnlyList<string> BuildInfoEntries(UnitController unit, IReadOnlyList<UnitCardInfoKey> infoKeys)
        {
            if (unit == null || infoKeys == null || infoKeys.Count == 0)
                return Array.Empty<string>();

            List<string> entries = new(infoKeys.Count);
            foreach (UnitCardInfoKey key in infoKeys)
            {
                if (!InfoFormatters.TryGetValue(key, out Func<UnitController, string> formatter))
                    continue;

                string line = formatter(unit);
                if (!string.IsNullOrEmpty(line))
                    entries.Add(line);
            }

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

        private void RenderAbilityList(UnitController unit)
        {
            IReadOnlyList<UnitAbilityRenderData> abilities = BuildAbilityEntries(unit);
            RenderIconList(abilities, _abilitiesContainer, _abilityElements, AbilityInfoClassName);
        }

        private void RenderEffectList(UnitController unit)
        {
            IReadOnlyList<UnitEffectRenderData> effects = BuildEffectEntries(unit);
            RenderIconList(effects, _effectsContainer, _effectElements, EffectInfoClassName);
        }

        private void RenderIconList<T>(
            IReadOnlyList<T> items,
            VisualElement container,
            List<VisualElement> pool,
            string className) where T : struct
        {
            if (container == null)
                return;

            int targetCount = items?.Count ?? 0;
            EnsureIconElementCount(pool, container, targetCount, className);

            for (int i = 0; i < pool.Count; i++)
            {
                VisualElement element = pool[i];
                if (element == null)
                    continue;

                if (i < targetCount)
                {
                    ApplyIconData(element, items[i]);
                    element.style.display = DisplayStyle.Flex;
                }
                else
                {
                    ResetIconElement(element);
                    element.style.display = DisplayStyle.None;
                }
            }

            container.style.display = targetCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void ApplyIconData<T>(VisualElement element, T data) where T : struct
        {
            (Sprite icon, string tooltip) = ExtractIconData(data);
            if (element == null)
                return;

            if (icon != null)
                element.style.backgroundImage = new StyleBackground(icon);
            else
                element.style.backgroundImage = new StyleBackground();

            element.tooltip = tooltip ?? string.Empty;
        }

        private static (Sprite icon, string tooltip) ExtractIconData<T>(T data) where T : struct
        {
            if (data is UnitAbilityRenderData abilityData)
                return (abilityData.Icon, abilityData.Tooltip);

            if (data is UnitEffectRenderData effectData)
                return (effectData.Icon, effectData.Tooltip);

            return (null, string.Empty);
        }

        private static void ResetIconElement(VisualElement element)
        {
            if (element == null)
                return;

            element.style.backgroundImage = new StyleBackground();
            element.tooltip = string.Empty;
        }

        private static void EnsureIconElementCount(
            List<VisualElement> pool,
            VisualElement container,
            int targetCount,
            string className)
        {
            if (container == null)
                return;

            while (pool.Count < targetCount)
            {
                VisualElement element = new();
                element.AddToClassList(className);
                element.style.display = DisplayStyle.None;
                container.Add(element);
                pool.Add(element);
            }
        }
        private IReadOnlyList<UnitAbilityRenderData> BuildAbilityEntries(UnitController unit)
        {
            _abilityBuffer.Clear();
            if (unit == null)
                return _abilityBuffer;

            IReadOnlyList<BattleAbilitySO> abilities = unit.GetAbilities();
            if (abilities == null)
                return _abilityBuffer;

            foreach (BattleAbilitySO ability in abilities)
            {
                if (ability == null)
                    continue;

                _abilityBuffer.Add(new UnitAbilityRenderData(ability.Icon, BuildAbilityTooltip(ability)));
            }

            return _abilityBuffer;
        }

        private IReadOnlyList<UnitEffectRenderData> BuildEffectEntries(UnitController unit)
        {
            _effectBuffer.Clear();
            if (unit == null)
                return _effectBuffer;

            IReadOnlyList<BattleEffectSO> effects = unit.GetEffects();
            if (effects == null)
                return _effectBuffer;

            foreach (BattleEffectSO effect in effects)
            {
                if (effect == null)
                    continue;

                _effectBuffer.Add(new UnitEffectRenderData(effect.Icon, BuildEffectTooltip(effect)));
            }

            return _effectBuffer;
        }

        private static string BuildAbilityTooltip(BattleAbilitySO ability)
        {
            if (ability == null)
                return string.Empty;

            string cooldownLabel = GetCooldownLabel(ability.Cooldown);
            return $"{ability.AbilityName}\n{ability.Description}\nПерезарядка: {ability.Cooldown} {cooldownLabel}";
        }

        private static string BuildEffectTooltip(BattleEffectSO effect)
        {
            if (effect == null)
                return string.Empty;

            return $"{effect.Name}\n{effect.Description}";
        }

        private static string GetCooldownLabel(int cooldown)
        {
            int absoluteCooldown = Math.Abs(cooldown);
            int lastTwoDigits = absoluteCooldown % 100;
            int lastDigit = absoluteCooldown % 10;

            if (lastDigit == 1 && lastTwoDigits != 11)
                return "раунд";

            if (lastDigit >= 2 && lastDigit <= 4 && (lastTwoDigits < 12 || lastTwoDigits > 14))
                return "раунда";

            return "раундов";
        }

        private readonly struct UnitAbilityRenderData
        {
            public UnitAbilityRenderData(Sprite icon, string tooltip = null)
            {
                Icon = icon;
                Tooltip = tooltip ?? string.Empty;
            }

            public Sprite Icon { get; }
            public string Tooltip { get; }
        }

        private readonly struct UnitEffectRenderData
        {
            public UnitEffectRenderData(Sprite icon, string tooltip = null)
            {
                Icon = icon;
                Tooltip = tooltip ?? string.Empty;
            }

            public Sprite Icon { get; }
            public string Tooltip { get; }
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
    }
}
