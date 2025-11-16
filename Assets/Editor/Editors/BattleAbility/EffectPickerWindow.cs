using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class EffectPickerWindow : EditorWindow
{
    private const string PickerUxmlPath = "Assets/Editor/Editors/BattleAbility/EffectPicker.uxml";
    private const string EffectsFolderPath = "Assets/Resources/GameData/BattleEffects";

    private readonly List<BattleEffectSO> _allEffects = new();
    private readonly List<BattleEffectSO> _visibleEffects = new();
    private readonly List<BattleEffectSO> _selectedEffects = new();

    private TextField _filterField;
    private ListView _listView;
    private Button _confirmButton;

    private Action<IReadOnlyList<BattleEffectSO>> _onConfirm;
    private string _filter = string.Empty;

    public static void ShowWindow(IEnumerable<BattleEffectSO> currentSelection, Action<IReadOnlyList<BattleEffectSO>> onConfirm)
    {
        var window = CreateInstance<EffectPickerWindow>();
        window.Initialize(currentSelection, onConfirm);
        window.titleContent = new GUIContent("Выбор эффектов");
        window.minSize = new Vector2(420, 520);
        window.ShowUtility();
    }

    private void Initialize(IEnumerable<BattleEffectSO> currentSelection, Action<IReadOnlyList<BattleEffectSO>> onConfirm)
    {
        _onConfirm = onConfirm;
        _selectedEffects.Clear();
        if (currentSelection != null)
        {
            foreach (var effect in currentSelection)
            {
                if (effect != null && !_selectedEffects.Contains(effect))
                {
                    _selectedEffects.Add(effect);
                }
            }
        }

        SortSelected();
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PickerUxmlPath);
        if (visualTree == null)
        {
            rootVisualElement.Add(new Label("EffectPicker.uxml not found"));
            return;
        }

        rootVisualElement.Add(visualTree.CloneTree());
        CacheControls();
        SetupListView();
        SetupEvents();
        LoadEffects();
        UpdateVisibleEffects();
    }

    private void CacheControls()
    {
        _filterField = rootVisualElement.Q<TextField>("FilterField");
        _listView = rootVisualElement.Q<ListView>("EffectList");
        _confirmButton = rootVisualElement.Q<Button>("ConfirmButton");
    }

    private void SetupEvents()
    {
        if (_filterField != null)
        {
            _filterField.RegisterValueChangedCallback(evt =>
            {
                _filter = evt.newValue ?? string.Empty;
                UpdateVisibleEffects();
            });
        }

        if (_confirmButton != null)
        {
            _confirmButton.clicked += () =>
            {
                _onConfirm?.Invoke(_selectedEffects.ToList());
                Close();
            };
        }
    }

    private void SetupListView()
    {
        if (_listView == null)
            return;

        _listView.selectionType = SelectionType.None;
        _listView.fixedItemHeight = 28;
        _listView.makeItem = CreateEffectItem;
        _listView.bindItem = BindEffectItem;
    }

    private VisualElement CreateEffectItem()
    {
        var container = new VisualElement();
        container.AddToClassList("effect-picker__item");
        var label = new Label { name = "EffectLabel" };
        label.AddToClassList("effect-picker__item-label");
        container.Add(label);
        container.AddManipulator(new Clickable(() =>
        {
            if (container.userData is BattleEffectSO effect)
            {
                ToggleEffect(effect);
            }
        }));
        return container;
    }

    private void BindEffectItem(VisualElement element, int index)
    {
        if (index < 0 || index >= _visibleEffects.Count)
            return;

        var effect = _visibleEffects[index];
        element.userData = effect;
        var label = element.Q<Label>("EffectLabel");
        if (label != null)
        {
            label.text = effect == null ? "-" : effect.name;
        }

        var isSelected = effect != null && _selectedEffects.Contains(effect);
        element.EnableInClassList("effect-picker__item--selected", isSelected);
    }

    private void LoadEffects()
    {
        _allEffects.Clear();
        var guids = AssetDatabase.FindAssets("t:BattleEffectSO", new[] { EffectsFolderPath });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var effect = AssetDatabase.LoadAssetAtPath<BattleEffectSO>(path);
            if (effect != null && !_allEffects.Contains(effect))
            {
                _allEffects.Add(effect);
            }
        }

        _allEffects.Sort((a, b) => string.Compare(GetEffectFileName(a), GetEffectFileName(b), StringComparison.OrdinalIgnoreCase));
    }

    private void UpdateVisibleEffects()
    {
        _visibleEffects.Clear();

        foreach (var effect in _selectedEffects)
        {
            if (effect != null && !_visibleEffects.Contains(effect))
            {
                _visibleEffects.Add(effect);
            }
        }

        foreach (var effect in _allEffects)
        {
            if (effect == null || _visibleEffects.Contains(effect))
                continue;

            if (!string.IsNullOrEmpty(_filter) && GetEffectFileName(effect).IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            _visibleEffects.Add(effect);
        }

        if (_listView != null)
        {
            _listView.itemsSource = _visibleEffects;
            _listView.Rebuild();
        }
    }

    private void ToggleEffect(BattleEffectSO effect)
    {
        if (effect == null)
            return;

        if (_selectedEffects.Contains(effect))
        {
            _selectedEffects.Remove(effect);
        }
        else
        {
            _selectedEffects.Add(effect);
        }

        SortSelected();
        UpdateVisibleEffects();
    }

    private void SortSelected()
    {
        _selectedEffects.Sort((a, b) => string.Compare(GetEffectFileName(a), GetEffectFileName(b), StringComparison.OrdinalIgnoreCase));
    }

    private static string GetEffectFileName(BattleEffectSO effect)
    {
        if (effect == null)
            return string.Empty;

        var path = AssetDatabase.GetAssetPath(effect);
        return string.IsNullOrEmpty(path)
            ? effect.name
            : Path.GetFileNameWithoutExtension(path);
    }
}
