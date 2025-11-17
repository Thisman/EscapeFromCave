using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _body;
    [SerializeField] private int _speed;

    [Inject] private readonly InputService _inputService;

    private InputAction _moveAction;
    private Vector2 _movement;

    private void Awake()
    {
        if (!_body)
            _body = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        SubscribeToInput();
    }

    private void OnDisable()
    {
        UnsubscribeFromInput();
        _movement = Vector2.zero;

        if (_body)
            _body.linearVelocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (!_body)
            return;

        _body.linearVelocity = _movement * _speed;
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
