using UnityEngine;
using VContainer;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private float _movementThreshold = 0.01f;
    [SerializeField] private float _scaleAmplitude = 0.03f;
    [SerializeField] private float _scaleFrequency = 6f;

    private Vector3 _initialScale;
    private float _scaleAnimationStartTime;

    private void Start()
    {
        _spriteRenderer.sprite = _playerController.GetPlayer().Definition.Icon;
        _initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        _initialScale = transform.localScale;
        ScheduleScaleAnimation();
    }

    private void OnDisable()
    {
        transform.localScale = _initialScale;
    }

    private void Update()
    {
        var velocity = _rigidbody.linearVelocity;

        if (velocity.x > _movementThreshold)
        {
            _spriteRenderer.flipX = true;
        }
        else if (velocity.x < -_movementThreshold)
        {
            _spriteRenderer.flipX = false;
        }

        AnimateScale();
    }

    private void AnimateScale()
    {
        if (_scaleAmplitude <= 0f || _scaleFrequency <= 0f)
        {
            transform.localScale = _initialScale;
            return;
        }

        float elapsed = Time.time - _scaleAnimationStartTime;
        if (elapsed < 0f)
        {
            transform.localScale = _initialScale;
            return;
        }

        float scaleOffset = Mathf.Sin(elapsed * _scaleFrequency) * _scaleAmplitude;
        var targetScale = _initialScale;
        targetScale.y = _initialScale.y * (1f + scaleOffset);
        transform.localScale = targetScale;
    }

    private void ScheduleScaleAnimation()
    {
        _scaleAnimationStartTime = Time.time + UnityEngine.Random.Range(0f, 0.5f);
    }
}
