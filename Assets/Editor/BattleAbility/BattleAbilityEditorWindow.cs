using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class BattleAbilityEditorWindow : EditorWindow
{
    private const string WindowUxmlPath = "Assets/Editor/BattleAbility/BattleAbilityEditor.uxml";
    private const string AbilitiesFolderPath = "Assets/Resources/GameData/BattleAbilities";

    private readonly List<BattleAbilitySO> _allAbilities = new();
    private readonly List<BattleAbilitySO> _filteredAbilities = new();
    private readonly List<BattleEffectSO> _effectBuffer = new();

    private ListView _abilitiesList;
    private TextField _searchField;
    private Button _createButton;
    private VisualElement _contentContainer;
    private VisualElement _iconDropArea;
    private Image _iconImage;
    private Label _iconHintLabel;
    private TextField _abilityIdField;
    private TextField _abilityNameField;
    private TextField _descriptionField;
    private IntegerField _cooldownField;
    private EnumField _abilityTypeField;
    private EnumField _targetTypeField;
    private Label _fileNameLabel;
    private Button _saveButton;
    private Button _addEffectButton;
    private ListView _effectsList;

    private BattleAbilitySO _selectedAbility;
    private BattleAbilitySO _editingAbility;
    private bool _hasUnsavedChanges;
    private bool _isCreatingNewAbility;
    private string _searchFilter = string.Empty;

    [MenuItem("Editors/Battle Ability")]
    public static void ShowWindow()
    {
        var window = GetWindow<BattleAbilityEditorWindow>();
        window.titleContent = new GUIContent("Ability Editor");
        window.minSize = new Vector2(1000, 600);
        window.Show();
    }

    private void OnDisable()
    {
        if (_editingAbility != null)
        {
            DestroyImmediate(_editingAbility);
            _editingAbility = null;
        }
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WindowUxmlPath);
        if (visualTree == null)
        {
            rootVisualElement.Add(new Label("BattleAbilityEditor.uxml not found"));
            return;
        }

        rootVisualElement.Add(visualTree.CloneTree());

        CacheControls();
        SetupSidebar();
        SetupContent();
        SetupIconDragAndDrop();
        SetupEffectsList();

        RefreshAbilities();
        UpdateContentState();
    }

    private void CacheControls()
    {
        _abilitiesList = rootVisualElement.Q<ListView>("AbilitiesList");
        _searchField = rootVisualElement.Q<TextField>("SearchField");
        _createButton = rootVisualElement.Q<Button>("CreateButton");
        _contentContainer = rootVisualElement.Q<VisualElement>("ContentContainer");
        _iconDropArea = rootVisualElement.Q<VisualElement>("IconDropArea");
        _iconImage = rootVisualElement.Q<Image>("IconImage");
        _iconHintLabel = _iconDropArea?.Q<Label>();
        _abilityIdField = rootVisualElement.Q<TextField>("AbilityIdField");
        _abilityNameField = rootVisualElement.Q<TextField>("AbilityNameField");
        _descriptionField = rootVisualElement.Q<TextField>("DescriptionField");
        _cooldownField = rootVisualElement.Q<IntegerField>("CooldownField");
        _abilityTypeField = rootVisualElement.Q<EnumField>("AbilityTypeField");
        _targetTypeField = rootVisualElement.Q<EnumField>("TargetTypeField");
        _fileNameLabel = rootVisualElement.Q<Label>("FileNameLabel");
        _saveButton = rootVisualElement.Q<Button>("SaveButton");
        _addEffectButton = rootVisualElement.Q<Button>("AddEffectButton");
        _effectsList = rootVisualElement.Q<ListView>("EffectsList");
    }

    private void SetupSidebar()
    {
        if (_abilitiesList == null)
            return;

        _abilitiesList.selectionType = SelectionType.Single;
        _abilitiesList.fixedItemHeight = 26;
        _abilitiesList.makeItem = () =>
        {
            var label = new Label();
            label.AddToClassList("ability-editor__item");
            return label;
        };
        _abilitiesList.bindItem = (element, index) =>
        {
            if (index < 0 || index >= _filteredAbilities.Count)
                return;

            if (element is Label label)
            {
                var ability = _filteredAbilities[index];
                label.text = ability == null ? "-" : GetAbilityFileName(ability);
            }
        };
        _abilitiesList.selectionChanged += objects =>
        {
            var ability = objects?.OfType<BattleAbilitySO>().FirstOrDefault();
            if (ability != _selectedAbility)
            {
                SelectAbility(ability);
            }
        };

        if (_searchField != null)
        {
            _searchField.label = string.Empty;
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _searchFilter = evt.newValue?.Trim() ?? string.Empty;
                ApplyFilter();
            });
        }

        if (_createButton != null)
        {
            _createButton.clicked += CreateAbilityAsset;
        }
    }

    private void SetupContent()
    {
        if (_abilityIdField != null)
        {
            _abilityIdField.RegisterValueChangedCallback(evt =>
            {
                if (_editingAbility == null)
                    return;

                _editingAbility.Id = evt.newValue;
                MarkDirty();
            });
        }

        if (_abilityNameField != null)
        {
            _abilityNameField.RegisterValueChangedCallback(evt =>
            {
                if (_editingAbility == null)
                    return;

                _editingAbility.AbilityName = evt.newValue;
                UpdateFileNameLabel();
                MarkDirty();
            });
        }

        if (_descriptionField != null)
        {
            _descriptionField.multiline = true;
            _descriptionField.RegisterValueChangedCallback(evt =>
            {
                if (_editingAbility == null)
                    return;

                _editingAbility.Description = evt.newValue;
                MarkDirty();
            });
        }

        ConfigureEnumField<BattleAbilityType>(_abilityTypeField, value => _editingAbility.AbilityType = value);
        ConfigureEnumField<BattleAbilityTargetType>(_targetTypeField, value => _editingAbility.AbilityTargetType = value);
        ConfigureIntegerField(_cooldownField, value => _editingAbility.Cooldown = Mathf.Max(0, value));

        if (_saveButton != null)
        {
            _saveButton.clicked += SaveCurrentAbility;
        }

        if (_addEffectButton != null)
        {
            _addEffectButton.clicked += OpenEffectPicker;
        }
    }

    private void SetupIconDragAndDrop()
    {
        if (_iconDropArea == null)
            return;

        _iconDropArea.RegisterCallback<DragUpdatedEvent>(evt =>
        {
            if (HasSpriteReference())
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            }
        });

        _iconDropArea.RegisterCallback<DragPerformEvent>(evt =>
        {
            if (!HasSpriteReference())
                return;

            DragAndDrop.AcceptDrag();
            var sprite = DragAndDrop.objectReferences.OfType<Sprite>().FirstOrDefault();
            if (sprite != null && _editingAbility != null)
            {
                _editingAbility.Icon = sprite;
                UpdateIconPreview();
                MarkDirty();
            }
        });
    }

    private void SetupEffectsList()
    {
        if (_effectsList == null)
            return;

        _effectsList.selectionType = SelectionType.None;
        _effectsList.fixedItemHeight = 24;
        _effectsList.makeItem = () =>
        {
            var label = new Label();
            label.AddToClassList("ability-editor__effect-item");
            label.RegisterCallback<PointerUpEvent>(OnEffectItemPointerUp);
            return label;
        };
        _effectsList.bindItem = (element, index) =>
        {
            if (element is Label label)
            {
                label.userData = index;
                if (index < 0 || index >= _effectBuffer.Count)
                {
                    label.text = "-";
                    return;
                }

                var effect = _effectBuffer[index];
                label.text = effect == null ? "-" : effect.name;
            }
        };
    }

    private void OnEffectItemPointerUp(PointerUpEvent evt)
    {
        if (evt.button != (int)MouseButton.MiddleMouse)
            return;

        if (evt.currentTarget is not VisualElement element)
            return;

        if (element.userData is not int index)
            return;

        if (index < 0 || index >= _effectBuffer.Count)
            return;

        evt.StopPropagation();
        _effectBuffer.RemoveAt(index);
        UpdateEffectList();
        MarkDirty();
    }

    private void RefreshAbilities()
    {
        if (!AssetDatabase.IsValidFolder(AbilitiesFolderPath))
        {
            _allAbilities.Clear();
            ApplyFilter();
            return;
        }

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
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _filteredAbilities.Clear();
        foreach (var ability in _allAbilities)
        {
            var fileName = GetAbilityFileName(ability);
            if (string.IsNullOrEmpty(_searchFilter) || fileName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _filteredAbilities.Add(ability);
            }
        }

        if (_abilitiesList != null)
        {
            _abilitiesList.itemsSource = _filteredAbilities;
            _abilitiesList.Rebuild();

            if (_selectedAbility != null)
            {
                var index = _filteredAbilities.IndexOf(_selectedAbility);
                if (index >= 0)
                {
                    _abilitiesList.SetSelection(index);
                }
                else
                {
                    _abilitiesList.ClearSelection();
                }
            }
        }
    }

    private void SelectAbility(BattleAbilitySO ability)
    {
        _selectedAbility = ability;
        _isCreatingNewAbility = false;

        if (_editingAbility != null)
        {
            DestroyImmediate(_editingAbility);
            _editingAbility = null;
        }

        _effectBuffer.Clear();

        if (_selectedAbility != null)
        {
            _editingAbility = Instantiate(_selectedAbility);
            _editingAbility.hideFlags = HideFlags.HideAndDontSave;

            if (_editingAbility.Effects != null)
            {
                _effectBuffer.AddRange(_editingAbility.Effects.Where(e => e != null));
            }

            PopulateFields();
        }
        else
        {
            ClearFields();
        }

        _hasUnsavedChanges = false;
        UpdateContentState();
    }

    private void PopulateFields()
    {
        if (_editingAbility == null)
            return;

        _abilityIdField?.SetValueWithoutNotify(_editingAbility.Id ?? string.Empty);
        _abilityNameField?.SetValueWithoutNotify(_editingAbility.AbilityName ?? string.Empty);
        _descriptionField?.SetValueWithoutNotify(_editingAbility.Description ?? string.Empty);
        _cooldownField?.SetValueWithoutNotify(_editingAbility.Cooldown);
        UpdateFileNameLabel();

        if (_abilityTypeField != null)
        {
            _abilityTypeField.Init(_editingAbility.AbilityType);
            _abilityTypeField.SetValueWithoutNotify(_editingAbility.AbilityType);
        }

        if (_targetTypeField != null)
        {
            _targetTypeField.Init(_editingAbility.AbilityTargetType);
            _targetTypeField.SetValueWithoutNotify(_editingAbility.AbilityTargetType);
        }

        UpdateIconPreview();
        UpdateEffectList();
        UpdateSaveButtonState();
    }

    private void ClearFields()
    {
        _abilityIdField?.SetValueWithoutNotify(string.Empty);
        _abilityNameField?.SetValueWithoutNotify(string.Empty);
        _descriptionField?.SetValueWithoutNotify(string.Empty);
        _cooldownField?.SetValueWithoutNotify(0);
        _abilityTypeField?.SetValueWithoutNotify(default(BattleAbilityType));
        _targetTypeField?.SetValueWithoutNotify(default(BattleAbilityTargetType));
        if (_fileNameLabel != null)
        {
            _fileNameLabel.text = "-";
        }

        UpdateIconPreview();
        UpdateEffectList();
        UpdateSaveButtonState();
    }

    private void UpdateFileNameLabel()
    {
        if (_fileNameLabel == null)
            return;

        var previewName = BuildFileName(_abilityNameField?.value);
        _fileNameLabel.text = string.IsNullOrEmpty(previewName) ? "-" : previewName;
    }

    private void UpdateIconPreview()
    {
        if (_iconImage != null)
        {
            _iconImage.sprite = _editingAbility != null ? _editingAbility.Icon : null;
        }

        if (_iconHintLabel != null)
        {
            _iconHintLabel.style.display = _editingAbility != null && _editingAbility.Icon != null
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }
    }

    private void UpdateContentState()
    {
        if (_contentContainer != null)
        {
            _contentContainer.SetEnabled(_editingAbility != null);
        }

        UpdateSaveButtonState();
    }

    private void UpdateSaveButtonState()
    {
        _saveButton?.SetEnabled(_editingAbility != null && _hasUnsavedChanges);
    }

    private void MarkDirty()
    {
        _hasUnsavedChanges = true;
        UpdateSaveButtonState();
    }

    private void ConfigureEnumField<T>(EnumField field, Action<T> setter) where T : Enum
    {
        if (field == null)
            return;

        field.Init(default(T));
        field.RegisterValueChangedCallback(evt =>
        {
            if (_editingAbility == null)
                return;

            setter((T)evt.newValue);
            MarkDirty();
        });
    }

    private void ConfigureIntegerField(IntegerField field, Action<int> setter)
    {
        if (field == null)
            return;

        field.RegisterValueChangedCallback(evt =>
        {
            if (_editingAbility == null)
                return;

            var value = evt.newValue;
            setter(value);
            field.SetValueWithoutNotify(value);
            MarkDirty();
        });
    }

    private void ConfigureToggle(Toggle toggle, Action<bool> setter)
    {
        if (toggle == null)
            return;

        toggle.RegisterValueChangedCallback(evt =>
        {
            if (_editingAbility == null)
                return;

            setter(evt.newValue);
            MarkDirty();
        });
    }

    private void UpdateEffectList()
    {
        if (_editingAbility != null)
        {
            _editingAbility.Effects = _effectBuffer.ToArray();
        }

        if (_effectsList != null)
        {
            _effectsList.itemsSource = _effectBuffer;
            _effectsList.Rebuild();
        }
    }

    private void OpenEffectPicker()
    {
        if (_editingAbility == null)
            return;

        EffectPickerWindow.ShowWindow(_effectBuffer, selectedEffects =>
        {
            _effectBuffer.Clear();
            _effectBuffer.AddRange(selectedEffects.Where(e => e != null));
            UpdateEffectList();
            MarkDirty();
        });
    }

    private void SaveCurrentAbility()
    {
        if (_editingAbility == null)
            return;

        if (_isCreatingNewAbility)
        {
            SaveNewAbilityAsset();
            return;
        }

        if (_selectedAbility == null)
            return;

        Undo.RecordObject(_selectedAbility, "Save Ability");
        EditorUtility.CopySerializedManagedFieldsOnly(_editingAbility, _selectedAbility);
        EditorUtility.SetDirty(_selectedAbility);

        var desiredName = BuildFileName(_editingAbility.AbilityName);
        var path = AssetDatabase.GetAssetPath(_selectedAbility);
        if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(desiredName))
        {
            var targetName = GetUniqueAssetName(desiredName, _selectedAbility);
            var currentName = Path.GetFileNameWithoutExtension(path);
            if (!string.Equals(currentName, targetName, StringComparison.Ordinal))
            {
                AssetDatabase.RenameAsset(path, targetName);
            }
        }

        AssetDatabase.SaveAssets();
        RefreshAbilities();
        _hasUnsavedChanges = false;
        UpdateSaveButtonState();
    }

    private void SaveNewAbilityAsset()
    {
        EnsureAbilitiesFolder();

        var desiredName = BuildFileName(_editingAbility.AbilityName);
        if (string.IsNullOrEmpty(desiredName))
        {
            desiredName = "Ability";
        }

        var targetName = GetUniqueAssetName(desiredName);
        var assetPath = Path.Combine(AbilitiesFolderPath, targetName + ".asset");

        var newAbility = CreateInstance<BattleAbilitySO>();
        EditorUtility.CopySerializedManagedFieldsOnly(_editingAbility, newAbility);
        newAbility.name = targetName;

        AssetDatabase.CreateAsset(newAbility, assetPath);
        AssetDatabase.SaveAssets();

        _isCreatingNewAbility = false;
        RefreshAbilities();
        SelectAbility(newAbility);

        if (_abilitiesList != null)
        {
            var index = _filteredAbilities.IndexOf(newAbility);
            if (index >= 0)
            {
                _abilitiesList.SetSelection(index);
            }
        }
    }

    private void CreateAbilityAsset()
    {
        if (_editingAbility != null)
        {
            DestroyImmediate(_editingAbility);
            _editingAbility = null;
        }

        _selectedAbility = null;
        _isCreatingNewAbility = true;

        _editingAbility = CreateInstance<BattleAbilitySO>();
        _editingAbility.hideFlags = HideFlags.HideAndDontSave;

        _effectBuffer.Clear();
        PopulateFields();
        UpdateContentState();
        MarkDirty();

        _abilitiesList?.ClearSelection();
    }

    private void EnsureAbilitiesFolder()
    {
        if (!Directory.Exists(AbilitiesFolderPath))
        {
            Directory.CreateDirectory(AbilitiesFolderPath);
        }
    }

    private string GetUniqueAssetName(string baseName, BattleAbilitySO ignoreAbility = null)
    {
        var uniqueName = baseName;
        var counter = 1;
        while (true)
        {
            var path = Path.Combine(AbilitiesFolderPath, uniqueName + ".asset");
            var existing = AssetDatabase.LoadAssetAtPath<BattleAbilitySO>(path);
            if (existing == null || existing == ignoreAbility)
            {
                return uniqueName;
            }

            uniqueName = baseName + counter;
            counter++;
        }
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

    private static string BuildFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var parts = value.Split(new[] { ' ', '\t', '\n', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length == 0)
                continue;

            var lower = part.ToLowerInvariant();
            var c = char.ToUpperInvariant(lower[0]);
            builder.Append(c);
            if (lower.Length > 1)
            {
                builder.Append(lower.Substring(1));
            }
        }

        return builder.Length == 0 ? string.Empty : builder.ToString();
    }

    private bool HasSpriteReference()
    {
        return DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.OfType<Sprite>().Any();
    }
}
