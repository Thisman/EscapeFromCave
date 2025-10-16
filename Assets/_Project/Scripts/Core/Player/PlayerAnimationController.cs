using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private float _movementThreshold = 0.01f;

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