using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HintAnimationController : MonoBehaviour
{
    [SerializeField] private RectTransform _target;
    [SerializeField, Min(0f)] private float _amplitude = 10f;
    [SerializeField, Min(0f)] private float _frequency = 1f;
    [SerializeField] private float _phaseOffset;
    [SerializeField] private bool _useUnscaledTime = true;

    private Vector2 _baseAnchoredPosition;
    private Tween _animationTween;

    private void Awake()
    {
        CacheBasePosition();
    }

    private void OnEnable()
    {
        CacheBasePosition();
        RestartAnimation();
    }

    private void OnDisable()
    {
        KillAnimation();
        if (_target)
        {
            _target.anchoredPosition = _baseAnchoredPosition;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheBasePosition();
        if (!Application.isPlaying || !isActiveAndEnabled)
        {
            return;
        }

        RestartAnimation();
    }
#endif

    private void CacheBasePosition()
    {
        if (!_target)
        {
            _target = transform as RectTransform;
        }

        if (_target)
        {
            _baseAnchoredPosition = _target.anchoredPosition;
        }
    }

    private void RestartAnimation()
    {
        KillAnimation();

        if (!_target)
        {
            return;
        }

        if (_frequency <= 0f || _amplitude <= 0f)
        {
            _target.anchoredPosition = _baseAnchoredPosition;
            return;
        }

        float period = 1f / _frequency;
        float startPhase = Mathf.Repeat(_phaseOffset, Mathf.PI * 2f);
        UpdateTargetPosition(startPhase);

        _animationTween = DOVirtual.Float(startPhase, startPhase + Mathf.PI * 2f, period, UpdateTargetPosition)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .SetUpdate(_useUnscaledTime);
    }

    private void KillAnimation()
    {
        if (_animationTween == null)
        {
            return;
        }

        _animationTween.Kill();
        _animationTween = null;
    }

    private void UpdateTargetPosition(float phase)
    {
        if (!_target)
        {
            return;
        }

        float offset = Mathf.Sin(phase) * _amplitude;
        _target.anchoredPosition = _baseAnchoredPosition + Vector2.up * offset;
    }
}
