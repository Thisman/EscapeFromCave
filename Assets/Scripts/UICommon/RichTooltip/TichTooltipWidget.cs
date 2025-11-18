using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UICommon.Widgets
{
    public sealed class TichTooltipWidget
    {
        private const string VisibleClassName = "rich-tooltip--visible";
        private const string TextElementName = "Text";
        private const float TooltipOffset = 8f;

        private readonly VisualElement _root;
        private readonly VisualElement _relativeRoot;
        private readonly Label _text;

        private VisualElement _currentTarget;

        public TichTooltipWidget(VisualElement root, VisualElement relativeRoot)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _relativeRoot = relativeRoot ?? throw new ArgumentNullException(nameof(relativeRoot));
            _text = _root.Q<Label>(TextElementName);
        }

        public void Show(string content, VisualElement target)
        {
            if (target == null)
                return;

            _currentTarget = target;

            if (_text != null)
                _text.text = content ?? string.Empty;

            _root.AddToClassList(VisibleClassName);
            UpdatePosition(target.worldBound);

            _root.schedule.Execute(() =>
            {
                if (_currentTarget == target)
                    UpdatePosition(target.worldBound);
            });
        }

        public void UpdatePositionFromTarget(VisualElement target)
        {
            if (target == null || !ReferenceEquals(_currentTarget, target))
                return;

            UpdatePosition(target.worldBound);
        }

        public void Hide()
        {
            _currentTarget = null;
            _root.RemoveFromClassList(VisibleClassName);
        }

        private void UpdatePosition(Rect targetWorldBounds)
        {
            if (_relativeRoot == null)
                return;

            Vector2 topCenter = new(targetWorldBounds.xMin + targetWorldBounds.width * 0.5f, targetWorldBounds.yMin);
            Vector2 localPosition = _relativeRoot.WorldToLocal(topCenter);

            float tooltipWidth = _root.resolvedStyle.width;
            float tooltipHeight = _root.resolvedStyle.height;

            float x = localPosition.x - tooltipWidth * 0.5f;
            float y = localPosition.y - tooltipHeight - TooltipOffset;

            float maxX = Mathf.Max(0f, _relativeRoot.resolvedStyle.width - tooltipWidth);
            x = Mathf.Clamp(x, 0f, maxX);
            y = Mathf.Max(0f, y);

            _root.style.left = x;
            _root.style.top = y;
        }
    }
}
