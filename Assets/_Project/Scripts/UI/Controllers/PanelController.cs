using System.Collections.Generic;
using UnityEngine;

public class PanelController
{
    private readonly Dictionary<string, GameObject[]> _layers = new();
    private string _currentLayer;
    private bool _currentLayerVisible;

    public PanelController(string layerName, GameObject[] layer)
    {
        if (string.IsNullOrEmpty(layerName))
        {
            return;
        }

        var elements = layer ?? System.Array.Empty<GameObject>();
        _layers[layerName] = elements;

        if (layerName == _currentLayer)
        {
            SetActive(elements, _currentLayerVisible);
        }
        else
        {
            SetActive(elements, false);
        }
    }

    public void Show(string layerName)
    {
        if (string.IsNullOrEmpty(layerName) || !_layers.TryGetValue(layerName, out _))
        {
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
        if (layer == null)
        {
            return;
        }

        foreach (var element in layer)
        {
            if (element != null)
            {
                element.SetActive(isActive);
            }
        }
    }
}
