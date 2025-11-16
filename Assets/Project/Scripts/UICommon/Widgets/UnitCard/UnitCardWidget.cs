using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UICommon.Widgets
{
    public readonly struct UnitAbilityRenderData
    {
        public UnitAbilityRenderData(Sprite icon, string tooltip = null)
        {
            Icon = icon;
            Tooltip = tooltip ?? string.Empty;
        }

        public Sprite Icon { get; }
        public string Tooltip { get; }
    }

    public readonly struct UnitEffectRenderData
    {
        public UnitEffectRenderData(Sprite icon, string tooltip = null)
        {
            Icon = icon;
            Tooltip = tooltip ?? string.Empty;
        }

        public Sprite Icon { get; }
        public string Tooltip { get; }
    }

    public readonly struct UnitCardRenderData
    {
        public UnitCardRenderData(
            string title,
            Sprite icon,
            IReadOnlyList<string> stats,
            string tooltip = null,
            IReadOnlyList<UnitAbilityRenderData> abilities = null,
            IReadOnlyList<UnitEffectRenderData> effects = null)
        {
            Title = title ?? string.Empty;
            Icon = icon;
            Tooltip = tooltip ?? title ?? string.Empty;
            Stats = stats ?? Array.Empty<string>();
            Abilities = abilities ?? Array.Empty<UnitAbilityRenderData>();
            Effects = effects ?? Array.Empty<UnitEffectRenderData>();
        }

        public string Title { get; }
        public Sprite Icon { get; }
        public string Tooltip { get; }
        public IReadOnlyList<string> Stats { get; }
        public IReadOnlyList<UnitAbilityRenderData> Abilities { get; }
        public IReadOnlyList<UnitEffectRenderData> Effects { get; }
    }

    public sealed class UnitCardWidget
    {
        public const string BlockClassName = "unit-card";
        public const string SelectedModifierClassName = "unit-card--selected";
        public const string InfoLineClassName = "unit-card__info-line";
        private const string AbilityInfoClassName = "ability-info";
        private const string EffectInfoClassName = "effect-info";

        private readonly List<Label> _infoLabels = new();
        private readonly List<VisualElement> _abilityElements = new();
        private readonly List<VisualElement> _effectElements = new();
        private readonly VisualElement _root;
        private readonly VisualElement _icon;
        private readonly Label _title;
        private readonly VisualElement _infoContainer;
        private readonly VisualElement _abilitiesContainer;
        private readonly VisualElement _effectsContainer;

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

            int statsCount = data.Stats?.Count ?? 0;
            EnsureInfoLabelCount(statsCount);

            for (int i = 0; i < _infoLabels.Count; i++)
            {
                Label label = _infoLabels[i];
                if (label == null)
                    continue;

                if (i < statsCount)
                {
                    label.text = data.Stats[i];
                    label.style.display = DisplayStyle.Flex;
                }
                else
                {
                    label.text = string.Empty;
                    label.style.display = DisplayStyle.None;
                }
            }

            RenderAbilityList(data.Abilities);
            RenderEffectList(data.Effects);

            _root?.EnableInClassList(SelectedModifierClassName, false);
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

        private void RenderAbilityList(IReadOnlyList<UnitAbilityRenderData> abilities)
        {
            RenderIconList(abilities, _abilitiesContainer, _abilityElements, AbilityInfoClassName);
        }

        private void RenderEffectList(IReadOnlyList<UnitEffectRenderData> effects)
        {
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
    }
}
