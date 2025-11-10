using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public sealed class CursorManager : MonoBehaviour
{
    [SerializeField] private Sprite[] _availableCursors = Array.Empty<Sprite>();
    [SerializeField] private string _defaultCursorName;

    private readonly Dictionary<string, CursorData> _cursorCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Texture2D> _generatedTextures = new();
    private readonly List<RaycastResult> _uiRaycastResults = new();
    private readonly HashSet<string> _missingCursorNames = new(StringComparer.OrdinalIgnoreCase);

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

    private void OnDestroy()
    {
        DisposeGeneratedTextures();
        _cursorCache.Clear();
        _uiRaycastResults.Clear();
        _missingCursorNames.Clear();
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
            if (_missingCursorNames.Add(cursorName))
            {
                Debug.LogWarning($"[{nameof(CursorManager)}.{nameof(TryApplyCursor)}] Cursor '{cursorName}' is not registered.");
            }
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
        DisposeGeneratedTextures();
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

            var texture = CreateTexture(sprite);
            if (texture == null)
            {
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

        var pointerEventData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
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

        var mousePosition = Input.mousePosition;
        var ray = camera.ScreenPointToRay(mousePosition);
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

        var worldPoint = camera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, Mathf.Abs(camera.transform.position.z)));
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

    private Texture2D CreateTexture(Sprite sprite)
    {
        var isFullTexture =
            Mathf.Approximately(sprite.rect.width, sprite.texture.width) &&
            Mathf.Approximately(sprite.rect.height, sprite.texture.height) &&
            Mathf.Approximately(sprite.rect.x, 0f) &&
            Mathf.Approximately(sprite.rect.y, 0f);

        if (isFullTexture)
        {
            return sprite.texture;
        }

        if (TryCopyTextureRegion(sprite, out var copiedTexture))
        {
            return PrepareGeneratedTexture(sprite, copiedTexture);
        }

        if (TryBlitTextureRegion(sprite, out var blittedTexture))
        {
            return PrepareGeneratedTexture(sprite, blittedTexture);
        }

        if (TryReadPixels(sprite, out var readableTexture))
        {
            return PrepareGeneratedTexture(sprite, readableTexture);
        }

        Debug.LogWarning($"[{nameof(CursorManager)}.{nameof(CreateTexture)}] Unable to create cursor texture for sprite '{sprite.name}'. Cursor will be skipped.");
        return null;
    }

    private Texture2D PrepareGeneratedTexture(Sprite sourceSprite, Texture2D texture)
    {
        texture.name = $"{sourceSprite.name}_Cursor";
        texture.hideFlags = HideFlags.HideAndDontSave;
        _generatedTextures.Add(texture);
        return texture;
    }

    private bool TryCopyTextureRegion(Sprite sprite, out Texture2D texture)
    {
        texture = null;

        if (SystemInfo.copyTextureSupport == CopyTextureSupport.None)
        {
            return false;
        }

        var sourceTexture = sprite.texture;
        if (sourceTexture == null)
        {
            return false;
        }

        if (GraphicsFormatUtility.IsCompressedFormat(sourceTexture.graphicsFormat))
        {
            return false;
        }

        var rect = sprite.rect;
        var width = (int)rect.width;
        var height = (int)rect.height;
        var originX = (int)rect.x;
        var originY = (int)rect.y;

        var requiresMatchingFormat = (SystemInfo.copyTextureSupport & CopyTextureSupport.DifferentTypes) == 0;
        var sourceFormat = sourceTexture.format;

        if (!TryCreateTexture(width, height, requiresMatchingFormat ? sourceFormat : TextureFormat.RGBA32, !requiresMatchingFormat, out texture))
        {
            return false;
        }

        try
        {
            Graphics.CopyTexture(sourceTexture, 0, 0, originX, originY, width, height, texture, 0, 0, 0, 0);
            return true;
        }
        catch (Exception copyException)
        {
            Debug.LogWarning($"[{nameof(CursorManager)}.{nameof(TryCopyTextureRegion)}] Failed to copy texture data for '{sprite.name}': {copyException.Message}");
            Destroy(texture);
            texture = null;
            return false;
        }
    }

    private bool TryBlitTextureRegion(Sprite sprite, out Texture2D texture)
    {
        texture = null;

        var sourceTexture = sprite.texture;
        if (sourceTexture == null)
        {
            return false;
        }

        if (!TryCreateTexture((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false, out texture))
        {
            return false;
        }

        RenderTexture renderTexture = null;
        var previousActive = RenderTexture.active;

        try
        {
            renderTexture = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            Graphics.Blit(sourceTexture, renderTexture);

            RenderTexture.active = renderTexture;

            var rect = sprite.rect;
            var readRect = new Rect(rect.x, rect.y, rect.width, rect.height);

            texture.ReadPixels(readRect, 0, 0);
            texture.Apply();

            return true;
        }
        catch (Exception blitException)
        {
            Debug.LogWarning($"[{nameof(CursorManager)}.{nameof(TryBlitTextureRegion)}] Failed to blit texture for '{sprite.name}': {blitException.Message}");
            Destroy(texture);
            texture = null;
            return false;
        }
        finally
        {
            RenderTexture.active = previousActive;

            if (renderTexture != null)
            {
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }
    }

    private bool TryReadPixels(Sprite sprite, out Texture2D texture)
    {
        texture = null;

        var sourceTexture = sprite.texture;
        if (sourceTexture == null || !sourceTexture.isReadable)
        {
            return false;
        }

        if (!TryCreateTexture((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, true, out texture))
        {
            return false;
        }

        try
        {
            var pixels = sourceTexture.GetPixels(
                (int)sprite.rect.x,
                (int)sprite.rect.y,
                (int)sprite.rect.width,
                (int)sprite.rect.height);

            texture.SetPixels(pixels);
            texture.Apply();
            return true;
        }
        catch (Exception readException)
        {
            Debug.LogWarning($"[{nameof(CursorManager)}.{nameof(TryReadPixels)}] Failed to read pixels for '{sprite.name}': {readException.Message}");
            Destroy(texture);
            texture = null;
            return false;
        }
    }

    private static bool TryCreateTexture(int width, int height, TextureFormat format, bool allowFallbackToRgba32, out Texture2D texture)
    {
        texture = null;

        if (!SystemInfo.SupportsTextureFormat(format))
        {
            if (!allowFallbackToRgba32 || !SystemInfo.SupportsTextureFormat(TextureFormat.RGBA32))
            {
                Debug.LogWarning($"[{nameof(CursorManager)}.{nameof(TryCreateTexture)}] Texture format '{format}' is not supported.");
                return false;
            }

            format = TextureFormat.RGBA32;
        }

        try
        {
            texture = new Texture2D(width, height, format, false);
            return true;
        }
        catch (Exception createException)
        {
            Debug.LogWarning($"[{nameof(CursorManager)}.{nameof(TryCreateTexture)}] Unable to create texture ({width}x{height}, {format}): {createException.Message}");
            return false;
        }
    }

    private static Vector2 CalculateHotspot(Sprite sprite)
    {
        var hotspotX = sprite.pivot.x;
        var hotspotY = sprite.rect.height - sprite.pivot.y;
        return new Vector2(hotspotX, hotspotY);
    }

    private void DisposeGeneratedTextures()
    {
        for (var i = 0; i < _generatedTextures.Count; i++)
        {
            var texture = _generatedTextures[i];
            if (texture != null)
            {
                Destroy(texture);
            }
        }

        _generatedTextures.Clear();
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
