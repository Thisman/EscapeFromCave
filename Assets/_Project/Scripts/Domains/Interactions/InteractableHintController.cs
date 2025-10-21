using System.Collections.Generic;
using UnityEngine;

public sealed class InteractableHintController : MonoBehaviour
{
    [SerializeField] private GameObject _hintPrefab;
    [SerializeField] private Vector3 _worldOffset = new(0, 1.0f, 0);

    [SerializeField, Range(2f, 30f)] private float _updatesPerSecond = 10f;
    [SerializeField, Min(0.1f)] private float _rescanInterval = 0.5f;

    private Transform _player;
    private readonly Dictionary<InteractionController, GameObject> _active = new();
    private readonly Stack<GameObject> _pool = new();
    private readonly List<InteractionController> _toRelease = new();

    private readonly List<InteractionController> _interactables = new(128);
    private float _updateAccum;
    private float _rescanAccum;
    private Vector3 _lastWorldOffset;

    private void Awake()
    {
        _lastWorldOffset = _worldOffset;
    }

    private void Start()
    {
        if (!_player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) _player = p.transform;
        }
        RescanInteractables();
    }

    private void Update()
    {
        if (!_player || !_hintPrefab) return;

        if ((_worldOffset - _lastWorldOffset).sqrMagnitude > 0.0001f)
        {
            RefreshActiveHintOffsets();
            _lastWorldOffset = _worldOffset;
        }

        _rescanAccum += Time.deltaTime;
        if (_rescanAccum >= _rescanInterval)
        {
            _rescanAccum = 0f;
            RescanInteractables();
        }

        _updateAccum += Time.deltaTime;
        float interval = 1f / _updatesPerSecond;
        if (_updateAccum < interval) return;
        _updateAccum = 0f;

        Vector2 ppos = _player.position;

        foreach (var oi in _interactables)
        {
            if (!oi || !oi.isActiveAndEnabled)
            {
                Hide(oi);
                continue;
            }

            float r = Mathf.Max(0f, oi.Definition.InteractionDistance);

            Vector2 opos = oi.transform.position;
            bool inRange = (opos - ppos).sqrMagnitude <= r * r;

            if (inRange) ShowOrUpdate(oi);
            else Hide(oi);
        }

        _toRelease.Clear();
        foreach (var kv in _active)
            if (!kv.Key) _toRelease.Add(kv.Key);
        foreach (var dead in _toRelease) Hide(dead);
    }

    private void ShowOrUpdate(InteractionController oi)
    {
        bool created = false;
        if (!_active.TryGetValue(oi, out var go) || !go)
        {
            go = GetFromPool();
            _active[oi] = go;
            go.transform.SetParent(oi.transform, worldPositionStays: true);
            created = true;
        }

        if (!created && go.transform.parent != oi.transform)
        {
            go.transform.SetParent(oi.transform, worldPositionStays: true);
            created = true;
        }

        if (created)
        {
            ApplyHintOffset(oi, go);
        }
    }

    private void Hide(InteractionController oi)
    {
        if (!_active.TryGetValue(oi, out var go)) return;
        _active.Remove(oi);
        ReturnToPool(go);
    }

    private void RescanInteractables()
    {
        _interactables.Clear();
#if UNITY_2022_2_OR_NEWER
        var found = FindObjectsByType<InteractionController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        _interactables.AddRange(found);
#else
        _interactables.AddRange(FindObjectsOfType<ObjectInteraction>());
#endif

        _toRelease.Clear();
        foreach (var kv in _active)
            if (!_interactables.Contains(kv.Key)) _toRelease.Add(kv.Key);
        foreach (var gone in _toRelease) Hide(gone);
    }

    private GameObject GetFromPool()
    {
        var go = _pool.Count > 0 ? _pool.Pop() : Instantiate(_hintPrefab, transform);
        go.SetActive(true);
        return go;
    }

    private void ReturnToPool(GameObject go)
    {
        if (!go) return;
        go.SetActive(false);
        go.transform.SetParent(transform, false);
        _pool.Push(go);
    }

    private void RefreshActiveHintOffsets()
    {
        foreach (var kv in _active)
        {
            var interaction = kv.Key;
            var hint = kv.Value;
            if (!interaction || !hint) continue;

            ApplyHintOffset(interaction, hint);
        }
    }

    private void ApplyHintOffset(InteractionController interaction, GameObject hint)
    {
        if (!interaction || !hint) return;

        hint.transform.position = interaction.transform.position + _worldOffset;

        if (hint.TryGetComponent<HintAnimationController>(out var animation))
        {
            animation.SyncBasePositionWithTarget();
        }
    }
}
