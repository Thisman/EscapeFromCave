using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HintAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _target;
    [SerializeField, Min(0f)] private float _amplitude = 0.5f;
    [SerializeField, Min(0f)] private float _frequency = 1f;
    [SerializeField] private float _phaseOffset;
    [SerializeField] private bool _useUnscaledTime = true;

    private Transform _targetTransform;
    private Vector3 _baseLocalPosition;
    private bool _hasCachedBasePosition;
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
        if (_targetTransform)
        {
            _targetTransform.localPosition = _baseLocalPosition;
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

    private void CacheBasePosition(bool force = false)
    {
        var previousTargetTransform = _targetTransform;
        _targetTransform = null;

        if (!_target)
        {
            _target = GetComponent<SpriteRenderer>();
        }

        if (_target)
        {
            _targetTransform = _target.transform;
        }

        if (_targetTransform == null)
        {
            _hasCachedBasePosition = false;
            return;
        }

        if (previousTargetTransform != _targetTransform)
        {
            _hasCachedBasePosition = false;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            _baseLocalPosition = _targetTransform.localPosition;
            _hasCachedBasePosition = true;
            return;
        }
#endif

        if (_hasCachedBasePosition && !force)
        {
            return;
        }

        _baseLocalPosition = _targetTransform.localPosition;
        _hasCachedBasePosition = true;
    }

    public void SyncBasePositionWithTarget()
    {
        CacheBasePosition(force: true);

        if (!_targetTransform)
        {
            return;
        }

        _baseLocalPosition = _targetTransform.localPosition;
        _hasCachedBasePosition = true;

        if (!isActiveAndEnabled)
        {
            return;
        }

        RestartAnimation();
    }

    private void RestartAnimation()
    {
        KillAnimation();

        if (!_targetTransform)
        {
            return;
        }

        if (_frequency <= 0f || _amplitude <= 0f)
        {
            _targetTransform.localPosition = _baseLocalPosition;
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
        if (!_targetTransform)
        {
            return;
        }

        float offset = Mathf.Sin(phase) * _amplitude;
        _targetTransform.localPosition = _baseLocalPosition + Vector3.up * offset;
    }
}
