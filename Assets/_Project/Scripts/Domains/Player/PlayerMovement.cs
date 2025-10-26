using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private PlayerController _playerController;

    [Inject] private readonly InputService _inputService;

    private InputAction _moveAction;
    private Vector2 _movement;

    private void Awake()
    {
        if (!body)
            body = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        SubscribeToInput();
    }

    private void OnDisable()
    {
        UnsubscribeFromInput();
        _movement = Vector2.zero;

        if (body)
            body.linearVelocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (!body)
            return;

        float speed = _playerController != null ? _playerController.GetMovementSpeed() : 0f;
        body.linearVelocity = _movement * speed;
    }

    private void SubscribeToInput()
    {
        _moveAction = _inputService.Actions.FindAction("Move", throwIfNotFound: true);
        _moveAction.performed += OnMovePerformed;
        _moveAction.canceled += OnMoveCanceled;
    }

    private void UnsubscribeFromInput()
    {
        _moveAction.performed -= OnMovePerformed;
        _moveAction.canceled -= OnMoveCanceled;
        _moveAction = null;
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        _movement = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        _movement = Vector2.zero;
    }
}
