using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class CursorManager : MonoBehaviour
{
    [SerializeField] private Texture2D[] _availableCursors = Array.Empty<Texture2D>();
    [SerializeField] private string _defaultCursorName;

    private readonly Dictionary<string, CursorData> _cursorCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<RaycastResult> _uiRaycastResults = new();

    private const string DefaultCursorKey = "__DEFAULT__";

    private string _currentCursorKey = DefaultCursorKey;
    private Camera _mainCamera;

    private void Awake()
    {
        BuildCursorCache();
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        ApplyDefaultCursorIfNeeded();
    }

    private void Update()
    {
        UpdateCursorFromPointer();
    }

    private void UpdateCursorFromPointer()
    {
        var cursorSource = FindCursorSourceUnderPointer();
        if (cursorSource == null)
        {
            ApplyDefaultCursorIfNeeded();
            return;
        }

        var cursorState = cursorSource.GetCursorState();
        if (cursorState == null || string.IsNullOrWhiteSpace(cursorState.Cursor))
        {
            ApplyDefaultCursorIfNeeded();
            return;
        }

        if (!TryApplyCursor(cursorState.Cursor))
        {
            ApplyDefaultCursorIfNeeded();
        }
    }

    private bool TryApplyCursor(string cursorName)
    {
        if (string.IsNullOrWhiteSpace(cursorName))
        {
            return false;
        }

        if (!_cursorCache.TryGetValue(cursorName, out var cursorData))
        {
            Debug.LogWarning($"[{nameof(CursorManager)}.{nameof(TryApplyCursor)}] Cursor '{cursorName}' is not registered.");
            return false;
        }

        if (string.Equals(_currentCursorKey, cursorData.Name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        Cursor.SetCursor(cursorData.Texture, cursorData.Hotspot, CursorMode.Auto);
        _currentCursorKey = cursorData.Name;
        return true;
    }

    private void ApplyDefaultCursorIfNeeded()
    {
        if (!string.IsNullOrWhiteSpace(_defaultCursorName) && TryApplyCursor(_defaultCursorName))
        {
            return;
        }

        if (string.Equals(_currentCursorKey, DefaultCursorKey, StringComparison.Ordinal))
        {
            return;
        }

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        _currentCursorKey = DefaultCursorKey;
    }

    private void BuildCursorCache()
    {
        _cursorCache.Clear();

        if (_availableCursors == null || _availableCursors.Length == 0)
        {
            return;
        }

        foreach (var texture in _availableCursors)
        {
            if (texture == null)
            {
                continue;
            }

            var cursorName = texture.name;
            if (string.IsNullOrEmpty(cursorName))
            {
                continue;
            }

            if (_cursorCache.ContainsKey(cursorName))
            {
                Debug.LogWarning($"[{nameof(CursorManager)}.{nameof(BuildCursorCache)}] Cursor '{cursorName}' is already registered.");
                continue;
            }

            if (!texture.isReadable)
            {
                Debug.LogWarning($"[{nameof(CursorManager)}.{nameof(BuildCursorCache)}] Cursor texture '{texture.name}' is not CPU accessible. Enable Read/Write in the import settings.");
                continue;
            }

            _cursorCache[cursorName] = new CursorData(cursorName, texture, Vector2.zero);
        }
    }

    private ICursorSource FindCursorSourceUnderPointer()
    {
        return FindCursorSourceFromWorld();
    }

    private ICursorSource FindCursorSourceFromWorld()
    {
        var camera = _mainCamera != null ? _mainCamera : Camera.main;
        if (camera == null)
        {
            return null;
        }

        if (!TryGetPointerScreenPosition(out var pointerPosition))
        {
            return null;
        }

        var ray = camera.ScreenPointToRay(new Vector3(pointerPosition.x, pointerPosition.y, 0f));
        if (Physics.Raycast(ray, out var hitInfo))
        {
            var collider = hitInfo.collider;
            if (collider != null)
            {
                var source = collider.GetComponent<ICursorSource>() ?? collider.GetComponentInParent<ICursorSource>();
                if (source != null)
                {
                    return source;
                }
            }
        }

        var worldPoint = camera.ScreenToWorldPoint(new Vector3(pointerPosition.x, pointerPosition.y, Mathf.Abs(camera.transform.position.z)));
        var collider2D = Physics2D.OverlapPoint(worldPoint);
        if (collider2D != null)
        {
            var source = collider2D.GetComponent<ICursorSource>() ?? collider2D.GetComponentInParent<ICursorSource>();
            if (source != null)
            {
                return source;
            }
        }

        return null;
    }

    private static bool TryGetPointerScreenPosition(out Vector2 position)
    {
        if (Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }

        if (Pointer.current != null)
        {
            position = Pointer.current.position.ReadValue();
            return true;
        }

        position = default;
        return false;
    }

    private readonly struct CursorData
    {
        public CursorData(string name, Texture2D texture, Vector2 hotspot)
        {
            Name = name;
            Texture = texture;
            Hotspot = hotspot;
        }

        public string Name { get; }
        public Texture2D Texture { get; }
        public Vector2 Hotspot { get; }
    }
}
