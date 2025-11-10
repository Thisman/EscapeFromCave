using System.Collections.Generic;
using UnityEngine;

public class PanelManager
{
    private string _currentLayer;
    private bool _currentLayerVisible;

    private readonly Dictionary<string, GameObject[]> _layers = new();

    public PanelManager(params (string layerName, GameObject[] layer)[] layers)
    {
        foreach (var (layerName, layer) in layers)
        {
            AddLayer(layerName, layer);
        }
    }

    public void AddLayer(string layerName, params GameObject[] layer)
    {
        var elements = layer ?? System.Array.Empty<GameObject>();
        _layers[layerName] = elements;

        var shouldBeActive = !string.IsNullOrEmpty(_currentLayer) && _currentLayer == layerName && _currentLayerVisible;
        SetActive(elements, shouldBeActive);
    }

    public void Show(string layerName)
    {
        if (!_layers.TryGetValue(layerName, out _))
        {
            Debug.LogWarning($"[{nameof(PanelManager)}.{nameof(Show)}] Unknown layer name: {layerName}");
            return;
        }

        foreach (var pair in _layers)
        {
            var isTarget = pair.Key == layerName;
            SetActive(pair.Value, isTarget);
        }

        _currentLayer = layerName;
        _currentLayerVisible = true;
    }

    public void SwitchCurrentLayer()
    {
        if (string.IsNullOrEmpty(_currentLayer) || !_layers.TryGetValue(_currentLayer, out var layer))
        {
            return;
        }

        _currentLayerVisible = !_currentLayerVisible;
        SetActive(layer, _currentLayerVisible);

        if (_currentLayerVisible)
        {
            foreach (var pair in _layers)
            {
                if (pair.Key != _currentLayer)
                {
                    SetActive(pair.Value, false);
                }
            }
        }
    }

    private static void SetActive(IEnumerable<GameObject> layer, bool isActive)
    {
        foreach (var element in layer)
        {
            if (element != null)
            {
                element.SetActive(isActive);
            }
        }
    }
}
