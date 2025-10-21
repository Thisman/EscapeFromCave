using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HintAnimationController : MonoBehaviour
{
    [SerializeField] private float _amplitude = 0.2f;
    [SerializeField] private float _cycleDuration = 1.5f;
    [SerializeField] private Ease _ease = Ease.InOutSine;

    private Tween _tween;
    private Vector3 _basePosition;
    private float _currentOffset;
    private bool _hasBasePosition;

    public void SetBasePosition(Vector3 worldPosition)
    {
        _basePosition = worldPosition;
        _hasBasePosition = true;
        ApplyPosition();
        TryStartTween();
    }

    private void OnEnable()
    {
        TryStartTween();
    }

    private void OnDisable()
    {
        _tween?.Pause();
    }

    private void OnDestroy()
    {
        _tween?.Kill();
    }

    private void TryStartTween()
    {
        if (!_hasBasePosition)
            return;

        if (_amplitude <= 0f)
        {
            StopTween();
            return;
        }

        float duration = Mathf.Max(0.01f, _cycleDuration);
        float halfCycle = duration * 0.5f;

        if (_tween != null && _tween.IsActive())
        {
            if (_tween.IsPlaying())
                return;

            _tween.Play();
            return;
        }

        _currentOffset = 0f;
        ApplyPosition();

        _tween = DOVirtual.Float(-_amplitude, _amplitude, halfCycle, value =>
        {
            _currentOffset = value;
            ApplyPosition();
        })
        .SetEase(_ease)
        .SetLoops(-1, LoopType.Yoyo)
        .SetLink(gameObject);

        _tween.Goto(halfCycle * 0.5f, true);
    }

    private void StopTween()
    {
        if (_tween == null)
            return;

        if (_tween.IsPlaying())
            _tween.Pause();

        _currentOffset = 0f;
        ApplyPosition();
    }

    private void ApplyPosition()
    {
        transform.position = _basePosition + Vector3.up * _currentOffset;
    }

    private void OnValidate()
    {
        _amplitude = Mathf.Max(0f, _amplitude);
        _cycleDuration = Mathf.Max(0.01f, _cycleDuration);
    }
}
