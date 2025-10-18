using System.Collections.Generic;
using UnityEngine;

public class PanelController
{
    private readonly Dictionary<string, GameObject[]> _layers = new();
    private string _currentLayerName;

    public void Register(string layerName, GameObject[] layer)
    {
        if (string.IsNullOrEmpty(layerName))
        {
            return;
        }

        var isFirstRegistration = _layers.Count == 0 && !_layers.ContainsKey(layerName);
        var wasCurrentLayer = _currentLayerName == layerName;
        var wasActive = wasCurrentLayer && _layers.TryGetValue(layerName, out var existingLayer) && IsLayerActive(existingLayer);

        var sanitizedLayer = layer ?? System.Array.Empty<GameObject>();
        _layers[layerName] = sanitizedLayer;

        if (isFirstRegistration && string.IsNullOrEmpty(_currentLayerName))
        {
            _currentLayerName = layerName;
        }

        if (wasCurrentLayer)
        {
            SetLayerActive(sanitizedLayer, wasActive);
        }
        else
        {
            SetLayerActive(sanitizedLayer, false);
        }
    }

    public void Unregistr(string layerName)
    {
        if (string.IsNullOrEmpty(layerName))
        {
            return;
        }

        if (!_layers.TryGetValue(layerName, out var layer))
        {
            return;
        }

        SetLayerActive(layer, false);
        _layers.Remove(layerName);

        if (_currentLayerName != layerName)
        {
            return;
        }

        _currentLayerName = null;

        foreach (var pair in _layers)
        {
            _currentLayerName = pair.Key;
            break;
        }
    }

    public void Show(string layerName)
    {
        if (!_layers.ContainsKey(layerName))
        {
            return;
        }

        foreach (var pair in _layers)
        {
            SetLayerActive(pair.Value, pair.Key == layerName);
        }

        _currentLayerName = layerName;
    }

    public void SwitchCurrentLayer()
    {
        if (string.IsNullOrEmpty(_currentLayerName))
        {
            return;
        }

        if (!_layers.TryGetValue(_currentLayerName, out var layer))
        {
            return;
        }

        if (IsLayerActive(layer))
        {
            SetLayerActive(layer, false);
        }
        else
        {
            Show(_currentLayerName);
        }
    }

    private static void SetLayerActive(IReadOnlyList<GameObject> layer, bool isActive)
    {
        if (layer == null)
        {
            return;
        }

        for (var i = 0; i < layer.Count; i++)
        {
            var gameObject = layer[i];
            if (gameObject != null && gameObject.activeSelf != isActive)
            {
                gameObject.SetActive(isActive);
            }
        }
    }

    private static bool IsLayerActive(IReadOnlyList<GameObject> layer)
    {
        if (layer == null)
        {
            return false;
        }

        for (var i = 0; i < layer.Count; i++)
        {
            var gameObject = layer[i];
            if (gameObject != null && gameObject.activeSelf)
            {
                return true;
            }
        }

        return false;
    }
}
