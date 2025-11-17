using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UICommon.Widgets
{
    public sealed class RichTooltipWidget
    {
        public const string BlockClassName = "rich-tooltip";
        public const string VisibleModifierClassName = "rich-tooltip--visible";

        private const float DefaultVerticalOffset = 8f;
        private const int FadeDurationMs = 150;

        private readonly VisualElement _root;
        private readonly Label _contentLabel;
        private readonly float _verticalOffset;

        private IVisualElementScheduledItem _hideSchedule;
        private Vector2 _anchorPosition;
        private bool _hasAnchor;
        private bool _isVisible;

        public RichTooltipWidget(VisualElement root, float verticalOffset = DefaultVerticalOffset)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _contentLabel = _root.Q<Label>("Content") ?? throw new ArgumentException("Content element is missing", nameof(root));
            _verticalOffset = verticalOffset;

            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            HideImmediate();
        }

        public VisualElement Root => _root;

        public void Show(string text, Vector2 anchor)
        {
            _contentLabel.text = text ?? string.Empty;
            _anchorPosition = anchor;
            _hasAnchor = true;

            if (_hideSchedule != null)
            {
                _hideSchedule.Pause();
                _hideSchedule = null;
            }

            if (_root.style.display != DisplayStyle.Flex)
                _root.style.display = DisplayStyle.Flex;

            _root.EnableInClassList(VisibleModifierClassName, true);
            _isVisible = true;

            UpdateAnchorPosition();
        }

        public void Hide()
        {
            if (!_isVisible)
                return;

            _root.EnableInClassList(VisibleModifierClassName, false);
            _isVisible = false;
            _hasAnchor = false;

            if (_hideSchedule != null)
                _hideSchedule.Pause();

            _hideSchedule = _root.schedule.Execute(() =>
            {
                if (!_isVisible)
                    _root.style.display = DisplayStyle.None;
            }).StartingIn(FadeDurationMs);
        }

        private void HideImmediate()
        {
            _root.EnableInClassList(VisibleModifierClassName, false);
            _root.style.display = DisplayStyle.None;
            _isVisible = false;
            _hasAnchor = false;
        }

        private void UpdateAnchorPosition()
        {
            if (!_hasAnchor)
                return;

            float width = _root.resolvedStyle.width;
            float height = _root.resolvedStyle.height;

            if (float.IsNaN(width) || width <= 0f || float.IsNaN(height) || height <= 0f)
                return;

            float left = _anchorPosition.x - (width * 0.5f);
            float top = _anchorPosition.y - height - _verticalOffset;

            _root.style.left = left;
            _root.style.top = top;
        }

        private void OnGeometryChanged(GeometryChangedEvent _)
        {
            if (_isVisible)
                UpdateAnchorPosition();
        }
    }
}
