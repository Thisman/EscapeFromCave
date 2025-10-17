using System.Collections.Generic;
using UnityEngine;

public sealed class InteractableHintSystem : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject hintPrefab;
    [SerializeField] private Vector3 worldOffset = new(0, 1.0f, 0);

    [Header("Performance")]
    [SerializeField, Range(2f, 30f)] private float updatesPerSecond = 10f;
    [SerializeField, Min(0.1f)] private float rescanInterval = 0.5f;

    private readonly Dictionary<ObjectInteraction, GameObject> _active = new();
    private readonly Stack<GameObject> _pool = new();

    private readonly List<ObjectInteraction> _interactables = new(128);
    private float _updateAccum;
    private float _rescanAccum;

    private void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        RescanInteractables();
    }

    private void Update()
    {
        if (!player || !hintPrefab) return;

        _rescanAccum += Time.deltaTime;
        if (_rescanAccum >= rescanInterval)
        {
            _rescanAccum = 0f;
            RescanInteractables();
        }

        _updateAccum += Time.deltaTime;
        float interval = 1f / updatesPerSecond;
        if (_updateAccum < interval) return;
        _updateAccum = 0f;

        Vector2 ppos = player.position;

        foreach (var oi in _interactables)
        {
            if (!oi || !oi.isActiveAndEnabled)
            {
                Hide(oi);
                continue;
            }

            var info = oi.GetInfo();
            float r = Mathf.Max(0f, info.InteractionDistance);

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

    // --- Внутренняя логика ---
    private void ShowOrUpdate(ObjectInteraction oi)
    {
        if (!_active.TryGetValue(oi, out var go) || !go)
        {
            go = GetFromPool();
            _active[oi] = go;
            go.transform.SetParent(oi.transform, worldPositionStays: true);

            // Если у подсказки есть биндинги — сделай тут:
            // go.GetComponent<HintView>()?.Bind(oi.GetInfo());
        }

        go.transform.position = oi.transform.position + worldOffset;

        // Если это Canvas World Space — можно управлять порядком:
        // var canvas = go.GetComponentInChildren<Canvas>();
        // if (canvas && canvas.renderMode == RenderMode.WorldSpace)
        //     canvas.sortingOrder = 10000 - Mathf.RoundToInt(((Vector2)(oi.transform.position - player.position)).sqrMagnitude);
    }

    private void Hide(ObjectInteraction oi)
    {
        if (!_active.TryGetValue(oi, out var go)) return;
        _active.Remove(oi);
        ReturnToPool(go);
    }

    private void RescanInteractables()
    {
        _interactables.Clear();
#if UNITY_2022_2_OR_NEWER
        // быстрый API без лишних аллокаций
        var found = FindObjectsByType<ObjectInteraction>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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
        var go = _pool.Count > 0 ? _pool.Pop() : Instantiate(hintPrefab, transform);
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

    private readonly List<ObjectInteraction> _toRelease = new();
}
