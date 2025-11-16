using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UICommon.Widgets
{
    public readonly struct UnitCardIconRenderData
    {
        public UnitCardIconRenderData(Sprite icon, string tooltip = null)
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
            IReadOnlyDictionary<string, string> infoEntries,
            IReadOnlyList<string> infoKeys,
            string tooltip = null,
            IReadOnlyList<UnitCardIconRenderData> abilities = null,
            IReadOnlyList<UnitCardIconRenderData> effects = null)
        {
            Title = title ?? string.Empty;
            Icon = icon;
            Tooltip = tooltip ?? title ?? string.Empty;
            InfoEntries = infoEntries ?? EmptyInfoEntries;
            InfoKeys = infoKeys ?? Array.Empty<string>();
            Abilities = abilities ?? Array.Empty<UnitCardIconRenderData>();
            Effects = effects ?? Array.Empty<UnitCardIconRenderData>();
        }

        private static readonly IReadOnlyDictionary<string, string> EmptyInfoEntries =
            new Dictionary<string, string>();

        public string Title { get; }
        public Sprite Icon { get; }
        public string Tooltip { get; }
        public IReadOnlyDictionary<string, string> InfoEntries { get; }
        public IReadOnlyList<string> InfoKeys { get; }
        public IReadOnlyList<UnitCardIconRenderData> Abilities { get; }
        public IReadOnlyList<UnitCardIconRenderData> Effects { get; }
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

        public static readonly IReadOnlyList<string> UnitDefinitionInfoTemplate = new[]
        {
            InfoKeys.Health,
            InfoKeys.Damage,
            InfoKeys.AttackKind,
            InfoKeys.DamageType,
            InfoKeys.PhysicalDefense,
            InfoKeys.MagicDefense,
            InfoKeys.AbsoluteDefense,
            InfoKeys.CritChance,
            InfoKeys.CritMultiplier,
            InfoKeys.MissChance
        };

        public static readonly IReadOnlyList<string> SquadInfoTemplate = new[]
        {
            InfoKeys.Count,
            InfoKeys.Health,
            InfoKeys.Damage,
            InfoKeys.Initiative,
            InfoKeys.PhysicalDefense,
            InfoKeys.MagicDefense,
            InfoKeys.AbsoluteDefense,
            InfoKeys.AttackKind,
            InfoKeys.DamageType,
            InfoKeys.CritChance,
            InfoKeys.CritMultiplier,
            InfoKeys.MissChance
        };

        private readonly List<Label> _infoLabels = new();
        private readonly VisualElement _root;
        private readonly VisualElement _icon;
        private readonly Label _title;
        private readonly VisualElement _infoContainer;
        private readonly VisualElement _abilityList;
        private readonly VisualElement _effectsList;

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

            int infoCount = data.InfoKeys?.Count ?? 0;
            EnsureInfoLabelCount(infoCount);

            for (int i = 0; i < _infoLabels.Count; i++)
            {
                Label label = _infoLabels[i];
                if (label == null)
                    continue;

                if (i < infoCount)
                {
                    string key = data.InfoKeys[i];
                    label.text = TryGetInfoEntry(data.InfoEntries, key);
                    label.style.display = string.IsNullOrEmpty(label.text) ? DisplayStyle.None : DisplayStyle.Flex;
                }
                else
                {
                    label.text = string.Empty;
                    label.style.display = DisplayStyle.None;
                }
            }

            _root?.EnableInClassList(SelectedModifierClassName, false);

            RenderIconList(_abilityList, data.Abilities, AbilityIconClassName);
            RenderIconList(_effectsList, data.Effects, EffectIconClassName);
        }

        private static string TryGetInfoEntry(IReadOnlyDictionary<string, string> entries, string key)
        {
            if (entries == null || string.IsNullOrEmpty(key))
                return string.Empty;

            return entries.TryGetValue(key, out string value) ? value ?? string.Empty : string.Empty;
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
