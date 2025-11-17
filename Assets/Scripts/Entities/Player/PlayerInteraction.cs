using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

[RequireComponent(typeof(Collider2D))]
public sealed class PlayerInteraction : MonoBehaviour
{
    [SerializeField, Min(1)] private int _maxCandidates = 16;
    [SerializeField] private LayerMask _interactableMask = ~0;
    [SerializeField, Min(0.1f)] private float _interactRadius = 1.5f;

    [Inject] private readonly SceneLoader _sceneLoader;
    [Inject] private readonly InputService _inputService;
    [Inject] private readonly DialogManager _dialogManager;

    private GameObject _actor;
    private Collider2D[] _hits;
    private ContactFilter2D _interactableFilter;
    private readonly HashSet<InteractionController> _currentInteractables = new();
    private readonly HashSet<InteractionController> _frameInteractables = new();
    private readonly List<InteractionController> _pendingRemoval = new();
    private InputAction _interactAction;
    private Collider2D _lastWarnedCollider;
    private InteractionController _currentTarget;

    public float InteractRadius => _interactRadius;
    public IReadOnlyCollection<InteractionController> InteractablesInRange => _currentInteractables;
    public event Action<InteractionController> InteractableEnteredRange;
    public event Action<InteractionController> InteractableExitedRange;

    private void OnValidate()
    {
        _maxCandidates = Mathf.Max(1, _maxCandidates);
        ConfigureContactFilter();
    }

    private void Awake()
    {
        _actor = gameObject;
        _hits = new Collider2D[_maxCandidates];
        ConfigureContactFilter();
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
        int hitsCount = Physics2D.OverlapCircle(center, _interactRadius, _interactableFilter, _hits);

        _frameInteractables.Clear();

        InteractionController firstValidInteractable = null;

        for (int i = 0; i < hitsCount; i++)
        {
            var collider = _hits[i];
            _hits[i] = null;
            if (collider == null)
                continue;

            if (!TryGetInteractable(collider, out var interactable))
            {
                if (_lastWarnedCollider != collider)
                {
                    Debug.LogWarning($"[{nameof(PlayerInteraction)}.{nameof(AcquireTargetInRadius)}] Collider '{collider.name}' does not provide an InteractionController.");
                    _lastWarnedCollider = collider;
                }
                continue;
            }

            _frameInteractables.Add(interactable);
            if (firstValidInteractable == null)
                firstValidInteractable = interactable;

            if (_currentInteractables.Add(interactable))
                InteractableEnteredRange?.Invoke(interactable);
        }

        _lastWarnedCollider = null;

        _pendingRemoval.Clear();
        foreach (var interactable in _currentInteractables)
            if (!_frameInteractables.Contains(interactable))
                _pendingRemoval.Add(interactable);

        foreach (var removed in _pendingRemoval)
        {
            _currentInteractables.Remove(removed);
            InteractableExitedRange?.Invoke(removed);
        }
        _frameInteractables.Clear();

        if (firstValidInteractable == null)
        {
            if (_currentTarget != null)
                _currentTarget = null;
            return;
        }

        if (!ReferenceEquals(firstValidInteractable, _currentTarget))
        {
            _currentTarget = firstValidInteractable;
            var targetName = (_currentTarget as MonoBehaviour)?.name ?? _currentTarget.ToString();
            Debug.Log($"[{nameof(PlayerInteraction)}.{nameof(AcquireTargetInRadius)}] '{name}' switched interaction target to '{targetName}'.");
        }
    }

    private async void OnInteractPressed(InputAction.CallbackContext _)
    {
        if (_currentTarget == null || !_currentTarget.gameObject.activeSelf)
        {
            _currentTarget = null;
            Debug.LogWarning($"[{nameof(PlayerInteraction)}.{nameof(OnInteractPressed)}] Interact input received for '{name}', but no interaction target is selected.");
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

    private void ConfigureContactFilter()
    {
        _interactableFilter = new ContactFilter2D();
        _interactableFilter.SetLayerMask(_interactableMask);
        _interactableFilter.SetDepth(float.NegativeInfinity, float.PositiveInfinity);
    }
}
