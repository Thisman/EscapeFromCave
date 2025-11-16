using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UICommon.Widgets
{
    public readonly struct UnitCardRenderData
    {
        public UnitCardRenderData(string title, Sprite icon, IReadOnlyList<string> stats, string tooltip = null)
        {
            Title = title ?? string.Empty;
            Icon = icon;
            Tooltip = tooltip ?? title ?? string.Empty;
            Stats = stats ?? Array.Empty<string>();
        }

        public string Title { get; }
        public Sprite Icon { get; }
        public string Tooltip { get; }
        public IReadOnlyList<string> Stats { get; }
    }

    public sealed class UnitCardWidget
    {
        public const string BlockClassName = "unit-card";
        public const string SelectedModifierClassName = "unit-card--selected";
        public const string InfoLineClassName = "unit-card__info-line";

        private readonly List<Label> _infoLabels = new();
        private readonly VisualElement _root;
        private readonly VisualElement _icon;
        private readonly Label _title;
        private readonly VisualElement _infoContainer;

        public UnitCardWidget(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _icon = _root.Q<VisualElement>("Icon");
            _title = _root.Q<Label>("Title");
            _infoContainer = _root.Q<VisualElement>("Info");

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
    }
}
