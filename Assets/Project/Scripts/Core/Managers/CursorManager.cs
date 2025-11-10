using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CursorManager : MonoBehaviour
{
    [SerializeField] private Sprite[] _availableCursors = Array.Empty<Sprite>();
    [SerializeField] private string _defaultCursorName;

    private readonly Dictionary<string, CursorData> _cursorCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Texture2D> _generatedTextures = new();

    private void Awake()
    {
        BuildCursorCache();
    }

    private void Start()
    {
        SetDefaultCursor();
    }

    private void OnDestroy()
    {
        DisposeGeneratedTextures();
        _cursorCache.Clear();
    }

    public void ShowCursor(string cursorName)
    {
        _ = TrySetCursor(cursorName);
    }

    public bool TrySetCursor(string cursorName)
    {
        if (string.IsNullOrWhiteSpace(cursorName))
        {
            Debug.LogWarning($"[{nameof(CursorManager)}.{nameof(TrySetCursor)}] Cursor name is empty.");
            return false;
        }

        if (!_cursorCache.TryGetValue(cursorName, out var cursorData))
        {
            Debug.LogWarning($"[{nameof(CursorManager)}.{nameof(TrySetCursor)}] Cursor '{cursorName}' is not registered.");
            return false;
        }

        Cursor.SetCursor(cursorData.Texture, cursorData.Hotspot, CursorMode.Auto);
        return true;
    }

    public void SetDefaultCursor()
    {
        if (!string.IsNullOrWhiteSpace(_defaultCursorName) && TrySetCursor(_defaultCursorName))
        {
            return;
        }

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
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
            var hotspot = CalculateHotspot(sprite);

            _cursorCache[cursorName] = new CursorData(texture, hotspot);
        }
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

        var width = (int)sprite.rect.width;
        var height = (int)sprite.rect.height;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = $"{sprite.name}_Cursor"
        };

        var pixels = sprite.texture.GetPixels(
            (int)sprite.rect.x,
            (int)sprite.rect.y,
            width,
            height);

        texture.SetPixels(pixels);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        _generatedTextures.Add(texture);

        return texture;
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
        public CursorData(Texture2D texture, Vector2 hotspot)
        {
            Texture = texture;
            Hotspot = hotspot;
        }

        public Texture2D Texture { get; }
        public Vector2 Hotspot { get; }
    }
}
