using UnityEngine;
using VContainer;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private float _movementThreshold = 0.01f;

    [Inject] private GameSession _gameSession;

    private void Start()
    {
        _spriteRenderer.sprite = _gameSession.HeroDefinition.Icon;
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
    }
}