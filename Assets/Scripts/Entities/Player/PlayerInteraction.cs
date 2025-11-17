using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

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
    private readonly HashSet<InteractionHintController> _visibleHints = new();
    private readonly List<InteractionHintController> _hintsInRange = new();
    private readonly HashSet<InteractionHintController> _hintsSet = new();
    private readonly List<InteractionHintController> _hintsToHide = new();

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
        HideAllHints();
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
        int hitCount = Physics2D.OverlapCircleNonAlloc(center, interactRadius, _hits, interactableMask);

        if (hitCount <= 0)
        {
            _currentTarget = null;
            HideAllHints();
            return;
        }

        InteractionController target = null;
        Collider2D firstCollider = null;
        _hintsInRange.Clear();
        _hintsSet.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            var collider = _hits[i];
            if (!collider)
                continue;

            firstCollider ??= collider;

            if (target == null && TryGetInteractable(collider, out InteractionController candidate))
                target = candidate;

            if (TryGetHint(collider, out InteractionHintController hint) && _hintsSet.Add(hint))
                _hintsInRange.Add(hint);
        }

        UpdateHintVisibility();

        if (target == null)
        {
            _currentTarget = null;
            if (firstCollider != null && _lastWarnedCollider != firstCollider)
            {
                Debug.LogWarning($"[{nameof(PlayerInteraction)}.{nameof(AcquireTargetInRadius)}] Collider '{firstCollider.name}' does not provide an InteractionController.");
                _lastWarnedCollider = firstCollider;
            }
            return;
        }

        _lastWarnedCollider = null;

        if (!ReferenceEquals(target, _currentTarget))
        {
            _currentTarget = target;
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

    private static bool TryGetHint(Collider2D col, out InteractionHintController hint)
    {
        if (col.TryGetComponent(out hint))
            return true;

        hint = col.GetComponentInParent<InteractionHintController>();
        return hint != null;
    }

    private void UpdateHintVisibility()
    {
        foreach (var hint in _hintsInRange)
            hint.ShowHint();

        _hintsToHide.Clear();
        foreach (var visibleHint in _visibleHints)
        {
            if (!_hintsSet.Contains(visibleHint))
                _hintsToHide.Add(visibleHint);
        }

        foreach (var hint in _hintsToHide)
        {
            hint.HideHint();
            _visibleHints.Remove(hint);
        }

        foreach (var hint in _hintsInRange)
            _visibleHints.Add(hint);
    }

    private void HideAllHints()
    {
        foreach (var hint in _visibleHints)
            hint.HideHint();

        _visibleHints.Clear();
        _hintsInRange.Clear();
        _hintsSet.Clear();
        _hintsToHide.Clear();
    }
}
