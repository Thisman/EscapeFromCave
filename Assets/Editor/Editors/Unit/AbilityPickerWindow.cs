using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class AbilityPickerWindow : EditorWindow
{
    private const string PickerUxmlPath = "Assets/Editor/Editors/Unit/AbilityPicker.uxml";
    private const string AbilitiesFolderPath = "Assets/Resources/GameData/BattleAbilities";

    private readonly List<BattleAbilitySO> _allAbilities = new();
    private readonly List<BattleAbilitySO> _visibleAbilities = new();
    private readonly List<BattleAbilitySO> _selectedAbilities = new();

    private TextField _filterField;
    private ListView _listView;
    private Button _confirmButton;

    private Action<IReadOnlyList<BattleAbilitySO>> _onConfirm;
    private string _filter = string.Empty;

    public static void ShowWindow(IEnumerable<BattleAbilitySO> currentSelection, Action<IReadOnlyList<BattleAbilitySO>> onConfirm)
    {
        var window = CreateInstance<AbilityPickerWindow>();
        window.Initialize(currentSelection, onConfirm);
        window.titleContent = new GUIContent("Выбор способностей");
        window.minSize = new Vector2(420, 520);
        window.ShowUtility();
    }

    private void Initialize(IEnumerable<BattleAbilitySO> currentSelection, Action<IReadOnlyList<BattleAbilitySO>> onConfirm)
    {
        _onConfirm = onConfirm;
        _selectedAbilities.Clear();
        if (currentSelection != null)
        {
            foreach (var ability in currentSelection)
            {
                if (ability != null && !_selectedAbilities.Contains(ability))
                {
                    _selectedAbilities.Add(ability);
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
            rootVisualElement.Add(new Label("AbilityPicker.uxml not found"));
            return;
        }

        rootVisualElement.Add(visualTree.CloneTree());
        CacheControls();
        SetupListView();
        SetupEvents();
        LoadAbilities();
        UpdateVisibleAbilities();
    }

    private void CacheControls()
    {
        _filterField = rootVisualElement.Q<TextField>("FilterField");
        _listView = rootVisualElement.Q<ListView>("AbilityList");
        _confirmButton = rootVisualElement.Q<Button>("ConfirmButton");
    }

    private void SetupEvents()
    {
        if (_filterField != null)
        {
            _filterField.RegisterValueChangedCallback(evt =>
            {
                _filter = evt.newValue ?? string.Empty;
                UpdateVisibleAbilities();
            });
        }

        if (_confirmButton != null)
        {
            _confirmButton.clicked += () =>
            {
                _onConfirm?.Invoke(_selectedAbilities.ToList());
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
        _listView.makeItem = CreateAbilityItem;
        _listView.bindItem = BindAbilityItem;
    }

    private VisualElement CreateAbilityItem()
    {
        var container = new VisualElement();
        container.AddToClassList("ability-picker__item");
        var label = new Label { name = "AbilityLabel" };
        label.AddToClassList("ability-picker__item-label");
        container.Add(label);
        container.AddManipulator(new Clickable(() =>
        {
            if (container.userData is BattleAbilitySO ability)
            {
                ToggleAbility(ability);
            }
        }));
        return container;
    }

    private void BindAbilityItem(VisualElement element, int index)
    {
        if (index < 0 || index >= _visibleAbilities.Count)
            return;

        var ability = _visibleAbilities[index];
        element.userData = ability;
        var label = element.Q<Label>("AbilityLabel");
        if (label != null)
        {
            label.text = ability == null ? "-" : ability.name;
        }

        var isSelected = ability != null && _selectedAbilities.Contains(ability);
        element.EnableInClassList("ability-picker__item--selected", isSelected);
    }

    private void LoadAbilities()
    {
        _allAbilities.Clear();
        var guids = AssetDatabase.FindAssets("t:BattleAbilitySO", new[] { AbilitiesFolderPath });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ability = AssetDatabase.LoadAssetAtPath<BattleAbilitySO>(path);
            if (ability != null && !_allAbilities.Contains(ability))
            {
                _allAbilities.Add(ability);
            }
        }

        _allAbilities.Sort((a, b) => string.Compare(GetAbilityFileName(a), GetAbilityFileName(b), StringComparison.OrdinalIgnoreCase));
    }

    private void UpdateVisibleAbilities()
    {
        _visibleAbilities.Clear();

        foreach (var ability in _selectedAbilities)
        {
            if (ability != null && !_visibleAbilities.Contains(ability))
            {
                _visibleAbilities.Add(ability);
            }
        }

        foreach (var ability in _allAbilities)
        {
            if (ability == null || _visibleAbilities.Contains(ability))
                continue;

            if (!string.IsNullOrEmpty(_filter) && GetAbilityFileName(ability).IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            _visibleAbilities.Add(ability);
        }

        if (_listView != null)
        {
            _listView.itemsSource = _visibleAbilities;
            _listView.Rebuild();
        }
    }

    private void ToggleAbility(BattleAbilitySO ability)
    {
        if (ability == null)
            return;

        if (_selectedAbilities.Contains(ability))
        {
            _selectedAbilities.Remove(ability);
        }
        else
        {
            _selectedAbilities.Add(ability);
        }

        SortSelected();
        UpdateVisibleAbilities();
    }

    private void SortSelected()
    {
        _selectedAbilities.Sort((a, b) => string.Compare(GetAbilityFileName(a), GetAbilityFileName(b), StringComparison.OrdinalIgnoreCase));
    }

    private static string GetAbilityFileName(BattleAbilitySO ability)
    {
        if (ability == null)
            return string.Empty;

        var path = AssetDatabase.GetAssetPath(ability);
        return string.IsNullOrEmpty(path)
            ? ability.name
            : Path.GetFileNameWithoutExtension(path);
    }
}
