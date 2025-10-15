using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using System.Linq;

[RequireComponent(typeof(Collider2D))]
public sealed class PlayerInteract2D : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField, Min(0.1f)] private float interactRadius = 1.5f;
    [SerializeField] private LayerMask interactableMask = ~0;

    [Header("Performance / Debug")]
    [SerializeField, Min(1)] private int maxCandidates = 16;
    [SerializeField] private bool drawGizmos = true;

    private IInputService _inputService;
    private InputAction _interactAction;

    private Collider2D[] _hits;
    private IInteractable _currentTarget;
    private GameObject _actor;

    [Inject]
    public void Construct(IInputService inputService)
    {
        _inputService = inputService;
        SubscribeToInput();
    }

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
        if (_interactAction != null || _inputService == null)
            return;

        _interactAction = _inputService.Actions.FindAction("Interact", throwIfNotFound: true);
        _interactAction.performed += OnInteractPressed;
    }

    private void UnsubscribeFromInput()
    {
        if (_interactAction == null)
            return;

        _interactAction.performed -= OnInteractPressed;
        _interactAction = null;
    }

    private void AcquireTargetInRadius()
    {
        Vector2 center = transform.position;
        _hits = Physics2D.OverlapCircleAll(center, interactRadius, interactableMask);

        IInteractable target = null;
        var collider = _hits.FirstOrDefault();
        if (collider == null)
            return;

        if (!TryGetInteractable(collider, out target))
            return;

        if (!ReferenceEquals(target, _currentTarget))
        {
            _currentTarget = target;
        }

        // Очистим ссылки для удобства
        for (int i = 0; i < _hits.Length - 1; i++)
            _hits[i] = null;
    }

    private static bool TryGetInteractable(Collider2D col, out IInteractable interactable)
    {
        if (col.TryGetComponent(out interactable))
            return true;

        interactable = col.GetComponentInParent<IInteractable>();
        return interactable != null;
    }

    private void OnInteractPressed(InputAction.CallbackContext _)
    {
        if (_currentTarget == null)
            return;

        var ctxData = new InteractionContext
        {
            Actor = _actor,
            Target = (_currentTarget as MonoBehaviour)?.gameObject,
            Point = transform.position,
            Time = Time.time
        };

        bool success = _currentTarget.TryInteract(ctxData);

#if UNITY_EDITOR
        string targetName = _currentTarget.GetInfo().DisplayName;
        Debug.Log(success
            ? $"[PlayerInteract2D] Успешно: {targetName}"
            : $"[PlayerInteract2D] Не удалось: {targetName}");
#endif
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
