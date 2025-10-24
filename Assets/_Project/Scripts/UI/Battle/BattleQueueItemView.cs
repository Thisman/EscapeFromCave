using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BattleQueueItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _icon;
    [SerializeField] private RectTransform _animatedTarget;
    [SerializeField] private float _hoverOffset = 10f;
    [SerializeField] private float _animationDuration = 0.2f;
    [SerializeField] private Ease _animationEase = Ease.OutQuad;

    private RectTransform _targetRectTransform;
    private Vector2 _initialAnchoredPosition;
    private Tween _hoverTween;

    public void Bind(IReadOnlyUnitModel unit)
    {
        if (_icon == null)
            return;

        if (unit?.Definition != null)
        {
            _icon.sprite = unit.Definition.Icon;
            _icon.enabled = unit.Definition.Icon != null;
        }
        else
        {
            _icon.sprite = null;
            _icon.enabled = false;
        }
    }

    private void Awake()
    {
        _targetRectTransform = _animatedTarget != null ? _animatedTarget : transform as RectTransform;

        if (_targetRectTransform != null)
            _initialAnchoredPosition = _targetRectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        if (_targetRectTransform == null)
            return;

        _initialAnchoredPosition = _targetRectTransform.anchoredPosition;
        ResetPosition();
    }

    private void OnDisable()
    {
        KillHoverTween();
        ResetPosition();
    }

    private void OnDestroy()
    {
        KillHoverTween();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_targetRectTransform == null)
            return;

        AnimateTo(_initialAnchoredPosition + new Vector2(0f, -_hoverOffset));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_targetRectTransform == null)
            return;

        AnimateTo(_initialAnchoredPosition);
    }

    private void AnimateTo(Vector2 targetPosition)
    {
        KillHoverTween();

        _hoverTween = _targetRectTransform
            .DOAnchorPos(targetPosition, _animationDuration)
            .SetEase(_animationEase);
    }

    private void ResetPosition()
    {
        if (_targetRectTransform == null)
            return;

        _targetRectTransform.anchoredPosition = _initialAnchoredPosition;
    }

    private void KillHoverTween()
    {
        if (_hoverTween == null)
            return;

        _hoverTween.Kill();
        _hoverTween = null;
    }
}
