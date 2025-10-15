using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField] private Rigidbody2D body;

    private IInputService _inputService;
    private InputAction _moveAction;
    private Vector2 _movement;

    [Inject]
    public void Construct(IInputService inputService)
    {
        _inputService = inputService;
        SubscribeToInput();
    }

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
            body.velocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (!body)
            return;

        body.velocity = _movement * moveSpeed;
    }

    private void SubscribeToInput()
    {
        if (_moveAction != null || _inputService == null)
            return;

        _moveAction = _inputService.Actions.FindAction("PlayerMove", throwIfNotFound: true);
        _moveAction.performed += OnMovePerformed;
        _moveAction.canceled += OnMoveCanceled;
    }

    private void UnsubscribeFromInput()
    {
        if (_moveAction == null)
            return;

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
