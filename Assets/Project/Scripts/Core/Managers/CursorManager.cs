using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class CursorManager : MonoBehaviour
{
    [SerializeField] private Sprite[] _availableCursors = Array.Empty<Sprite>();
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

        foreach (var sprite in _availableCursors)
        {
            if (sprite == null)
            {
                continue;
            }

            var cursorName = sprite.name;
            if (string.IsNullOrEmpty(cursorName))
            {
                continue;
            }

            if (_cursorCache.ContainsKey(cursorName))
            {
                Debug.LogWarning($"[{nameof(CursorManager)}.{nameof(BuildCursorCache)}] Cursor '{cursorName}' is already registered.");
                continue;
            }

            if (!IsFullTexture(sprite))
            {
                Debug.LogWarning($"[{nameof(CursorManager)}.{nameof(BuildCursorCache)}] Cursor sprite '{cursorName}' must cover the entire texture when using Texture Type Cursor.");
                continue;
            }

            var texture = sprite.texture;
            if (texture == null)
            {
                Debug.LogWarning($"[{nameof(CursorManager)}.{nameof(BuildCursorCache)}] Cursor sprite '{cursorName}' does not have a texture.");
                continue;
            }

            if (!texture.isReadable)
            {
                Debug.LogWarning($"[{nameof(CursorManager)}.{nameof(BuildCursorCache)}] Cursor texture '{texture.name}' is not CPU accessible. Enable Read/Write in the import settings.");
                continue;
            }

            var hotspot = CalculateHotspot(sprite);

            _cursorCache[cursorName] = new CursorData(cursorName, texture, hotspot);
        }
    }

    private ICursorSource FindCursorSourceUnderPointer()
    {
        var uiSource = FindCursorSourceFromUI();
        if (uiSource != null)
        {
            return uiSource;
        }

        return FindCursorSourceFromWorld();
    }

    private ICursorSource FindCursorSourceFromUI()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return null;
        }

        if (!TryGetPointerScreenPosition(out var pointerPosition))
        {
            return null;
        }

        var pointerEventData = new PointerEventData(eventSystem)
        {
            position = pointerPosition
        };

        _uiRaycastResults.Clear();
        eventSystem.RaycastAll(pointerEventData, _uiRaycastResults);

        for (var i = 0; i < _uiRaycastResults.Count; i++)
        {
            var raycastResult = _uiRaycastResults[i];
            var raycastObject = raycastResult.gameObject;
            if (raycastObject == null)
            {
                continue;
            }

            if (raycastObject.TryGetComponent<ICursorSource>(out var source))
            {
                return source;
            }

            source = raycastObject.GetComponentInParent<ICursorSource>();
            if (source != null)
            {
                return source;
            }
        }

        return null;
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

    private static Vector2 CalculateHotspot(Sprite sprite)
    {
        var hotspotX = sprite.pivot.x;
        var hotspotY = sprite.rect.height - sprite.pivot.y;
        return new Vector2(hotspotX, hotspotY);
    }

    private static bool IsFullTexture(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
        {
            return false;
        }

        var rect = sprite.rect;
        var texture = sprite.texture;

        return Mathf.Approximately(rect.width, texture.width) &&
               Mathf.Approximately(rect.height, texture.height) &&
               Mathf.Approximately(rect.x, 0f) &&
               Mathf.Approximately(rect.y, 0f);
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
