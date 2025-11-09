using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using System.Linq;

[RequireComponent(typeof(Collider2D))]
public sealed class PlayerInteraction : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxCandidates = 16;
    [SerializeField] private LayerMask interactableMask = ~0;
    [SerializeField, Min(0.1f)] private float interactRadius = 1.5f;

    [Inject] private readonly SceneLoader _sceneLoader;
    [Inject] private readonly InputService _inputService;
    [Inject] private readonly DialogManager _dialogManager;

    private GameObject _actor;
    private Collider2D[] _hits;
    private InputAction _interactAction;
    private Collider2D _lastWarnedCollider;
    private InteractionController _currentTarget;

    private void Awake()
    {
        _actor = gameObject;
        _hits = new Collider2D[maxCandidates];
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
        _interactAction = _inputService.Actions.FindAction("Interact", throwIfNotFound: true);
        _interactAction.performed += OnInteractPressed;
    }

    private void UnsubscribeFromInput()
    {
        _interactAction.performed -= OnInteractPressed;
        _interactAction = null;
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
                GameLogger.Warn($"[PlayerInteraction] Collider '{collider.name}' does not provide an InteractionController.");
                _lastWarnedCollider = collider;
            }
            return;
        }

        _lastWarnedCollider = null;

        if (!ReferenceEquals(target, _currentTarget))
        {
            _currentTarget = target;
            var targetName = (_currentTarget as MonoBehaviour)?.name ?? _currentTarget.ToString();
            GameLogger.Log($"[PlayerInteraction] '{name}' switched interaction target to '{targetName}'.");
        }

        for (int i = 0; i < _hits.Length - 1; i++)
            _hits[i] = null;
    }

    private async void OnInteractPressed(InputAction.CallbackContext _)
    {
        if (_currentTarget == null || !_currentTarget.gameObject.activeSelf)
        {
            _currentTarget = null;
            GameLogger.Warn($"[PlayerInteraction] Interact input received for '{name}', but no interaction target is selected.");
            return;
        }

        var ctxData = new InteractionContext
        {
            Actor = _actor,
            Time = Time.time,
            SceneLoader = _sceneLoader,
            InputService = _inputService,
            DialogManager = _dialogManager,
            Target = _currentTarget.gameObject,
        };

        await _currentTarget.TryInteract(ctxData);
    }

    private static bool TryGetInteractable(Collider2D col, out InteractionController interactable)
    {
        if (col.TryGetComponent(out interactable))
            return true;

        interactable = col.GetComponentInParent<InteractionController>();
        return interactable != null;
    }
}
