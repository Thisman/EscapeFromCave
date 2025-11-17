using System.Collections.Generic;
using UnityEngine;

public sealed class InteractionHintController : MonoBehaviour
{
    [SerializeField] private GameObject _hintPrefab;
    [SerializeField] private PlayerInteraction _playerInteraction;
    [SerializeField] private Vector3 _worldOffset = new(0, 1.0f, 0);

    private readonly Dictionary<InteractionController, GameObject> _active = new();
    private readonly Stack<GameObject> _pool = new();
    private readonly List<InteractionController> _toRelease = new();

    private void Awake()
    {
        EnsurePlayerInteraction();
    }

    private void OnEnable()
    {
        EnsurePlayerInteraction();
        if (_playerInteraction == null)
            return;

        _playerInteraction.InteractableEnteredRange += OnInteractableEnteredRange;
        _playerInteraction.InteractableExitedRange += OnInteractableExitedRange;

        foreach (var interactable in _playerInteraction.InteractablesInRange)
            OnInteractableEnteredRange(interactable);
    }

    private void OnDisable()
    {
        if (_playerInteraction != null)
        {
            _playerInteraction.InteractableEnteredRange -= OnInteractableEnteredRange;
            _playerInteraction.InteractableExitedRange -= OnInteractableExitedRange;
        }

        ReleaseAllHints();
    }

    private void Update()
    {
        if (_active.Count == 0)
            return;

        _toRelease.Clear();
        foreach (var kv in _active)
        {
            if (!kv.Key || !kv.Key.isActiveAndEnabled || !kv.Value)
            {
                _toRelease.Add(kv.Key);
                continue;
            }

            UpdateHintPosition(kv.Key, kv.Value);
        }

        foreach (var controller in _toRelease)
            Hide(controller);
    }

    private void OnInteractableEnteredRange(InteractionController controller)
    {
        if (!controller)
            return;

        Show(controller);
    }

    private void OnInteractableExitedRange(InteractionController controller)
    {
        Hide(controller);
    }

    private void Show(InteractionController controller)
    {
        if (!_hintPrefab)
            return;

        if (!_active.TryGetValue(controller, out var go) || !go)
        {
            go = GetFromPool();
            _active[controller] = go;
            go.transform.SetParent(controller.transform, worldPositionStays: true);
        }

        UpdateHintPosition(controller, go);
    }

    private void UpdateHintPosition(InteractionController controller, GameObject hint)
    {
        Vector3 targetPosition = controller.transform.position + _worldOffset;
        if (hint.TryGetComponent<HintAnimationController>(out var animation))
            animation.SetBasePosition(targetPosition);
        else
            hint.transform.position = targetPosition;
    }

    private void Hide(InteractionController controller)
    {
        if (controller == null)
            return;

        if (!_active.TryGetValue(controller, out var go))
            return;

        _active.Remove(controller);
        ReturnToPool(go);
    }

    private void ReleaseAllHints()
    {
        _toRelease.Clear();
        foreach (var kv in _active)
            _toRelease.Add(kv.Key);

        foreach (var controller in _toRelease)
            Hide(controller);
    }

    private void EnsurePlayerInteraction()
    {
        if (_playerInteraction)
            return;

        _playerInteraction = FindObjectOfType<PlayerInteraction>();
    }

    private GameObject GetFromPool()
    {
        var go = _pool.Count > 0 ? _pool.Pop() : Instantiate(_hintPrefab, transform);
        go.SetActive(true);
        return go;
    }

    private void ReturnToPool(GameObject go)
    {
        if (!go)
            return;

        go.SetActive(false);
        go.transform.SetParent(transform, false);
        _pool.Push(go);
    }
}
