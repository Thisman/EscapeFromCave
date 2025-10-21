using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public sealed class HintAnimationController : MonoBehaviour
{
    [SerializeField] private float _amplitude = 0.3f;
    [SerializeField, Min(0.01f)] private float _cycleDuration = 1.25f;
    [SerializeField] private AnimationCurve _offsetCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool _useUnscaledTime;

    private Vector3 _baseLocalPosition;
    private bool _hasBaseLocalPosition;
    private Tween _animation;

    private void Awake()
    {
        CacheBasePosition();
    }

    private void OnEnable()
    {
        CacheBasePosition();
    }

    public void Play()
    {
        _baseLocalPosition = transform.localPosition;
        _hasBaseLocalPosition = true;
        RestartAnimation();
    }

    public void Stop()
    {
        StopInternal(resetPosition: true);
    }

    private void RestartAnimation()
    {
        StopInternal(resetPosition: false);

        transform.localPosition = _baseLocalPosition;

        float duration = Mathf.Max(0.01f, _cycleDuration);
        if (Mathf.Approximately(duration, 0f) || Mathf.Approximately(_amplitude, 0f))
        {
            return;
        }

        float halfDuration = duration * 0.5f;

        var sequence = DOTween.Sequence();
        sequence.Append(transform.DOLocalMoveY(_baseLocalPosition.y + _amplitude, halfDuration).SetEase(_offsetCurve));
        sequence.Append(transform.DOLocalMoveY(_baseLocalPosition.y, halfDuration).SetEase(_offsetCurve));
        sequence.SetLoops(-1, LoopType.Restart)
                .SetUpdate(_useUnscaledTime)
                .OnKill(() => _animation = null);

        _animation = sequence;
    }

    private void StopInternal(bool resetPosition)
    {
        if (_animation != null)
        {
            _animation.Kill();
            _animation = null;
        }

        if (resetPosition && _hasBaseLocalPosition)
        {
            transform.localPosition = _baseLocalPosition;
        }
    }

    private void OnDisable()
    {
        StopInternal(resetPosition: true);
    }

    private void CacheBasePosition()
    {
        _baseLocalPosition = transform.localPosition;
        _hasBaseLocalPosition = true;
    }
}
