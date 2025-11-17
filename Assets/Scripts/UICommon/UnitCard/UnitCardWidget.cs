using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UICommon.Widgets
{
    public enum UnitCardStatField
    {
        Count,
        Health,
        DamageRange,
        Initiative,
        PhysicalDefense,
        MagicDefense,
        AbsoluteDefense,
        AttackKind,
        DamageType,
        CritChance,
        CritMultiplier,
        MissChance
    }

    public readonly struct UnitCardRenderData
    {
        public UnitCardRenderData(
            IReadOnlySquadModel squad,
            IReadOnlyList<UnitCardStatField> stats,
            IReadOnlyList<BattleAbilitySO> abilities = null,
            IReadOnlyList<BattleEffectSO> effects = null,
            string tooltip = null)
        {
            Squad = squad;
            Title = squad?.UnitName ?? string.Empty;
            Icon = squad?.Icon;
            Tooltip = tooltip ?? Title;
            Stats = stats ?? Array.Empty<UnitCardStatField>();
            Abilities = abilities ?? (squad?.Abilities ?? Array.Empty<BattleAbilitySO>());
            Effects = effects ?? Array.Empty<BattleEffectSO>();
        }

        public IReadOnlySquadModel Squad { get; }
        public string Title { get; }
        public Sprite Icon { get; }
        public string Tooltip { get; }
        public IReadOnlyList<UnitCardStatField> Stats { get; }
        public IReadOnlyList<BattleAbilitySO> Abilities { get; }
        public IReadOnlyList<BattleEffectSO> Effects { get; }
    }

    public sealed class UnitCardWidget
    {
        public const string BlockClassName = "unit-card";
        public const string SelectedModifierClassName = "unit-card--selected";
        public const string InfoLineClassName = "unit-card__info-line";
        private const string AbilityInfoClassName = "ability-info";
        private const string EffectInfoClassName = "effect-info";

        private readonly List<Label> _infoLabels = new();
        private readonly VisualElement _root;
        private readonly VisualElement _icon;
        private readonly Label _title;
        private readonly VisualElement _infoContainer;
        private readonly VisualElement _abilityList;
        private readonly VisualElement _effectsList;
        private readonly RichTooltipWidget _abilityTooltip;

        public UnitCardWidget(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _icon = _root.Q<VisualElement>("Icon");
            _title = _root.Q<Label>("Title");
            _infoContainer = _root.Q<VisualElement>("Info");
            _abilityList = _root.Q<VisualElement>("AbilityList");
            _effectsList = _root.Q<VisualElement>("EffectsList");
            VisualElement tooltipRoot = _root.Q<VisualElement>("AbilityTooltip");
            if (tooltipRoot != null)
                _abilityTooltip = new RichTooltipWidget(tooltipRoot);

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
            ApplyHeader(data);
            RenderStats(data);
            RenderAbilities(data);
            RenderEffects(data);

            _root?.EnableInClassList(SelectedModifierClassName, false);
        }

        private void ApplyHeader(UnitCardRenderData data)
        {
            if (_icon != null)
            {
                if (data.Icon != null)
                    _icon.style.backgroundImage = new StyleBackground(data.Icon);
                else
                    _icon.style.backgroundImage = new StyleBackground();

                _icon.tooltip = data.Tooltip ?? string.Empty;
            }

            if (_title != null)
                _title.text = data.Title ?? string.Empty;
        }

        private void RenderStats(UnitCardRenderData data)
        {
            int statsCount = data.Stats?.Count ?? 0;
            EnsureInfoLabelCount(statsCount);

            for (int i = 0; i < _infoLabels.Count; i++)
            {
                Label label = _infoLabels[i];
                if (label == null)
                    continue;

                if (i < statsCount)
                {
                    label.text = FormatStat(data.Stats[i], data.Squad);
                    label.style.display = DisplayStyle.Flex;
                }
                else
                {
                    label.text = string.Empty;
                    label.style.display = DisplayStyle.None;
                }
            }
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

        private void RenderAbilities(UnitCardRenderData data)
        {
            if (_abilityList == null)
                return;

            _abilityList.Clear();
            HideAbilityTooltip();

            IReadOnlyList<BattleAbilitySO> abilities = data.Abilities;
            if (abilities == null || abilities.Count == 0)
            {
                _abilityList.style.display = DisplayStyle.None;
                return;
            }

            foreach (BattleAbilitySO ability in abilities)
            {
                if (ability == null)
                    continue;

                VisualElement abilityElement = new();
                abilityElement.AddToClassList(AbilityInfoClassName);
                RegisterAbilityTooltipEvents(abilityElement, ability);

                if (ability.Icon != null)
                    abilityElement.style.backgroundImage = new StyleBackground(ability.Icon);

                _abilityList.Add(abilityElement);
            }

            _abilityList.style.display = _abilityList.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RenderEffects(UnitCardRenderData data)
        {
            if (_effectsList == null)
                return;

            _effectsList.Clear();

            IReadOnlyList<BattleEffectSO> effects = data.Effects;
            if (effects == null || effects.Count == 0)
            {
                _effectsList.style.display = DisplayStyle.None;
                return;
            }

            foreach (BattleEffectSO effect in effects)
            {
                if (effect == null)
                    continue;

                VisualElement effectElement = new();
                effectElement.AddToClassList(EffectInfoClassName);
                effectElement.tooltip = effect.Name ?? string.Empty;

                if (effect.Icon != null)
                    effectElement.style.backgroundImage = new StyleBackground(effect.Icon);

                _effectsList.Add(effectElement);
            }

            _effectsList.style.display = _effectsList.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RegisterAbilityTooltipEvents(VisualElement abilityElement, BattleAbilitySO ability)
        {
            if (abilityElement == null)
                return;

            abilityElement.RegisterCallback<PointerEnterEvent>(_ => ShowAbilityTooltip(ability, abilityElement));
            abilityElement.RegisterCallback<PointerLeaveEvent>(_ => HideAbilityTooltip());
        }

        private void ShowAbilityTooltip(BattleAbilitySO ability, VisualElement sourceElement)
        {
            if (_abilityTooltip == null || ability == null || sourceElement == null || _root == null)
                return;

            Rect worldBound = sourceElement.worldBound;
            Vector2 worldAnchor = new(worldBound.center.x, worldBound.yMin);
            Vector2 localAnchor = _root.WorldToLocal(worldAnchor);

            string tooltipText = string.IsNullOrWhiteSpace(ability.Description)
                ? ability.AbilityName ?? string.Empty
                : ability.Description;

            _abilityTooltip.Show(tooltipText, localAnchor);
        }

        private void HideAbilityTooltip()
        {
            _abilityTooltip?.Hide();
        }

        private static string FormatStat(UnitCardStatField stat, IReadOnlySquadModel squad)
        {
            if (squad == null)
                return string.Empty;

            return stat switch
            {
                UnitCardStatField.Count => $"Количество: {FormatValue(squad.Count)}",
                UnitCardStatField.Health => $"Здоровье: {FormatValue(squad.Health)}",
                UnitCardStatField.DamageRange => FormatDamageRange(squad),
                UnitCardStatField.Initiative => $"Инициатива: {FormatValue(squad.Initiative)}",
                UnitCardStatField.PhysicalDefense => $"Физическая защита: {FormatPercent(squad.PhysicalDefense)}",
                UnitCardStatField.MagicDefense => $"Магическая защита: {FormatPercent(squad.MagicDefense)}",
                UnitCardStatField.AbsoluteDefense => $"Абсолютная защита: {FormatPercent(squad.AbsoluteDefense)}",
                UnitCardStatField.AttackKind => $"Тип атаки: {FormatAttackKind(squad.AttackKind)}",
                UnitCardStatField.DamageType => $"Тип урона: {FormatDamageType(squad.DamageType)}",
                UnitCardStatField.CritChance => $"Шанс критического удара: {FormatPercent(squad.CritChance)}",
                UnitCardStatField.CritMultiplier => $"Критический множитель: {FormatValue(squad.CritMultiplier)}",
                UnitCardStatField.MissChance => $"Шанс промаха: {FormatPercent(squad.MissChance)}",
                _ => string.Empty
            };
        }

        private static string FormatDamageRange(IReadOnlySquadModel squad)
        {
            if (squad == null)
                return string.Empty;

            (float minDamage, float maxDamage) = squad.GetBaseDamageRange();
            return $"Урон: {FormatValue(minDamage)} - {FormatValue(maxDamage)}";
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
