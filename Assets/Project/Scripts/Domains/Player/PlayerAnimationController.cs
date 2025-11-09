using UnityEngine;
using VContainer;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private float _movementThreshold = 0.01f;
    [SerializeField] private float _scaleAmplitude = 0.1f;
    [SerializeField] private float _scaleFrequency = 2f;

    [Inject] private readonly GameSession _gameSession;

    private Vector3 _initialScale;

    private void Start()
    {
        _spriteRenderer.sprite = _gameSession.HeroDefinition.Icon;
        _initialScale = transform.localScale;
    }

    private void Update()
    {
        if (_spriteRenderer == null || _rigidbody == null)
            return;

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
        var scaleOffset = Mathf.Sin(Time.time * _scaleFrequency) * _scaleAmplitude;
        var targetScale = _initialScale * (1f + scaleOffset);
        transform.localScale = targetScale;
    }
}