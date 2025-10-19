using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using System.Linq;

[RequireComponent(typeof(Collider2D))]
public sealed class PlayerInteraction : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float interactRadius = 1.5f;
    [SerializeField] private LayerMask interactableMask = ~0;
    [SerializeField, Min(1)] private int maxCandidates = 16;
    [SerializeField] private bool drawGizmos = true;

    [Inject] private IInputService _inputService;
    [Inject] private SceneLoader _sceneLoader;
    [Inject] private DialogController _dialogController;

    private InputAction _interactAction;
    private Collider2D[] _hits;
    private InteractionController _currentTarget;
    private GameObject _actor;
    private Collider2D _lastWarnedCollider;

    private void Awake()
    {
        _actor = gameObject;
        _hits = new Collider2D[maxCandidates];
    }

    public void Start()
    {
        SubscribeToInput();
    }

    private void OnEnable()
    {
        SubscribeToInput();
    }

    private void OnDisable()
    {
        UnsubscribeFromInput();
    }

    private void Update()
    {
        AcquireTargetInRadius();
    }

    private void SubscribeToInput()
    {
        if (_interactAction != null)
            return;

        if (_inputService == null)
        {
            Debug.LogError($"[PlayerInteraction] Input service is missing on '{name}'. Interaction input will be disabled.");
            return;
        }

        try
        {
            _interactAction = _inputService.Actions.FindAction("Interact", throwIfNotFound: true);
        }
        catch (InvalidOperationException ex)
        {
            Debug.LogError($"[PlayerInteraction] Failed to find 'Interact' action for '{name}'. {ex.Message}");
            return;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerInteraction] Unexpected error while subscribing to interact action for '{name}': {ex}");
            return;
        }

        _interactAction.performed += OnInteractPressed;
        Debug.Log($"[PlayerInteraction] Subscribed to interact input for '{name}'.");
    }

    private void UnsubscribeFromInput()
    {
        if (_interactAction == null)
            return;

        _interactAction.performed -= OnInteractPressed;
        _interactAction = null;
        Debug.Log($"[PlayerInteraction] Unsubscribed from interact input for '{name}'.");
    }

    private void AcquireTargetInRadius()
    {
        Vector2 center = transform.position;
        _hits = Physics2D.OverlapCircleAll(center, interactRadius, interactableMask);

        var collider = _hits.FirstOrDefault();
        if (collider == null)
            return;

        if (!TryGetInteractable(collider, out InteractionController target))
        {
            if (_lastWarnedCollider != collider)
            {
                Debug.LogWarning($"[PlayerInteraction] Collider '{collider.name}' does not provide an InteractionController.");
                _lastWarnedCollider = collider;
            }
            return;
        }

        _lastWarnedCollider = null;

        if (!ReferenceEquals(target, _currentTarget))
        {
            _currentTarget = target;
            var targetName = (_currentTarget as MonoBehaviour)?.name ?? _currentTarget.ToString();
            Debug.Log($"[PlayerInteraction] '{name}' switched interaction target to '{targetName}'.");
        }

        // Очистим ссылки для удобства
        for (int i = 0; i < _hits.Length - 1; i++)
            _hits[i] = null;
    }

    private static bool TryGetInteractable(Collider2D col, out InteractionController interactable)
    {
        if (col.TryGetComponent(out interactable))
            return true;

        interactable = col.GetComponentInParent<InteractionController>();
        return interactable != null;
    }

    private void OnInteractPressed(InputAction.CallbackContext _)
    {
        if (_currentTarget == null)
        {
            Debug.LogWarning($"[PlayerInteraction] Interact input received for '{name}', but no interaction target is selected.");
            return;
        }

        var ctxData = new InteractionContext
        {
            Actor = _actor,
            Target = (_currentTarget as MonoBehaviour)?.gameObject,
            Point = transform.position,
            Time = Time.time,
            SceneLoader = _sceneLoader,
            DialogController = _dialogController,
        };

        if (_sceneLoader == null)
        {
            Debug.LogWarning($"[PlayerInteraction] SceneLoader was not injected for '{name}'. Scene-based interactions may fail.");
        }

        _currentTarget.TryInteract(ctxData);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
