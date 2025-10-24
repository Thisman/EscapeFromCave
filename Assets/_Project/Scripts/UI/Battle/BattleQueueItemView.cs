using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BattleQueueItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _icon;
    [SerializeField] private Transform _animatedTarget;
    [SerializeField] private float _hoverOffset = 10f;
    [SerializeField] private float _animationDuration = 0.2f;
    [SerializeField] private Ease _animationEase = Ease.OutQuad;

    private Transform _targetTransform;
    private Vector3 _initialLocalPosition;
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
        _targetTransform = _animatedTarget != null ? _animatedTarget : transform;

        if (_targetTransform != null)
            _initialLocalPosition = _targetTransform.localPosition;
    }

    private void OnEnable()
    {
        if (_targetTransform == null)
            return;

        _initialLocalPosition = _targetTransform.localPosition;
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
        if (_targetTransform == null)
            return;

        AnimateToY(_initialLocalPosition.y - _hoverOffset);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_targetTransform == null)
            return;

        AnimateToY(_initialLocalPosition.y);
    }

    private void AnimateToY(float targetY)
    {
        KillHoverTween();

        _hoverTween = _targetTransform
            .DOLocalMoveY(targetY, _animationDuration)
            .SetEase(_animationEase);
    }

    private void ResetPosition()
    {
        if (_targetTransform == null)
            return;

        _targetTransform.localPosition = _initialLocalPosition;
    }

    private void KillHoverTween()
    {
        if (_hoverTween == null)
            return;

        _hoverTween.Kill();
        _hoverTween = null;
    }
}
