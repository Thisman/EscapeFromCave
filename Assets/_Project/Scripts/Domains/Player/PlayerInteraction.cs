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

    private InputAction _interactAction;
    private Collider2D[] _hits;
    private InteractionController _currentTarget;
    private GameObject _actor;

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

        var collider = _hits.FirstOrDefault();
        if (collider == null)
            return;

        Debug.Log(collider);

        if (!TryGetInteractable(collider, out InteractionController target))
            return;

        if (!ReferenceEquals(target, _currentTarget))
        {
            _currentTarget = target;
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
            return;

        var ctxData = new InteractionContext
        {
            Actor = _actor,
            Target = (_currentTarget as MonoBehaviour)?.gameObject,
            Point = transform.position,
            Time = Time.time,
            SceneLoader = _sceneLoader,
        };

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
