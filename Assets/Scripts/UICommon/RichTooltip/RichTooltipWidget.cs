using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UICommon.Widgets
{
    public sealed class RichTooltipWidget
    {
        public const string BlockClassName = "rich-tooltip";
        public const string VisibleModifierClassName = BlockClassName + "--visible";

        private const string TooltipRootName = "Tooltip";
        private const string ContentElementName = "Content";
        private const float VerticalOffset = 8f;

        private readonly VisualElement _root;
        private readonly Label _content;

        public RichTooltipWidget(VisualElement root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            _root = root.name == TooltipRootName
                ? root
                : root.Q<VisualElement>(TooltipRootName) ?? root;

            _content = _root.Q<Label>(ContentElementName);
        }

        public VisualElement Root => _root;

        public void Show(string text, Vector2 anchor)
        {
            if (_root == null)
                return;

            if (_content != null)
                _content.text = text ?? string.Empty;

            PositionAt(anchor);
            _root.EnableInClassList(VisibleModifierClassName, true);
        }

        public void Hide()
        {
            if (_root == null)
                return;

            _root.EnableInClassList(VisibleModifierClassName, false);
        }

        private void PositionAt(Vector2 anchor)
        {
            _root.style.left = anchor.x;
            _root.style.top = anchor.y - VerticalOffset;
        }
    }
}
