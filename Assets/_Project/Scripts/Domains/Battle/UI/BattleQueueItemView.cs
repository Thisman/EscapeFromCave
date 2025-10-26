using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BattleQueueItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private Transform _animatedTarget;
    [SerializeField] private float _hoverOffset = 10f;
    [SerializeField] private float _animationDuration = 0.2f;
    [SerializeField] private Ease _animationEase = Ease.OutQuad;

    private Transform _targetTransform;
    private Vector3 _initialLocalPosition;
    private Tween _hoverTween;
    private bool _pendingPointerExit;
    private bool _isHoverTweenRunning;

    public void Bind(IReadOnlySquadModel unit)
    {
        if (_icon == null)
            return;

        if (unit?.Definition != null)
        {
            _icon.sprite = unit.Definition.Icon;
            _icon.enabled = unit.Definition.Icon != null;
            _countText.text = unit.Count.ToString();
        }
        else
        {
            _icon.sprite = null;
            _icon.enabled = false;
            _countText.text = "";
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

        CacheInitialLocalPosition();
        _pendingPointerExit = false;
        _isHoverTweenRunning = false;
        ResetPosition();
    }

    private void OnDisable()
    {
        KillHoverTween();
        _pendingPointerExit = false;
        _isHoverTweenRunning = false;
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

        if (!IsTweenActive())
            CacheInitialLocalPosition();
        _pendingPointerExit = false;

        PlayHoverTween();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_targetTransform == null)
            return;

        if (IsTweenActive())
        {
            if (_isHoverTweenRunning)
                _pendingPointerExit = true;
            return;
        }

        _pendingPointerExit = false;
        PlayReturnTween();
    }

    private void PlayHoverTween()
    {
        PlayTween(_initialLocalPosition.y - _hoverOffset, true, OnHoverTweenCompleted);
    }

    private void PlayReturnTween()
    {
        PlayTween(_initialLocalPosition.y, false);
    }

    private void PlayTween(float targetY, bool isHoverTween, TweenCallback onComplete = null)
    {
        KillHoverTween();

        _isHoverTweenRunning = isHoverTween;
        _hoverTween = _targetTransform
            .DOLocalMoveY(targetY, _animationDuration)
            .SetEase(_animationEase)
            .OnComplete(() =>
            {
                _hoverTween = null;
                if (_isHoverTweenRunning == isHoverTween)
                    _isHoverTweenRunning = false;
                onComplete?.Invoke();
            });
    }

    private void OnHoverTweenCompleted()
    {
        if (!_pendingPointerExit)
            return;

        _pendingPointerExit = false;
        PlayReturnTween();
    }

    private void ResetPosition()
    {
        if (_targetTransform == null)
            return;

        _targetTransform.localPosition = _initialLocalPosition;
    }

    private void CacheInitialLocalPosition()
    {
        if (_targetTransform == null)
            return;

        _initialLocalPosition = _targetTransform.localPosition;
    }

    private bool IsTweenActive()
    {
        return _hoverTween != null && _hoverTween.IsActive() && _hoverTween.IsPlaying();
    }

    private void KillHoverTween()
    {
        if (_hoverTween == null)
            return;

        _hoverTween.Kill();
        _hoverTween = null;
        _isHoverTweenRunning = false;
    }
}
