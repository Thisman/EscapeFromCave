using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class UnitEditorWindow : EditorWindow
{
    private const string WindowUxmlPath = "Assets/Editor/Unit/UnitEditor.uxml";
    private const string UnitsFolderPath = "Assets/Resources/GameData/Units";

    private readonly List<UnitSO> _allUnits = new();
    private readonly List<UnitSO> _filteredUnits = new();
    private readonly List<BattleAbilitySO> _abilityBuffer = new();

    private ListView _unitsList;
    private TextField _searchField;
    private Button _createButton;
    private VisualElement _contentContainer;
    private VisualElement _iconDropArea;
    private Image _iconImage;
    private Label _iconHintLabel;
    private TextField _unitNameField;
    private Label _fileNameLabel;
    private Button _saveButton;
    private Button _addAbilityButton;
    private ListView _abilitiesList;

    private EnumField _unitKindField;
    private EnumField _attackKindField;
    private EnumField _damageTypeField;
    private FloatField _baseHealthField;
    private FloatField _basePhysicalDefenseField;
    private FloatField _baseMagicDefenseField;
    private FloatField _baseAbsoluteDefenseField;
    private FloatField _minDamageField;
    private FloatField _maxDamageField;
    private FloatField _speedField;
    private FloatField _baseCritChanceField;
    private FloatField _baseCritMultiplierField;
    private FloatField _baseMissChanceField;
    private ObjectField _progressionTemplateField;
    private EnumField _levelExpFunctionField;

    private UnitSO _selectedUnit;
    private UnitSO _editingUnit;
    private bool _hasUnsavedChanges;
    private bool _isCreatingNewUnit;
    private string _searchFilter = string.Empty;

    [MenuItem("Editors/Unit")]
    public static void ShowWindow()
    {
        var window = GetWindow<UnitEditorWindow>();
        window.titleContent = new GUIContent("Unit Editor");
        window.minSize = new Vector2(1000, 600);
        window.Show();
    }

    private void OnDisable()
    {
        if (_editingUnit != null)
        {
            DestroyImmediate(_editingUnit);
            _editingUnit = null;
        }
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WindowUxmlPath);
        if (visualTree == null)
        {
            rootVisualElement.Add(new Label("UnitEditor.uxml not found"));
            return;
        }

        rootVisualElement.Add(visualTree.CloneTree());

        CacheControls();
        SetupSidebar();
        SetupContent();
        SetupIconDragAndDrop();
        SetupAbilitiesList();

        RefreshUnits();
        UpdateContentState();
    }

    private void CacheControls()
    {
        _unitsList = rootVisualElement.Q<ListView>("UnitsList");
        _searchField = rootVisualElement.Q<TextField>("SearchField");
        _createButton = rootVisualElement.Q<Button>("CreateButton");
        _contentContainer = rootVisualElement.Q<VisualElement>("ContentContainer");
        _iconDropArea = rootVisualElement.Q<VisualElement>("IconDropArea");
        _iconImage = rootVisualElement.Q<Image>("IconImage");
        _iconHintLabel = _iconDropArea?.Q<Label>();
        _unitNameField = rootVisualElement.Q<TextField>("UnitNameField");
        _fileNameLabel = rootVisualElement.Q<Label>("FileNameLabel");
        _saveButton = rootVisualElement.Q<Button>("SaveButton");
        _addAbilityButton = rootVisualElement.Q<Button>("AddAbilityButton");
        _abilitiesList = rootVisualElement.Q<ListView>("AbilitiesList");

        _unitKindField = rootVisualElement.Q<EnumField>("UnitKindField");
        _attackKindField = rootVisualElement.Q<EnumField>("AttackKindField");
        _damageTypeField = rootVisualElement.Q<EnumField>("DamageTypeField");
        _baseHealthField = rootVisualElement.Q<FloatField>("BaseHealthField");
        _basePhysicalDefenseField = rootVisualElement.Q<FloatField>("BasePhysicalDefenseField");
        _baseMagicDefenseField = rootVisualElement.Q<FloatField>("BaseMagicDefenseField");
        _baseAbsoluteDefenseField = rootVisualElement.Q<FloatField>("BaseAbsoluteDefenseField");
        _minDamageField = rootVisualElement.Q<FloatField>("MinDamageField");
        _maxDamageField = rootVisualElement.Q<FloatField>("MaxDamageField");
        _speedField = rootVisualElement.Q<FloatField>("SpeedField");
        _baseCritChanceField = rootVisualElement.Q<FloatField>("BaseCritChanceField");
        _baseCritMultiplierField = rootVisualElement.Q<FloatField>("BaseCritMultiplierField");
        _baseMissChanceField = rootVisualElement.Q<FloatField>("BaseMissChanceField");
        _progressionTemplateField = rootVisualElement.Q<ObjectField>("ProgressionTemplateField");
        _levelExpFunctionField = rootVisualElement.Q<EnumField>("LevelExpFunctionField");
    }

    private void SetupSidebar()
    {
        if (_unitsList == null)
            return;

        _unitsList.selectionType = SelectionType.Single;
        _unitsList.fixedItemHeight = 26;
        _unitsList.makeItem = () =>
        {
            var label = new Label();
            label.AddToClassList("unit-editor__unit-item");
            return label;
        };
        _unitsList.bindItem = (element, index) =>
        {
            if (index < 0 || index >= _filteredUnits.Count)
                return;

            var label = element as Label;
            var unit = _filteredUnits[index];
            label.text = unit == null ? "-" : GetUnitFileName(unit);
        };
        _unitsList.selectionChanged += objects =>
        {
            var unit = objects?.OfType<UnitSO>().FirstOrDefault();
            if (unit != _selectedUnit)
            {
                SelectUnit(unit);
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
            _createButton.clicked += CreateUnitAsset;
        }
    }

    private void SetupContent()
    {
        if (_unitNameField != null)
        {
            _unitNameField.RegisterValueChangedCallback(evt =>
            {
                if (_editingUnit == null)
                    return;

                _editingUnit.UnitName = evt.newValue;
                UpdateFileNameLabel();
                MarkDirty();
            });
        }

        ConfigureEnumField<UnitKind>(_unitKindField, value => _editingUnit.Kind = value);
        ConfigureEnumField<AttackKind>(_attackKindField, value => _editingUnit.AttackKind = value);
        ConfigureEnumField<DamageType>(_damageTypeField, value => _editingUnit.DamageType = value);

        ConfigureFloatField(_baseHealthField, value => _editingUnit.BaseHealth = Mathf.Max(1f, value));
        ConfigureFloatField(_basePhysicalDefenseField, value => _editingUnit.BasePhysicalDefense = Mathf.Clamp01(value));
        ConfigureFloatField(_baseMagicDefenseField, value => _editingUnit.BaseMagicDefense = Mathf.Clamp01(value));
        ConfigureFloatField(_baseAbsoluteDefenseField, value => _editingUnit.BaseAbsoluteDefense = Mathf.Clamp01(value));
        ConfigureFloatField(_minDamageField, value => _editingUnit.MinDamage = Mathf.Max(0f, value));
        ConfigureFloatField(_maxDamageField, value => _editingUnit.MaxDamage = Mathf.Max(0f, value));
        ConfigureFloatField(_speedField, value => _editingUnit.Speed = Mathf.Max(1f, value));
        ConfigureFloatField(_baseCritChanceField, value => _editingUnit.BaseCritChance = Mathf.Clamp01(value));
        ConfigureFloatField(_baseCritMultiplierField, value => _editingUnit.BaseCritMultiplier = Mathf.Max(1f, value));
        ConfigureFloatField(_baseMissChanceField, value => _editingUnit.BaseMissChance = Mathf.Clamp01(value));
        ConfigureObjectField<UnitProgressionTemplateSO>(_progressionTemplateField, value => _editingUnit.ProgressionTemplate = value);
        ConfigureEnumField<UnitLevelExpFunction>(_levelExpFunctionField, value => _editingUnit.LevelExpFunction = value);

        if (_saveButton != null)
        {
            _saveButton.clicked += SaveCurrentUnit;
        }

        if (_addAbilityButton != null)
        {
            _addAbilityButton.clicked += OpenAbilityPicker;
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
            if (sprite != null && _editingUnit != null)
            {
                _editingUnit.Icon = sprite;
                UpdateIconPreview();
                MarkDirty();
            }
        });
    }

    private void SetupAbilitiesList()
    {
        if (_abilitiesList == null)
            return;

        _abilitiesList.selectionType = SelectionType.None;
        _abilitiesList.fixedItemHeight = 24;
        _abilitiesList.makeItem = () =>
        {
            var label = new Label();
            label.AddToClassList("unit-editor__ability-item");
            label.RegisterCallback<PointerUpEvent>(OnAbilityItemPointerUp);
            return label;
        };
        _abilitiesList.bindItem = (element, index) =>
        {
            if (!(element is Label label))
                return;

            label.userData = index;

            if (index < 0 || index >= _abilityBuffer.Count)
            {
                label.text = "-";
                return;
            }

            var ability = _abilityBuffer[index];
            label.text = ability == null ? "-" : ability.name;
        };
    }

    private void OnAbilityItemPointerUp(PointerUpEvent evt)
    {
        if (evt.button != (int)MouseButton.MiddleMouse)
            return;

        if (!(evt.currentTarget is VisualElement element))
            return;

        if (!(element.userData is int index))
            return;

        if (index < 0 || index >= _abilityBuffer.Count)
            return;

        evt.StopPropagation();
        _abilityBuffer.RemoveAt(index);
        UpdateAbilityList();
        MarkDirty();
    }

    private void RefreshUnits()
    {
        if (!AssetDatabase.IsValidFolder(UnitsFolderPath))
        {
            _allUnits.Clear();
            ApplyFilter();
            return;
        }

        _allUnits.Clear();
        var guids = AssetDatabase.FindAssets("t:UnitSO", new[] { UnitsFolderPath });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var unit = AssetDatabase.LoadAssetAtPath<UnitSO>(path);
            if (unit != null && !_allUnits.Contains(unit))
            {
                _allUnits.Add(unit);
            }
        }

        _allUnits.Sort((a, b) => string.Compare(GetUnitFileName(a), GetUnitFileName(b), StringComparison.OrdinalIgnoreCase));
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _filteredUnits.Clear();

        foreach (var unit in _allUnits)
        {
            var fileName = GetUnitFileName(unit);
            if (string.IsNullOrEmpty(_searchFilter) || fileName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _filteredUnits.Add(unit);
            }
        }

        if (_unitsList != null)
        {
            _unitsList.itemsSource = _filteredUnits;
            _unitsList.Rebuild();

            if (_selectedUnit != null)
            {
                var index = _filteredUnits.IndexOf(_selectedUnit);
                if (index >= 0)
                {
                    _unitsList.SetSelection(index);
                }
                else
                {
                    _unitsList.ClearSelection();
                }
            }
        }
    }

    private void SelectUnit(UnitSO unit)
    {
        _selectedUnit = unit;
        _isCreatingNewUnit = false;

        if (_editingUnit != null)
        {
            DestroyImmediate(_editingUnit);
            _editingUnit = null;
        }

        _abilityBuffer.Clear();

        if (_selectedUnit != null)
        {
            _editingUnit = Instantiate(_selectedUnit);
            _editingUnit.hideFlags = HideFlags.HideAndDontSave;

            if (_editingUnit.Abilities != null)
            {
                _abilityBuffer.AddRange(_editingUnit.Abilities.Where(a => a != null));
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
        if (_editingUnit == null)
            return;

        _unitNameField?.SetValueWithoutNotify(_editingUnit.UnitName);
        UpdateFileNameLabel();

        if (_unitKindField != null)
        {
            _unitKindField.Init(_editingUnit.Kind);
            _unitKindField.SetValueWithoutNotify(_editingUnit.Kind);
        }

        if (_attackKindField != null)
        {
            _attackKindField.Init(_editingUnit.AttackKind);
            _attackKindField.SetValueWithoutNotify(_editingUnit.AttackKind);
        }

        if (_damageTypeField != null)
        {
            _damageTypeField.Init(_editingUnit.DamageType);
            _damageTypeField.SetValueWithoutNotify(_editingUnit.DamageType);
        }

        _baseHealthField?.SetValueWithoutNotify(_editingUnit.BaseHealth);
        _basePhysicalDefenseField?.SetValueWithoutNotify(_editingUnit.BasePhysicalDefense);
        _baseMagicDefenseField?.SetValueWithoutNotify(_editingUnit.BaseMagicDefense);
        _baseAbsoluteDefenseField?.SetValueWithoutNotify(_editingUnit.BaseAbsoluteDefense);
        _minDamageField?.SetValueWithoutNotify(_editingUnit.MinDamage);
        _maxDamageField?.SetValueWithoutNotify(_editingUnit.MaxDamage);
        _speedField?.SetValueWithoutNotify(_editingUnit.Speed);
        _baseCritChanceField?.SetValueWithoutNotify(_editingUnit.BaseCritChance);
        _baseCritMultiplierField?.SetValueWithoutNotify(_editingUnit.BaseCritMultiplier);
        _baseMissChanceField?.SetValueWithoutNotify(_editingUnit.BaseMissChance);
        _progressionTemplateField?.SetValueWithoutNotify(_editingUnit.ProgressionTemplate);

        if (_levelExpFunctionField != null)
        {
            _levelExpFunctionField.Init(_editingUnit.LevelExpFunction);
            _levelExpFunctionField.SetValueWithoutNotify(_editingUnit.LevelExpFunction);
        }

        UpdateIconPreview();
        UpdateAbilityList();
        UpdateSaveButtonState();
    }

    private void ClearFields()
    {
        _unitNameField?.SetValueWithoutNotify(string.Empty);
        _fileNameLabel.text = "-";

        _baseHealthField?.SetValueWithoutNotify(0);
        _basePhysicalDefenseField?.SetValueWithoutNotify(0);
        _baseMagicDefenseField?.SetValueWithoutNotify(0);
        _baseAbsoluteDefenseField?.SetValueWithoutNotify(0);
        _minDamageField?.SetValueWithoutNotify(0);
        _maxDamageField?.SetValueWithoutNotify(0);
        _speedField?.SetValueWithoutNotify(0);
        _baseCritChanceField?.SetValueWithoutNotify(0);
        _baseCritMultiplierField?.SetValueWithoutNotify(0);
        _baseMissChanceField?.SetValueWithoutNotify(0);
        _progressionTemplateField?.SetValueWithoutNotify(null);

        if (_levelExpFunctionField != null)
        {
            _levelExpFunctionField.Init(UnitLevelExpFunction.Linear);
            _levelExpFunctionField.SetValueWithoutNotify(UnitLevelExpFunction.Linear);
        }

        UpdateIconPreview();
        UpdateAbilityList();
        UpdateSaveButtonState();
    }

    private void UpdateFileNameLabel()
    {
        if (_fileNameLabel == null)
            return;

        var previewName = BuildFileName(_unitNameField?.value);
        _fileNameLabel.text = string.IsNullOrEmpty(previewName) ? "-" : previewName;
    }

    private void UpdateIconPreview()
    {
        if (_iconImage != null)
        {
            _iconImage.sprite = _editingUnit != null ? _editingUnit.Icon : null;
        }

        if (_iconHintLabel != null)
        {
            _iconHintLabel.style.display = _editingUnit != null && _editingUnit.Icon != null
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }
    }

    private void UpdateContentState()
    {
        if (_contentContainer != null)
        {
            _contentContainer.SetEnabled(_editingUnit != null);
        }

        UpdateSaveButtonState();
    }

    private void UpdateSaveButtonState()
    {
        if (_saveButton != null)
        {
            _saveButton.SetEnabled(_editingUnit != null && _hasUnsavedChanges);
        }
    }

    private void MarkDirty()
    {
        _hasUnsavedChanges = true;
        UpdateSaveButtonState();
    }

    private void ConfigureEnumField<T>(EnumField field, Action<T> onChange) where T : Enum
    {
        if (field == null)
            return;

        field.Init(default(T));
        field.RegisterValueChangedCallback(evt =>
        {
            if (_editingUnit == null)
                return;

            onChange((T)evt.newValue);
            MarkDirty();
        });
    }

    private void ConfigureFloatField(FloatField field, Action<float> setter)
    {
        if (field == null)
            return;

        field.RegisterValueChangedCallback(evt =>
        {
            if (_editingUnit == null)
                return;

            var value = evt.newValue;
            setter(value);
            field.SetValueWithoutNotify(value);
            MarkDirty();
        });
    }

    private void ConfigureObjectField<T>(ObjectField field, Action<T> setter) where T : UnityEngine.Object
    {
        if (field == null)
            return;

        field.objectType = typeof(T);
        field.allowSceneObjects = false;
        field.RegisterValueChangedCallback(evt =>
        {
            if (_editingUnit == null)
                return;

            setter(evt.newValue as T);
            MarkDirty();
        });
    }

    private void UpdateAbilityList()
    {
        if (_editingUnit != null)
        {
            _editingUnit.Abilities = _abilityBuffer.ToArray();
        }

        if (_abilitiesList != null)
        {
            _abilitiesList.itemsSource = _abilityBuffer;
            _abilitiesList.Rebuild();
        }
    }

    private void OpenAbilityPicker()
    {
        if (_editingUnit == null)
            return;

        AbilityPickerWindow.ShowWindow(_abilityBuffer, selectedAbilities =>
        {
            _abilityBuffer.Clear();
            _abilityBuffer.AddRange(selectedAbilities.Where(a => a != null));
            UpdateAbilityList();
            MarkDirty();
        });
    }

    private void SaveCurrentUnit()
    {
        if (_editingUnit == null)
            return;

        if (_isCreatingNewUnit)
        {
            SaveNewUnitAsset();
            return;
        }

        if (_selectedUnit == null)
            return;

        Undo.RecordObject(_selectedUnit, "Save Unit");
        EditorUtility.CopySerializedManagedFieldsOnly(_editingUnit, _selectedUnit);
        EditorUtility.SetDirty(_selectedUnit);

        var desiredName = BuildFileName(_editingUnit.UnitName);
        var path = AssetDatabase.GetAssetPath(_selectedUnit);
        if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(desiredName))
        {
            var targetName = GetUniqueAssetName(desiredName, _selectedUnit);
            var currentName = Path.GetFileNameWithoutExtension(path);
            if (!string.Equals(currentName, targetName, StringComparison.Ordinal))
            {
                AssetDatabase.RenameAsset(path, targetName);
            }
        }

        AssetDatabase.SaveAssets();
        RefreshUnits();
        _hasUnsavedChanges = false;
        UpdateSaveButtonState();
    }

    private void SaveNewUnitAsset()
    {
        EnsureUnitsFolder();

        var desiredName = BuildFileName(_editingUnit.UnitName);
        if (string.IsNullOrEmpty(desiredName))
        {
            desiredName = "Unit";
        }

        var targetName = GetUniqueAssetName(desiredName);
        var assetPath = Path.Combine(UnitsFolderPath, targetName + ".asset");

        var newUnit = CreateInstance<UnitSO>();
        EditorUtility.CopySerializedManagedFieldsOnly(_editingUnit, newUnit);
        newUnit.name = targetName;

        AssetDatabase.CreateAsset(newUnit, assetPath);
        AssetDatabase.SaveAssets();

        _isCreatingNewUnit = false;
        RefreshUnits();
        SelectUnit(newUnit);

        if (_unitsList != null)
        {
            var index = _filteredUnits.IndexOf(newUnit);
            if (index >= 0)
            {
                _unitsList.SetSelection(index);
            }
        }
    }

    private void CreateUnitAsset()
    {
        if (_editingUnit != null)
        {
            DestroyImmediate(_editingUnit);
            _editingUnit = null;
        }

        _selectedUnit = null;
        _isCreatingNewUnit = true;

        _editingUnit = CreateInstance<UnitSO>();
        _editingUnit.hideFlags = HideFlags.HideAndDontSave;

        _abilityBuffer.Clear();
        PopulateFields();
        UpdateContentState();
        MarkDirty();

        _unitsList?.ClearSelection();
    }

    private void EnsureUnitsFolder()
    {
        if (!Directory.Exists(UnitsFolderPath))
        {
            Directory.CreateDirectory(UnitsFolderPath);
        }
    }

    private string GetUniqueAssetName(string baseName, UnitSO ignoreUnit = null)
    {
        var uniqueName = baseName;
        var counter = 1;
        while (true)
        {
            var path = Path.Combine(UnitsFolderPath, uniqueName + ".asset");
            var existing = AssetDatabase.LoadAssetAtPath<UnitSO>(path);
            if (existing == null || existing == ignoreUnit)
            {
                return uniqueName;
            }

            uniqueName = baseName + counter;
            counter++;
        }
    }

    private static string GetUnitFileName(UnitSO unit)
    {
        if (unit == null)
            return string.Empty;

        var path = AssetDatabase.GetAssetPath(unit);
        return string.IsNullOrEmpty(path)
            ? unit.name
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
